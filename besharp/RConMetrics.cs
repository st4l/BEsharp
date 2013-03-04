// ----------------------------------------------------------------------------------------------------
// <copyright file="RConMetrics.cs" company="Me">Copyright (c) 2013 St4l.</copyright>
// ----------------------------------------------------------------------------------------------------
namespace BESharp
{
    using System;

    public class RConMetrics
    {
        public RConMetrics()
        {
            this.StartTime = DateTimeOffset.Now;
        }


        public DateTimeOffset StartTime { get; set; }

        public DateTimeOffset StopTime { get; set; }

        public TimeSpan TotalRuntime { get; set; }

        public int InboundDatagramCount { get; set; }

        public int OutboundDatagramCount { get; set; }

        public int ParsedDatagramsCount { get; set; }

        public int DispatchedConsoleMessages { get; set; }

        public int KeepAliveDatagramsSent { get; set; }

        public int KeepAliveDatagramsAcknowledgedByServer { get; set; }


        public void StopCollecting()
        {
            this.StopTime = DateTimeOffset.Now;
            this.TotalRuntime = this.StopTime - this.StartTime;
        }
    }
}
