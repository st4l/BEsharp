﻿// ----------------------------------------------------------------------------------------------------
// <copyright file="RConClient.cs" company="Me">Copyright (c) 2012 St4l.</copyright>
// ----------------------------------------------------------------------------------------------------

using System;
using System.Diagnostics;
using System.Globalization;
using System.Runtime.ExceptionServices;
using System.Security.Authentication;
using System.Threading;
using System.Threading.Tasks;
using BNet.Client.Datagrams;
using log4net;
using log4net.Core;

namespace BNet.Client
{
    /// <summary>
    ///     The <see cref='RConClient' /> class provides access to BattlEye RCon services.
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         The [BattlEye RCon protocol](http://www.battleye.com/downloads/BERConProtocol.txt)
    ///         uses the ArmA game server's network interface, i.e. its UDP game port.
    ///     </para>
    ///     <para>
    ///         The use of the underlying UDP protocol means that a persistent connection
    ///         to the server is not maintained, and the client only registers with the
    ///         server in order to receive messages from it, and subsequently be able to
    ///         send further messages which the server will interpret as belonging to the
    ///         same session.
    ///     </para>
    ///     <para>
    ///         The use of UDP also implies networking constraints that this client deals
    ///         with: inbound and outbound UDP messages are not guaranteed to arrive, nor
    ///         are they guaranteed to arrive in the order in which they were sent by either
    ///         the server or the client.
    ///     </para>
    ///     <para>
    ///         <see cref="RConClient" /> encapsulates and augments
    ///         <see cref='System.Net.Sockets.UdpClient' />.
    ///     </para>
    /// </remarks>
    public sealed class RConClient : IDisposable
    {
        private readonly string host;

        private readonly int port;

        private readonly string password;

        private MessageDispatcher msgDispatcher;

        private bool closed;

        private bool disposed;

        private readonly object msgReceivedEventAccesorsLockObject = new object();

        private readonly object packetProblemEventAccesorsLockObject = new object();

        private EventHandler<MessageReceivedEventArgs> subscribedMsgReceivedHandler;

        private EventHandler<PacketProblemEventArgs> subscribedPktProblemHandler;

        internal RConMetrics Metrics { get; set; }

        internal IUdpClient Client { get; set; }

        public ShutdownReason ShutdownReason { get; private set; }
        
        private ILog Log { get; set; }

#if DEBUG
        // will block until this client shuts down
        private readonly ManualResetEvent runningLock = new ManualResetEvent(false);
#endif


        public RConClient(string host, int port, string password)
        {
            this.host = host;
            this.port = port;

            this.password = password;
            NetUdpClient client = null;
            try
            {
                client = new NetUdpClient(this.host, this.port)
                             {
                                 DontFragment = true,
                                 EnableBroadcast = false,
                                 MulticastLoopback = false
                             };
            }
            catch (Exception ex)
            {
                if (client != null)
                {
                    client.Close();
                }
                ExceptionDispatchInfo nex = ExceptionDispatchInfo.Capture(ex);
                nex.Throw();
            }
            this.Client = client;
            this.Initialize();
        }


        internal RConClient(IUdpClient client, string password)
        {
            //throw new ArgumentException("asdf");
            this.Client = client;
            this.password = password;
            this.Initialize();
        }


        /// <summary>
        ///     Use C# destructor syntax for finalization code. 
        /// </summary>
        /// <remarks>
        ///     This destructor will run only if the Dispose method 
        ///     does not get called. 
        ///     It gives your base class the opportunity to finalize. 
        ///     Do not provide destructors in types derived from this class.
        /// </remarks>
        ~RConClient()
        {
            this.Dispose(false);
        }


        /// <summary>
        ///     Occurs when a console message is received from the RCon server.
        /// </summary>
        /// <remarks>
        ///     In <see cref="StartListening" /> we are passing along the
        ///     multicast delegate directly to
        ///     <see cref="MessageDispatcher.MessageReceived" />, so we
        ///     need to update it if we already passed it (subscribed).
        /// </remarks>
        public event EventHandler<MessageReceivedEventArgs> MessageReceived
        {
            add
            {
                lock (this.msgReceivedEventAccesorsLockObject)
                {
                    this.MsgReceived += value;
                    if (this.msgDispatcher == null)
                    {
                        return;
                    }
                    if (this.subscribedMsgReceivedHandler != null)
                    {
                        this.msgDispatcher.MessageReceived -= this.subscribedMsgReceivedHandler;
                    }
                    this.subscribedMsgReceivedHandler = this.MsgReceived;
                    this.msgDispatcher.MessageReceived += this.MsgReceived;
                }
            }

            remove
            {
                lock (this.msgReceivedEventAccesorsLockObject)
                {
                    this.MsgReceived -= value;
                    if (this.msgDispatcher == null)
                    {
                        return;
                    }
                    if (this.subscribedMsgReceivedHandler != null)
                    {
                        this.msgDispatcher.MessageReceived -= this.subscribedMsgReceivedHandler;
                    }
                    this.subscribedMsgReceivedHandler = this.MsgReceived;
                    this.msgDispatcher.MessageReceived += this.MsgReceived;
                }
            }
        }

        private event EventHandler<MessageReceivedEventArgs> MsgReceived;


        /// <summary>
        ///     Occurs when some problem is detected in the incoming 
        ///     packets from the server, such as corrupted packets or
        ///     lost packets.
        /// </summary>
        /// <remarks>
        ///     In <see cref="StartListening" /> we are passing along the
        ///     multicast delegate directly to
        ///     <see cref="MessageDispatcher.PacketProblem" />, so we
        ///     need to update it if we already passed it (subscribed).
        /// </remarks>
        public event EventHandler<PacketProblemEventArgs> PacketProblem
        {
            add
            {
                lock (this.packetProblemEventAccesorsLockObject)
                {
                    this.PktProblem += value;
                    if (this.msgDispatcher == null)
                    {
                        return;
                    }
                    if (this.subscribedPktProblemHandler != null)
                    {
                        this.msgDispatcher.PacketProblem -= this.subscribedPktProblemHandler;
                    }
                    this.subscribedPktProblemHandler = this.PktProblem;
                    this.msgDispatcher.PacketProblem += this.PktProblem;
                }
            }

            remove
            {
                lock (this.packetProblemEventAccesorsLockObject)
                {
                    this.PktProblem -= value;
                    if (this.msgDispatcher == null)
                    {
                        return;
                    }
                    if (this.subscribedPktProblemHandler != null)
                    {
                        this.msgDispatcher.PacketProblem -= this.subscribedPktProblemHandler;
                    }
                    this.subscribedPktProblemHandler = this.PktProblem;
                    this.msgDispatcher.PacketProblem += this.PktProblem;
                }
            }
        }

        private event EventHandler<PacketProblemEventArgs> PktProblem;


        /// <summary>
        ///     Gets or sets a <see cref="bool" /> value that specifies
        ///     whether this <see cref="RConClient" /> tries to keep the
        ///     connection to the remote RCon server alive.
        /// </summary>
        public bool KeepAlive
        {
            get { throw new NotImplementedException(); }
            set { throw new NotImplementedException(); }
        }


        /// <summary>
        ///     Gets or sets a <see cref="bool" /> value that specifies
        ///     whether this <see cref="RConClient" /> discards all
        ///     console message datagrams received (the <see cref="MessageReceived" />
        ///     event is never raised).
        /// </summary>
        public bool DiscardConsoleMessages { get; set; }


        /// <summary>
        ///     Registers with the established remote Battleye RCon server
        ///     using the provided password and starts listening for messages
        ///     from it.
        /// </summary>
        /// <returns>
        ///     True if connection and login are successful,
        ///     false otherwise.
        /// </returns>
        public async Task<bool> ConnectAsync()
        {
            if (this.closed)
            {
                throw new ObjectDisposedException(
                    "RConClient", "This RConClient has been disposed.");
            }

            this.StartListening();

            bool loggedIn = false;
            try
            {
                this.LogTrace("BEFORE LOGIN await Login()");
                loggedIn = await this.Login();
                this.LogTrace("AFTER LOGIN await Login()");
            }
            finally
            {
                this.LogTrace("FINALLY LOGIN await Login()");
                if (!loggedIn)
                {
                    this.StopListening();
                }
            }

            return loggedIn;
        }


        public async Task<ResponseHandler> SendCommandAsync(string commandText)
        {
            var dgram = new CommandDatagram(commandText);
            return await this.msgDispatcher.SendDatagramAsync(dgram);
        }


        /// <summary>
        ///     Stops all processing gracefully and disposes this instance.
        /// </summary>
        public void Close()
        {
            this.StopListening();
            this.Metrics.StopCollecting();
            this.closed = true;
            this.Dispose();
        }


        /// <summary>
        ///     Implement IDisposable.
        /// </summary>
        public void Dispose()
        {
            this.Dispose(true);
            GC.SuppressFinalize(this);
        }


        private void Initialize()
        {
            this.Log = LogManager.GetLogger(this.GetType());
            this.Metrics = new RConMetrics();
        }


        /// <summary>
        ///     Dispose managed and unmanaged resources.
        /// </summary>
        /// <param name="disposing">
        ///     True unless we're called from the finalizer,
        ///     in which case only unmanaged resources can be disposed.
        /// </param>
        private void Dispose(bool disposing)
        {
            // Check to see if Dispose has already been called. 
            if (!this.disposed)
            {
                // If disposing equals true, dispose all managed 
                // and unmanaged resources. 
                if (disposing)
                {
                    // Dispose managed resources.
                    if (this.msgDispatcher != null)
                    {
                        this.msgDispatcher.Close();
                    }

                    if (this.Client != null)
                    {
                        this.Client.Close();
                    }

#if DEBUG
                    if (this.runningLock != null)
                    {
                        this.runningLock.Close();
                    }
#endif
                }

                // Note disposing has been done.
                this.disposed = true;
            }
        }


        [Conditional("TRACE")]
        private void LogTrace(string msg)
        {
            this.Log.Logger.Log(
                this.Log.GetType(),
                Level.Trace,
                msg,
                null);
        }


        [Conditional("TRACE")]
        private void LogTraceFormat(string fmt, params object[] args)
        {
            this.Log.Logger.Log(
                this.Log.GetType(),
                Level.Trace,
                string.Format(CultureInfo.InvariantCulture, fmt, args),
                null);
        }


        private async Task<bool> Login()
        {
            this.LogTrace("BEFORE LOGIN await SendDatagramAsync");
            ResponseHandler responseHandler =
                await this.msgDispatcher.SendDatagramAsync(new LoginDatagram(this.password));
            this.LogTrace("AFTER  LOGIN await SendDatagramAsync");

            this.LogTrace("BEFORE LOGIN await WaitForResponse");
            bool received = await responseHandler.WaitForResponse();
            this.LogTrace("AFTER  LOGIN await WaitForResponse");
            if (!received)
            {
                this.LogTrace("       LOGIN TIMEOUT");
                throw new TimeoutException("Timeout while trying to login to the remote host.");
            }

            var result = (LoginResponseDatagram)responseHandler.ResponseDatagram;
            if (!result.Success)
            {
                this.LogTrace("       LOGIN INCORRECT");
                throw new InvalidCredentialException(
                    "RCon server actively refused access with the specified password.");
            }

            this.LogTrace("       LOGIN SUCCESS");
            return result.Success;
        }


        private void StartListening()
        {
            this.msgDispatcher = new MessageDispatcher(this.Client)
                                     {DiscardConsoleMessages = this.DiscardConsoleMessages};
            this.subscribedMsgReceivedHandler = this.MsgReceived;
            this.msgDispatcher.MessageReceived += this.subscribedMsgReceivedHandler;
            this.subscribedPktProblemHandler = this.PktProblem;
            this.msgDispatcher.PacketProblem += this.subscribedPktProblemHandler;
            this.msgDispatcher.Disconnected += this.MsgDispatcherOnDisconnected;
            this.msgDispatcher.Start();
        }


        private void MsgDispatcherOnDisconnected(object sender, DisconnectedEventArgs e)
        {
            this.StopListening();
#if DEBUG
            this.runningLock.Set();
#endif
            this.OnDisconnected(e);
        }


#if DEBUG
        internal void WaitUntilShutdown()
        {
            this.runningLock.WaitOne();
        }
#endif

        public event EventHandler<DisconnectedEventArgs> Disconnected;


        public void OnDisconnected(DisconnectedEventArgs e)
        {
            if (this.Disconnected != null)
            {
                this.Disconnected(this, e);
            }
        }


        private void StopListening()
        {
            if (this.msgDispatcher == null)
            {
                return;
            }
            if (this.subscribedMsgReceivedHandler != null)
            {
                this.msgDispatcher.MessageReceived -= this.subscribedMsgReceivedHandler;
            }
            this.subscribedMsgReceivedHandler = null;
            if (this.subscribedPktProblemHandler != null)
            {
                this.msgDispatcher.PacketProblem -= this.subscribedPktProblemHandler;
            }
            this.subscribedMsgReceivedHandler = null;
            this.msgDispatcher.Disconnected -= this.MsgDispatcherOnDisconnected;
            this.msgDispatcher.UpdateMetrics(this.Metrics);
            this.ShutdownReason = this.msgDispatcher.ShutdownReason;
            this.msgDispatcher.Close(); // disposes
            this.msgDispatcher = null;
        }


        
    }
}
