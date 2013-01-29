// ----------------------------------------------------------------------------------------------------
// <copyright file="MessageDispatcher.cs" company="Me">Copyright (c) 2012 St4l.</copyright>
// ----------------------------------------------------------------------------------------------------


namespace BESharp
{

    using System;
    using System.ComponentModel;
    using System.Diagnostics;
    using System.Globalization;
    using System.Linq;
    using System.Net.Sockets;
    using System.Runtime.ExceptionServices;
    using System.Security.Permissions;
    using System.Threading;
    using System.Threading.Tasks;
    using Datagrams;
    using log4net;
    using log4net.Core;

    
    /// <summary>
    ///     Receives messages from a remote Battleye RCon server
    ///     using the supplied <see cref="UdpClient" /> and
    ///     dispatches them accordingly.
    /// </summary>
    internal sealed partial class MessageDispatcher : IDisposable
    {
        private readonly SequenceTracker cmdsTracker = new SequenceTracker();
        private readonly SequenceTracker conMsgsTracker = new SequenceTracker();
        private readonly ResponseMessageDispatcher responseDispatcher;

        private AsyncOperation asyncOperation;
        private int dispatchedConsoleMessages;
        private bool disposed;

        private bool forceShutdown;
        private bool hasStarted;
        private int inCount;
        private int keepAlivePacketsAcks;
        private KeepAliveTracker keepAliveTracker;
        private DateTime lastCmdSentTime;
        // private DateTime lastDgramReceivedTime;

        private bool mainLoopDead;
        private int outCount;
        private int parsedDatagramsCount;
        private ManualResetEventSlim shutdownLock;
        private IUdpClient udpClient;
        private int keepAlivePacketsSent;

        private int lastCommandSequenceNumber = -1;


        /// <summary>
        ///     Initializes a new instance of <see cref="MessageDispatcher" />
        ///     and establishes the <see cref="UdpClient" /> to be used.
        /// </summary>
        /// <param name="udpClient">
        ///     The <see cref="UdpClient" /> to be used to connect to the
        ///     RCon server.
        /// </param>
        internal MessageDispatcher(IUdpClient udpClient)
        {
            // throw new ArgumentException("Test shall not pass.");
            this.udpClient = udpClient;
            this.responseDispatcher = new ResponseMessageDispatcher();
            this.Log = LogManager.GetLogger(this.GetType());
        }


        public ShutdownReason ShutdownReason { get; private set; }


        private ILog Log { get; set; }

        /// <summary>
        ///     Gets or sets a <see cref="Boolean" /> value that specifies
        ///     whether this <see cref="MessageDispatcher" /> discards all
        ///     console message datagrams received (the <see cref="MessageReceived" />
        ///     event is never raised).
        /// </summary>
        internal bool DiscardConsoleMessages { get; set; }


        #region IDisposable Members

        /// <summary>
        ///     Implement IDisposable.
        /// </summary>
        public void Dispose()
        {
            this.Dispose(true);
            GC.SuppressFinalize(this);
        }

        #endregion


        /// <summary>
        ///     Use C# destructor syntax for finalization code. 
        /// </summary>
        /// <remarks>
        ///     This destructor will run only if the Dispose method 
        ///     does not get called. 
        ///     It gives your base class the opportunity to finalize. 
        ///     Do not provide destructors in types derived from this class.
        /// </remarks>
        ~MessageDispatcher()
        {
            this.Dispose(false);
        }


        /// <summary>
        ///     Occurs when a console message is received from the RCon server.
        /// </summary>
        internal event EventHandler<MessageReceivedEventArgs> MessageReceived;


        /// <summary>
        ///     Occurs when a console message is received from the RCon server.
        /// </summary>
        internal event EventHandler<DisconnectedEventArgs> Disconnected;


