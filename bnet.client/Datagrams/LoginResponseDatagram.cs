// ----------------------------------------------------------------------------------------------------
// <copyright file="LoginResponseDatagram.cs" company="Me">Copyright (c) 2012 St4l.</copyright>
// ----------------------------------------------------------------------------------------------------
namespace BESharp.Datagrams
{
    using System;


    public class LoginResponseDatagram : InboundDatagramBase
    {
        public LoginResponseDatagram(byte[] buffer)
        {
            if (buffer == null)
            {
                throw new ArgumentNullException("buffer");
            }
            this.Success = buffer[Constants.LoginReturnCodeIndex] == 1;
        }


        public bool Success { get; private set; }

        public override DatagramType Type
        {
            get { return DatagramType.Login; }
        }
    }
}
