// ----------------------------------------------------------------------------------------------------
// <copyright file="RConClientTests.cs" company="Me">Copyright (c) 2012 St4l.</copyright>
// ----------------------------------------------------------------------------------------------------

using System.Diagnostics;
using System.Threading.Tasks;
using BESharp.Datagrams;

namespace BESharp.Tests
{
    using System;
    using System.Security.Authentication;
    using BESharp;
    using Microsoft.VisualStudio.TestTools.UnitTesting;
    using log4net.Config;
    using log4net.Core;
    using log4net.Filter;
    using log4net.Layout;


    [TestClass]
    public class RConClientTests
    {
        ///<summary>
        ///  Gets or sets the test context which provides
        ///  information about and functionality for the current test run.
        ///</summary>
        public TestContext TestContext { get; set; }


        [ClassInitialize]
        public static void Initialize(TestContext ctx)
        {
            var appender = new MyDebugAppender
                {
                    Layout =
                        new PatternLayout(
                        "%date{HH:mm:ss:fffff} %-10logger{1} %-5level - [%thread] %message%newline")
                };

            var filterLevels = new LevelRangeFilter { LevelMin = Level.All, AcceptOnMatch = true };
            //filterLevels.LevelMax = Level.Fatal;
            filterLevels.ActivateOptions();

            // var filterDeny = new DenyAllFilter();

            appender.AddFilter(filterLevels);
            appender.Threshold = Level.Trace;

            appender.ActivateOptions();

            BasicConfigurator.Configure(appender);
        }


        [TestMethod]
        [TestCategory("Protocol Compliance")]
        public void ShouldLogin()
        {
            var client = CreateClient(new MockServerSetup { OnlyLogin = true });
            var rcc = new RConClient(client, client.ServerSetup.Password);
            var connected = rcc.ConnectAsync().Result;
            Assert.IsTrue(connected, "not connected");
        }


        [TestMethod]
        [TestCategory("Protocol Compliance")]
        public void ShouldThrowOnLoginWithWrongPassword()
        {
            var client = CreateClient(new MockServerSetup { OnlyLogin = true });
            var rcc = new RConClient(client, "fnipw93457");

            bool threw = false;
            try
            {
                var connected = rcc.ConnectAsync().Result;
                Assert.IsFalse(connected, "should not return true");
            }
            catch (AggregateException aex)
            {
                if (aex.InnerExceptions.Count == 1
                    && aex.InnerExceptions[0] is InvalidCredentialException)
                {
                    threw = true;
                }
                else
                {
                    throw;
                }
            }
            catch (InvalidCredentialException)
            {
                threw = true;
            }

            if (!threw)
            {
                Assert.Fail("Should have thrown an Invalid Credential Exception");
            }
        }


        [TestMethod]
        [TestCategory("Protocol Compliance")]
        public void ShouldThrowWhenServerDown()
        {
            var conf = new MockServerSetup { LoginServerDown = true, OnlyLogin = true };
            var client = CreateClient(conf);
            var rcc = new RConClient(client, client.ServerSetup.Password);

            bool threw = false;
            try
            {
                var connected = rcc.ConnectAsync().Result;
                Assert.IsFalse(connected, "should not return true");
            }
            catch (AggregateException aex)
            {
                if (aex.InnerExceptions.Count == 1 && aex.InnerExceptions[0] is TimeoutException)
                {
                    threw = true;
                }
                else
                {
                    throw;
                }
            }
            catch (TimeoutException)
            {
                threw = true;
            }

            if (!threw)
            {
                Assert.Fail("Should have thrown a Timeout Exception");
            }
        }


        [TestMethod]
        [TestCategory("Correctness")]
        public void ShouldDiscardConsolePackets()
        {
            var conf = new MockServerSetup
            {
                LoadTestConsoleMessages = 100,
                LoadTestOnly = true
            };

            var client = CreateClient(conf);
            var rcc = new RConClient(client, client.ServerSetup.Password) {DiscardConsoleMessages = true};
            RunUntilShutdown(rcc);
            Assert.IsTrue(rcc.Metrics.DispatchedConsoleMessages == 0);
        }


        [TestMethod]
        [TestCategory("Protocol Compliance")]
        public void ShouldDiscardCorruptedPackets()
        {
            var conf = new MockServerSetup
            {
                LoadTestConsoleMessages = 100,
                LoadTestOnly = true,
                CorruptConsoleMessages = true
            };

            var client = CreateClient(conf);
            var rcc = new RConClient(client, client.ServerSetup.Password);
            RunUntilShutdown(rcc);
            Assert.IsTrue(rcc.Metrics.InboundPacketCount > 100);
            Assert.IsTrue(rcc.Metrics.DispatchedConsoleMessages == 0);
        }


