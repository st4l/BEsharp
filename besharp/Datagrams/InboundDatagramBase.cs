// ----------------------------------------------------------------------------------------------------
// <copyright file="InboundDatagramBase.cs" company="Me">Copyright (c) 2013 St4l.</copyright>
// ----------------------------------------------------------------------------------------------------
namespace BESharp.Datagrams
{
    using System;

    internal abstract class InboundDatagramBase : DatagramBase, IInboundDatagram
    {
        /// <summary>
        ///   The inbound datagram base.
        /// </summary>
        protected InboundDatagramBase()
        {
            this.Timestamp = DateTime.Now;
        }


        public DateTime Timestamp { get; private set; }


        /// <summary>
        ///   Parses received bytes from a BattlEye RCon server.
        /// </summary>
        /// <param name="buffer"> The received bytes. </param>
        /// <returns> An <see cref="IInboundDatagram" /> containing the received information. </returns>
        /// <remarks>
        ///   The RCon protocol specification for incoming packets:
        /// 
        ///   #### MAIN HEADER PRESENT IN ALL PACKETS
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
        ///   |Index       |    9   |               10                    |                11                   |              12 . . .                        |
        ///   |:---------- | :----: | :---------------------------------: | :---------------------------------: | :--------------------------------: | 
        ///   |Description |  0x00  | number of packets for this response | 0-based index of the current packet | response (ASCII string without null-terminator) |
        /// 
        /// 
        ///   #### CONSOLE MESSAGE
        ///   |Index       |   7    |                8                       |      9  . . .       |
        ///   |:---------- | :----: | :------------------------------------: | :-----------------: |
        ///   |Description |  0x02  | 1-byte sequence number (starting at 0) | server message (ASCII string without null-terminator) |
        /// </remarks>
        public static IInboundDatagram ParseReceivedBytes(byte[] buffer)
        {
            if (buffer == null)
            {
                throw new ArgumentNullException("buffer");
            }

            var type = (DatagramType)Buffer.GetByte(buffer, Constants.DatagramTypeIndex);
            switch (type)
            {
                case DatagramType.Login:
                    return new LoginResponseDatagram(buffer);
                case DatagramType.Command:
                    if (Buffer.ByteLength(buffer) > Constants.CommandResponseMultipartMarkerIndex
                        && Buffer.GetByte(buffer, Constants.CommandResponseMultipartMarkerIndex) == 0x00)
                    {
                        return new CommandResponsePartDatagram(buffer);
                    }

                    return new CommandSinglePacketResponseDatagram(buffer);
                case DatagramType.Message:
                    return new ConsoleMessageDatagram(buffer);
                default:
                    throw new ArgumentOutOfRangeException("buffer");
            }
        }
    }
}
