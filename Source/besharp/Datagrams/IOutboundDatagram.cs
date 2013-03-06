// ----------------------------------------------------------------------------------------------------
// <copyright file="IOutboundDatagram.cs" company="Me">Copyright (c) 2013 St4l.</copyright>
// ----------------------------------------------------------------------------------------------------
namespace BESharp.Datagrams
{
    using System;

    internal interface IOutboundDatagram : IDatagram
    {
        /// <summary>
        ///   The date and time this message was sent. Set automatically by 
        ///   <see cref="RConClient" />.
        /// </summary>
        DateTime SentTime { get; set; }

        bool ExpectsResponse { get; }

        byte[] Build();
    }
}
