// ----------------------------------------------------------------------------------------------------
// <copyright file="CommandDatagram.cs" company="Me">Copyright (c) 2013 St4l.</copyright>
// ----------------------------------------------------------------------------------------------------
namespace BESharp.Datagrams
{
    using System;
    using System.Text;

    internal class CommandDatagram : OutboundDatagramBase
    {
        internal CommandDatagram(byte sequenceNumber, string commandText)
        {
            this.SequenceNumber = sequenceNumber;
            this.CommandText = commandText;
        }


        public string CommandText { get; private set; }


        public override DatagramType Type
        {
            get { return DatagramType.Command; }
        }


        public override bool ExpectsResponse
        {
            get { return true; }
        }

        public byte SequenceNumber { get; private set; }


        protected override byte[] BuildPayload()
        {
            var cmdBytes = Encoding.ASCII.GetBytes(this.CommandText);
            var len = Buffer.ByteLength(cmdBytes);
            var result = new byte[len + 3];

            Buffer.SetByte(result, 0, 0xFF);
            Buffer.SetByte(result, 1, 0x01);
            Buffer.SetByte(result, 2, this.SequenceNumber);
            Buffer.BlockCopy(cmdBytes, 0, result, 3, len);
            return result;
        }
    }
}
