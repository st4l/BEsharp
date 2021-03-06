﻿// ----------------------------------------------------------------------------------------------------
// <copyright file="RConClient.cs" company="Me">Copyright (c) 2013 St4l.</copyright>
namespace BESharp
// ----------------------------------------------------------------------------------------------------
{
    using System;
    using System.ComponentModel;
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

#if DEBUG
        // will block until this client shuts down
        private readonly ManualResetEvent runningLock = new ManualResetEvent(false);
#endif


        private DatagramDispatcher dispatcher;

        private bool closed;

        private bool disposed;

        private AsyncOperation asyncOperation;


        public RConClient(string host, int port, string password)
        {
            this.password = password;

            this.UdpClient = CreateUdpClient();
            this.UdpClient.Connect(host, port);

            this.Initialize();
        }


        internal RConClient(IUdpClient client, string password)
        {
            this.UdpClient = client;
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
        public event EventHandler<MessageReceivedEventArgs> MessageReceived;


        /// <summary>
        ///   Occurs when some problem is detected in the incoming 
        ///   datagrams from the server, such as corrupted or lost data.
        /// </summary>
        public event EventHandler<ConnectionProblemEventArgs> ConnectionProblem;


        /// <summary>
        ///   Occurs when a disconnection is detected.
        /// </summary>
        public event EventHandler<DisconnectedEventArgs> Disconnected;


        /// <summary>
        ///   Gets or sets a <see cref="bool" /> value that specifies
        ///   whether this <see cref="RConClient" /> tries to keep the
        ///   connection to the remote RCon server alive after a 
        ///   disconnect (the server didn't respond for a time).
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


        /// <summary>
        ///   Provides the reason for the last connection shutdown.
        /// </summary>
        public ShutdownReason ShutdownReason { get; private set; }


        /// <summary>
        ///   Provides a set of metrics regarding the operation of
        ///   this instance.
        /// </summary>
        public RConMetrics Metrics { get; private set; }


        internal IUdpClient UdpClient { get; private set; }


        private ILog Log { get; set; }


        /// <summary>
        ///   Safely creates and instance of the Udp client.
        /// </summary>
        /// <returns> A newly created <see cref="IUdpClient"/> instance. </returns>
        private static IUdpClient CreateUdpClient()
        {
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
            }
            catch (Exception ex)
            {
                // dispose and throw 
                if (client != null)
                {
                    client.Close();
                }

                // re-throw with original call stack
                ExceptionDispatchInfo.Capture(ex).Throw();
            }
            return client;
        }


        /// <summary>
        ///   Stops all processing gracefully and disposes this instance.
        /// </summary>
        public void Close()
        {
            this.closed = true;
            this.Metrics.StopCollecting();
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


        /// <summary>
        ///   Registers with the established remote Battleye RCon server
        ///   using the provided password and starts listening for messages
        ///   from it.
        /// </summary>
        /// <returns> True if connection and login are successful, false otherwise. </returns>
        /// <exception cref="InvalidCredentialException"> 
        ///     The server rejected the connection with the specified credentials. 
        /// </exception>
        /// <exception cref="TimeoutException"> 
        ///     The server did not respond to the login request.
        /// </exception>
        public async Task<bool> ConnectAsync()
        {
            if (this.closed)
            {
                throw new ObjectDisposedException(
                        "RConClient", "This RConClient has been disposed.");
            }

            // Start listening for messages from the server
            this.dispatcher = new DatagramDispatcher(this);

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
                    if (this.dispatcher != null)
                    {
                        this.dispatcher.Close(); // disposes
                    }
                }
            }

            return loggedIn;
        }


#if DEBUG
        internal void WaitUntilShutdown()
        {
            this.runningLock.WaitOne();
        }
