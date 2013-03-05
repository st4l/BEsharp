// ----------------------------------------------------------------------------------------------------
// <copyright file="DatagramDispatcher.cs" company="Me">Copyright (c) 2013 St4l.</copyright>
// ----------------------------------------------------------------------------------------------------
namespace BESharp
{
    using System;
    using System.Diagnostics;
    using System.Net.Sockets;
    using System.Runtime.ExceptionServices;
    using System.Security.Permissions;
    using System.Threading;
    using System.Threading.Tasks;
    using Datagrams;
    using log4net;

    /// <summary>
    ///   Receives datagrams from a remote Battleye RCon server
    ///   using the supplied <see cref="UdpClient" /> and
    ///   dispatches them accordingly.
    /// </summary>
    internal sealed class DatagramDispatcher : IDisposable
    {
        internal IUdpClient UdpClient { get; private set; }

        private KeepAliveTracker keepAliveTracker;

        private Task mainLoopTask;

        private bool hasStarted;

        private bool disposed;

        private readonly RConClient rconClient;

        private readonly DatagramSender datagramSender;


        /// <summary>
        ///   Initializes a new instance of <see cref="DatagramDispatcher" />
        ///   and establishes the <see cref="UdpClient" /> to be used.
        /// </summary>
        /// <param name="client"> The <see cref="RConClient" /> to be used to connect to the RCon server. </param>
        internal DatagramDispatcher(RConClient client)
        {
            this.Metrics = new RConMetrics();
            this.KeepAlivePeriod = TimeSpan.FromSeconds(25);
            // throw new ArgumentException("Test shall not pass.");
            this.ConMsgsTracker = new SequenceTracker();
            this.CmdsTracker = new SequenceTracker();
            this.rconClient = client;
            this.UdpClient = client.UdpClient;
            this.ResponseDispatcher = new ResponseDispatcher();
            this.datagramSender = new DatagramSender(this);
            this.Log = LogManager.GetLogger(this.GetType());
        }


        /// <summary>
        ///   Use C# destructor syntax for finalization code.
        /// </summary>
        /// <remarks>
        ///   This destructor will run only if the Dispose method 
        ///   does not get called. 
        ///   Do not provide destructors in types derived from this class.
        /// </remarks>
        ~DatagramDispatcher()
        {
            this.Dispose(false);
        }



        /// <summary>
        ///     Signals whether a shutdown is in progress and the reason why it is.
        /// </summary>
        private ShutdownReason ShutdownReason { get; set; }


        /// <summary>
        ///   Gets or sets a <see cref="Boolean" /> value that specifies
        ///   whether this <see cref="DatagramDispatcher" /> discards all
        ///   console message datagrams received.
        /// </summary>
        public bool DiscardConsoleMessages { get; set; }


        internal SequenceTracker CmdsTracker { get; private set; }

        internal SequenceTracker ConMsgsTracker { get; private set; }

        internal RConMetrics Metrics { get; set; }

        private ILog Log { get; set; }

        internal ResponseDispatcher ResponseDispatcher { get; private set; }

        internal TimeSpan KeepAlivePeriod { get; private set; }


        /// <summary>
        ///   Implement IDisposable.
        /// </summary>
        public void Dispose()
        {
            this.Dispose(true);
            GC.SuppressFinalize(this);
        }


        /// <summary>
        ///   Starts acquiring and dispatching inbound datagrams in a new thread.
        /// </summary>
        /// <remarks>
        ///   Starts the main datagram pump in a new thread.
        /// </remarks>
        public void Start()
        {
            if (this.disposed)
            {
                throw new InvalidOperationException("Called Start() on a disposed instance.");
            }
            if (this.hasStarted)
            {
                throw new InvalidOperationException("Already running.");
            }

            this.hasStarted = true;

            this.mainLoopTask = new Task(this.MainLoop);
            this.mainLoopTask.ContinueWith(this.HandleMainLoopException, TaskContinuationOptions.OnlyOnFaulted);
            this.mainLoopTask.ContinueWith(this.AfterMainLoop, TaskContinuationOptions.OnlyOnRanToCompletion);
            this.mainLoopTask.ConfigureAwait(continueOnCapturedContext: true);
            this.mainLoopTask.Start();
        }


