// ----------------------------------------------------------------------------------------------------
// <copyright file="CommandSinglePacketResponseDatagram.cs" company="Me">Copyright (c) 2012 St4l.</copyright>
// ----------------------------------------------------------------------------------------------------

using System;
using System.Text;

namespace BESharp.Datagrams
{
    public class CommandSinglePacketResponseDatagram : CommandResponseDatagram 
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
