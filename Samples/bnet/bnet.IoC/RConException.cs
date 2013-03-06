// ----------------------------------------------------------------------------------------------------
// <copyright file="RConException.cs" company="Me">Copyright (c) 2012 St4l.</copyright>
// ----------------------------------------------------------------------------------------------------
namespace BNet.IoC
{
    using System;


    public class RConException : ApplicationException
    {
        public RConException(string message)
            : base(message)
        {
        }


        public RConException(string message, Exception innerException)
            : base(message, innerException)
        {
        }
    }
}
