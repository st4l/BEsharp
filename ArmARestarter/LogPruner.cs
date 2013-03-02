using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;

namespace ArmARestarter
{
    class LogPruner
    {
        private readonly string armaConfigPath;


        public LogPruner(string armaConfigPath)
        {
            this.armaConfigPath = armaConfigPath;
        }


        public void PruneFiles(IEnumerable<FileInfo> logFiles)
        {
            foreach (FileInfo fileInfo in logFiles)
            {
                this.PruneFile(fileInfo, 3);
            }
        }


        public void PruneFile(FileInfo originalFile, int remainingMb)
        {
            if (!originalFile.Exists)
            {
                throw new FileNotFoundException("File not found", originalFile.FullName);
            }

            var size = originalFile.Length;
            var remainingSize = remainingMb * 1024 * 1024;

            if (remainingSize >= size)
            {
                // File {0} does not need pruning.
                return;
            }


            var tempFilename = originalFile.DirectoryName + "\\dummyf98vfw45.tmp";
            var tempFile = new FileInfo(tempFilename);
            if (tempFile.Exists)
            {
                tempFile.Delete();
            }

            // check if file is in use
            try
            {
                originalFile.MoveTo(tempFilename);
            }
            catch 
            {
                throw new ApplicationException("File in use.");
            }

            
            // Processing file {0}
            var dateTimeStamp = string.Format("{0:YYYYmmdd}", DateTime.Now);
            var dailyFileName = string.Format("{0}\\{1}-{2}.{3}",
                                              originalFile.DirectoryName,
                                              originalFile.Name,
                                              dateTimeStamp,
                                              ".log");
            var dailyFile = new FileInfo(dailyFileName);
            this.Tail(tempFile, originalFile, remainingMb);
            this.Head(tempFile, dailyFile, remainingMb);

            tempFile.Delete();
            // File {0} processed.

        }


        private void Tail(FileInfo sourceFile, FileInfo targetFile, int remainingMb)
        {
            // tail --bytes="%2m" tmp > file
            const string exeFile = "tail.exe";
            string args = string.Format("--bytes=\"{0}m\" \"{1}\" > \"{2}\"", remainingMb, sourceFile, targetFile);
            this.ExecuteExternalCommand(exeFile, args);
        }


        private void Head(FileInfo sourceFile, FileInfo targetFile, int remainingMb)
        {
            // head --bytes="-%2m" tmp >> dailyFile
            const string exeFile = "head.exe";
            string args = string.Format("--bytes=\"{0}m\" \"{1}\" > \"{2}\"", remainingMb, sourceFile, targetFile);
            this.ExecuteExternalCommand(exeFile, args);
        }


        private void ExecuteExternalCommand(string fileName, string arguments)
        {
            // Use ProcessStartInfo class
            var startInfo = new ProcessStartInfo
                                {
                                        CreateNoWindow = false,
                                        UseShellExecute = false,
                                        FileName = fileName,
                                        WindowStyle = ProcessWindowStyle.Hidden,
                                        Arguments = arguments
                                };

            Process exeProcess = null;
            try
            {
                // Start the process with the info we specified.
                // Call WaitForExit and then the using statement will close.
                exeProcess = Process.Start(startInfo);
                exeProcess.WaitForExit();
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message + Environment.NewLine + ex.StackTrace);
            }
            finally
            {
                if (exeProcess != null)
                {
                    exeProcess.Dispose();
                }
            }

        }
    }
}
