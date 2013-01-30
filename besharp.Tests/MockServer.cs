// ----------------------------------------------------------------------------------------------------
// <copyright file="MockServer.cs" company="Me">Copyright (c) 2013 St4l.</copyright>
// ----------------------------------------------------------------------------------------------------
namespace BESharp.Tests
{
    using System;
    using System.Collections.Concurrent;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Linq;
    using System.Net.Sockets;
    using System.Text;
    using System.Threading;
    using Datagrams;
    using Microsoft.VisualStudio.TestTools.UnitTesting;

    public class MockServer
    {
        private readonly int avgResponseTime;

        private readonly ConcurrentQueue<byte[]> outboundQueue = new ConcurrentQueue<byte[]>();

        private readonly List<TestDatagram> receivedDatagrams = new List<TestDatagram>();

        private readonly MockServerSetup setup;

        private int ackPacketsReceivedCount;

        private byte conMsgSequenceNum;

        private int keepAlivePacketsReceivedCount;

        private int loginAttempts;

        private bool shutdown;

        private int totalConMsgsGenerated;

        private bool lastConMsgRepeated;

        private int waitShutdown;


        public MockServer(MockServerSetup setup)
        {
            this.avgResponseTime = setup.AverageResponseTime; // ms
            this.setup = setup;
        }


        public UdpReceiveResult SendPacket()
        {
            var spinWait = new SpinWait();
            while (!this.shutdown && DateTime.Now < this.setup.ShutdownServerTime)
            {
                // wait once
                if (this.avgResponseTime > 0)
                {
                    Thread.Sleep(this.avgResponseTime);
                }
                else
                {
                    spinWait.SpinOnce();
                }

                // if this server should be down, keep looping
                if (this.setup.LoginServerDown)
                {
                    continue;
                }


                // if we have outbound packets to send, send the first one
                byte[] sendBytes;
                if (this.outboundQueue.TryDequeue(out sendBytes))
                {
                    if (sendBytes[7] == 0xFF)
                    {
                        // wait a couple loop cycles so the client receives
                        // everything (keepalive might need a couple cycles to pick it up)
                        if (++this.waitShutdown > 20)
                        {
                            this.shutdown = true;
                        }
                    }
                    return new UdpReceiveResult(sendBytes, this.setup.ServerEndpoint);
                }

                // else, check if we need to send dummy console messages for load testing
                byte[] loadTestMsg = this.CheckSendLoadTestMsg();
                if (loadTestMsg != null)
                {
                    return new UdpReceiveResult(loadTestMsg, this.setup.ServerEndpoint);
                }
            }

            // shutting down, we're done sending packets for this test
            this.shutdown = true;
            byte[] shutdownPacket = this.BuildShutdownPacket();
            return new UdpReceiveResult(shutdownPacket, this.setup.ServerEndpoint); // EOF
        }


        public int ReceivePacket(byte[] bytes, int length)
        {
            byte[] payload = ValidateInboundHeader(bytes, length);

            byte type = Buffer.GetByte(payload, 1);
            Assert.IsTrue(type <= 2);

            this.receivedDatagrams.Add(new TestDatagram(payload));

            switch (type)
            {
                case 0:
                    this.ReceiveLoginPacket(payload);
                    break;
                case 1:
                    this.ReceiveCommandPacket(payload);
                    break;
                case 2:
                    this.ReceiveAcknowledgePacket(payload);
                    break;
                default:
                    Assert.Fail("Invalid packet type");
                    break;
            }

            return length;
        }


        public void Shutdown()
        {
            this.shutdown = true;
        }


        public RConServerMetrics GetMetrics()
        {
            return new RConServerMetrics
                       {
                               AckPacketsReceived = this.ackPacketsReceivedCount,
                               KeepAlivePacketsReceived = this.keepAlivePacketsReceivedCount,
                               TotalConsoleMessagesGenerated = this.totalConMsgsGenerated
                       };
        }


        private static byte[] ValidateInboundHeader(byte[] bytes, int length)
        {
            Assert.IsTrue(length > 8);
            Assert.IsTrue(bytes.Length >= length);

            var buffer = new byte[length];
            Buffer.BlockCopy(bytes, 0, buffer, 0, length);


            Assert.AreEqual(Buffer.GetByte(buffer, 0), 0x42); // "B"
            Assert.AreEqual(Buffer.GetByte(buffer, 1), 0x45); // "E"

            var checksum = new byte[4];
            Buffer.BlockCopy(buffer, 2, checksum, 0, 4);

            var payload = new byte[length - 6];
            Buffer.BlockCopy(buffer, 6, payload, 0, length - 6);

            byte[] computedChecksum;
            using (var crc = new Crc32(Crc32.DefaultPolynomialReversed, Crc32.DefaultSeed))
            {
                computedChecksum = crc.ComputeHash(payload);
                Array.Reverse(computedChecksum);
            }

            Assert.IsTrue(checksum.SequenceEqual(computedChecksum));

            Assert.AreEqual(Buffer.GetByte(payload, 0), 0xFF);
            return payload;
        }


