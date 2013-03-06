// ----------------------------------------------------------------------------------------------------
// <copyright file="Extensions.cs" company="Me">Copyright (c) 2012 St4l.</copyright>
// ----------------------------------------------------------------------------------------------------
namespace BNet.AdvCommands.misc
{
    using System.Data.Entity;


    public static class Extensions
    {
        public static void HookSaveChanges(
            this DbContext dbContext, EntityFrameworkHook.SaveChangesHookHandler funcDelegate)
        {
            new EntityFrameworkHook(dbContext, funcDelegate);
        }
    }
}