        /// <summary>
        ///   Stops all processing gracefully and disposes this instance.
        /// </summary>
        /// <remarks>
        ///   Exits the main pump thread politely.
        /// </remarks>
        public void Close()
        {
            if (this.disposed)
            {
                throw new InvalidOperationException("Called Close() on disposed instance.");
            }

            if (this.ShutdownReason == ShutdownReason.None)
            {
                this.ShutdownReason = ShutdownReason.UserRequested;
            }
        }


        /// <summary>
        ///   Meant to be called after Close(), i.e. once after each session.
        /// </summary>
        /// <param name="rconMetrics"> The <see cref="RConMetrics" /> to update. </param>
        public void UpdateMetrics(RConMetrics rconMetrics)
        {
            rconMetrics.InboundDatagramCount += this.Metrics.InboundDatagramCount;
            rconMetrics.OutboundDatagramCount += this.Metrics.OutboundDatagramCount;
            rconMetrics.ParsedDatagramsCount += this.Metrics.ParsedDatagramsCount;
            rconMetrics.DispatchedConsoleMessages += this.Metrics.DispatchedConsoleMessages;
            rconMetrics.KeepAliveDatagramsSent += this.Metrics.KeepAliveDatagramsSent;
            rconMetrics.KeepAliveDatagramsAcknowledgedByServer += this.Metrics.KeepAliveDatagramsAcknowledgedByServer;
        }


        /// <summary>
        ///   The main datagram pump.
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

            this.ResponseDispatcher.LastAcknowledgedDatagramSentTime = DateTime.Now.Subtract(this.KeepAlivePeriod).AddSeconds(5);

            while (this.ShutdownReason == ShutdownReason.None)
            {
                this.Log.Trace("Scheduling new receive task.");
                Task task = this.ReceiveDatagramAsync();
                this.Log.Trace("AFTER  scheduling new receive task.");

                // do the following at least once and until the receive task
                // has completed (or we're shutting down for some reason)
                do
                {
                    this.CheckKeepAlive(this.KeepAlivePeriod);

                    this.Log.TraceFormat("===== WAITING RECEIVE =====, Status={0}", task.Status);
                    task.Wait(500);
                    this.Log.TraceFormat("====== DONE WAITING =======, Status={0}", task.Status);
                }
                while (!task.IsCompleted && this.ShutdownReason == ShutdownReason.None);
            }

            this.Log.Trace("Main loop exited.");

            // wait a bit for the last datagrams to be parsed, processed.
            Thread.SpinWait(200);
        }


