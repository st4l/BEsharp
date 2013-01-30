// ----------------------------------------------------------------------------------------------------
// <copyright file="ResponseMessageDispatcher.cs" company="Me">Copyright (c) 2012 St4l.</copyright>
// ----------------------------------------------------------------------------------------------------

using System.Collections.Generic;
using System.Diagnostics;
using BESharp.Datagrams;

namespace BESharp
{
    partial class MessageDispatcher
    {
        private class ResponseMessageDispatcher
        {
            public ResponseMessageDispatcher(MessageDispatcher dispatcher)
            {
                this.dispatcher = dispatcher;
            }


            private readonly MessageDispatcher dispatcher;
            private ResponseHandler loginHandler;

            private readonly Dictionary<byte, ResponseHandler> cmdResponseHandlers =
                new Dictionary<byte, ResponseHandler>();


            public ResponseHandler CreateOrGetHandler(IOutboundDatagram dgram)
            {
                ResponseHandler result = null;
                if (dgram.Type == DatagramType.Login)
                {
                    if (this.loginHandler != null)
                    {
                        return this.loginHandler;
                    }
                    this.loginHandler = new ResponseHandler(dgram);
                    return this.loginHandler;
                }

                // it's a command.
                var cmdDgram = (CommandDatagram)dgram;
                lock (this.cmdResponseHandlers)
                {
                    if (this.cmdResponseHandlers.ContainsKey(cmdDgram.SequenceNumber))
                    {
                        return this.cmdResponseHandlers[cmdDgram.SequenceNumber];
                    }
                    var newHandler = new ResponseHandler(dgram);
                    this.cmdResponseHandlers.Add(cmdDgram.SequenceNumber, newHandler);
                    return newHandler;
                }
            }


            public void Dispatch(IInboundDatagram dgram)
            {
                
                if (dgram.Type == DatagramType.Login
                    && this.loginHandler != null)
                {
                    this.dispatcher.UpdateLastAckSentTime(this.loginHandler.SentDatagram.SentTime);
                    this.loginHandler.Complete(dgram);
                    return;
                }

                // it's a command response.
                if (dgram is CommandSinglePacketResponseDatagram)
                {
                    var cmdDgram = (CommandSinglePacketResponseDatagram)dgram;
                    lock (this.cmdResponseHandlers)
                    {
                        var handler = this.cmdResponseHandlers[cmdDgram.OriginalSequenceNumber];
                        this.dispatcher.UpdateLastAckSentTime(handler.SentDatagram.SentTime);
                        this.cmdResponseHandlers.Remove(cmdDgram.OriginalSequenceNumber);
                        handler.Complete(cmdDgram);
                        Debug.WriteLine("handler for command packet {0} invoked", cmdDgram.OriginalSequenceNumber);
                    }
                }
                else if (dgram is CommandResponsePartDatagram)
                {
                    var partDgram = (CommandResponsePartDatagram)dgram;
                    CommandMultiPacketResponseDatagram masterCmd = null;
                    var handler = this.cmdResponseHandlers[partDgram.OriginalSequenceNumber];
                    this.dispatcher.UpdateLastAckSentTime(handler.SentDatagram.SentTime);
                    if (handler.ResponseDatagram == null)
                    {
                        masterCmd = new CommandMultiPacketResponseDatagram(partDgram);
                        handler.ResponseDatagram = masterCmd;
                    }
                    else
                    {
                        masterCmd = (CommandMultiPacketResponseDatagram)handler.ResponseDatagram;
                        masterCmd.AddPart(partDgram);
                    }

                    if (masterCmd.Complete)
                    {
                        lock (this.cmdResponseHandlers)
                        {
                            this.cmdResponseHandlers.Remove(masterCmd.OriginalSequenceNumber);
                            handler.Complete(masterCmd);
                            Debug.WriteLine("handler for multi-part command packet {0} invoked",
                                            masterCmd.OriginalSequenceNumber);
                        }
                    }
                }
            }
        }
    }
}
