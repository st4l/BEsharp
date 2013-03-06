namespace BNet.BaseCommands
{
    public class PlayerInfo
    {
        // 20:43:35 - [#] [IP Address]:[Port] [Ping] [GUID] [Name]
        // 20:43:35 - 0   78.9.36.119:2304      47   9553448ab4a0ec6bd5ef39fc4b6f28ba(OK) nino
        public string Guid { get; set; }

        public int Id { get; set; }

        public bool InLobby { get; set; }

        public string IpAddress { get; set; }

        public string Name { get; set; }

        public int Ping { get; set; }

        public bool Verified { get; set; }
    }
}
