// ----------------------------------------------------------------------------------------------------
// <copyright file="NetUdpClient.cs" company="Me">Copyright (c) 2013 St4l.</copyright>
// ----------------------------------------------------------------------------------------------------

namespace BESharp
{
    using System.Net.Sockets;


    /// <summary>
    /// Wraps UdpClient for test time replacement.
    /// </summary>
    internal class NetUdpClient : UdpClient, IUdpClient
    {
        public NetUdpClient(int localPort) : base(localPort)
        {
        }


        public NetUdpClient()
        {
        }


        internal NetUdpClient(string hostname, int port) : base(hostname, port)
        {
        }
    }
}
