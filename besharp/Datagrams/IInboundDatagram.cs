﻿// ----------------------------------------------------------------------------------------------------
// <copyright file="IInboundDatagram.cs" company="Me">Copyright (c) 2013 St4l.</copyright>
// ----------------------------------------------------------------------------------------------------
namespace BESharp.Datagrams
{
    using System;


    public interface IInboundDatagram : IDatagram
    {
        DateTime Timestamp { get; }
    }
}
