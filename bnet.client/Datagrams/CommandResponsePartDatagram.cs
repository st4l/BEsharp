using System;

namespace BESharp.Datagrams
{
    public class CommandResponsePartDatagram : CommandResponseDatagram
    {
        public CommandResponsePartDatagram(byte[] buffer) : base(buffer)
        {
            this.PartNumber = Buffer.GetByte(buffer, Constants.CommandResponseMultipartPartNumberIndex);
            this.TotalParts = Buffer.GetByte(buffer, Constants.CommandResponseMultipartTotalPartsIndex);
            var len = Buffer.ByteLength(buffer);
            this.BodyLength = len - 12;
            this.BodyBytes = new byte[this.BodyLength];
            Buffer.BlockCopy(buffer, 12, this.BodyBytes, 0, this.BodyLength);
        }


        public byte[] BodyBytes { get; private set; }

        public int BodyLength { get; private set; }


        public byte PartNumber { get; private set; }


        public byte TotalParts { get; private set; }
    }
}