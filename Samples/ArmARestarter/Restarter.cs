using System;
using System.IO;
using System.ServiceProcess;
using System.Text.RegularExpressions;
using System.Threading;
using BESharp;
namespace ArmARestarter
{
    internal class Restarter
    {
        private string host;

        private int port;

        private string password;

        private string serviceName;

        private string armaConfigPath; 


        public string ServerName { get; set; }


        public void Restart()
        {
            this.Say("ATTENTION: SERVER IS RESTARTING IN 5 MINUTES");
            Thread.Sleep(1000 * 60 * 3);
            
            this.Say("ATTENTION: SERVER IS RESTARTING IN 2 MINUTES - PREPARE TO GET OUT OF ANY VEHICLES AND ABORT BEFORE THE RESTART FOR YOUR CHARACTER TO BE SAFE");
            Thread.Sleep(1000 * 60);

            this.Say("ATTENTION: SERVER IS RESTARTING IN 1 MINUTE - GET OUT OF ANY VEHICLES NOW AND ABORT FOR YOUR CHARACTER TO BE SAFE AFTER THE RESTART");
            this.Lock();
            this.Say("ATTENTION: SERVER HAS BEEN LOCKED.");
            Thread.Sleep(1000 * 30);

            this.Say("YOU WILL BE KICKED IN 30 SECONDS TO MAKE SURE YOUR DATA IS SAVED BEFORE THE RESTART.");
            Thread.Sleep(1000 * 30);


            this.KickAll();

            // Wait 2 minutes for everything to be saved
            Thread.Sleep(1000 * 10);

            Console.WriteLine("Restarting ServerName {0} - {1}", this.ServerName, DateTime.Now);

            this.StopService();

            this.PruneLogs();

            this.SetTime();

            this.StartService();

        }

        private void SendCommand(string cmd)
        {
            var rcc = new RConClient(this.host, this.port, this.password);
            rcc.ConnectAsync().Wait();
            rcc.SendCommandAsync(cmd);
            
        }

        public void Say(string msg)
        {
            this.SendCommand("Say -1 " + msg);
        }


        public void Lock()
        {
            this.SendCommand("#lock");
        }


        public void KickAll()
        {
            var rcc = new RConClient(this.host, this.port, this.password);
            rcc.ConnectAsync().Wait();
            
            for (int i = 0; i < 100; i++)
            {
                var cmd = string.Format("kick {0} SERVER IS RESTARTING", i);
                rcc.SendCommandAsync(cmd);
            }
        }


        private void PruneLogs()
        {
            var pruner = new LogPruner(armaConfigPath);

            var mainDir = new DirectoryInfo(this.armaConfigPath);
            pruner.PruneFiles(mainDir.EnumerateFiles("*.log"));

            var beDir = new DirectoryInfo(this.armaConfigPath + "\\" + "BattlEye");
            pruner.PruneFiles(beDir.EnumerateFiles("*.log"));
        }


        private void SetTime()
        {
            var hiveConfigFilename = armaConfigPath + "\\HiveExt.ini";
            
            var thisHour = DateTime.Now.Hour;
            bool shouldBeNight = thisHour == 23 || thisHour == 0;
            string newLine = shouldBeNight ? "Hour=5\r\n" : "Hour=11\r\n";
            ReplaceInFile(hiveConfigFilename, @"^Hour=\d+$", newLine);
        }


        private void StartService()
        {
            var service = new ServiceController(serviceName);
            try
            {
                TimeSpan timeout = TimeSpan.FromMilliseconds(1000 * 60);

                service.Start();
                service.WaitForStatus(ServiceControllerStatus.Running, timeout);
            }
            catch
            {
                throw new ApplicationException("Timeout trying to start service " + this.serviceName + ".");
            }
        }


        private void StopService()
        {
            // net stop dayz1 >> "c:\server\dayz\,logs\restarts.log"
            var service = new ServiceController(this.serviceName);
            try
            {
                TimeSpan timeout = TimeSpan.FromMilliseconds(1000 * 60);

                service.Stop();
                service.WaitForStatus(ServiceControllerStatus.Stopped, timeout);
            }
            catch
            {
                throw new ApplicationException("Timeout trying to stop service " + this.serviceName + ".");
            }
        }


        /// <summary>
        /// Replaces text in a file.
        /// </summary>
        /// <param name="filePath">Path of the text file.</param>
        /// <param name="searchText">Text to search for.</param>
        /// <param name="replaceText">Text to replace the search text.</param>
        private static void ReplaceInFile(string filePath, string searchText, string replaceText)
        {
            var reader = new StreamReader(filePath);
            string content = reader.ReadToEnd();
            reader.Close();

            content = Regex.Replace(content, searchText, replaceText, RegexOptions.Multiline);

            var writer = new StreamWriter(filePath);
            writer.Write(content);
            writer.Close();
        }
    }
}