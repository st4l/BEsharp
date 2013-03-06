// ----------------------------------------------------------------------------------------------------
// <copyright file="Args.cs" company="Me">Copyright (c) 2012 St4l.</copyright>
// ----------------------------------------------------------------------------------------------------
namespace BNet
{
    using System.Collections.Generic;
    using Plossum.CommandLine;


    [CommandLineManager(ApplicationName = "bnet", Copyright = "Copyright (C) 2012 St4l", 
        EnabledOptionStyles = OptionStyles.Unix | OptionStyles.Windows)]
    internal class Args
    {
        public Args()
        {
            this.Servers = new List<string>();
            this.Commands = new List<string>();
        }

        [CommandLineOption(Name = "svc", Aliases = "s", MinOccurs = 0, MaxOccurs = 1, 
            Description = "Run as a service, execute commands every n seconds")]
        public int AsService { get; set; }

        [CommandLineOption(Name = "batch", Aliases = "b", MinOccurs = 0, 
            Prohibits = "cmd,uri", Description = "Run bnet batch file.")]
        public string BatchFile { get; set; }

        [CommandLineOption(Name = "uri", Aliases = "u", MinOccurs = 0, Prohibits = "batch", 
            Description = "Server connection details. Format: password@hostname:port")]
        public List<string> Servers { get; set; }

        [CommandLineOption(Name = "cmd", Aliases = "c", MinOccurs = 0, Prohibits = "batch", 
            Description = "Command to send")]
        public List<string> Commands { get; set; }

        [CommandLineOption(Name = "help", Aliases = "?", Description = "Shows this help text")]
        public bool Help { get; set; }

        [CommandLineOption(Name = "v", Aliases = "verbose", Description = "Produce verbose output")]
        public bool Verbose { get; set; }
    }
}
