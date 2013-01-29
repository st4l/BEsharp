// ----------------------------------------------------------------------------------------------------
// <copyright file="OutboundDatagramQueue.cs" company="Me">Copyright (c) 2012 St4l.</copyright>
// ----------------------------------------------------------------------------------------------------

using BESharp.Datagrams;

namespace BESharp
{
    using System.Collections.Concurrent;
    using BESharp.Datagrams;


    internal class OutboundDatagramQueue : ConcurrentQueue<IDatagram>
    {
    }
}
