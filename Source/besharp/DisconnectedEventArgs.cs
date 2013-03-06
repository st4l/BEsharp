// ----------------------------------------------------------------------------------------------------
// <copyright file="DisconnectedEventArgs.cs" company="Me">Copyright (c) 2013 St4l.</copyright>
// ----------------------------------------------------------------------------------------------------
namespace BESharp
{
    using System;

    public class DisconnectedEventArgs : EventArgs
    {
        public DisconnectedEventArgs(ShutdownReason shutdownReason)
        {
            ShutdownReason = shutdownReason;
        }


        public ShutdownReason ShutdownReason { get; private set; }
    }
}
