namespace BESharp.Tests
{
    public class RConServerMetrics
    {
        public int KeepAlivePacketsReceived { get; set; }

        public int AckPacketsReceived { get; set; }

        public int TotalConsoleMessagesGenerated { get; set; }
    }
}