// ----------------------------------------------------------------------------------------------------
// <copyright file="LoginDatagram.cs" company="Me">Copyright (c) 2012 St4l.</copyright>
// ----------------------------------------------------------------------------------------------------
namespace BESharp.Datagrams
{
    using System;
    using System.Diagnostics.Contracts;
    using System.Text;


    public class LoginDatagram : OutboundDatagramBase
    {
        private readonly string password;


        public LoginDatagram(string password)
        {
            Contract.Assert(password != null, "password != null");
            this.password = password;
        }


        public override DatagramType Type
        {
            get { return DatagramType.Login; }
        }

        public override bool ExpectsResponse
        {
            get { return true; }
        }


        protected override byte[] BuildPayload()
        {
            var passBytes = Encoding.ASCII.GetBytes(this.password);

            var len = Buffer.ByteLength(passBytes);
            var result = new byte[len + 2];
            Buffer.SetByte(result, 0, 0xFF);
            
            // result[1] (packet type) is already initialized to 0x00;
            Buffer.BlockCopy(passBytes, 0, result, 2, len);
            return result;
        }
    }
}
