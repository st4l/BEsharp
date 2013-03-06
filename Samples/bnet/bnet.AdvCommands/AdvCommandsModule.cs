// ----------------------------------------------------------------------------------------------------
// <copyright file="AdvCommandsModule.cs" company="Me">Copyright (c) 2012 St4l.</copyright>
// ----------------------------------------------------------------------------------------------------
namespace BNet.AdvCommands
{
    using Autofac;
    using Autofac.Builder;
    using BNet.IoC;


    public class AdvCommandsModule : Module
    {
        protected override void Load(ContainerBuilder builder)
        {
            builder.RegisterType<UpdateDbPlayersCommand>()
                   .As<IRConCommand>()
                   .Named<IRConCommand>("update_dbplayers")
                   .WithMetadata<RConCommandMetadata>(GetUpdateDbPlayersMetadata)
                   .PropertiesAutowired();
        }


        private static void GetUpdateDbPlayersMetadata(
            MetadataConfiguration<RConCommandMetadata> metadataConfiguration)
        {
            metadataConfiguration
                .For(am => am.Name, "update_dbplayers")
                .For(am => am.Description, "Updates online players in the database.");
        }
    }
}
