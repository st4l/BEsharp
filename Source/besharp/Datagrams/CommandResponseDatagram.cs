// ----------------------------------------------------------------------------------------------------
// <copyright file="CommandResponseDatagram.cs" company="Me">Copyright (c) 2013 St4l.</copyright>
// ----------------------------------------------------------------------------------------------------
namespace BESharp.Datagrams
{
    using System;

    
    internal class CommandResponseDatagram : InboundDatagramBase
    {
        internal CommandResponseDatagram()
        {
        }


        internal CommandResponseDatagram(byte[] buffer)
        {
            if (buffer == null)
            {
                throw new ArgumentNullException("buffer");
            }

            this.OriginalSequenceNumber = buffer[Constants.CommandResponseSequenceNumberIndex];
        }


        public override DatagramType Type
        {
            get { return DatagramType.Command; }
        }

        public string Body { get; protected set; }

        public byte OriginalSequenceNumber { get; protected set; }
    }
}
