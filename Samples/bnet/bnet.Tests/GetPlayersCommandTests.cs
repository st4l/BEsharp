namespace BNet.Tests
{
    using BNet.BaseCommands;
    using Microsoft.VisualStudio.TestTools.UnitTesting;


    [TestClass]
    public class GetPlayersCommandTests
    {
        #region Constants

        private const string TestResponse1 = @"Players on server:
[#] [IP Address]:[Port] [Ping] [GUID] [Name]
--------------------------------------------------
0   99.244.182.206:2304   125  abd7216bcb59c5daaf804f9c1087a9c5(OK) Akiba
1   173.16.17.189:2304    125  e974c758a2cb62e253ac4fc7ab8b05f2(OK) Trevor
2   66.212.206.113:2304   63   c103c5d03978311693a52c88caf5ed02(OK) Hill_Bicks
3   208.117.124.224:1027  78   2d38d2ebbfbc15c64138be2bdce62e3b(OK) Gunter
4   74.76.72.186:2304     62   cada68c7b780a1e1adf5ba48ee2d02f7(OK) Mr.Happy
5   50.55.41.73:2304      47   651f7ebe5a585b3663b34d59fb5298b2(OK) Eubanks
6   76.4.114.204:2304     46   e3861e062eea969136fc16973874c413(OK) _Ghost
7   66.229.193.76:2304    32   f6ab4bc5427c878dc7548f2eac28cbdc(OK) noAccess
8   186.213.17.51:2304    93   cd8b8ee31eeed25b5008317551d6c43f(OK) Allan (Lobby)
9   24.118.199.30:2304    79   ffd727c99ee201eec683641ab3aef1fa(OK) Goku
10  75.162.245.23:2304    93   20f5d5ba3ec24f5cce42733196b46e72(OK) Marcus Fenix
11  208.117.124.224:2304  78   80cbece4499a4a2b8079be4bb1c52913(OK) Exodus
12  71.32.8.66:2304       250  7249161ac6429e7635f8b4bd6a3b2a90(OK) Scout (Lobby)
13  86.28.244.56:2304     141  ff2a45766e6fe251ad879a22ff4362f2(OK) nomster (Lobby)
14  92.25.141.110:2304    172  0741845fc9f54c6b47475a7d432d0314(OK) McDarnit
15  97.103.173.7:2304     16   211ead0333445f3a6018bd58ce7f5c9c(OK) 7r1gg3r[M4L]
16  80.192.83.160:2304    157  2b4c77fd487b87ed603e524eb425fd94(OK) [OLD]GlasgowJag
17  108.173.21.245:2304   94   df493504b0e7524f9c4a607e15acdb4d(OK) dpx
18  98.229.242.117:2304   125  b50089044baa8b3160bd4a3950360e3a(OK) Officer Friendly (Lobby)
20  24.42.52.60:2304      47   9636229dd81de073093ff32be3fca483(OK) sk8chris7
21  68.36.83.92:2304      94   fd0c8a0d920e661c0922d5ac7c2dc4e4(OK) Alcatraz
22  99.37.164.102:2304    47   6ed620683607ead423773144f5847302(OK) August (Lobby)
24  90.202.174.143:2304   141  4ef3bd3c8e953da6c1f6ad04571efcc3(OK) flatline
28  99.170.241.99:2304    125  504ba4fe8eb1436769172c0076056a8b(OK) onewhofarted
31  24.176.86.145:2304    47   8180570fbe886dafa60a80b4a7ec81ed(OK) Rick Grimes
(25 players in total)";

        #endregion


        [TestMethod]
        [TestCategory("Correctness")]
        public void TestParseResponse()
        {
            var cmd = new GetPlayersCommand();
            cmd.Log = DebugLogger.GetLogger(typeof(GetPlayersCommand));

            var accessor = new PrivateObject(cmd);
            object result = accessor.Invoke("ParseResponse", TestResponse1);


            Assert.IsNotNull(result);
        }
    }
}
