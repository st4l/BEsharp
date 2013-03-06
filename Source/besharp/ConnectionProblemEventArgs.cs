// ----------------------------------------------------------------------------------------------------
// <copyright file="ConnectionProblemEventArgs.cs" company="Me">Copyright (c) 2013 St4l.</copyright>
// ----------------------------------------------------------------------------------------------------
namespace BESharp
{
    using System;

    public class ConnectionProblemEventArgs : EventArgs
    {
        public ConnectionProblemEventArgs(ConnectionProblemType connectionProblemType)
        {
            this.ConnectionProblemType = connectionProblemType;
        }


        public ConnectionProblemType ConnectionProblemType { get; private set; }
    }
}
