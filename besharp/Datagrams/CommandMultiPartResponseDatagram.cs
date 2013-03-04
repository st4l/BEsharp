// ----------------------------------------------------------------------------------------------------
// <copyright file="CommandMultiPartResponseDatagram.cs" company="Me">Copyright (c) 2013 St4l.</copyright>
// ----------------------------------------------------------------------------------------------------
namespace BESharp.Datagrams
{
    using System;
    using System.Text;

    internal class CommandMultiPartResponseDatagram : CommandResponseDatagram
    {
        private byte[][] parts;

        internal CommandMultiPartResponseDatagram(CommandResponsePartDatagram partDatagram)
        {
            this.IsComplete = false;
            this.IsMultipart = true;
            this.AddFirstPart(partDatagram);
        }


        public bool IsMultipart { get; private set; }

        public bool IsComplete { get; set; }

        public int TotalParts { get; private set; }


        public void AddPart(CommandResponsePartDatagram partDatagram)
        {
            if (partDatagram == null)
            {
                throw new ArgumentNullException("partDatagram");
            }
            

            if (partDatagram.TotalParts != this.TotalParts)
            {
                throw new InvalidOperationException("Total parts varies in multi-part command response datagram.");
            }

            this.parts[partDatagram.PartNumber] = partDatagram.GetBytes();
            this.CheckForCompletion();
        }


        private void AddFirstPart(CommandResponsePartDatagram partDgram)
        {
            this.TotalParts = partDgram.TotalParts;
            this.parts = new byte[this.TotalParts][];
            this.parts[partDgram.PartNumber] = partDgram.GetBytes();
            this.CheckForCompletion();
        }


        private void CheckForCompletion()
        {
            bool somePartMissing = false;
            for (int i = 0; i < this.TotalParts; i++)
            {
                if (this.parts[i] == null)
                {
                    somePartMissing = true;
                }
            }

            this.IsComplete = !somePartMissing;
            if (this.IsComplete)
            {
                this.ComposeFinal();
            }
        }


        private void ComposeFinal()
        {
            var sb = new StringBuilder();
            for (int i = 0; i < this.TotalParts; i++)
            {
                sb.Append(Encoding.UTF8.GetString(this.parts[i]));
            }

            this.Body = sb.ToString();
            this.parts = null;
        }
    }
}