        private byte[] BuildShutdownPacket()
        {
            var shutdownPayload = new byte[2];
            Buffer.SetByte(shutdownPayload, 0, 0xFF);
            Buffer.SetByte(shutdownPayload, 1, 0xFF);
            return this.BuildOutboundPacket(shutdownPayload);
        }


        private byte[] CheckSendLoadTestMsg()
        {
            if (this.setup.LoadTestConsoleMessages == -1 ||
                this.totalConMsgsGenerated < this.setup.LoadTestConsoleMessages)
            {
                return this.GenerateRandomConsoleMessage();
            }

            if (this.setup.LoadTestConsoleMessages > 0 &&
                this.setup.LoadTestOnly)
            {
                return this.BuildShutdownPacket();
            }
            return null;
        }


        private void ReceiveLoginPacket(byte[] payload)
        {
            this.loginAttempts++;
            string recvdPass = Encoding.ASCII.GetString(payload, 2, payload.Length - 2);

            if (this.setup.LoginAtThirdTry)
            {
                // remain mute unless this is the third login attempt
                if (this.loginAttempts < 3)
                {
                    return;
                }
            }

            var outPayload = new byte[3];
            Buffer.SetByte(outPayload, 0, 0xFF);
            Buffer.SetByte(outPayload, 1, 0x00);
            if (this.setup.Password == recvdPass && !this.setup.LoginIncorrect)
            {
                // Logged in
                Buffer.SetByte(outPayload, 2, 0x01);
                this.outboundQueue.Enqueue(this.BuildOutboundPacket(outPayload));
                this.LoggedIn();
            }
            else
            {
                Buffer.SetByte(outPayload, 2, 0x00);
                this.outboundQueue.Enqueue(this.BuildOutboundPacket(outPayload));
            }
            if (this.setup.OnlyLogin)
            {
                this.SendShutdownPacket();
            }
        }


        private void LoggedIn()
        {
        }


        private void SendShutdownPacket()
        {
            Debug.WriteLine("SHUTDOWN packet sent to client");
            byte[] shutdownPacket = this.BuildShutdownPacket();
            this.outboundQueue.Enqueue(shutdownPacket); // EOF
        }


        private byte[] GenerateRandomConsoleMessage()
        {
            var payload = new byte[500];
            Buffer.SetByte(payload, 0, 0xFF);
            Buffer.SetByte(payload, 1, 0x02);
            Buffer.SetByte(payload, 2, this.conMsgSequenceNum);
            this.IncrementConMsgSequenceNum();

            var random = new Random(3452445);
            for (int i = 3; i < 500; i++)
            {
                Buffer.SetByte(payload, i, (byte)random.Next(33, 168));
            }
            this.totalConMsgsGenerated++;
            return this.BuildOutboundPacket(payload, this.setup.CorruptConsoleMessages);
        }


        private void IncrementConMsgSequenceNum()
        {
            if (this.setup.RepeatedConsoleMessages)
            {
                if (!this.lastConMsgRepeated)
                {
                    this.lastConMsgRepeated = true;
                    return;
                }
            }
            this.conMsgSequenceNum = this.conMsgSequenceNum == (byte)255 ? (byte)0 : (byte)(this.conMsgSequenceNum + 1);
            this.lastConMsgRepeated = false;
        }


        private void ReceiveCommandPacket(byte[] payload)
        {
            // just the keep alive packet
            if (payload.Length == 3)
            {
                Assert.AreEqual(payload[0], (byte)0xFF);
                Assert.AreEqual(payload[1], (byte)0x01);
                var seqNum = payload[2];
                this.keepAlivePacketsReceivedCount++;
                if (!this.setup.DontAnswerKeepAlive)
                {
                    this.AcknowledgeEmptyResponseCommand(seqNum);
                }

                if (this.setup.KeepAliveOnly)
                {
                    this.SendShutdownPacket();
                    return;
                }

                return;
            }

            this.ProcessCommand(payload);
        }


