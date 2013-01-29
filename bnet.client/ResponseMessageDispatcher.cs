// ----------------------------------------------------------------------------------------------------
// <copyright file="ResponseMessageDispatcher.cs" company="Me">Copyright (c) 2012 St4l.</copyright>
// ----------------------------------------------------------------------------------------------------

using System.Collections.Generic;
using System.Diagnostics;
using BESharp.Datagrams;

namespace BESharp
{
    internal class ResponseMessageDispatcher
    {
        private ResponseHandler loginHandler;
        private readonly Dictionary<byte, ResponseHandler> cmdResponseHandlers =
            new Dictionary<byte, ResponseHandler>();

        public void Register(ResponseHandler handler)
        {
            if (handler.SentDatagram.Type == DatagramType.Login)
            {
                this.loginHandler = handler;
                return;
            }

            // it's a command.
            var cmdDgram = (CommandDatagram)handler.SentDatagram;
            lock (this.cmdResponseHandlers)
            {
                this.cmdResponseHandlers.Add(cmdDgram.SequenceNumber, handler);
            }
        }


        public void Dispatch(IInboundDatagram dgram)
        {
            if (dgram.Type == DatagramType.Login
                && this.loginHandler != null)
            {
                this.loginHandler.Return(dgram);
                return;
            }

            // it's a command response.
            if (dgram is CommandSinglePacketResponseDatagram)
            {
                var cmdDgram = (CommandSinglePacketResponseDatagram)dgram;
                lock (this.cmdResponseHandlers)
                {
                    var handler = this.cmdResponseHandlers[cmdDgram.OriginalSequenceNumber];
                    this.cmdResponseHandlers.Remove(cmdDgram.OriginalSequenceNumber);
                    handler.Return(cmdDgram);
                    Debug.WriteLine("handler for command packet {0} invoked", cmdDgram.OriginalSequenceNumber);
                }
            }
            else if (dgram is CommandResponsePartDatagram)
            {
                var partDgram = (CommandResponsePartDatagram)dgram;
                CommandMultiPacketResponseDatagram masterCmd = null;
                var handler = this.cmdResponseHandlers[partDgram.OriginalSequenceNumber];
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
                        handler.Return(masterCmd);
                        Debug.WriteLine("handler for multi-part command packet {0} invoked",
                                        masterCmd.OriginalSequenceNumber);
                    }
                }
            }
        }
    }
}
