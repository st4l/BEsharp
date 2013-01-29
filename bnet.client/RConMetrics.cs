using System;

namespace BESharp
{
    public class RConMetrics
    {
        public RConMetrics()
        {
            this.StartTime = DateTimeOffset.Now;
        }


        public DateTimeOffset StartTime { get; set; }
        public DateTimeOffset StopTime { get; set; }
        public TimeSpan TotalRuntime { get; set; }
        public int InboundPacketCount { get; set; }
        public int OutboundPacketCount { get; set; }
        public int ParsedDatagramsCount { get; set; }
        public int DispatchedConsoleMessages { get; set; }
        public int KeepAlivePacketsSent { get; set; }
        public int KeepAlivePacketsAcknowledgedByServer { get; set; }



        public void StopCollecting()
        {
            this.StopTime = DateTimeOffset.Now;
            this.TotalRuntime = this.StopTime - this.StartTime;
        }
    }
}
