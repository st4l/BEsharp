﻿// ----------------------------------------------------------------------------------------------------
// <copyright file="MessageDispatcher.cs" company="Me">Copyright (c) 2013 St4l.</copyright>
// ----------------------------------------------------------------------------------------------------
namespace BESharp
{
    using System;
    using System.ComponentModel;
    using System.Diagnostics;
    using System.Linq;
    using System.Net.Sockets;
    using System.Runtime.ExceptionServices;
    using System.Security.Permissions;
    using System.Threading;
    using System.Threading.Tasks;
    using Datagrams;
    using log4net;

    /// <summary>
    ///   Receives messages from a remote Battleye RCon server
    ///   using the supplied <see cref="UdpClient" /> and
    ///   dispatches them accordingly.
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

        private bool mainLoopDead;

        private int outCount;

        private int parsedDatagramsCount;

        private ManualResetEventSlim shutdownLock;

        private IUdpClient udpClient;

        private int keepAlivePacketsSent;

        private int lastCommandSequenceNumber = -1;

        private DateTime lastCommandSentTime;

        private DateTime lastAcknowledgedCmdSentTime;

        private Task mainLoopTask;


        /// <summary>
        ///   Initializes a new instance of <see cref="MessageDispatcher" />
        ///   and establishes the <see cref="UdpClient" /> to be used.
        /// </summary>
        /// <param name="udpClient"> The <see cref="UdpClient" /> to be used to connect to the RCon server. </param>
        internal MessageDispatcher(IUdpClient udpClient)
        {
            // throw new ArgumentException("Test shall not pass.");
            this.udpClient = udpClient;
            this.responseDispatcher = new ResponseMessageDispatcher(this);
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
        ~MessageDispatcher()
        {
            this.Dispose(false);
        }


        /// <summary>
        ///   Occurs when a console message is received from the RCon server.
        /// </summary>
        internal event EventHandler<MessageReceivedEventArgs> MessageReceived;


        /// <summary>
        ///   Occurs when a problem is detected in the incoming packets,
        ///   such as corrupted data.
        /// </summary>
        internal event EventHandler<PacketProblemEventArgs> PacketProblem;


        /// <summary>
        ///   Occurs when a disconnection is detected.
        /// </summary>
        internal event EventHandler<DisconnectedEventArgs> Disconnected;


        public ShutdownReason ShutdownReason { get; private set; }


        /// <summary>
        ///   Gets or sets a <see cref="Boolean" /> value that specifies
        ///   whether this <see cref="MessageDispatcher" /> discards all
        ///   console message datagrams received (the <see cref="MessageReceived" />
        ///   event is never raised).
        /// </summary>
        internal bool DiscardConsoleMessages { get; set; }


        private ILog Log { get; set; }


        /// <summary>
        ///   Implement IDisposable.
        /// </summary>
        public void Dispose()
        {
            this.Dispose(true);
            GC.SuppressFinalize(this);
        }


        /// <summary>
        ///   Starts acquiring and dispatching inbound messages in a new thread.
        /// </summary>
        /// <remarks>
        ///   Starts the main message pump in a new thread.
        /// </remarks>
        internal void Start()
        {
            if (this.hasStarted)
            {
                throw new InvalidOperationException("Already running.");
            }

            this.hasStarted = true;

            this.asyncOperation = AsyncOperationManager.CreateOperation(null);

            this.mainLoopTask = new Task(this.MainLoop);
            this.mainLoopTask.ContinueWith(this.ExceptionHandler, TaskContinuationOptions.OnlyOnFaulted);
            this.mainLoopTask.ContinueWith(this.AfterMainLoop, TaskContinuationOptions.OnlyOnRanToCompletion);
            this.mainLoopTask.ConfigureAwait(true);
            this.mainLoopTask.Start();
        }


        /// <summary>
        ///   Stops all processing gracefully and disposes this instance.
        /// </summary>
        /// <remarks>
        ///   Exits the main pump thread politely.
        /// </remarks>
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


        /// <summary>
        ///   Registers a handler to be notified when a response
        ///   message arrives, and which accepts the response message itself.
        /// </summary>
        /// <param name="dgram"> </param>
        internal ResponseHandler GetResponseHandler(IOutboundDatagram dgram)
        {
            return this.responseDispatcher.CreateOrGetHandler(dgram);
        }


        /// <summary>
        ///   Raises the <see cref="MessageReceived" /> event.
        /// </summary>
        /// <param name="e"> An <see cref="MessageReceivedEventArgs" /> that contains the event data. </param>
        internal void OnMessageReceived(MessageReceivedEventArgs e)
        {
            if (this.MessageReceived != null)
            {
                this.MessageReceived(this, e);
            }
        }


        /// <summary>
        ///   Raises the <see cref="Disconnected" /> event.
        /// </summary>
        /// <param name="e"> An <see cref="DisconnectedEventArgs" /> that contains the event data. </param>
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
                handler = this.GetResponseHandler(dgram);
            }

