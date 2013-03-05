// ----------------------------------------------------------------------------------------------------
// <copyright file="DatagramSender.cs" company="Me">Copyright (c) 2013 St4l.</copyright>
// ----------------------------------------------------------------------------------------------------
namespace BESharp
{
    using System;
    using System.Diagnostics;
    using Datagrams;
    using log4net;

    internal class DatagramSender
    {
        private RConMetrics Metrics { get; set; }

        private readonly ResponseDispatcher responseDispatcher;

        internal DateTime LastCommandSentTime { get; private set; }

        private ILog Log { get; set; }

        private readonly IUdpClient udpClient;

        private int lastCommandSequenceNumber;


        internal DatagramSender(DatagramDispatcher dispatcher)
        {
            if (dispatcher == null)
            {
                throw new ArgumentNullException("dispatcher");
            }

            this.lastCommandSequenceNumber = -1;
            this.LastCommandSentTime = DateTime.Now.Subtract(dispatcher.KeepAlivePeriod).AddSeconds(5);
            this.Metrics = dispatcher.Metrics;
            this.responseDispatcher = dispatcher.ResponseDispatcher;
            this.udpClient = dispatcher.UdpClient;
            this.Log = LogManager.GetLogger(this.GetType());
        }


        public ResponseHandler SendDatagram(IOutboundDatagram dgram)
        {
            // this.outboundQueue.Enqueue(dgram);
            byte[] bytes = dgram.Build();

            ResponseHandler handler = null;
            if (dgram.ExpectsResponse)
            {
                handler = this.responseDispatcher.CreateOrGetHandler(dgram);
            }

            this.Log.Trace("BEFORE SendDatagram");


            // socket is thread safe
            // i.e. it is ok to send & receive at the same time from different threads
            // also, there's no UDP ack, so there's no need to send asynchronously
            int transferredBytes = this.udpClient.Send(bytes, bytes.Length);

            this.Log.Trace("AFTER  SendDatagram");

            dgram.SentTime = DateTime.Now;
            this.Metrics.OutboundDatagramCount++;

            if (dgram.Type == DatagramType.Command)
            {
                this.lastCommandSequenceNumber = this.lastCommandSequenceNumber + 1;
            }

            Debug.Assert(
                         transferredBytes == bytes.Length,
                         "Sent bytes count equal count of bytes meant to be sent.");

            if (dgram.Type == DatagramType.Command)
            {
                this.LastCommandSentTime = DateTime.Now;
            }

            return handler;
        }


        public byte GetNextCommandSequenceNumber()
        {
            var next = this.lastCommandSequenceNumber + 1;
            if (next > 255)
            {
                next = 0;
            }

            return (byte)next;
        }


        public ResponseHandler SendCommand(string commandText)
        {
            var dgram = new CommandDatagram(this.GetNextCommandSequenceNumber(), commandText);
            return this.SendDatagram(dgram);
        }
    }
}