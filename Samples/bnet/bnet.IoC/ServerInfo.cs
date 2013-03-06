// ----------------------------------------------------------------------------------------------------
// <copyright file="ServerInfo.cs" company="Me">Copyright (c) 2012 St4l.</copyright>
// ----------------------------------------------------------------------------------------------------
namespace BNet.IoC
{
    using BattleNET;


    public class ServerInfo
    {
        public BattlEyeLoginCredentials LoginCredentials { get; set; }

        public int ServerId { get; set; }

        public string ServerName { get; set; }
    }
}
