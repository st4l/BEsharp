// ----------------------------------------------------------------------------------------------------
// <copyright file="PacketProblemEventArgs.cs" company="Me">Copyright (c) 2013 St4l.</copyright>
// ----------------------------------------------------------------------------------------------------
namespace BESharp
{
    using System;

    public class PacketProblemEventArgs : EventArgs
    {
        public PacketProblemEventArgs(PacketProblemType packetProblemType)
        {
            this.PacketProblemType = packetProblemType;
        }


        public PacketProblemType PacketProblemType { get; private set; }
    }
}
