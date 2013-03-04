// ----------------------------------------------------------------------------------------------------
// <copyright file="ConsoleMessageDatagram.cs" company="Me">Copyright (c) 2013 St4l.</copyright>
// ----------------------------------------------------------------------------------------------------
namespace BESharp.Datagrams
{
    using System;
    using System.Text;

    internal class ConsoleMessageDatagram : InboundDatagramBase
    {
        internal ConsoleMessageDatagram(byte[] buffer)
        {
            this.Parse(buffer);
        }


        public override DatagramType Type
        {
            get { return DatagramType.ConsoleMessage; }
        }


        public byte SequenceNumber { get; private set; }


        public string MessageBody { get; private set; }


        private void Parse(byte[] buffer)
        {
            this.SequenceNumber = Buffer.GetByte(buffer, Constants.ConsoleMessageSequenceNumberIndex);

            this.MessageBody = Encoding.ASCII.GetString(
                                                        buffer,
                                                        Constants.ConsoleMessageBodyStartIndex,
                                                        buffer.Length - Constants.ConsoleMessageBodyStartIndex);
        }
    }
}
