using System;
using System.Text;

namespace BESharp.Datagrams
{
    public class CommandMultiPacketResponseDatagram : CommandResponseDatagram
    {
        public CommandMultiPacketResponseDatagram(CommandResponsePartDatagram partDgram) 
        {
            this.Complete = false;
            this.IsMultipart = true;
            this.AddFirstPart(partDgram);
        }

        public bool IsMultipart { get; private set; }
        public bool Complete { get; set; }
        public int TotalParts { get; private set; }
        protected byte[][] Parts { get; set; }

        private void AddFirstPart(CommandResponsePartDatagram partDgram)
        {
            this.TotalParts = partDgram.TotalParts;
            this.Parts = new byte[this.TotalParts][];
            this.Parts[partDgram.PartNumber] = partDgram.BodyBytes;
            this.CheckForCompletion();
        }


        public void AddPart(CommandResponsePartDatagram partDgram)
        {
            if (partDgram.TotalParts != this.TotalParts)
            {
                throw new InvalidOperationException("Total parts varies in multi-part command response packet.");
            }

            this.Parts[partDgram.PartNumber] = partDgram.BodyBytes;
            this.CheckForCompletion();
        }


        private void CheckForCompletion()
        {
            var somePartMissing = false;
            for (int i = 0; i < this.TotalParts; i++)
            {
                if (this.Parts[i] == null)
                {
                    somePartMissing = true;
                }
            }

            this.Complete = !somePartMissing;
            if (this.Complete)
            {
                this.ComposeFinal();
            }
        }


        private void ComposeFinal()
        {
            var sb = new StringBuilder();
            for (int i = 0; i < this.TotalParts; i++)
            {
                sb.Append(Encoding.UTF8.GetString(this.Parts[i]));
            }
            this.Body = sb.ToString();
            this.Parts = null;
        }
    }
}