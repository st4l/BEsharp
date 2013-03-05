// ----------------------------------------------------------------------------------------------------
// <copyright file="InboundProcessor.cs" company="Me">Copyright (c) 2013 St4l.</copyright>
// ----------------------------------------------------------------------------------------------------
using System;
using System.Diagnostics;
using System.Linq;
using BESharp.Datagrams;
using log4net;
namespace BESharp
{
    internal sealed class InboundProcessor
    {
        private readonly DatagramDispatcher dispatcher;

        private readonly byte[] buffer;

        private readonly DatagramType type;


        internal InboundProcessor(DatagramDispatcher dispatcher, byte[] buffer, ILog log)
        {
            if (dispatcher == null)
            {
                throw new ArgumentNullException("dispatcher");
            }
            if (buffer == null)
            {
                throw new ArgumentNullException("buffer");
            }
            if (log == null)
            {
                throw new ArgumentNullException("log");
            }

            this.dispatcher = dispatcher;
            this.buffer = buffer;
            this.Log = log;

            this.type = (DatagramType)Buffer.GetByte(this.buffer, Constants.DatagramTypeIndex);
        }


        private ILog Log { get; set; }

        public bool IsShutDownDatagram
        {
            get
            {
                // shutdown message from server (not in protocol, used only for testing)
                return this.type == DatagramType.TestServerShutdown;
            }
        }


        public bool IsValidLength
        {
            get
            {
                return this.IsShutDownDatagram 
                    || this.buffer.Length >= Constants.DatagramMinLength;
            }
        }


        /// <summary>
        ///   Tries to pre-process the datagram.
        /// </summary>
        /// <returns> True if the datagram was processed and does not need further processing; otherwise false. </returns>
        public bool TryPreProcess()
        {
            this.Log.TraceFormat("{0:0}    Type dgram received.", this.type);

            if (this.type == DatagramType.ConsoleMessage)
            {
                return this.PreProcessConsoleMessage();
            }

            if (this.type == DatagramType.Command)
            {
                return this.PreProcessCommandResponse();
            }

            return false;
        }


        public bool VerifyCrc()
        {
            int payloadLength = Buffer.ByteLength(this.buffer) - 6;
            var payload = new byte[payloadLength];
            Buffer.BlockCopy(this.buffer, 6, payload, 0, payloadLength);
            byte[] computedChecksum;
            using (var crc = new Crc32(Crc32.DefaultPolynomialReversed, Crc32.DefaultSeed))
            {
                computedChecksum = crc.ComputeHash(payload);
                Array.Reverse(computedChecksum);
            }

            var originalChecksum = new byte[4];
            Buffer.BlockCopy(this.buffer, 2, originalChecksum, 0, 4);

            return computedChecksum.SequenceEqual(originalChecksum);
        }


        /// <summary>
        ///   Parses received bytes from a BattlEye RCon server.
        /// </summary>
        /// <returns> An <see cref="IInboundDatagram" /> containing the received information. </returns>
        /// <remarks>
        ///   The RCon protocol specification for incoming datagrams:
        /// 
        ///   #### MAIN HEADER PRESENT IN ALL DATAGRAMS
        /// 
        ///   |Index       |      0    |      1    |     2     ,     3     ,     4     ,     5     |  6   |
        ///   |:---------- | :-------: | :-------: | :-------------------------------------------: | :--: |
        ///   |Description | 'B'(0x42) | 'E'(0x45) | 4-byte CRC32 checksum of the subsequent bytes | 0xFF |
        /// 
        ///   #### LOGIN RESPONSE
        ///   |Index       |   7    |                          8                                |
        ///   |:---------- | :----: | :-------------------------------------------------------: |
        ///   |Description |  0x00  |     (0x01 (successfully logged in) OR 0x00 (failed))      |
        /// 
        ///   #### COMMAND RESPONSE MESSAGE
        ///   |Index       |   7    |                8                    |      9  . . .       |
        ///   |:---------- | :----: | :---------------------------------: | :-----------------: |
        ///   |Description |  0x01  |   received 1-byte sequence number   | NOTHING, OR response (ASCII string without null-terminator), OR multi-part header (see below) + response |
        /// 
        /// 
        ///   #### COMMAND RESPONSE MULTI-PART HEADER
        ///   |Index       |    9   |               10                   |                11                  |              12 . . .                        |
        ///   |:---------- | :----: | :--------------------------------: | :--------------------------------: | :--------------------------------: | 
        ///   |Description |  0x00  | number of dgrams for this response | 0-based index of the current dgram | response (ASCII string without null-terminator) |
        /// 
        /// 
        ///   #### CONSOLE MESSAGE
        ///   |Index       |   7    |                8                       |      9  . . .       |
        ///   |:---------- | :----: | :------------------------------------: | :-----------------: |
        ///   |Description |  0x02  | 1-byte sequence number (starting at 0) | server message (ASCII string without null-terminator) |
        /// </remarks>
        public IInboundDatagram Parse()
        {
            switch (this.type)
            {
                case DatagramType.Login:
                    return new LoginResponseDatagram(this.buffer);
                case DatagramType.Command:
                    if (Buffer.ByteLength(this.buffer) > Constants.CommandResponseMultipartMarkerIndex
                        && Buffer.GetByte(this.buffer, Constants.CommandResponseMultipartMarkerIndex) == 0x00)
                    {
                        return new CommandResponsePartDatagram(this.buffer);
                    }

                    return new CommandSinglePartResponseDatagram(this.buffer);
                case DatagramType.ConsoleMessage:
                    return new ConsoleMessageDatagram(this.buffer);
                default:
                    throw new InvalidOperationException("Invalid datagram type");
            }
        }


        private bool PreProcessConsoleMessage()
        {
            byte conMsgSeq = Buffer.GetByte(this.buffer, Constants.ConsoleMessageSequenceNumberIndex);
            this.Log.TraceFormat("M#{0:000} Received", conMsgSeq);

            this.AcknowledgeMessage(conMsgSeq);
            if (this.dispatcher.DiscardConsoleMessages)
            {
                return true;
            }

            // if we already received a console message with this seq number
            bool repeated = this.dispatcher.ConMsgsTracker.Contains(conMsgSeq);
            if (repeated)
            {
                // if we did, just acknowledge it and don't process it
                // (the server probably didn't receive our previous ack)
                return true;
            }

            // register the sequence number and continue processing the message
            this.dispatcher.ConMsgsTracker.StartTracking(conMsgSeq);
            return false;
        }


        private bool PreProcessCommandResponse()
        {
            byte cmdSeq = Buffer.GetByte(this.buffer, Constants.CommandResponseSequenceNumberIndex);
            bool repeated = this.dispatcher.CmdsTracker.Contains(cmdSeq);
            if (repeated)
            {
                // doesn't repeat because multipart?
                if (Buffer.GetByte(this.buffer, Constants.CommandResponseMultipartMarkerIndex) != 0x00)
                {
                    return true;
                } // else go ahead and dispatch the part
            }
            else
            {
                this.dispatcher.CmdsTracker.StartTracking(cmdSeq);
            }

            return false;
        }


        /// <summary>
        ///   Sends a datagram back to the server acknowledging receipt of
        ///   a console message datagram.
        /// </summary>
        /// <param name="seqNumber"> The sequence number of the received <see cref="ConsoleMessageDatagram" /> . </param>
        private void AcknowledgeMessage(byte seqNumber)
        {
            this.dispatcher.SendDatagram(new AcknowledgeMessageDatagram(seqNumber));
            this.Log.TraceFormat("M#{0:000} Acknowledged", seqNumber);
        }
    }
}