        /// <summary>
        ///   Checks whether we need to send a keep alive datagram,
        ///   and if needed sends it and checks for its acknowledgment.
        /// </summary>
        /// <param name="keepAlivePeriod"> How often to send keep alive datagrams. </param>
        private void CheckKeepAlive(TimeSpan keepAlivePeriod)
        {
            // test whether we sent a command too long ago, or
            // whether we received the last acknowledgment from the server too long ago
            // (prevents case: 
            //      * we sent a command 10 secs ago but wasn't received by the server
            //        and not acknowledged, but we have "10 secs ago" in lastCommandSentTime)
            var keepAliveAgo = DateTime.Now.Subtract(keepAlivePeriod);
            if (this.datagramSender.LastCommandSentTime < keepAliveAgo ||
                this.ResponseDispatcher.LastAcknowledgedDatagramSentTime < keepAliveAgo)
            {
                // spawn a keep alive tracker until server acknowledges
                if (this.keepAliveTracker == null)
                {
                    this.keepAliveTracker = new KeepAliveTracker(this.datagramSender, this.Metrics, this.Log);
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
        }


        private void HandleMainLoopException(Task task)
        {
            var excInfo = ExceptionDispatchInfo.Capture(task.Exception);
            this.ShutdownReason = ShutdownReason.FatalException;
            excInfo.Throw();
        }


        private void AfterMainLoop(Task task)
        {
            this.Log.Trace("AFTER MAIN LOOP");

            this.Dispose();

            this.rconClient.HandleDispatcherClosed(this.ShutdownReason);
        }


        /// <summary>
        ///   Dispose managed and unmanaged resources.
        /// </summary>
        /// <param name="notFromFinalizer"> True unless we're called from the finalizer, 
        /// in which case only unmanaged resources can be disposed. </param>
        private void Dispose(bool notFromFinalizer)
        {
            // Check to see if Dispose has already been called. 
            if (!this.disposed)
            {
                // If notFromFinalizer, dispose all managed resources. 
                if (notFromFinalizer)
                {
                    // Release managed resources.
                    this.UdpClient = null;

                    if (this.ResponseDispatcher != null)
                    {
                        this.ResponseDispatcher.Dispose();
                    }

                    if (this.mainLoopTask != null)
                    {
                        this.mainLoopTask.Wait();
                        this.mainLoopTask.Dispose();
                    }
                }

                this.disposed = true;
                this.Log.Trace("DISPOSED");
            }
        }


        /// <summary>
        ///   Handles a datagram asynchronously that was received from
        ///   the RCon server.
        /// </summary>
        [HostProtection(Synchronization = true, ExternalThreading = true)]
        private async Task ReceiveDatagramAsync()
        {
            this.Log.Trace("BEFORE await ReceiveAsync");

            // ReceiveAsync (BeginRead) will spawn a new thread
            // which blocks head-on against the IO Completion Port
            // http://msdn.microsoft.com/en-us/library/windows/desktop/aa364986(v=vs.85).aspx
            UdpReceiveResult result = await this.UdpClient.ReceiveAsync()
                                                    //// do not incurr in ANOTHER context switch cost
                                                    .ConfigureAwait(false);

            this.Log.Trace("AFTER  await ReceiveAsync");
            var processor = new InboundProcessor(this, result.Buffer, this.Log);
            if (this.TryPreProcessInboundDatagram(processor))
            {
                return;
            }

            var concreteDgram = processor.Parse();
            this.Metrics.ParsedDatagramsCount++;

            this.DispatchReceivedDatagram(concreteDgram);
        }


        /// <summary>
        ///     Tries to pre-process a datagram without further parsing
        ///     and processing.
        /// </summary>
        /// <param name="processor">The inbound processor for the datagram to be pre-processed.</param>
        /// <returns>True if it was pre-processed and no further processing is needed; false otherwise.</returns>
        private bool TryPreProcessInboundDatagram(InboundProcessor processor)
        {
            if (!processor.IsValidLength)
            {
                this.DispatchConnectionProblem(new ConnectionProblemEventArgs(ConnectionProblemType.InvalidLength));
                this.Log.Trace("INVALID datagram received");
                return true;
            }
            this.Metrics.InboundDatagramCount++;

#if DEBUG
            if (processor.IsShutDownDatagram && this.ShutdownReason == ShutdownReason.None)
            {
                this.Log.Trace("SHUTDOWN DATAGRAM RECEIVED - SHUTTING DOWN.");
                this.ShutdownReason = ShutdownReason.ServerRequested;
                return true;
            }
#endif

            if (processor.TryPreProcess())
            {
                return true;
            }

            if (!processor.VerifyCrc())
            {
                this.DispatchConnectionProblem(new ConnectionProblemEventArgs(ConnectionProblemType.Corrupted));
                return true;
            }
            return false;
        }


        /// <summary>
        ///   Dispatches the received datagram to the appropriate target.
        /// </summary>
        /// <param name="dgram"> The received datagram. </param>
        private void DispatchReceivedDatagram(IInboundDatagram dgram)
        {
            if (dgram.Type == DatagramType.ConsoleMessage)
            {
                var conMsg = (ConsoleMessageDatagram)dgram;
                this.DispatchConsoleMessage(conMsg);
                return;
            }

            // else, dgram is a response (to either a login or a command)
            this.Log.Trace("BEFORE response.DispatchResponse");
            this.ResponseDispatcher.DispatchResponse(dgram);
            this.Log.Trace("AFTER  response.DispatchResponse");
        }


        /// <summary>
        ///   Dispatches received console messages.
        /// </summary>
        /// <param name="dgram"> The <see cref="ConsoleMessageDatagram" /> representing the received console message. </param>
        private void DispatchConsoleMessage(ConsoleMessageDatagram dgram)
        {
            var args = new MessageReceivedEventArgs(dgram);
            this.rconClient.OnMessageReceived(args);

            this.Metrics.DispatchedConsoleMessages++;
        }


        /// <summary>
        ///   Dispatches connection problem events.
        /// </summary>
        private void DispatchConnectionProblem(ConnectionProblemEventArgs args)
        {
            this.rconClient.OnConnectionProblem(args);
        }


        internal ResponseHandler SendDatagram(IOutboundDatagram acknowledgeMessageDatagram)
        {
            return this.datagramSender.SendDatagram(acknowledgeMessageDatagram);
        }


        public ResponseHandler SendCommand(string commandText)
        {
            return this.datagramSender.SendCommand(commandText);
        }
    }
}
