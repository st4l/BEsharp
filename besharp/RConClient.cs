// ----------------------------------------------------------------------------------------------------
// <copyright file="RConClient.cs" company="Me">Copyright (c) 2013 St4l.</copyright>
// ----------------------------------------------------------------------------------------------------
namespace BESharp
{
    using System;
    using System.Runtime.ExceptionServices;
    using System.Security.Authentication;
    using System.Threading;
    using System.Threading.Tasks;
    using Datagrams;
    using log4net;


    /// <summary>
    ///   The <see cref='RConClient' /> class provides access to BattlEye RCon services.
    /// </summary>
    /// <remarks>
    ///   <para> The [BattlEye RCon protocol](http://www.battleye.com/downloads/BERConProtocol.txt) uses the ArmA game server's network interface, i.e. its UDP game port. </para>
    ///   <para> The use of the underlying UDP protocol means that a persistent connection to the server is not maintained, and the client only registers with the server in order to receive messages from it, and subsequently be able to send further messages which the server will interpret as belonging to the same session. </para>
    ///   <para> The use of UDP also implies networking constraints that this client deals with: inbound and outbound UDP messages are not guaranteed to arrive, nor are they guaranteed to arrive in the order in which they were sent by either the server or the client. </para>
    ///   <para> <see cref="RConClient" /> encapsulates and augments <see cref='System.Net.Sockets.UdpClient' /> . </para>
    /// </remarks>
    public sealed class RConClient : IDisposable
    {
        private readonly string password;

        private readonly object msgReceivedEventAccesorsLockObject = new object();

        private readonly object packetProblemEventAccesorsLockObject = new object();


#if DEBUG
        // will block until this client shuts down
        private readonly ManualResetEvent runningLock = new ManualResetEvent(false);
#endif


        private MessageDispatcher msgDispatcher;

        private bool closed;

        private bool disposed;

        private EventHandler<MessageReceivedEventArgs> subscribedMsgReceivedHandler;

        private EventHandler<PacketProblemEventArgs> subscribedPktProblemHandler;


        public RConClient(string host, int port, string password)
        {
            this.password = password;

            NetUdpClient client = null;
            try
            {
                client = new NetUdpClient
                             {
                                     ExclusiveAddressUse = true,
                                     DontFragment = true,
                                     EnableBroadcast = false,
                                     MulticastLoopback = false
                             };
                client.Connect(host, port);
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
            this.Client = client;
            this.password = password;
            this.Initialize();
        }


        /// <summary>
        ///   Use C# destructor syntax for finalization code.
        /// </summary>
        /// <remarks>
        ///   This destructor will run only if the Dispose method 
        ///   does not get called. 
        ///   Do not provide destructors in types derived from this class.
        /// </remarks>
        ~RConClient()
        {
            this.Dispose(false);
        }


        /// <summary>
        ///   Occurs when a console message is received from the RCon server.
        /// </summary>
        /// <remarks>
        ///   In <see cref="StartListening" /> we are passing along the
        ///   multicast delegate directly to
        ///   <see cref="MessageDispatcher.MessageReceived" />, so we
        ///   need to update it if we already passed it (subscribed).
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


        /// <summary>
        ///   Occurs when some problem is detected in the incoming 
        ///   packets from the server, such as corrupted packets or
        ///   lost packets.
        /// </summary>
        /// <remarks>
        ///   In <see cref="StartListening" /> we are passing along the
        ///   multicast delegate directly to
        ///   <see cref="MessageDispatcher.PacketProblem" />, so we
        ///   need to update it if we already passed it (subscribed).
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

        public event EventHandler<DisconnectedEventArgs> Disconnected;


        private event EventHandler<PacketProblemEventArgs> PktProblem;

        private event EventHandler<MessageReceivedEventArgs> MsgReceived;


        /// <summary>
        ///   Gets or sets a <see cref="bool" /> value that specifies
        ///   whether this <see cref="RConClient" /> tries to keep the
        ///   connection to the remote RCon server alive.
        /// </summary>
        public bool KeepAlive
        {
            get { throw new NotImplementedException(); }
            set { throw new NotImplementedException(); }
        }


        /// <summary>
        ///   Gets or sets a <see cref="bool" /> value that specifies
        ///   whether this <see cref="RConClient" /> discards all
        ///   console message datagrams received (the <see cref="MessageReceived" />
        ///   event is never raised).
        /// </summary>
        public bool DiscardConsoleMessages { get; set; }


        public ShutdownReason ShutdownReason { get; private set; }


        internal RConMetrics Metrics { get; set; }

        internal IUdpClient Client { get; set; }

        private ILog Log { get; set; }


        /// <summary>
        ///   Registers with the established remote Battleye RCon server
        ///   using the provided password and starts listening for messages
        ///   from it.
        /// </summary>
        /// <returns> True if connection and login are successful, false otherwise. </returns>
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
                this.Log.Trace("BEFORE LOGIN await Login()");
                loggedIn = await this.Login();
                this.Log.Trace("AFTER LOGIN await Login()");
            }
            finally
            {
                this.Log.Trace("FINALLY LOGIN await Login()");
                if (!loggedIn)
                {
                    this.StopListening();
                }
            }

