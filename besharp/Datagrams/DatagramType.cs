// ----------------------------------------------------------------------------------------------------
// <copyright file="DatagramType.cs" company="Me">Copyright (c) 2013 St4l.</copyright>
// ----------------------------------------------------------------------------------------------------
namespace BESharp.Datagrams
{
    public enum DatagramType
    {
        /// <summary>
        ///   The message is either a login request from the client or a login response from the server.
        /// </summary>
        Login = 0,

        /// <summary>
        ///   The message is either a sent command or a received command response.
        /// </summary>
        Command = 1,

        /// <summary>
        ///   The message is either a received remote console message or the receipt acknowledgment the client 
        ///   sends to the server.
        /// </summary>
        Message = 2,

        /// <summary>
        ///   The message is a shutdown request from the testing server (see Tests.MockServer).
        /// </summary>
        TestServerShutdown = 0xFF
    }
}