            // socket is thread safe
            // i.e. it is ok to send & receive at the same time from different threads
            this.Log.Trace("BEFORE await SendDatagramAsync");
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

            this.Log.Trace("AFTER  await SendDatagramAsync");

            Debug.Assert(
                         transferredBytes == bytes.Length,
                         "Sent bytes count equal count of bytes meant to be sent.");

            if (dgram.Type == DatagramType.Command)
            {
                this.lastCommandSentTime = DateTime.Now;
            }

            return handler;
        }


        /// <summary>
        ///   Meant to be called after Close(), i.e. once after each session.
        /// </summary>
        /// <param name="rconMetrics"> The <see cref="RConMetrics" /> to update. </param>
        internal void UpdateMetrics(RConMetrics rconMetrics)
        {
            rconMetrics.InboundPacketCount += this.inCount;
            rconMetrics.OutboundPacketCount += this.outCount;
            rconMetrics.ParsedDatagramsCount += this.parsedDatagramsCount;
            rconMetrics.DispatchedConsoleMessages += this.dispatchedConsoleMessages;
            rconMetrics.KeepAlivePacketsSent += this.keepAlivePacketsSent;
            rconMetrics.KeepAlivePacketsAcknowledgedByServer += this.keepAlivePacketsAcks;
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


        private static bool PreValidateReceivedDatagram(byte[] buffer)
        {
            if (buffer == null || buffer.Length < 7)
            {
                return false;
            }

            return true;
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


        private void AfterMainLoop(Task task)
        {
            this.Log.Trace("AFTER MAIN LOOP");
            this.InternalClose();
        }


        private void RaiseDisconnected(object args)
        {
            this.OnDisconnected((DisconnectedEventArgs)args);
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
                    // Release managed resources.
                    this.udpClient = null;

                    if (this.shutdownLock != null)
                    {
                        this.shutdownLock.Dispose();
                    }

                    if (this.responseDispatcher != null)
                    {
                        this.responseDispatcher.Dispose();
                    }

                    if (this.mainLoopTask != null)
                    {
                        this.mainLoopTask.Dispose();
                    }
                }

                // Note disposing has been done.
                this.disposed = true;
            }
        }


        /// <summary>
        ///   The main message pump.
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
            this.lastCommandSentTime = this.lastAcknowledgedCmdSentTime = DateTime.Now.AddSeconds(-10);

            while (this.ShutdownReason == ShutdownReason.None)
            {
                this.Log.Trace("Scheduling new receive task.");
                Task task = this.ReceiveDatagramAsync();
                this.Log.Trace("AFTER  scheduling new receive task.");

                // do the following at least once and until the receive task
                // has completed (or we're shutting down for some reason)
                do
                {
                    // test whether we sent a command too long ago, or
                    // whether we received the last acknowledgment from the server too long ago
                    // (prevents case: 
                    //      * we sent a command 10 secs ago but wasn't received by the server
                    //        and not acknowledged, but we have "10 secs ago" in lastCommandSentTime)
                    var keepAliveAgo = DateTime.Now.Subtract(keepAlivePeriod);
                    if (this.lastCommandSentTime < keepAliveAgo ||
                        this.lastAcknowledgedCmdSentTime < keepAliveAgo)
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

                    this.Log.TraceFormat("===== WAITING RECEIVE =====, Status={0}", task.Status);
                    task.Wait(500);
                    this.Log.TraceFormat("====== DONE WAITING =======, Status={0}", task.Status);
                }
                while (!task.IsCompleted && this.ShutdownReason == ShutdownReason.None);
            }

            this.mainLoopDead = true;
            this.Log.Trace("Main loop exited.");

            // signal we're exiting the thread
            this.ExitMainLoop();
        }


        private void ExitMainLoop()
        {
            this.Log.Trace("EXIT MAIN LOOP");

            if (this.shutdownLock != null)
            {
                // signal we're exiting the thread
                this.Log.Trace("shutdownLock set.");
                this.shutdownLock.Set();
            }
        }


        private void InternalClose()
        {
            this.Log.Trace("CLOSE");

            if (!this.forceShutdown)
            {
                this.Log.Trace("SHUTDOWN COMMENCING");

                if (!this.mainLoopDead)
                {
                    this.shutdownLock = new ManualResetEventSlim(false);

                    // wait until the main thread is exited
                    this.Log.Trace("WAITING FOR THREADS TO EXIT");
                    this.shutdownLock.Wait();
                }

                this.Log.Trace("SHUTDOWN ACHIEVED - DISPOSING");
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
        ///   Handles a message asynchronously that was received from
        ///   the RCon server.
        /// </summary>
        [HostProtection(Synchronization = true, ExternalThreading = true)]
        private async Task ReceiveDatagramAsync()
        {
            // ReceiveAsync (BeginRead) will spawn a new thread
            // which blocks head-on against the IO Completion Port
            // http://msdn.microsoft.com/en-us/library/windows/desktop/aa364986(v=vs.85).aspx
            Task<UdpReceiveResult> task = this.udpClient.ReceiveAsync();

            this.Log.Trace("BEFORE await ReceiveAsync");
            UdpReceiveResult result = await task
                                                    //// do not incurr in ANOTHER context switch cost
                                                    .ConfigureAwait(false);

            this.Log.Trace("AFTER  await ReceiveAsync");
            byte[] buffer = result.Buffer;
            if (!PreValidateReceivedDatagram(buffer))
            {
                this.DispatchPacketProblem(new PacketProblemEventArgs(PacketProblemType.InvalidLength));
                this.Log.Trace("INVALID datagram received");
                return;
            }

            this.inCount++;

            bool proceed = await this.PreProcessReceivedDatagram(buffer);
            if (!proceed)
            {
                return;
            }

            if (!VerifyCrc(buffer))
            {
                this.DispatchPacketProblem(new PacketProblemEventArgs(PacketProblemType.Corrupted));
                return;
            }

            await this.DispatchReceivedDatagram(buffer);
        }


        private async Task<bool> PreProcessReceivedDatagram(byte[] buffer)
        {
            byte dgramType = Buffer.GetByte(buffer, Constants.DatagramTypeIndex);
            this.Log.TraceFormat("{0:0}    Type dgram received.", dgramType);

#if DEBUG
            // shutdown message from server (not in protocol, used only for testing)
            if (dgramType == (byte)DatagramType.TestServerShutdown
                && this.ShutdownReason == ShutdownReason.None)
            {
                this.Log.Trace("SHUTDOWN DATAGRAM RECEIVED - SHUTTING DOWN.");
                this.ShutdownReason = ShutdownReason.ServerRequested;
                return false;
            }
#endif

            if (dgramType == (byte)DatagramType.Message)
            {
                return await this.PreProcessConsoleMessage(buffer);
            }

            if (dgramType == (byte)DatagramType.Command)
            {
                return this.PreProcessCommandResponse(buffer);
            }

            return true;
        }


        private async Task<bool> PreProcessConsoleMessage(byte[] buffer)
        {
            byte conMsgSeq = Buffer.GetByte(buffer, Constants.ConsoleMessageSequenceNumberIndex);
            this.Log.TraceFormat("M#{0:000} Received", conMsgSeq);

            if (this.DiscardConsoleMessages)
            {
                await this.AcknowledgeMessage(conMsgSeq);
                return false;
            }

            // if we already received a console message with this seq number
            bool repeated = this.conMsgsTracker.Contains(conMsgSeq);
            if (repeated)
            {
                // if we did, just acknowledge it and don't process it
                // (the server probably didn't receive our previous ack)
                await this.AcknowledgeMessage(conMsgSeq);
                return false;
            }

            // register the sequence number and continue processing the message
            this.conMsgsTracker.StartTracking(conMsgSeq);
            return true;
        }


        private bool PreProcessCommandResponse(byte[] buffer)
        {
            byte cmdSeq = Buffer.GetByte(buffer, Constants.CommandResponseSequenceNumberIndex);
            bool repeated = this.cmdsTracker.Contains(cmdSeq);
            if (repeated)
            {
                // doesn't repeat because multipart?
                if (Buffer.GetByte(buffer, Constants.CommandResponseMultipartMarkerIndex) != 0x00)
                {
                    return false;
                } // else go ahead and dispatch the part
            }
            else
            {
                this.cmdsTracker.StartTracking(cmdSeq);
            }

            return true;
        }


        /// <summary>
        ///   Dispatches the received datagram to the appropriate target.
        /// </summary>
        /// <param name="buffer"> The received bytes. </param>
        private async Task DispatchReceivedDatagram(byte[] buffer)
        {
            IInboundDatagram dgram = InboundDatagramBase.ParseReceivedBytes(buffer);
            this.parsedDatagramsCount++;
            if (dgram.Type == DatagramType.Message)
            {
                var conMsg = (ConsoleMessageDatagram)dgram;
                await this.AcknowledgeMessage(conMsg.SequenceNumber);
                this.DispatchConsoleMessage(conMsg);
                return;
            }

            // else, dgram is a response (to either a login or a command)
            this.Log.Trace("BEFORE response.Dispatch");
            this.responseDispatcher.Dispatch(dgram);
            this.Log.Trace("AFTER  response.Dispatch");
        }


        /// <summary>
        ///   Sends a datagram back to the server acknowledging receipt of
        ///   a console message datagram.
        /// </summary>
        /// <param name="seqNumber"> The sequence number of the received <see cref="ConsoleMessageDatagram" /> . </param>
        private async Task AcknowledgeMessage(byte seqNumber)
        {
            await this.SendDatagramAsync(new AcknowledgeMessageDatagram(seqNumber));
            this.Log.TraceFormat("M#{0:000} Acknowledged", seqNumber);
        }


        /// <summary>
        ///   Dispatches received console messages to the appropriate
        ///   threading context (e.g. the UI thread or the ASP.NET context),
        ///   by using AsyncOperation.
        /// </summary>
        /// <param name="dgram"> The <see cref="ConsoleMessageDatagram" /> representing the received console message. </param>
        /// <remarks>
        ///   The context switch is costly, but usually what the
        ///   library user will expect.
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
        ///   Dispatches packet problem events to the appropriate
        ///   threading context (e.g. the UI thread or the ASP.NET context),
        ///   by using AsyncOperation.
        /// </summary>
        /// <remarks>
        ///   The context switch is costly, but usually what the
        ///   library user will expect.
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
            var excInfo = ExceptionDispatchInfo.Capture(task.Exception);
            this.forceShutdown = true;
            this.ShutdownReason = ShutdownReason.FatalException;
            excInfo.Throw();
        }


        private void UpdateLastAckSentTime(DateTime sentTime)
        {
            if (sentTime > this.lastAcknowledgedCmdSentTime)
            {
                this.lastAcknowledgedCmdSentTime = sentTime;
            }
        }
    }
}