            return loggedIn;
        }




        /// <summary>
        ///   Stops all processing gracefully and disposes this instance.
        /// </summary>
        public void Close()
        {
            this.StopListening();
            this.Metrics.StopCollecting();
            this.closed = true;
            this.Dispose();
        }


        /// <summary>
        ///   Implement IDisposable.
        /// </summary>
        public void Dispose()
        {
            this.Dispose(true);
            GC.SuppressFinalize(this);
        }


        private void OnDisconnected(DisconnectedEventArgs e)
        {
            if (this.Disconnected != null)
            {
                this.Disconnected(this, e);
            }
        }


#if DEBUG
        internal void WaitUntilShutdown()
        {
            this.runningLock.WaitOne();
        }
#endif


        private void Initialize()
        {
            this.Log = LogManager.GetLogger(this.GetType());
            this.Metrics = new RConMetrics();
        }


        /// <summary>
        ///   Dispose managed and unmanaged resources.
        /// </summary>
        /// <param name="disposing"> True unless we're called from the finalizer, in which case only unmanaged resources can be disposed. </param>
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


        private async Task<bool> Login()
        {
            this.Log.Trace("BEFORE LOGIN await SendDatagram");
            ResponseHandler responseHandler =
                    this.msgDispatcher.SendDatagram(new LoginDatagram(this.password));
            this.Log.Trace("AFTER  LOGIN await SendDatagram");

            this.Log.Trace("BEFORE LOGIN await WaitForResponse");
            bool received = await responseHandler.WaitForResponse();
            this.Log.Trace("AFTER  LOGIN await WaitForResponse");
            if (!received)
            {
                this.Log.Trace("       LOGIN TIMEOUT");
                throw new TimeoutException("Timeout while trying to login to the remote host.");
            }

            var result = (LoginResponseDatagram)responseHandler.ResponseDatagram;
            if (!result.Success)
            {
                this.Log.Trace("       LOGIN INCORRECT");
                throw new InvalidCredentialException(
                        "RCon server actively refused access with the specified password.");
            }

            this.Log.Trace("       LOGIN SUCCESS");
            return result.Success;
        }


        public async Task<CommandResult> SendCommandAsync(string commandText)
        {
            return await this.SendCommandAsync(commandText, 1000 * 3);
        }


        public async Task<CommandResult> SendCommandAsync(string commandText, int timeout)
        {
            this.Log.Trace("       COMMAND SendDatagram");
            ResponseHandler responseHandler = this.SendCommand(commandText);

            this.Log.Trace("BEFORE COMMAND await WaitForResponse");
            bool received = await responseHandler.WaitForResponse(timeout);
            this.Log.Trace("AFTER  COMMAND await WaitForResponse");
            if (!received)
            {
                this.Log.Trace("       COMMAND TIMEOUT");
                return new CommandResult(false, string.Empty);
            }

            var result = (CommandResponseDatagram)responseHandler.ResponseDatagram;

            this.Log.Trace("       COMMAND SUCCESS");
            return new CommandResult(true, result.Body);
        }



        internal ResponseHandler SendCommand(string commandText)
        {
            var dgram = new CommandDatagram(this.msgDispatcher.GetNextCommandSequenceNumber(), commandText);
            return this.msgDispatcher.SendDatagram(dgram);
        }


        internal ResponseHandler SendCommand(byte sequenceNumber, string commandText)
        {
            var dgram = new CommandDatagram(sequenceNumber, commandText);
            return this.msgDispatcher.SendDatagram(dgram);
        }


        private void StartListening()
        {
            this.msgDispatcher = new MessageDispatcher(this.Client)
                                     {
                                             DiscardConsoleMessages = this.DiscardConsoleMessages
                                     };
            this.subscribedMsgReceivedHandler = this.MsgReceived;
            this.msgDispatcher.MessageReceived += this.subscribedMsgReceivedHandler;
            this.subscribedPktProblemHandler = this.PktProblem;
            this.msgDispatcher.PacketProblem += this.subscribedPktProblemHandler;
            this.msgDispatcher.Disconnected += this.MsgDispatcherOnDisconnected;
            this.msgDispatcher.Start();
        }


        private void MsgDispatcherOnDisconnected(object sender, DisconnectedEventArgs e)
        {
            if (e.ShutdownReason != ShutdownReason.UserRequested)
            {
                this.StopListening();
            }
#if DEBUG
            this.runningLock.Set();
#endif
            this.OnDisconnected(e);
        }


        private bool closingSession = false;

        private void StopListening()
        {
            if (this.closingSession || this.msgDispatcher == null)
            {
                return;
            }
            this.closingSession = true;

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
            this.msgDispatcher.Close(); // disposes

            this.ShutdownReason = this.msgDispatcher.ShutdownReason;
            this.msgDispatcher = null;
            this.closingSession = false;
        }
    }
}
