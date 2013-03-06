// ----------------------------------------------------------------------------------------------------
// <copyright file="InboundProcessor.cs" company="Me">Copyright (c) 2013 St4l.</copyright>
// ----------------------------------------------------------------------------------------------------
using System;
using System.Linq;
using BESharp.Datagrams;
using log4net;
namespace BESharp
{
    /// <summary>
    ///   Encapsulates logic to pre-process and parse received bytes representing 
    ///   a datagram received from the server.
    /// </summary>
    internal sealed class InboundProcessor
    {
        private readonly DatagramSender dgramSender;

        private readonly byte[] buffer;

        private readonly DatagramType type;

        private readonly ILog log;


        /// <summary>
        ///   Creates a new instance of this class.
        /// </summary>
        /// <param name="buffer"> The received bytes. </param>
        /// <param name="dgramSender"> The <see cref="DatagramSender"/> to use to send datagrams to the server. </param>
        /// <param name="log"> The <see cref="ILog"/> instance to be used to log tracing messages. </param>
        internal InboundProcessor(byte[] buffer, DatagramSender dgramSender, ILog log)
        {
            if (dgramSender == null)
            {
                throw new ArgumentNullException("dgramSender");
            }
            if (buffer == null)
            {
                throw new ArgumentNullException("buffer");
            }
            if (log == null)
            {
                throw new ArgumentNullException("log");
            }

            this.dgramSender = dgramSender;
            this.buffer = buffer;
            this.log = log;

            if (this.buffer.Length > Constants.DatagramTypeIndex)
            {
                this.type = (DatagramType)Buffer.GetByte(this.buffer, Constants.DatagramTypeIndex);
            }
        }


        public bool IsShutDownDatagram
        {
            get
            {
                // shutdown message from server (not in protocol, used only for testing)
                return this.type == DatagramType.TestServerShutdown;
            }
        }


        public bool ValidateLength()
        {
            return this.IsShutDownDatagram
                   || this.buffer.Length >= Constants.DatagramMinLength;
        }


        /// <summary>
        ///   Tries to pre-process the datagram.
        /// </summary>
        /// <returns> True if the datagram was processed and does not need further processing; otherwise false. </returns>
        public bool TryPreProcess(bool discardConsoleMessages, SequenceTracker consoleMessagesTracker, SequenceTracker commandsTracker)
        {
            this.log.TraceFormat("{0:0}    Type dgram received.", this.type);

            if (this.type == DatagramType.ConsoleMessage)
            {
                return this.PreProcessConsoleMessage(discardConsoleMessages, consoleMessagesTracker);
            }

            if (this.type == DatagramType.Command)
            {
                return this.PreProcessCommandResponse(commandsTracker);
            }

            return false;
        }


        /// <summary>
        ///   Verifies that the CRC32 checksum of the datagram being processed is valid.
        /// </summary>
        /// <returns> True if the CRC32 checsum is valid; false otherwise. </returns>
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
        ///   Parses received bytes from a BattlEye RCon server and returns an
        ///   <see cref="IInboundDatagram"/> object representing the received datagram.
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


        /// <summary>
        ///   Acknowledges, checks for repeated sequence numbers, and signals to
        ///   skip further processing when <paramref name="discardConsoleMessages"/> is true.
        /// </summary>
        /// <param name="discardConsoleMessages"> Whether to signal to skip further processing. </param>
        /// <param name="consoleMessagesTracker"> The <see cref="SequenceTracker"/> used to keep track of sequence numbers. </param>
        /// <returns> True if no further processing should be done; false otherwise. </returns>
        private bool PreProcessConsoleMessage(bool discardConsoleMessages, SequenceTracker consoleMessagesTracker)
        {
            byte conMsgSeq = Buffer.GetByte(this.buffer, Constants.ConsoleMessageSequenceNumberIndex);
            this.log.TraceFormat("M#{0:000} Received", conMsgSeq);

            this.dgramSender.AcknowledgeMessage(conMsgSeq);
            if (discardConsoleMessages)
            {
                return true;
            }

            // if we already received a console message with this seq number
            bool repeated = consoleMessagesTracker.Contains(conMsgSeq);
            if (repeated)
            {
                // if we did, just acknowledge it and don't process it
                // (the server probably didn't receive our previous ack)
                return true;
            }

            // register the sequence number and continue processing the message
            consoleMessagesTracker.StartTracking(conMsgSeq);
            return false;
        }


        /// <summary>
        ///   Checks for repeated command responses.
        /// </summary>
        /// <param name="commandsTracker"> The <see cref="SequenceTracker"/> used to keep track of sequence numbers. </param>
        /// <returns> True if no further processing should be done; false otherwise. </returns>
        private bool PreProcessCommandResponse(SequenceTracker commandsTracker)
        {
            byte cmdSeq = Buffer.GetByte(this.buffer, Constants.CommandResponseSequenceNumberIndex);
            bool repeated = commandsTracker.Contains(cmdSeq);
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
                commandsTracker.StartTracking(cmdSeq);
            }

            return false;
        }
    }
}
