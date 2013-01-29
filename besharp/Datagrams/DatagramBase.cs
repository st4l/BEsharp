// ----------------------------------------------------------------------------------------------------
// <copyright file="DatagramBase.cs" company="Me">Copyright (c) 2012 St4l.</copyright>
// ----------------------------------------------------------------------------------------------------
namespace BESharp.Datagrams
{
    public abstract class DatagramBase : IDatagram
    {
        public abstract DatagramType Type { get; }
    }
}
