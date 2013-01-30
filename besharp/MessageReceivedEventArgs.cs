// ----------------------------------------------------------------------------------------------------
// <copyright file="MessageReceivedEventArgs.cs" company="Me">Copyright (c) 2013 St4l.</copyright>
// ----------------------------------------------------------------------------------------------------
namespace BESharp
{
    using System;
    using Datagrams;


    public class MessageReceivedEventArgs : EventArgs
    {
        public MessageReceivedEventArgs(ConsoleMessageDatagram datagram)
        {
            if (datagram == null)
            {
                throw new ArgumentNullException("datagram");
            }

            this.Datagram = datagram;
            this.MessageBody = datagram.MessageBody;
        }


        public string MessageBody { get; set; }

        internal ConsoleMessageDatagram Datagram { get; private set; }
    }
}