        [TestMethod]
        [TestCategory("Correctness")]
        public void ShouldNotifyAboutCorruptedPackets()
        {
            var conf = new MockServerSetup
            {
                LoadTestConsoleMessages = 100,
                LoadTestOnly = true,
                CorruptConsoleMessages = true
            };
            var client = CreateClient(conf);
            var rcc = new RConClient(client, client.ServerSetup.Password);

            int notificationsCount = 0;
            rcc.PacketProblem += (sender, args) =>
                                     { 
                                         if (args.PacketProblemType == PacketProblemType.Corrupted) notificationsCount++;
                                     };

            RunUntilShutdown(rcc);

            var serverMetrics = client.Server.GetMetrics();
            Debug.WriteLine("Console Messages generated by server: {0}", serverMetrics.TotalConsoleMessagesGenerated);
            Debug.WriteLine("Inbound Packets received by client: {0}", rcc.Metrics.InboundPacketCount);
            Debug.WriteLine("Console Packets Acknowledgments received by server: {0}", serverMetrics.AckPacketsReceived);
            Debug.WriteLine("Keep Alive packets received by server: {0}", serverMetrics.KeepAlivePacketsReceived);
            Assert.IsTrue(notificationsCount == serverMetrics.TotalConsoleMessagesGenerated);
        }


        [TestMethod]
        [TestCategory("Protocol Compliance")]
        public void ShouldDiscardRepeatedConsoleMessages()
        {
            var conf = new MockServerSetup
            {
                LoadTestConsoleMessages = 100,
                LoadTestOnly = true,
                RepeatedConsoleMessages = true
            };
            var client = CreateClient(conf);
            var rcc = new RConClient(client, client.ServerSetup.Password);
            RunUntilShutdown(rcc);
            Assert.IsTrue(rcc.Metrics.DispatchedConsoleMessages > 10);
            Assert.IsTrue(rcc.Metrics.DispatchedConsoleMessages < 52);
            Debug.WriteLine("Inbound Packets received by client: {0}", rcc.Metrics.InboundPacketCount);
            Debug.WriteLine("Console messages dispatched by client: {0}", rcc.Metrics.DispatchedConsoleMessages);
        }


        [TestMethod]
        [TestCategory("Protocol Compliance")]
        public async Task ShouldReturnCommandResponsesCorrectly()
        {
            var conf = new MockServerSetup();
            var client = CreateClient(conf);
            var rcc = new RConClient(client, client.ServerSetup.Password);
            var connected = rcc.ConnectAsync().Result;
            Assert.IsTrue(connected, "not connected");

            var handler = rcc.SendCommand("getplayers");
            Assert.IsNotNull(handler);

            CommandSinglePacketResponseDatagram singlePacketResponse = null;
            if (await handler.WaitForResponse())
            {
                singlePacketResponse = handler.ResponseDatagram as CommandSinglePacketResponseDatagram;
            }

            Assert.IsNotNull(singlePacketResponse);
            Assert.IsTrue(singlePacketResponse.Body.StartsWith("Players on server:"));
            rcc.Close();
        }


        [TestMethod]
        [TestCategory("Protocol Compliance")]
        public async Task ShouldParseMultipartCommandResponsesCorrectly()
        {
            var conf = new MockServerSetup();
            var client = CreateClient(conf);
            var rcc = new RConClient(client, client.ServerSetup.Password);
            var connected = rcc.ConnectAsync().Result;
            Assert.IsTrue(connected, "not connected");

            var handler = rcc.SendCommand("getplayersmulti");
            Assert.IsNotNull(handler);

            CommandMultiPacketResponseDatagram multiPacketResponseDatagram = null;
            if (await handler.WaitForResponse())
            {
                multiPacketResponseDatagram = handler.ResponseDatagram as CommandMultiPacketResponseDatagram;
            }

            Assert.IsNotNull(multiPacketResponseDatagram);
            Assert.IsNotNull(multiPacketResponseDatagram.Body);
            Assert.IsTrue(multiPacketResponseDatagram.Body.StartsWith("Players on server:"));
            for (int i = 1; i <= 10; i++)
            {
                Assert.IsTrue(multiPacketResponseDatagram.Body.Contains(
                    string.Format("(part {0:000}/010)", i)));
            }
            rcc.Close();
        }
        