        /// <summary>
        ///     Starts acquiring and dispatching inbound messages in a new thread.
        /// </summary>
        /// <remarks>Starts the main message pump in a new thread.</remarks>
        internal void Start()
        {
            if (this.hasStarted)
            {
                throw new InvalidOperationException("Already running.");
            }

            this.hasStarted = true;

            this.asyncOperation = AsyncOperationManager.CreateOperation(null);

            var task = new Task(this.MainLoop);
            task.ContinueWith(this.ExceptionHandler, TaskContinuationOptions.OnlyOnFaulted);
            task.ContinueWith(this.AfterMainLoop, TaskContinuationOptions.OnlyOnRanToCompletion);
            task.ConfigureAwait(true);
            task.Start();
        }


        private void AfterMainLoop(Task task)
        {
            this.LogTrace("AFTER MAIN LOOP");
            this.InternalClose();
        }


        /// <summary>
        ///     Stops all processing gracefully and disposes this instance.
        /// </summary>
        /// <remarks>Exits the main pump thread politely.</remarks>
        internal void Close()
        {
            if (this.disposed)
            {
                return;
            }

            if (this.ShutdownReason == ShutdownReason.None)
            {
                this.ShutdownReason = ShutdownReason.UserRequested;
            }
            this.InternalClose();
        }


        private void RaiseDisconnected(object args)
        {
            this.OnDisconnected((DisconnectedEventArgs)args);
        }


        /// <summary>
        ///     Registers a handler to be notified when a response
        ///     message arrives, and which accepts the response message itself.
        /// </summary>
        /// <param name="handler"></param>
        internal void RegisterResponseHandler(ResponseHandler handler)
        {
            this.responseDispatcher.Register(handler);
        }


        /// <summary>
        ///     Raises the <see cref="MessageReceived" /> event.
        /// </summary>
        /// <param name="e">
        ///     An <see cref="MessageReceivedEventArgs" /> that
        ///     contains the event data.
        /// </param>
        internal void OnMessageReceived(MessageReceivedEventArgs e)
        {
            if (this.MessageReceived != null)
            {
                this.MessageReceived(this, e);
            }
        }


        /// <summary>
        ///     Raises the <see cref="Disconnected" /> event.
        /// </summary>
        /// <param name="e">
        ///     An <see cref="DisconnectedEventArgs" /> that
        ///     contains the event data.
        /// </param>
        internal void OnDisconnected(DisconnectedEventArgs e)
        {
            if (this.Disconnected != null)
            {
                this.Disconnected(this, e);
            }
        }


        [HostProtection(Synchronization = true, ExternalThreading = true)]
        internal async Task<ResponseHandler> SendDatagramAsync(IOutboundDatagram dgram)
        {
            // this.outboundQueue.Enqueue(dgram);
            byte[] bytes = dgram.Build();

            ResponseHandler handler = null;
            if (dgram.ExpectsResponse)
            {
                handler = new ResponseHandler(dgram);
                this.RegisterResponseHandler(handler);
            }

            // socket is thread safe
            // i.e. it is ok to send & receive at the same time from different threads
            this.LogTrace("BEFORE await SendDatagramAsync");
            int transferredBytes =
                await
                this.udpClient.SendAsync(bytes, bytes.Length)
                    .ConfigureAwait(continueOnCapturedContext: false);
            dgram.SentTime = DateTime.Now;
            this.outCount++;
            if (dgram.Type == DatagramType.Command)
            {
                this.lastCommandSequenceNumber++;
            }

            this.LogTrace("AFTER  await SendDatagramAsync");

            Debug.Assert(
                transferredBytes == bytes.Length,
                "Sent bytes count equal count of bytes meant to be sent.");

            if (dgram.Type == DatagramType.Command)
            {
                this.lastCmdSentTime = DateTime.Now;
            }

            return handler;
        }


        /// <summary>
        ///     Meant to be called after Close(), i.e. once after each session.
        /// </summary>
        /// <param name="rConMetrics">The <see cref="RConMetrics"/> to update.</param>
        internal void UpdateMetrics(RConMetrics rConMetrics)
        {
            rConMetrics.InboundPacketCount += this.inCount;
            rConMetrics.OutboundPacketCount += this.outCount;
            rConMetrics.ParsedDatagramsCount += this.parsedDatagramsCount;
            rConMetrics.DispatchedConsoleMessages += this.dispatchedConsoleMessages;
            rConMetrics.KeepAlivePacketsSent += this.keepAlivePacketsSent;
            rConMetrics.KeepAlivePacketsAcknowledgedByServer += this.keepAlivePacketsAcks;
        }


