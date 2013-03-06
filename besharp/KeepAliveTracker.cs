// ----------------------------------------------------------------------------------------------------
// <copyright file="KeepAliveTracker.cs" company="Me">Copyright (c) 2013 St4l.</copyright>
// ----------------------------------------------------------------------------------------------------
using log4net;
namespace BESharp
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Linq;
    using System.Threading;
    using Datagrams;


    /// <summary>
    ///    Sends a "keep alive" datagram to the server and checks for its
    ///    acknowledgment by the server. If an acknowledgment isn't received,
    ///    keeps sending more datagrams until one is acknowledged, up to a 
    ///    limit of <see cref="MaxTries"/>.
    /// </summary>
    /// <remarks>
    ///     This class is intended to be called assiduously while a keep alive
    ///     acknowledgment by the server is needed. An instance should be 
    ///     created when one is needed, and <see cref="SendAndCheckForAck"/>
    ///     should be called at least once a second to check whether a datagram
    ///     needs to be sent or a previous try has already been acknowledged by
    ///     the server.
    /// </remarks>
    internal sealed class KeepAliveTracker
    {
        private readonly DatagramSender datagramSender;

        private readonly RConMetrics metrics;

        private readonly TimeSpan period = TimeSpan.FromSeconds(1);

        private readonly List<ResponseHandler> sentHandlers = new List<ResponseHandler>();

        private DateTime lastSendTime = DateTime.MinValue;

        private int sentCount;

        private int sequenceNumber = -1;


        internal KeepAliveTracker(DatagramSender datagramSender, RConMetrics metrics, ILog log)
        {
            if (datagramSender == null)
            {
                throw new ArgumentNullException("datagramSender");
            }
            if (metrics == null)
            {
                throw new ArgumentNullException("metrics");
            }
            if (log == null)
            {
                throw new ArgumentNullException("log");
            }


            this.Log = log;
            this.MaxTries = 5;
            this.datagramSender = datagramSender;
            this.metrics = metrics;
        }


        public bool Expired { get; private set; }

        private ILog Log { get; set; }

        private bool Acknowledged { get; set; }

        private int MaxTries { get; set; }


        /// <summary>
        ///     Sends keep alive datagrams every <see cref="period" />
        ///     and / or returns true when any of them is acknowledged
        ///     by the server.
        /// </summary>
        /// <returns>True if a keep alive datagram was acknowledged by the server; otherwise, false.</returns>
        public bool SendAndCheckForAck()
        {
            if (this.Acknowledged)
            {
                return true;
            }

            Thread.SpinWait(10);

            // check if we received an ack for any of the sent ones
            int acks = this.sentHandlers.Count(handler => handler.ResponseDatagram != null);
            //// Debug.WriteLine("{1:mm:ss:fffff} acks = {0}", acks, DateTime.Now);
            if (acks > 0)
            {
                this.metrics.KeepAliveDatagramsAcknowledgedByServer += acks;
                this.Acknowledged = true;
                return true;
            }

            // if we haven't sent one
            // or last one sent more than (period) ago
            if (DateTime.Now - this.lastSendTime > this.period)
            {
                if (this.sentCount == this.MaxTries)
                {
                    // we already sent (maxTries) and we're past waiting for the last sent one
                    this.Expired = true;
                    return false;
                }

                this.SendKeepAliveDatagram();
            }

            return false;
        }


        private void SendKeepAliveDatagram()
        {
            if (this.sequenceNumber == -1)
            {
                this.sequenceNumber = this.datagramSender.GetNextCommandSequenceNumber();
            }

            Debug.WriteLine("keep alive datagram {0} sent", this.sentCount + 1);
            var keepAliveDgram = new CommandDatagram((byte)this.sequenceNumber, string.Empty);
            var responseHandler = this.datagramSender.SendDatagram(keepAliveDgram);
            this.sentHandlers.Add(responseHandler);
            this.lastSendTime = DateTime.Now;
            this.sentCount++;
            this.metrics.KeepAliveDatagramsSent++;
            this.Log.TraceFormat("C#{0:000} Sent keep alive command.", keepAliveDgram.SequenceNumber);
        }
    }
}
