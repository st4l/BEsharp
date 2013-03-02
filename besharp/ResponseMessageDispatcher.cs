// ----------------------------------------------------------------------------------------------------
// <copyright file="ResponseMessageDispatcher.cs" company="Me">Copyright (c) 2013 St4l.</copyright>
// ----------------------------------------------------------------------------------------------------
namespace BESharp
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using Datagrams;

    internal sealed partial class MessageDispatcher
    {
        private class ResponseMessageDispatcher : IDisposable
        {
            private readonly Dictionary<byte, ResponseHandler> cmdResponseHandlers =
                    new Dictionary<byte, ResponseHandler>();

            private MessageDispatcher dispatcher;

            private ResponseHandler loginHandler;

            private bool disposed;


            public ResponseMessageDispatcher(MessageDispatcher dispatcher)
            {
                this.dispatcher = dispatcher;
            }


            /// <summary>
            ///   Use C# destructor syntax for finalization code.
            /// </summary>
            /// <remarks>
            ///   This destructor will run only if the Dispose method 
            ///   does not get called. 
            ///   Do not provide destructors in types derived from this class.
            /// </remarks>
            ~ResponseMessageDispatcher()
            {
                this.Dispose(false);
            }


            public ResponseHandler CreateOrGetHandler(IOutboundDatagram dgram)
            {
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


            public void DispatchResponse(IInboundDatagram dgram)
            {
                if (dgram.Type == DatagramType.Login)
                {
                    if (this.loginHandler != null)
                    {
                        this.dispatcher.RegisterAcknowledgedPacket(this.loginHandler.SentDatagram);
                        this.loginHandler.AcceptResponse(dgram);
                    }
                    return;
                }

                // it's a command response.
                // is it a single-packet response?
                var cmdDgram = dgram as CommandSinglePacketResponseDatagram;
                if (cmdDgram != null)
                {
                    // yes
                    lock (this.cmdResponseHandlers)
                    {
                        ResponseHandler handler = this.cmdResponseHandlers[cmdDgram.OriginalSequenceNumber];
                        this.dispatcher.RegisterAcknowledgedPacket(handler.SentDatagram);
                        this.cmdResponseHandlers.Remove(cmdDgram.OriginalSequenceNumber);
                        handler.AcceptResponse(cmdDgram);
                        Debug.WriteLine("handler for command packet {0} invoked", cmdDgram.OriginalSequenceNumber);
                    }

                    return;
                }


                // is it a part of a multi-packet response?
                var partDgram = dgram as CommandResponsePartDatagram;
                if (partDgram != null)
                {
                    // yes
                    CommandMultiPacketResponseDatagram masterCmd;
                    ResponseHandler handler = this.cmdResponseHandlers[partDgram.OriginalSequenceNumber];
                    this.dispatcher.RegisterAcknowledgedPacket(handler.SentDatagram);

                    // is this the first part we ever received?
                    if (handler.ResponseDatagram == null)
                    {
                        // create the master object that will hold and process the parts
                        masterCmd = new CommandMultiPacketResponseDatagram(partDgram);
                        handler.ResponseDatagram = masterCmd;
                    }
                    else
                    {
                        // get the previously created master and add this part to it
                        masterCmd = (CommandMultiPacketResponseDatagram)handler.ResponseDatagram;
                        masterCmd.AddPart(partDgram);
                    }

                    // was this the last part?
                    if (masterCmd.IsComplete)
                    {
                        lock (this.cmdResponseHandlers)
                        {
                            this.cmdResponseHandlers.Remove(masterCmd.OriginalSequenceNumber);
                            handler.AcceptResponse(masterCmd);
                            Debug.WriteLine(
                                            "handler for complete multi-part command packet {0} invoked",
                                            masterCmd.OriginalSequenceNumber);
                        }
                    }
                }
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
                        this.dispatcher = null;

                        if (this.loginHandler != null)
                        {
                            this.loginHandler.Dispose();
                        }

                        if (this.cmdResponseHandlers != null)
                        {
                            lock (this.cmdResponseHandlers)
                            {
                                foreach (KeyValuePair<byte, ResponseHandler> pair in this.cmdResponseHandlers)
                                {
                                    var handler = pair.Value;
                                    handler.Dispose();
                                }

                                this.cmdResponseHandlers.Clear();
                            }
                        }
                    }

                    // Note disposing has been done.
                    this.disposed = true;
                }
            }
        }
    }
}