        [TestMethod]
        [TestCategory("Protocol Compliance")]
        public async Task ShouldAcceptOutOfOrderCommandResponsesCorrectly()
        {
            var conf = new MockServerSetup
            {
                DisorderedMultiPacketResponses = true
            };
            var client = CreateClient(conf);
            var rcc = new RConClient(client, client.ServerSetup.Password);
            var connected = rcc.ConnectAsync().Result;
            Assert.IsTrue(connected, "not connected");

            var handler = rcc.SendCommand("getplayersmulti");
            Assert.IsNotNull(handler);

            CommandMultiPacketResponseDatagram multiPacketResponseDatagram = null;
            if (await handler.WaitForResponse())
            {
                multiPacketResponseDatagram = handler.ResponseDatagram as CommandMultiPacketResponseDatagram;
            }
            rcc.Close();

            Assert.IsNotNull(multiPacketResponseDatagram);
            Assert.IsTrue(multiPacketResponseDatagram.Body.StartsWith("Players on server:"));
            for (int i = 1; i <= 10; i++)
            {
                Assert.IsTrue(multiPacketResponseDatagram.Body.Contains(
                    string.Format("(part {0:000}/010)", i)));
            }

            Debug.WriteLine("Response assembled: {0}", (object)multiPacketResponseDatagram.Body);
            Debug.WriteLine("Client shutdown reason: {0}", rcc.ShutdownReason);

        }




        [TestMethod]
        [TestCategory("Performance")]
        [TestProperty("Time", "Long Running")]
        public void ShouldAcceptTonsOfPackets()
        {
            var conf = new MockServerSetup
            {
                LoadTestConsoleMessages = 50000,
#if TRACE
                MaxRunSeconds = 120,
#endif
                LoadTestOnly = true
            };
            var client = CreateClient(conf);
            var rcc = new RConClient(client, client.ServerSetup.Password) { DiscardConsoleMessages = true };
            RunUntilShutdown(rcc);
            Assert.IsTrue(rcc.Metrics.InboundPacketCount > 50000);
        }


        [TestMethod]
        [TestCategory("Performance")]
        [TestProperty("Time", "Long Running")]
        public void ShouldParseTonsOfPackets()
        {
            var conf = new MockServerSetup
            {
                LoadTestConsoleMessages = 50000,
#if TRACE
                MaxRunSeconds = 120,
#endif
                LoadTestOnly = true
            };
            var client = CreateClient(conf);
            var rcc = new RConClient(client, client.ServerSetup.Password);
            int parsedCount = 0;
            rcc.MessageReceived += (sender, args) => { parsedCount++; };

            RunUntilShutdown(rcc);
            Assert.IsTrue(parsedCount >= 50000, "parsedCount < 50000: it's {0}", parsedCount);
        }


        [TestMethod]
        [TestCategory("Correctness")]
        [TestProperty("Time", "Long Running")]
        public void ShouldSendKeepAliveUnderHeavyLoad()
        {
            var conf = new MockServerSetup
            {
                LoadTestConsoleMessages = -1,
                KeepAliveOnly = true,
                MaxRunSeconds = 20
            };

            var client = CreateClient(conf);
            var rcc = new RConClient(client, client.ServerSetup.Password);
            RunUntilShutdown(rcc);
            var serverMetrics = client.Server.GetMetrics();
            Debug.WriteLine("Console Messages generated by server: {0}", serverMetrics.TotalConsoleMessagesGenerated);
            Debug.WriteLine("Inbound Packets received by client: {0}", rcc.Metrics.InboundPacketCount);
            Debug.WriteLine("Console Packets Acknowledgments received by server: {0}", serverMetrics.AckPacketsReceived);
            Debug.WriteLine("Keep Alive packets received by server: {0}", serverMetrics.KeepAlivePacketsReceived);
            Debug.WriteLine("Keep Alive acknowlegments received by client: {0}", rcc.Metrics.KeepAlivePacketsAcknowledgedByServer);
            Debug.WriteLine("Client shutdown reason: {0}", rcc.ShutdownReason);
            Assert.IsTrue(serverMetrics.KeepAlivePacketsReceived > 0,
                "Server didn't receive any keep alive packets");
            Assert.IsTrue(rcc.Metrics.KeepAlivePacketsAcknowledgedByServer > 0,
                "Client didn't register server's keep alive acknowledgment");
        }


