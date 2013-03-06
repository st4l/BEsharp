// ----------------------------------------------------------------------------------------------------
// <copyright file="BaseCommandsModule.cs" company="Me">Copyright (c) 2012 St4l.</copyright>
// ----------------------------------------------------------------------------------------------------
namespace BNet.BaseCommands
{
    using Autofac;
    using Autofac.Builder;
    using BNet.IoC;


    public class BaseCommandsModule : Module
    {
        protected override void Load(ContainerBuilder builder)
        {
            builder.RegisterType<GetPlayersCommand>()
                   .As<IRConCommand>()
                   .Named<IRConCommand>("getplayers")
                   .WithMetadata<RConCommandMetadata>(GetGetPlayersMetadata)
                   .PropertiesAutowired();

            builder.RegisterType<KickAllCommand>()
                   .As<IRConCommand>()
                   .Named<IRConCommand>("kickall")
                   .WithMetadata<RConCommandMetadata>(GetKickAllMetadata)
                   .PropertiesAutowired();
        }


        private static void GetGetPlayersMetadata(
            MetadataConfiguration<RConCommandMetadata> metadataConfiguration)
        {
            metadataConfiguration
                .For(am => am.Name, "getplayers")
                .For(am => am.Description, "Retrieves the list of players online.");
        }


        private static void GetKickAllMetadata(
            MetadataConfiguration<RConCommandMetadata> metadataConfiguration)
        {
            metadataConfiguration
                .For(am => am.Name, "kickall")
                .For(am => am.Description, "Kicks all players from the server.");
        }
    }
}
