// ----------------------------------------------------------------------------------------------------
// <copyright file="CommandExecutor.cs" company="Me">Copyright (c) 2012 St4l.</copyright>
// ----------------------------------------------------------------------------------------------------
namespace BNet.IoC
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading;
    using Autofac;
    using BattleNET;
    using BNet.IoC.Data;
    using log4net;


    public class CommandExecutor
    {
        private List<Timer> runningTimers;


        public CommandExecutor(IEnumerable<Lazy<IRConCommand, RConCommandMetadata>> commands)
        {
            this.BNetCommandsMetadata = commands.ToDictionary(
                lazy => lazy.Metadata.Name, lazy => lazy.Metadata);
        }


        public IEnumerable<ServerInfo> Servers { get; set; }

        public IEnumerable<string> Commands { get; set; }

        public string DbConnectionString { get; set; }

        public IContainer Container { get; set; }

        // ReSharper disable MemberCanBePrivate.Global
        public ILog Log { get; set; }

        // ReSharper restore MemberCanBePrivate.Global
        public Dictionary<string, RConCommandMetadata> BNetCommandsMetadata { get; set; }


        public static string ConstructBNetEntityConnectionString(string connectionString)
        {
            if (connectionString == null)
            {
                return null;
            }

            connectionString = connectionString.Replace("\"", "'");
            return string.Format(
                    "metadata=res://*/Data.BNetDb.csdl|res://*/Data.BNetDb.ssdl|res://*/Data.BNetDb.msl;provider=MySql.Data.MySqlClient;"
                    + "provider connection string=\"{0}\";",
                    connectionString);
        }


        public static string ConstructBNetConnectionString(
            string user, string pwd, string host, string port, string db)
        {
            Uri uri;
            try
            {
                string uriString = string.Format("mysql://{0}:{1}", host, port);
                uri = new Uri(uriString);
            }
            catch (UriFormatException)
            {
                uri = null;
            }

            if (uri == null || !uri.IsWellFormedOriginalString() || string.IsNullOrWhiteSpace(user)
                || string.IsNullOrWhiteSpace(pwd) || string.IsNullOrWhiteSpace(db))
            {
                return null;
            }

            var connString =
                string.Format(
                    "server='{2}';port={3};database='{4}';User Id='{0}';Password='{1}';"
                    + "Check Parameters=false;Persist Security Info=False;Allow Zero Datetime=True;Convert Zero Datetime=True;",
                    user,
                    pwd,
                    host,
                    port,
                    db);
            
            return ConstructBNetEntityConnectionString(connString);
        }


        public void StartService(int period)
        {
            int start = 0;
            period *= 1000;
            var interleave =
                (int)Math.Floor((decimal)(period / this.Servers.Count() / this.Commands.Count()));
            interleave = Math.Max(1000, interleave);

            this.runningTimers = new List<Timer>();
            foreach (var context in this.GetCommandContexts())
            {
                var timer = new Timer(
                    state => this.ExecuteCommand((CommandExecContext)state), context, start, period);
                this.runningTimers.Add(timer);
                start += interleave;
            }
        }


        public void ExecuteCommands()
        {
            foreach (var context in this.GetCommandContexts())
            {
                this.ExecuteCommand(context);
            }
        }


        public IEnumerable<ServerInfo> GetDbServers()
        {
            IEnumerable<ServerInfo> servers;
            using (var db = new BNetDb(this.DbConnectionString))
            {
                db.Configuration.ProxyCreationEnabled = false;

                var dayzServers = db.dayz_server.Where(s => s.server_id == 1).ToArray();
                servers =
                    dayzServers.Select(
                        s => new ServerInfo
                            {
                                ServerId = (int)s.id, 
                                ServerName = s.short_name, 
                                LoginCredentials =
                                    new BattlEyeLoginCredentials(
                                    s.rcon_host, s.rcon_port, s.rcon_pwd)
                            });
            }

            return servers;
        }


        private void ExecuteCommand(CommandExecContext context)
        {
            try
            {
                if (this.BNetCommandsMetadata.ContainsKey(context.CommandString))
                {
                    this.ExecRconCommand(context);
                }
                else
                {
                    this.ExecCustomCmd(context);
                }
            }
            catch (RConException e)
            {
                this.Log.Error("ERROR executing command '" + context.CommandString + "'.", e);
            }
        }


        private void ExecRconCommand(CommandExecContext context)
        {
            // new cmd()
            var cmdInstance = this.Container.ResolveNamed<IRConCommand>(context.CommandString);
            cmdInstance.Metadata = this.BNetCommandsMetadata[context.CommandString];
            cmdInstance.Context = context;

            // covariance FTW
            var instanceWithResults = cmdInstance as IRConCommand<object>;
            if (instanceWithResults != null)
            {
                // use covariance to get the generic version's virtual table               
                instanceWithResults.ExecSingleAwaitResponse();
            }
            else
            {
                cmdInstance.ExecuteSingle();
            }

            // ~cmd()
        }


        private void ExecCustomCmd(CommandExecContext commandCtx)
        {
            string command = commandCtx.CommandString;
            var beClient = new BattlEyeClient(commandCtx.Server.LoginCredentials)
                               {
                                   ReconnectOnPacketLoss
                                       = true, 
                                   DiscardConsoleMessages
                                       = true
                               };

            beClient.DisconnectEvent += this.Disconnected;
            beClient.Connect();

            this.Log.Info("> " + command);
            BattlEyeCommandResult result = beClient.SendCommandPacket(command, false);
            this.Log.Info(result.ToString());

            while (beClient.CommandQueue > 0)
            {
                /* wait until server received all packets */
            }

            beClient.Disconnect();

            // ~beClient();
        }


        private IEnumerable<CommandExecContext> GetCommandContexts()
        {
            var contexts = from serverInfo in this.Servers
                           from command in this.Commands
                           select
                               new CommandExecContext
                                   {
                                       CommandString = command.ToLowerInvariant(), 
                                       Server = serverInfo, 
                                       DbConnectionString = this.DbConnectionString
                                   };
            return contexts;
        }


        private void Disconnected(BattlEyeDisconnectEventArgs args)
        {
            this.Log.Info(args.Message);
        }


        private void OutputMessage(BattlEyeMessageEventArgs args)
        {
            this.Log.Info(args.Message);
        }
    }
}
