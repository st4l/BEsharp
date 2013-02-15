// ----------------------------------------------------------------------------------------------------
// <copyright file="CommandSinglePacketResponseDatagram.cs" company="Me">Copyright (c) 2013 St4l.</copyright>
// ----------------------------------------------------------------------------------------------------
namespace BESharp.Datagrams
{
    using System;
    using System.Text;

    internal class CommandSinglePacketResponseDatagram : CommandResponseDatagram
    {
        internal CommandSinglePacketResponseDatagram(byte[] buffer) : base(buffer)
        {
            var len = Buffer.ByteLength(buffer);
            var body = new byte[len - 9];
            Buffer.BlockCopy(buffer, 9, body, 0, len - 9);
            this.Body = Encoding.ASCII.GetString(body);
        }
    }
}
