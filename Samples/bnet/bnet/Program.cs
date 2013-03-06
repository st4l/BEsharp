// ----------------------------------------------------------------------------------------------------
// <copyright file="Program.cs" company="Me">Copyright (c) 2012 St4l.</copyright>
// ----------------------------------------------------------------------------------------------------
namespace BNet
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Reflection;
    using System.Text;
    using Autofac;
    using BattleNET;
    using BNet.IoC;
    using BNet.IoC.Log4Net;
    using IniParser;
    using log4net.Config;
    using Plossum.CommandLine;


    internal static class Program
    {
        private static IContainer Container { get; set; }


        private static void Main()
        {
            Console.OutputEncoding = Encoding.UTF8;
            Console.Title = "bnet - " + Environment.MachineName;
            
            Console.WriteLine("bnet is starting...");
#if DEBUG

            // Give me a chance to attach the debugger... (launching from .bat)
            // Console.WriteLine("Press Enter to begin...");
            // Console.ReadLine();
#endif

            XmlConfigurator.ConfigureAndWatch(new FileInfo("log4net.config"));
            SetupIoC();

            var app = Container.Resolve<CommandExecutor>();
            app.Container = Container;

            Args options;
            try
            {
                options = GetAppArguments(app);
            }
            catch (Exception e)
            {
                app.Log.Error("Could not parse arguments.", e);
                throw;
            }


            // No errors present and all arguments correct 
            // Do work according to arguments   
            if (options.AsService > 0)
            {
                try
                {
                    app.StartService(options.AsService);
                    while (true)
                    {
                        Console.ReadKey(true);
                    }
                }
                catch (Exception e)
                {
                    app.Log.Error("Error running as a service.", e);
                }
            }

            try
            {
                app.ExecuteCommands();
            }
            catch (Exception e)
            {
                app.Log.Error("Error executing command.", e);
            }

            Environment.Exit(0);
        }


        /// <summary>
        ///     Sets up IoC container for dependency injections.
        /// </summary>
        private static void SetupIoC()
        {
            var builder = new ContainerBuilder();

            // builder.RegisterInstance(Console.Out).As<TextWriter>().ExternallyOwned();
            builder.RegisterModule<Log4NetModule>();
            RegisterAssembly(builder, "bnet.BaseCommands");
            RegisterAssembly(builder, "bnet.AdvCommands");
            builder.RegisterType<CommandExecutor>().AsSelf().PropertiesAutowired();
            Container = builder.Build();
        }


        private static void RegisterAssembly(ContainerBuilder builder, string assemblyName)
        {
            var baseCommands = Assembly.Load(assemblyName);
            if (baseCommands == null)
            {
                throw new FileNotFoundException("File not found.", assemblyName + ".dll");
            }

            builder.RegisterAssemblyModules(baseCommands);
        }


        /// <summary>
        ///     Parses command line or batch file arguments and configures the
        ///     <paramref name="executor" /> according to them.
        /// </summary>
        /// <param name="executor">
        ///     The <see cref="CommandExecutor" /> to be configured
        /// </param>
        /// <returns></returns>
        private static Args GetAppArguments(CommandExecutor executor)
        {
            var options = new Args();
            var parser = new CommandLineParser(options);

            parser.Parse();

            if (options.Help)
            {
                Exit(executor, parser, 0);
            } 
            else if (parser.HasErrors)
            {
                Exit(executor, parser, 1);
            }

#if DEBUG
            options.BatchFile = "debug.bnet";
            options.AsService = 30;
#endif
            // batch file or command-line?
            if (options.BatchFile != null)
            {
                if (!ParseBatchFile(options.BatchFile, executor))
                {
                    Exit(executor, parser, 1);
                }
            }
            else
            {
                executor.Servers = ParseServerUris(options.Servers);
                if (executor.Servers == null || !executor.Servers.Any())
                {
                    Console.WriteLine("No servers specified.");
                    Exit(executor, parser, 1);
                }

                executor.Commands = options.Commands;
                if (executor.Commands == null || options.Commands.Count == 0)
                {
                    Console.WriteLine("No commands specified.");
                    Exit(executor, parser, 1);
                }
            }

            return options;
        }


        private static string GetCommandsHelp(CommandExecutor executor)
        {
            var sb = new StringBuilder();
            sb.AppendLine("Available extra commands:");

            foreach (var command in executor.BNetCommandsMetadata)
            {
                sb.AppendFormat("{0} - {1}", command.Key, command.Value.Description);
                sb.AppendLine();
            }

            return sb.ToString();
        }


        private static void Exit(CommandExecutor executor, CommandLineParser parser, int errorLevel)
        {
            var sb = new StringBuilder();
            sb.AppendLine();
            sb.AppendLine();
            sb.AppendLine(parser.UsageInfo.ToString(78, true));
            sb.AppendLine(GetCommandsHelp(executor));
            sb.AppendLine();
            sb.AppendLine("Examples: bnet -b SampleBatch.bnet -svc 60");
            sb.AppendLine(
                "          bnet -u rconpass@127.0.0.1:2302 -u rconpass@127.0.0.1:3302 -svc 60 -c getplayers -c update_dbplayers");
            sb.AppendLine();
            sb.AppendLine("Press any key to exit...");
            
            Console.Write(sb);
            Console.ReadKey();
            Environment.Exit(errorLevel);
        }


        private static bool ParseBatchFile(string batchFile, CommandExecutor executor)
        {
            var parser = new FileIniDataParser { CommentDelimiter = (char)0 };
            IniData data;
            try
            {
                data = parser.LoadFile(batchFile);
            }
            catch (ParsingException pex)
            {
                Console.WriteLine(pex.Message);
                return false;
            }

            if (!ValidateIniSettingExists(data, "Servers")
                || !ValidateIniSettingExists(data, "Commands"))
            {
                return false;
            }

            // Get db settings even if we don't need to get the servers from it
            executor.DbConnectionString = GetIniDbConnectionString(data);

            if (IsIniTrue(data["Servers"]["UseBNetDb"]))
            {
                if (executor.DbConnectionString == null)
                {
                    Console.WriteLine("Invalid or not specified connection string.");
                    return false;
                }

                executor.Servers = executor.GetDbServers();
            }
            else
            {
                var serverUris = from keyData in data["Servers"]
                                 where keyData.KeyName != "UseBNetDb"
                                 select keyData.Value;
                executor.Servers = ParseServerUris(serverUris);
            }

            if (executor.Servers == null || !executor.Servers.Any())
            {
                Console.WriteLine("No servers specified.");
                return false;
            }

            executor.Commands = from keyData in data["Commands"] select keyData.Value;
            if (!executor.Commands.Any())
            {
                Console.WriteLine("No commands specified.");
                return false;
            }

            return true;
        }


        private static string GetIniDbConnectionString(IniData data)
        {
            if (!ValidateIniSettingExists(data, "Database")
                || !ValidateIniSettingExists(data, "Database", "Host")
                || !ValidateIniSettingExists(data, "Database", "Port")
                || !ValidateIniSettingExists(data, "Database", "Database")
                || !ValidateIniSettingExists(data, "Database", "Username")
                || !ValidateIniSettingExists(data, "Database", "Password"))
            {
                return null;
            }

            return CommandExecutor.ConstructBNetConnectionString(
                data["Database"]["Username"], 
                data["Database"]["Password"], 
                data["Database"]["Host"], 
                data["Database"]["Port"], 
                data["Database"]["Database"]);
        }


        private static bool ValidateIniSettingExists(
            IniData data, string sectionName, string keyName = null)
        {
            if (data[sectionName] == null)
            {
                Console.WriteLine("Section '{0}' not found in batch file.", sectionName);
                return false;
            }

            if (keyName != null)
            {
                if (data[sectionName][keyName] == null)
                {
                    Console.WriteLine(
                        "Setting '{1}' in section '{0}' not found in batch file.", 
                        sectionName, 
                        keyName);
                    return false;
                }
            }

            return true;
        }


        private static bool IsIniTrue(string setting)
        {
            if (setting == null)
            {
                return false;
            }

            setting = setting.ToLower();
            return setting == "1" || setting == "true" || setting == "yes";
        }


        private static IEnumerable<ServerInfo> ParseServerUris(IEnumerable<string> serverStrings)
        {
            var results = new List<ServerInfo>();
            int id = 0;
            foreach (string server in serverStrings)
            {
                id++;
                Uri uri;

                try
                {
                    uri = new Uri("bnet://" + server);
                }
                catch (UriFormatException)
                {
                    uri = null;
                }

                if (uri == null || !uri.IsWellFormedOriginalString()
                    || string.IsNullOrEmpty(uri.UserInfo) || uri.UserInfo.IndexOf(':') > -1)
                {
                    Console.WriteLine("Invalid server: " + server);
                    return null;
                }

                int port = uri.IsDefaultPort ? 2302 : uri.Port;

                results.Add(
                    new ServerInfo
                        {
                            ServerId = id, 
                            ServerName = uri.DnsSafeHost + ":" + port, 
                            LoginCredentials =
                                new BattlEyeLoginCredentials
                                    {
                                        Host = uri.DnsSafeHost, 
                                        Port = port, 
                                        Password = uri.UserInfo
                                    }
                        });
            }

            if (results.Count > 0)
            {
                return results;
            }

            return null;
        }
    }
}
