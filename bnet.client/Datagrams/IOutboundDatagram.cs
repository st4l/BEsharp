﻿// ----------------------------------------------------------------------------------------------------
// <copyright file="IOutboundDatagram.cs" company="Me">Copyright (c) 2012 St4l.</copyright>
// ----------------------------------------------------------------------------------------------------
namespace BNet.Client.Datagrams
{
    using System;

    public interface IOutboundDatagram : IDatagram
    {
        /// <summary>
        /// The date and time this message was sent. Set automatically by 
        /// <see cref="RConClient"/>.
        /// </summary>
        DateTime SentTime { get; set; }

        bool ExpectsResponse { get; }

        byte[] Build();
    }
}