        internal byte GetNextCommandSequenceNumber()
        {
            var next = this.lastCommandSequenceNumber + 1;
            if (next > 255)
            {
                next = 0;
            }
            return (byte)next;
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
                    // Release managed resources.
                    this.udpClient = null;

                    if (this.shutdownLock != null)
                    {
                        this.shutdownLock.Dispose();
                    }
                }

                // Note disposing has been done.
                this.disposed = true;
            }
        }


        /// <summary>
        ///     The main message pump.
        /// </summary>
        [HostProtection(Synchronization = true, ExternalThreading = true)]
        private void MainLoop()
        {
            // throw new ArgumentException("Test shall not pass.");

            // Check whether the thread has previously been named 
            // to avoid a possible InvalidOperationException. 
            if (Thread.CurrentThread.Name == null)
            {
                Thread.CurrentThread.Name = "MainPUMP" + Thread.CurrentThread.ManagedThreadId;
            }

            TimeSpan keepAlivePeriod = TimeSpan.FromSeconds(25);
            this.lastCmdSentTime = DateTime.Now.AddSeconds(-10);

            while (this.ShutdownReason == ShutdownReason.None)
            {
                this.LogTrace("Scheduling new receive task.");
                Task task = this.ReceiveDatagramAsync();
                this.LogTrace("AFTER  scheduling new receive task.");

                // do the following at least once and until the receive task
                // has completed (or we're shutting down for some reason)
                do
                {
                    if (DateTime.Now - this.lastCmdSentTime > keepAlivePeriod)
                    {
                        // spawn a keep alive tracker until server acknowledges
                        if (this.keepAliveTracker == null)
                        {
                            this.keepAliveTracker = new KeepAliveTracker(this);
                        }
                    }

                    // if keepAliveTracker is alive, ping and check for ack
                    if (this.keepAliveTracker != null)
                    {
                        if (this.keepAliveTracker.Ping())
                        {
                            // success, no need to keep pinging
                            this.keepAliveTracker = null;
                        }
                        else if (this.keepAliveTracker.Expired)
                        {
                            // no ack after several tries, shutdown
                            this.ShutdownReason = ShutdownReason.NoResponseFromServer;
                        }
                    }

                    this.LogTraceFormat("===== WAITING RECEIVE =====, Status={0}", task.Status);
                    task.Wait(500);
                    this.LogTraceFormat("====== DONE WAITING =======, Status={0}", task.Status);
                }
                while (!task.IsCompleted && this.ShutdownReason == ShutdownReason.None );
            }

            this.mainLoopDead = true;
            this.LogTrace("Main loop exited.");

            // signal we're exiting the thread
            this.ExitMainLoop();
        }


        private void ExitMainLoop()
        {
            this.LogTrace("EXIT MAIN LOOP");

            if (this.shutdownLock != null)
            {
                // signal we're exiting the thread
                this.LogTrace("shutdownLock set.");
                this.shutdownLock.Set();
            }
        }


        private void InternalClose()
        {
            this.LogTrace("CLOSE");

            if (!this.forceShutdown)
            {
                this.LogTrace("SHUTDOWN COMMENCING");

                if (!this.mainLoopDead)
                {
                    this.shutdownLock = new ManualResetEventSlim(false);

                    // wait until the main thread is exited
                    this.LogTrace("WAITING FOR THREADS TO EXIT");
                    this.shutdownLock.Wait();
                }

                this.LogTrace("SHUTDOWN ACHIEVED - DISPOSING");
            }

            this.Dispose();

            var args = new DisconnectedEventArgs();
            if (this.asyncOperation != null)
            {
                this.asyncOperation.Post(this.RaiseDisconnected, args);
            }
            else
            {
                this.RaiseDisconnected(args);
            }
        }


        /// <summary>
        ///     Handles a message asynchronously that was received from
        ///     the RCon server.
        /// </summary>
        [HostProtection(Synchronization = true, ExternalThreading = true)]
        private async Task ReceiveDatagramAsync()
        {
            // ReceiveAsync (BeginRead) will spawn a new thread
            // which blocks head-on against the IO Completion Port
            // http://msdn.microsoft.com/en-us/library/windows/desktop/aa364986(v=vs.85).aspx
            Task<UdpReceiveResult> task = this.udpClient.ReceiveAsync();

            this.LogTrace("BEFORE await ReceiveAsync");
            UdpReceiveResult result = await task

                                                // do not incurr in ANOTHER context switch cost
                                                .ConfigureAwait(false);
            this.LogTrace("AFTER  await ReceiveAsync");
            if (!this.ValidateReceivedDatagram(result))
            {
                this.DispatchPacketProblem(new PacketProblemEventArgs(PacketProblemType.InvalidLength));
                this.LogTrace("INVALID datagram received");
                return;
            }

            byte dgramType = result.Buffer[Constants.DatagramTypeIndex];
            this.inCount++;
            this.LogTraceFormat("{0:0}    Type dgram received.", dgramType);

#if DEBUG
            // shutdown msg from server (not in protocol, used only for testing)
            if (dgramType == 0xFF && this.ShutdownReason == ShutdownReason.None)
            {
                this.LogTrace("SHUTDOWN DATAGRAM RECEIVED - SHUTTING DOWN.");
                this.ShutdownReason = ShutdownReason.ServerRequested;
                return;
            }
#endif

            if (dgramType == (byte)DatagramType.Message)
            {
                byte conMsgSeq = result.Buffer[Constants.ConsoleMessageSequenceNumberIndex];
                this.LogTraceFormat("M#{0:000} Received", conMsgSeq);

                if (this.DiscardConsoleMessages)
                {
                    await this.AcknowledgeMessage(conMsgSeq);
                    return;
                }

                // if we already received a console message with this seq number
                bool repeated = this.conMsgsTracker.Contains(conMsgSeq);
                if (repeated)
                {
                    // if we did, just acknowledge it and don't process it
                    // (the server probably didn't receive our previous ack)
                    await this.AcknowledgeMessage(conMsgSeq);
                    return;
                }

                // register the sequence number and continue processing the msg
                this.conMsgsTracker.StartTracking(conMsgSeq);
            }


            if (dgramType == (byte)DatagramType.Command)
            {
                // command response
                byte cmdSeq = result.Buffer[Constants.CommandResponseSequenceNumberIndex];
                bool repeated = this.cmdsTracker.Contains(cmdSeq);
                if (repeated)
                {
                    // doesn't repeat because multipart?
                    if (result.Buffer[Constants.CommandResponseMultipartFlag] != 0x00)
                    {
                        return;
                    } // else go ahead and dispatch the part
                }
                else
                {
                    this.cmdsTracker.StartTracking(cmdSeq);
                }
            }


            await this.DispatchReceivedDatagram(result.Buffer);
        }


        private bool ValidateReceivedDatagram(UdpReceiveResult result)
        {
            if (result.Buffer == null || result.Buffer.Length < 7)
            {
                return false;
            }
            return true;
        }


        /// <summary>
        ///     Dispatches the received datagram to the appropriate target.
        /// </summary>
        /// <param name="buffer">
        ///     The received bytes.
        /// </param>
        private async Task DispatchReceivedDatagram(byte[] buffer)
        {
            if (!VerifyCrc(buffer))
            {
                this.DispatchPacketProblem(new PacketProblemEventArgs(PacketProblemType.Corrupted));
                return;
            }

            IInboundDatagram dgram = InboundDatagramBase.ParseReceivedBytes(buffer);
            this.parsedDatagramsCount++;
            if (dgram.Type == DatagramType.Message)
            {
                var conMsg = (ConsoleMessageDatagram)dgram;
                await this.AcknowledgeMessage(conMsg.SequenceNumber);
                this.DispatchConsoleMessage(conMsg);
                return;
            }

            // else, dgram is either login or command response
            this.LogTrace("BEFORE response.Dispatch");
            this.responseDispatcher.Dispatch(dgram);
            this.LogTrace("AFTER  response.Dispatch");
        }


        /// <summary>
        ///     Sends a datagram back to the server acknowledging receipt of
        ///     a console message datagram.
        /// </summary>
        /// <param name="seqNumber">
        ///     The sequence number of the received <see cref="ConsoleMessageDatagram" />.
        /// </param>
        private async Task AcknowledgeMessage(byte seqNumber)
        {
            await this.SendDatagramAsync(new AcknowledgeMessageDatagram(seqNumber));
            this.LogTraceFormat("M#{0:000} Acknowledged", seqNumber);
        }


        /// <summary>
        ///     Dispatches received console messages to the appropriate
        ///     threading context (e.g. the UI thread or the ASP.NET context),
        ///     by using AsyncOperation.
        /// </summary>
        /// <param name="dgram">
        ///     The <see cref="ConsoleMessageDatagram" />
        ///     representing the received console message.
        /// </param>
        /// <remarks>
        ///     The context switch is costly, but usually what the
        ///     library user will expect.
        /// </remarks>
        private void DispatchConsoleMessage(ConsoleMessageDatagram dgram)
        {
            if (this.MessageReceived != null)
            {
                var args = new MessageReceivedEventArgs(dgram);

                if (this.asyncOperation != null)
                {
                    this.asyncOperation.Post(o => this.OnMessageReceived((MessageReceivedEventArgs)o), args);
                }
                else
                {
                    this.OnMessageReceived(args);
                }
            }
            this.dispatchedConsoleMessages++;
        }


        /// <summary>
        ///     Dispatches packet problem events to the appropriate
        ///     threading context (e.g. the UI thread or the ASP.NET context),
        ///     by using AsyncOperation.
        /// </summary>
        /// <remarks>
        ///     The context switch is costly, but usually what the
        ///     library user will expect.
        /// </remarks>
        private void DispatchPacketProblem(PacketProblemEventArgs args)
        {
            if (this.PacketProblem != null)
            {
                if (this.asyncOperation != null)
                {
                    this.asyncOperation.Post(o => this.OnPacketProblem((PacketProblemEventArgs)o), args);
                }
                else
                {
                    this.OnPacketProblem(args);
                }
            }
        }


        private void OnPacketProblem(PacketProblemEventArgs e)
        {
            if (this.PacketProblem != null)
            {
                this.PacketProblem(this, e);
            }
        }


        private void ExceptionHandler(Task task)
        {
            ExceptionDispatchInfo exInfo = ExceptionDispatchInfo.Capture(task.Exception);
            this.forceShutdown = true;
            exInfo.Throw();
        }


        private static bool VerifyCrc(byte[] buffer)
        {
            int payloadLength = Buffer.ByteLength(buffer) - 6;
            var payload = new byte[payloadLength];
            Buffer.BlockCopy(buffer, 6, payload, 0, payloadLength);
            byte[] computedChecksum;
            using (var crc = new Crc32(Crc32.DefaultPolynomialReversed, Crc32.DefaultSeed))
            {
                computedChecksum = crc.ComputeHash(payload);
                Array.Reverse(computedChecksum);
            }

            var originalChecksum = new byte[4];
            Buffer.BlockCopy(buffer, 2, originalChecksum, 0, 4);

            return computedChecksum.SequenceEqual(originalChecksum);
        }


        [Conditional("TRACE")]
        private void LogTrace(string msg)
        {
            this.Log.Logger.Log(
                this.Log.GetType(),
                Level.Debug,
                msg,
                null);
        }


        [Conditional("TRACE")]
        private void LogTraceFormat(string fmt, params object[] args)
        {
            this.Log.Logger.Log(
                this.Log.GetType(),
                Level.Debug,
                string.Format(CultureInfo.InvariantCulture, fmt, args),
                null);
        }


        public event EventHandler<PacketProblemEventArgs> PacketProblem;
    }
}
