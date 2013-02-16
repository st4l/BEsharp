using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ArmARestarter
{
    class LogPruner
    {
        private readonly string armaConfigPath;


        public LogPruner(string armaConfigPath)
        {
            this.armaConfigPath = armaConfigPath;
        }


        public void Prune(FileInfo originalFile, int remainingMb)
        {
            if (!originalFile.Exists)
            {
                throw new FileNotFoundException("File not found", originalFile.FullName);
            }

            var size = originalFile.Length;
            var remainingSize = remainingMb * 1024 * 1024;
            var removeSize = size - remainingSize;

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

            throw new NotImplementedException();
        }


        private void Head(FileInfo sourceFile, FileInfo targetFile, int remainingMb)
        {
            // head --bytes="-%2m" tmp >> dailyFile

            throw new NotImplementedException();
        }


        public void PruneAll()
        {
            var mainDir = new DirectoryInfo(this.armaConfigPath);
            this.PruneDirectory(mainDir);

            var beDir = new DirectoryInfo(this.armaConfigPath + "\\" + "BattlEye");
            this.PruneDirectory(beDir);
        }


        private void PruneDirectory(DirectoryInfo directory)
        {
            var logFiles = directory.EnumerateFiles("*.log");
            foreach (FileInfo fileInfo in logFiles)
            {
                this.Prune(fileInfo, 3);
            }
        }
    }
}
