// ----------------------------------------------------------------------------------------------------
// <copyright file="KeepAliveTracker.cs" company="Me">Copyright (c) 2013 St4l.</copyright>
// ----------------------------------------------------------------------------------------------------
namespace BESharp
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Linq;
    using System.Threading;
    using Datagrams;


    internal sealed partial class MessageDispatcher
    {
        private class KeepAliveTracker
        {
            private readonly MessageDispatcher msgDispatcher;

            private readonly TimeSpan period = TimeSpan.FromSeconds(1);

            private readonly List<ResponseHandler> sentHandlers = new List<ResponseHandler>();

            private DateTime lastSendTime = DateTime.MinValue;

            private int sentCount;

            private SpinWait spinWait = new SpinWait();

            private int sequenceNumber = -1;


            public KeepAliveTracker(MessageDispatcher msgDispatcher)
            {
                this.MaxTries = 5;
                this.msgDispatcher = msgDispatcher;
            }


            public bool Expired { get; private set; }

            private bool Acknowledged { get; set; }

            private int MaxTries { get; set; }


            public bool Ping()
            {
                if (this.Acknowledged)
                {
                    return true;
                }

                this.spinWait.SpinOnce();

                // check if we received an ack for any of the sent ones
                int acks = this.sentHandlers.Count(handler => handler.ResponseDatagram != null);
                //// Debug.WriteLine("{1:mm:ss:fffff} acks = {0}", acks, DateTime.Now);
                if (acks > 0)
                {
                    this.msgDispatcher.keepAlivePacketsAcks += acks;
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

                    this.SendKeepAlivePacket();
                }

                return false;
            }


            private void SendKeepAlivePacket()
            {
                if (this.sequenceNumber == -1)
                {
                    this.sequenceNumber = this.msgDispatcher.GetNextCommandSequenceNumber();
                }

                Debug.WriteLine("keep alive packet {0} sent", this.sentCount + 1);
                var keepAliveDgram = new CommandDatagram((byte)this.sequenceNumber, string.Empty);
                this.sentHandlers.Add(this.msgDispatcher.SendDatagram(keepAliveDgram));
                this.lastSendTime = DateTime.Now;
                this.sentCount++;
                this.msgDispatcher.keepAlivePacketsSent++;
                this.msgDispatcher.Log.TraceFormat("C#{0:000} Sent keep alive command.", keepAliveDgram.SequenceNumber);
            }
        }
    }
}
