// ----------------------------------------------------------------------------------------------------
// <copyright file="DatagramType.cs" company="Me">Copyright (c) 2012 St4l.</copyright>
// ----------------------------------------------------------------------------------------------------
namespace BESharp.Datagrams
{
    public enum DatagramType : int
    {
        Login = 0, 

        Command = 1, 

        Message = 2,

        TestServerShutdown = 0xFF
    }
}
