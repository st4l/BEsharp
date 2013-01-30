
namespace BESharp.Tests
{
    using System;
    using System.Net;

    public class MockServerSetup
    {
        private DateTime startTime;


        public MockServerSetup()
        {
            this.ServerEndpoint = new IPEndPoint(IPAddress.Parse("45.87.98.123"), 2302);
            this.Password = DefaultPassword;
            this.MaxRunSeconds = 10;
            this.startTime = DateTime.Now;
        }


        public static string DefaultPassword
        {
            get { return "fnu(\\$lkd\"fh"; }
        }

        public IPEndPoint ServerEndpoint { get; set; }
        public string Password { get; set; }
        public DateTime ShutdownServerTime
        {
            get { return startTime.AddSeconds(this.MaxRunSeconds); }
        }

        internal int AverageResponseTime { get; set; } // ms

        public bool LoginAtThirdTry { get; set; }
        public bool LoginIncorrect { get; set; }
        public bool LoginServerDown { get; set; }
        public bool LoginSlow { get; set; }
        public bool CorruptConsoleMessages { get; set; }

        public bool LoadTestOnly { get; set; }
        public int LoadTestConsoleMessages { get; set; }

        public bool OnlyLogin { get; set; }

        public bool KeepAliveOnly { get; set; }

        public bool RepeatedConsoleMessages { get; set; }

        public bool DontAnswerKeepAlive { get; set; }

        public int MaxRunSeconds { get; set; }

        public bool DisorderedMultiPacketResponses { get; set; }
    }
}
