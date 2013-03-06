// ----------------------------------------------------------------------------------------------------
// <copyright file="EntityFrameworkHook.cs" company="Me">Copyright (c) 2012 St4l.</copyright>
// ----------------------------------------------------------------------------------------------------
namespace BNet.AdvCommands.misc
{
    using System;
    using System.Collections.Generic;
    using System.Data.Common;
    using System.Data.Entity;
    using System.Data.EntityClient;
    using System.Data.Objects;
    using System.Diagnostics.CodeAnalysis;
    using System.Globalization;
    using System.Linq;
    using System.Reflection;
    using System.Text;


    public class EntityFrameworkHook
    {
        private readonly DbContext context;

        [SuppressMessage("StyleCop.CSharp.NamingRules", "SA1309:FieldNamesMustNotBeginWithUnderscore", Justification = "Reviewed. Suppression is OK here.")]
        private readonly SaveChangesHookHandler funcDelegate;


        public EntityFrameworkHook(DbContext context, SaveChangesHookHandler funcDelegate)
        {
            this.context = context;
            this.funcDelegate = funcDelegate;

            this.SaveChanges += this.funcDelegate;

            var internalContext =
                this.context.GetType()
                    .GetProperties(BindingFlags.NonPublic | BindingFlags.Instance)
                    .Where(p => p.Name == "InternalContext")
                    .Select(p => p.GetValue(this.context, null))
                    .SingleOrDefault();

            var objectContext =
                internalContext.GetType()
                               .GetProperties(BindingFlags.Public | BindingFlags.Instance)
                               .Where(p => p.Name == "ObjectContext")
                               .Select(p => p.GetValue(internalContext, null))
                               .SingleOrDefault();

            var saveChangesEvent =
                objectContext.GetType()
                             .GetEvents(BindingFlags.Public | BindingFlags.Instance)
                             .SingleOrDefault(e => e.Name == "SavingChanges");

            var handler = Delegate.CreateDelegate(
                saveChangesEvent.EventHandlerType, this, "OnSaveChanges");
            saveChangesEvent.AddEventHandler(objectContext, handler);
        }


        public delegate void SaveChangesHookHandler(DbContext dbContext, string command);


        public event SaveChangesHookHandler SaveChanges;


        private void OnSaveChanges(object sender, EventArgs e)
        {
            var commandText = new StringBuilder();

            var conn =
                sender.GetType()
                      .GetProperties(BindingFlags.Public|BindingFlags.Instance)
                      .Where(p => p.Name == "Connection")
                      .Select(p => p.GetValue(sender, null))
                      .SingleOrDefault();

            var entityConn = (EntityConnection)conn;

            var objStateManager =
                (ObjectStateManager)
                sender.GetType()
                      .GetProperty("ObjectStateManager", BindingFlags.Instance|BindingFlags.Public)
                      .GetValue(sender, null);

            var workspace = entityConn.GetMetadataWorkspace();

            var translatorT =
                sender.GetType()
                      .Assembly.GetType("System.Data.Mapping.Update.Internal.UpdateTranslator");
            var translator = Activator.CreateInstance(
                translatorT, 
                BindingFlags.Instance|BindingFlags.NonPublic, 
                null, 
                new object[]
                    {
                       objStateManager, workspace, entityConn, entityConn.ConnectionTimeout 
                    }, 
                CultureInfo.InvariantCulture);

            var produceCommands = translator.GetType()
                                            .GetMethod(
                                                "ProduceCommands", 
                                                BindingFlags.NonPublic|BindingFlags.Instance);
            var commands = (IEnumerable<object>)produceCommands.Invoke(translator, null);

            foreach (var cmd in commands)
            {
                var identifierValues = new Dictionary<int, object>();
                var dcmd =
                    (DbCommand)
                    cmd.GetType()
                       .GetMethod("CreateCommand", BindingFlags.Instance|BindingFlags.NonPublic)
                       .Invoke(cmd, new[] { translator, identifierValues });

                foreach (DbParameter param in dcmd.Parameters)
                {
                    commandText.AppendLine(
                        string.Format(
                            "declare {0} {1} {2}", 
                            param.ParameterName, 
                            param.DbType.ToString().ToLower(), 
                            param.Size > 0 ? "(" + param.Size + ")" : string.Empty));

                    commandText.AppendLine(
                        string.Format("set {0} = '{1}'", param.ParameterName, param.Value));
                }

                commandText.AppendLine();
                commandText.AppendLine(dcmd.CommandText);
                commandText.AppendLine("go");
                commandText.AppendLine();
            }

            if (this.SaveChanges != null)
            {
                this.SaveChanges.Invoke(this.context, commandText.ToString());
            }
        }
    }
}
