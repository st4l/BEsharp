namespace BESharp.Tests
{
    using System.Net.Sockets;
    using System.Threading.Tasks;

    public class MockUdpClient : UdpClient, IUdpClient
    {
        public MockServer Server { get; set; }

        public MockServerSetup ServerSetup { get; private set; }


        #region IUdpClient Members

        /// <summary>
        /// Should spawn a new thread on continuation.
        /// </summary>
        public new Task<int> SendAsync(byte[] buffer, int length)
        {
            Task<int> task = Task.Factory.StartNew(
                () => this.Server.ReceivePacket(buffer, length));
            return task;
        }


        /// <summary>
        /// Should spawn a new thread on continuation.
        /// </summary>
        public new Task<UdpReceiveResult> ReceiveAsync()
        {
            Task<UdpReceiveResult> task = Task.Factory.StartNew(
                () => this.Server.SendPacket());
            return task;
        }


        public new void Close()
        {
            this.Server.Shutdown();
        }

        #endregion


        internal void Setup(MockServerSetup setup)
        {
            this.Server = new MockServer(setup);
            this.ServerSetup = setup;
        }
    }
}
