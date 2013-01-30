// ----------------------------------------------------------------------------------------------------
// <copyright file="IUdpClient.cs" company="Me">Copyright (c) 2013 St4l.</copyright>
// ----------------------------------------------------------------------------------------------------
namespace BESharp
{
    using System.Net.Sockets;
    using System.Threading.Tasks;


    internal interface IUdpClient
    {
        Task<int> SendAsync(byte[] buffer, int length);

        Task<UdpReceiveResult> ReceiveAsync();

        void Close();
    }
}
