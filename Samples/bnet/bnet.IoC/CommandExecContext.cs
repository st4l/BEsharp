// ----------------------------------------------------------------------------------------------------
// <copyright file="CommandExecContext.cs" company="Me">Copyright (c) 2012 St4l.</copyright>
// ----------------------------------------------------------------------------------------------------

namespace BNet.IoC
{
    public class CommandExecContext
    {
        public ServerInfo Server { get; set; }

        public string CommandString { get; set; }

        public string DbConnectionString { get; set; }
    }
}