#endif

        /// <summary>
        ///   Initializes this instance when it's created.
        /// </summary>
        private void Initialize()
        {
            this.asyncOperation = AsyncOperationManager.CreateOperation(null);
            this.Log = LogManager.GetLogger(this.GetType());
            this.Metrics = new RConMetrics();
        }


        /// <summary>
        ///   Dispose managed and unmanaged resources.
        /// </summary>
        /// <param name="notFromFinalizer"> True unless we're called from the finalizer, in which case only unmanaged resources can be disposed. </param>
        private void Dispose(bool notFromFinalizer)
        {
            // Check to see if Dispose has already been called. 
            if (!this.disposed)
            {
                // If notFromFinalizer, dispose all managed resources. 
                if (notFromFinalizer)
                {
                    // Dispose managed resources.
                    if (this.dispatcher != null)
                    {
                        this.dispatcher.Close();
                    }

                    if (this.UdpClient != null)
                    {
                        this.UdpClient.Close();
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


        /// <summary>
        ///   Tries to login to the specified server.
        /// </summary>
        /// <returns> True if the login succeeded; false otherwise. </returns>
        /// <exception cref="InvalidCredentialException"> 
        ///     The server rejected the connection with the specified credentials. 
        /// </exception>
        /// <exception cref="TimeoutException"> 
        ///     The server did not respond to the login request.
        /// </exception>
        private async Task<bool> Login()
        {
            this.Log.Trace("BEFORE LOGIN await SendDatagram");
            ResponseHandler responseHandler =
                    this.dispatcher.SendDatagram(new LoginDatagram(this.password));
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


        /// <summary>
        ///   Sends a command to the server and returns the response from the server, 
        ///   waiting for a maximum of 3 seconds for it to respond.
        /// </summary>
        /// <param name="commandText"> The command text to send. </param>
        /// <returns>
        ///  A <see cref="CommandResult"/> object that details the results
        ///  of the operation. 
        /// </returns>
        public async Task<CommandResult> SendCommandAsync(string commandText)
        {
            return await this.SendCommandAsync(commandText, 1000 * 3);
        }


        /// <summary>
        ///   Sends a command to the server and returns the response from the server.
        /// </summary>
        /// <param name="commandText"> The command text to send. </param>
        /// <param name="timeout"> 
        ///   The period of time in milliseconds to wait for a response from the
        ///   server.
        /// </param>
        /// <returns>
        ///  A <see cref="CommandResult"/> object that details the results
        ///  of the operation. 
        /// </returns>
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


        /// <summary>
        ///   Sends a command to the server and returns a <see cref="ResponseHandler"/>
        ///   object that can be used to wait for the response and to retrieve it.
        /// </summary>
        /// <param name="commandText"> The command text to send. </param>
        /// <returns>
        ///   A <see cref="ResponseHandler"/> object that can be used used to 
        ///   wait for the response and to retrieve it.
        /// </returns>
        internal ResponseHandler SendCommand(string commandText)
        {
            return this.dispatcher.SendCommand(commandText);
        }


        /// <summary>
        ///   Sends a command to the server with the specified sequenceNumber,
        ///   and returns a <see cref="ResponseHandler"/> object that can be 
        ///   used to wait for the response and to retrieve it.
        /// </summary>
        /// <param name="sequenceNumber"> The sequence number for the datagram to be sent. </param>
        /// <param name="commandText"> The command text to send. </param>
        /// <returns>
        ///   A <see cref="ResponseHandler"/> object that can be used used to 
        ///   wait for the response and to retrieve it.
        /// </returns>
        /// <remarks> This method is only used for testing. </remarks>
        internal ResponseHandler SendCommand(byte sequenceNumber, string commandText)
        {
            var dgram = new CommandDatagram(sequenceNumber, commandText);
            return this.dispatcher.SendDatagram(dgram);
        }


        /// <summary>
        ///   Handles the event of the dispatcher closing (and disposing)
        ///   because of a disconnect or a user request.
        /// </summary>
        /// <param name="reason"> The reason why the dispatcher closed. </param>
        internal void HandleDispatcherClosed(ShutdownReason reason)
        {
            this.ShutdownReason = reason;
            this.dispatcher = null;

#if DEBUG
            this.runningLock.Set();
#endif

            this.OnDisconnected(new DisconnectedEventArgs(reason));
        }


        /// <summary>
        ///   Raises the <see cref="MessageReceived" /> event.
        /// </summary>
        /// <param name="e"> A <see cref="MessageReceivedEventArgs" /> that contains the event data. </param>
        internal void OnMessageReceived(MessageReceivedEventArgs e)
        {
            if (this.MessageReceived != null)
            {
                this.RaiseEventAsync(o => this.MessageReceived(this, (MessageReceivedEventArgs)o), e);
            }
        }


        /// <summary>
        ///   Raises the <see cref="ConnectionProblem" /> event.
        /// </summary>
        /// <param name="e"> A <see cref="ConnectionProblemEventArgs" /> that contains the event data. </param>
        internal void OnConnectionProblem(ConnectionProblemEventArgs e)
        {
            if (this.ConnectionProblem != null)
            {
                this.RaiseEventAsync(o => this.ConnectionProblem(this, (ConnectionProblemEventArgs)o), e);
            }
        }


        /// <summary>
        ///   Raises the <see cref="Disconnected" /> event.
        /// </summary>
        /// <param name="e"> A <see cref="DisconnectedEventArgs" /> that contains the event data. </param>
        private void OnDisconnected(DisconnectedEventArgs e)
        {
            if (this.Disconnected != null)
            {
                this.RaiseEventAsync(o => this.Disconnected(this, (DisconnectedEventArgs)o), e);
            }
        }


        /// <remarks>
        ///   Raises an event in the appropriate threading context (e.g. the UI thread or the ASP.NET context),
        ///   by using AsyncOperation. The context switch is costly, but usually what the library user will expect.
        /// </remarks>
        private void RaiseEventAsync(SendOrPostCallback call, object args)
        {
            if (this.asyncOperation != null)
            {
                this.asyncOperation.Post(call, args);
            }
            else
            {
                call(args);
            }
        }
    }

}