        [TestMethod]
        [TestCategory("Correctness")]
        [TestProperty("Time", "Long Running")]
        public void ShouldDetectDisconnectUnderHeavyLoad()
        {
            var conf = new MockServerSetup
            {
                LoadTestConsoleMessages = -1,
                DontAnswerKeepAlive = true,
                MaxRunSeconds = 40
            };

            var client = CreateClient(conf);
            var rcc = new RConClient(client, client.ServerSetup.Password);
            RunUntilShutdown(rcc);
            var serverMetrics = client.Server.GetMetrics();
            Debug.WriteLine("Console Messages generated by server: {0}", serverMetrics.TotalConsoleMessagesGenerated);
            Debug.WriteLine("Inbound Packets received by client: {0}", rcc.Metrics.InboundPacketCount);
            Debug.WriteLine("Console Packets Acknowledgments received by server: {0}", serverMetrics.AckPacketsReceived);
            Debug.WriteLine("Keep Alive packets sent by client: {0}", rcc.Metrics.KeepAlivePacketsSent);
            Debug.WriteLine("Keep Alive packets received by server: {0}", serverMetrics.KeepAlivePacketsReceived);
            Debug.WriteLine("Keep Alive packets acknowledged by server: {0}", rcc.Metrics.KeepAlivePacketsAcknowledgedByServer);
            Debug.WriteLine("Client shutdown reason: {0}", rcc.ShutdownReason);
            Assert.IsTrue(rcc.Metrics.KeepAlivePacketsSent > 0);
            Assert.IsTrue(serverMetrics.KeepAlivePacketsReceived > 0);
            Assert.IsTrue(rcc.Metrics.KeepAlivePacketsAcknowledgedByServer == 0);
            Assert.IsTrue(rcc.ShutdownReason == ShutdownReason.NoResponseFromServer,
                "Shutdown reason should be NoResponseFromServer, it's {0}", rcc.ShutdownReason);
        }



        [TestMethod]
        [Ignore]
        [TestProperty("Skip", "Not Yet Implemented")]
        [TestCategory("Correctness")]
        public void ShouldLoginOnThirdTry()
        {
            throw new NotImplementedException();
        }


        [TestMethod]
        [Ignore]
        [TestProperty("Skip", "Not Yet Implemented")]
        [TestCategory("Correctness")]
        public void ShoulNotifyPacketLoss()
        {
            throw new NotImplementedException();
        }


        [TestMethod]
        [Ignore]
        [TestProperty("Skip", "Not Yet Implemented")]
        [TestCategory("Protocol Compliance")]
        public void ShouldDiscardRepeatedCommandResponses()
        {
            // TODO: should be working, just need to write this test
            throw new NotImplementedException();
        }


        [TestMethod]
        [Ignore]
        [TestProperty("Skip", "Not Yet Implemented")]
        [TestCategory("Protocol Compliance")]
        public void ShouldDiscardRepeatedCommandResponseParts()
        {
            // TODO: should be working, just need to write this test
            throw new NotImplementedException();
        }



        [TestMethod]
        [Ignore]
        [TestProperty("Skip", "Will Not Implement")]
        [TestCategory("Correctness")]
        public async Task ShouldRetrySendingCommand()
        {
            var rcc = new RConClient("ip", 3333, "pass");
            var connected = await rcc.ConnectAsync();
            Assert.IsTrue(connected, "not connected");
            var handler1 = rcc.SendCommand(0, "missions");
            var handler2 = rcc.SendCommand(0, "missions");
            var handler3 = rcc.SendCommand(0, "#shutdown");
            Task.WaitAll(handler1.WaitForResponse(), handler2.WaitForResponse(), handler3.WaitForResponse());
            rcc.Close();

            Assert.IsTrue(handler1.Completed);
            Assert.IsTrue(handler2.Completed);
            Assert.IsTrue(handler3.Completed);
            var response1 = (CommandResponseDatagram)handler1.ResponseDatagram;
            var response2 = (CommandResponseDatagram)handler2.ResponseDatagram;
            var response3 = (CommandResponseDatagram)handler3.ResponseDatagram;
            Assert.AreEqual(response1.OriginalSequenceNumber, 0);
            Assert.AreEqual(response2.OriginalSequenceNumber, 0);
            Assert.AreEqual(response3.OriginalSequenceNumber, 0);
            string body1 = response1.Body;
            string body2 = response2.Body;
            string body3 = response3.Body;
            Assert.IsTrue(body1.Length > 0);
            Assert.IsTrue(body2.Length > 0);
            Assert.IsTrue(body3.Length > 0);
            
            // WOW. We can't retry sending commands with the same seq num, it executes 
            // the three of them including #shutdown. 
            // Maybe contact BattlEye with a bug report.
        }


        private static MockUdpClient CreateClient(MockServerSetup conf)
        {
            var client = new MockUdpClient();
            client.Setup(conf);
            return client;
        }


        private static void RunUntilShutdown(RConClient rcc)
        {
            var connected = rcc.ConnectAsync().Result;
            Assert.IsTrue(connected, "not connected");
            rcc.WaitUntilShutdown();
            rcc.Close();
        }
    }
}
