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


    }
}
