// ----------------------------------------------------------------------------------------------------
// <copyright file="CommandResponsePartDatagram.cs" company="Me">Copyright (c) 2013 St4l.</copyright>
// ----------------------------------------------------------------------------------------------------
namespace BESharp.Datagrams
{
    using System;

    public class CommandResponsePartDatagram : CommandResponseDatagram
    {
        private readonly byte[] bodyBytes;


        public CommandResponsePartDatagram(byte[] buffer) : base(buffer)
        {
            this.PartNumber = Buffer.GetByte(buffer, Constants.CommandResponseMultipartPartNumberIndex);
            this.TotalParts = Buffer.GetByte(buffer, Constants.CommandResponseMultipartTotalPartsIndex);
            var len = Buffer.ByteLength(buffer);
            this.BodyLength = len - 12;
            this.bodyBytes = new byte[this.BodyLength];
            Buffer.BlockCopy(buffer, 12, this.bodyBytes, 0, this.BodyLength);
        }


        public int BodyLength { get; private set; }


        public byte PartNumber { get; private set; }


        public byte TotalParts { get; private set; }


        public byte[] GetBytes()
        {
            return this.bodyBytes;
        }
    }
}