        private void AcknowledgeEmptyResponseCommand(byte seqNumber)
        {
            var outPayload = new byte[3];
            Buffer.SetByte(outPayload, 0, 0xFF);
            Buffer.SetByte(outPayload, 1, 0x01);
            Buffer.SetByte(outPayload, 2, seqNumber);
            this.outboundQueue.Enqueue(this.BuildOutboundPacket(outPayload));
            Debug.WriteLine("command packet {0} acknowledged", seqNumber);
        }


        private void ProcessCommand(byte[] payload)
        {
            byte seqNum = payload[2];
            string cmdText = Encoding.ASCII.GetString(payload, 3, payload.Length - 3);

            if (cmdText == "getplayers")
            {
                this.SendGetPlayersSimple(seqNum);
            }
            if (cmdText == "getplayersmulti")
            {
                this.SendGetPlayersMulti(seqNum);
            }
        }


        private void SendGetPlayersMulti(byte seqNum)
        {
            if (this.setup.DisorderedMultiPacketResponses)
            {
                for (int i = 9; i >= 0; i--)
                {
                    this.SendGetPlayersPart(seqNum, (byte)i, 10);
                }
            }
            else
            {
                for (byte i = 0; i < 10; i++)
                {
                    this.SendGetPlayersPart(seqNum, i, 10);
                }
            }
        }


        private void SendGetPlayersPart(byte seqNum, byte partNum, byte total)
        {
            const string response = @"Players on server: (part {0:000}/{1:000})
[#] [IP Address]:[Port] [Ping] [GUID] [Name]
--------------------------------------------------
0   103.77.52.177:2304    32   1ef92993d1e8f2512422da34c9f975f1(OK) Jhon Denton (Lobby)
0   103.77.52.177:2304    32   -  Pixie
(19 players in total)
";
            string output = string.Format(response, partNum + 1, total);
            byte[] responseBytes = Encoding.ASCII.GetBytes(output);

            var outPayload = new byte[responseBytes.Length + 6];
            Buffer.SetByte(outPayload, 0, 0xFF);
            Buffer.SetByte(outPayload, 1, 0x01);
            Buffer.SetByte(outPayload, 2, seqNum);

            Buffer.SetByte(outPayload, 3, 0x00);
            Buffer.SetByte(outPayload, 4, total);
            Buffer.SetByte(outPayload, 5, partNum);

            Buffer.BlockCopy(responseBytes, 0, outPayload, 6, responseBytes.Length);
            this.outboundQueue.Enqueue(this.BuildOutboundPacket(outPayload));
        }


        private void SendGetPlayersSimple(byte seqNum)
        {
            const string response = @"Players on server:
[#] [IP Address]:[Port] [Ping] [GUID] [Name]
--------------------------------------------------
0   103.77.52.177:2304    32   1ef92993d1e8f2512422da34c9f975f1(OK) Jhon Denton (Lobby)
0   103.77.52.177:2304    32   -  Pixie
(19 players in total)
";
            byte[] responseBytes = Encoding.ASCII.GetBytes(response);

            var outPayload = new byte[responseBytes.Length + 3];
            Buffer.SetByte(outPayload, 0, 0xFF);
            Buffer.SetByte(outPayload, 1, 0x01);
            Buffer.SetByte(outPayload, 2, seqNum);
            Buffer.BlockCopy(responseBytes, 0, outPayload, 3, responseBytes.Length);
            this.outboundQueue.Enqueue(this.BuildOutboundPacket(outPayload));
        }


        private void ReceiveAcknowledgePacket(byte[] payload)
        {
            this.ackPacketsReceivedCount++;
        }


        private byte[] BuildOutboundPacket(byte[] payload, bool corrupted = false)
        {
            byte[] checksum;
            if (corrupted)
            {
                checksum = new byte[] { 0x34, 0x78, 0xF2, 0xC1 };
            }
            else
            {
                using (var crc = new Crc32(Crc32.DefaultPolynomialReversed, Crc32.DefaultSeed))
                {
                    checksum = crc.ComputeHash(payload);
                    Array.Reverse(checksum);
                }
            }

            int payloadLen = Buffer.ByteLength(payload);
            var result = new byte[6 + payloadLen];
            Buffer.SetByte(result, 0, 0x42); // "B"
            Buffer.SetByte(result, 1, 0x45); // "E"
            Buffer.BlockCopy(checksum, 0, result, 2, 4);
            Buffer.BlockCopy(payload, 0, result, 6, payloadLen);

            return result;
        }
    }
}
