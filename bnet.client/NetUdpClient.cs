using System.Net.Sockets;

namespace BESharp
{
    internal class NetUdpClient : UdpClient, IUdpClient
    {
        internal NetUdpClient(string hostname, int port) : base(hostname, port)
        {
        }


        public NetUdpClient(int localPort) : base(localPort)
        {
        }


        public NetUdpClient()
        {
        }
    }
}
