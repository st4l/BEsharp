// ----------------------------------------------------------------------------------------------------
// <copyright file="GetPlayersCommand.cs" company="Me">Copyright (c) 2012 St4l.</copyright>
// ----------------------------------------------------------------------------------------------------
namespace BNet.BaseCommands
{
    using System;
    using System.Collections.Generic;
    using System.Globalization;
    using IoC;


    public class GetPlayersCommand : RConCommandBase<List<PlayerInfo>>
    {

        public override string RConCommandText
        {
            get { return "players"; }
        }


        protected override List<PlayerInfo> ParseResponse(string rawResponse)
        {
            /*  Players on server:
             *  [#] [IP Address]:[Port] [Ping] [GUID] [Name]
             *  --------------------------------------------------
             *  0   103.77.52.177:2304    32   1ef92993d1e8f2512422da34c9f975f1(OK) Jhon Denton (Lobby)
             *  0   103.77.52.177:2304    32   -  Pixie
             *  (19 players in total)                            
            */
            var results = new List<PlayerInfo>();
            var lines = rawResponse.Split(
                new[] { (char)0x0A }, StringSplitOptions.RemoveEmptyEntries);
            for (int i = 3; i <= lines.Length - 2; i++)
            {
                string[] tokens = lines[i].Split(new char[0], StringSplitOptions.RemoveEmptyEntries);

                bool lobby;
                string name = ParseName(tokens, out lobby);

                // split Guid token
                string[] guidtokens = tokens[3] == "-"
                                          ? new[] { string.Empty, string.Empty }
                                          : tokens[3].Split('(');

                results.Add(
                    new PlayerInfo
                        {
                            Id = int.Parse(tokens[0], CultureInfo.InvariantCulture), 
                            IpAddress = tokens[1], 
                            Ping = int.Parse(tokens[2], CultureInfo.InvariantCulture), 
                            Guid = guidtokens[0], 
                            Verified = guidtokens[1] == "OK)", 
                            Name = name, 
                            InLobby = lobby
                        });
            }

            this.LogResults(results);
            return results;
        }


        private static string ParseName(string[] tokens, out bool lobby)
        {
            lobby = false;
            if (tokens[tokens.Length - 1] == "(Lobby)")
            {
                lobby = true;
                return string.Join(" ", tokens, 4, tokens.Length - 5);
            }

            return string.Join(" ", tokens, 4, tokens.Length - 4);
        }


        private void LogResults(List<PlayerInfo> results)
        {
            this.Log.Info("Players received: ");
            this.Log.InfoFormat(
                "{0,2}  {1,-30}  {2,32} {3,1} {4,5} {5,22} {6}", 
                "Id", 
                "Name", 
                "Guid", 
                "V", 
                "Ping", 
                "Ip address", 
                "In Lobby");

            foreach (PlayerInfo p in results)
            {
                this.Log.InfoFormat(
                    "{0:00}  {1,-30}  {2,32} {3,1} {4,5} {5,22} {6}", 
                    p.Id, 
                    p.Name, 
                    p.Guid, 
                    p.Verified.ToString()[0], 
                    p.Ping, 
                    p.IpAddress, 
                    p.InLobby ? "(in lobby)" : string.Empty);
            }

            this.Log.InfoFormat("{0} players", results.Count);
            this.Log.Info("---");
        }
    }
}
