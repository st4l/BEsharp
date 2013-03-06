// ----------------------------------------------------------------------------------------------------
// <copyright file="RConCommandMetadata.cs" company="Me">Copyright (c) 2012 St4l.</copyright>
// ----------------------------------------------------------------------------------------------------
namespace BNet.IoC
{
    using System.Collections.Generic;


    public class RConCommandMetadata
    {
        public RConCommandMetadata(IDictionary<string, object> metadata)
        {
            this.Name = (string)metadata["Name"];
            this.Description = (string)metadata["Description"];
        }


        public string Name { get; set; }

        public string Description { get; set; }
    }
}
