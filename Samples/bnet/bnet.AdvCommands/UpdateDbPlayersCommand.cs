// ----------------------------------------------------------------------------------------------------
// <copyright file="UpdateDbPlayersCommand.cs" company="Me">Copyright (c) 2012 St4l.</copyright>
// ----------------------------------------------------------------------------------------------------
namespace BNet.AdvCommands
{
    using System;
    using System.Data.Entity;
    using System.Linq;
    using System.Transactions;
    using BattleNET;
    using BNet.AdvCommands.misc;
    using BNet.BaseCommands;
    using BNet.IoC;
    using BNet.IoC.Data;
    using BNet.IoC.Log4Net;


    public class UpdateDbPlayersCommand : GetPlayersCommand
    {
        private static readonly object SyncRoot = new object();


        public override bool ExecAwaitResponse(BattlEyeClient beClient, int timeoutSecs = 10)
        {
            if (base.ExecAwaitResponse(beClient, timeoutSecs))
            {
                // let's do the db access one at a time shall we?
                lock (SyncRoot)
                {
                    try
                    {
                        this.UpdateDatabase();
                    }
                    catch (Exception e)
                    {
                        throw new RConException("ERROR: Could not update database.", e);
                    }
                }

                return true;
            }

            return false;
        }


        private void UpdateDatabase()
        {
            // TODO: rethrow exs using System.Runtime.ExceptionServices.ExceptionDispatchInfo
            using (var db = new BNetDb(this.Context.DbConnectionString))
            {
                db.HookSaveChanges(this.LogSql);
                int serverId = this.Context.Server.ServerId;
                using (var scope = new TransactionScope(TransactionScopeOption.RequiresNew))
                {
                    // db.dayz_clear_online(serverId);
                    // TODO: DateTimeOffset.UtcNow;
                    var now = DateTime.UtcNow;
                    var areOnlineGuids = this.Result.Select(r => r.Guid).ToList();
                    var whereOnline = from o in db.dayz_online
                                      where o.dayz_server_id == serverId && o.online == 1
                                      select o;

                    foreach (var wasOnlinePlayer in whereOnline)
                    {
                        if (areOnlineGuids.Contains(wasOnlinePlayer.guid))
                        {
                            // he's still online
                            wasOnlinePlayer.last_seen = now;
                            areOnlineGuids.Remove(wasOnlinePlayer.guid);
                        }
                        else
                        {
                            // he's gone
                            wasOnlinePlayer.online = 0;
                        }
                    }

                    var newOnline = this.Result.Where(r => areOnlineGuids.Contains(r.Guid));
                    foreach (var p in newOnline)
                    {
                        db.dayz_online.Add(
                            new dayz_online
                                {
                                    dayz_server_id = this.Context.Server.ServerId, 
                                    guid = p.Guid, 
                                    ip_address = p.IpAddress, 
                                    lobby = (byte)(p.InLobby ? 1 : 0), 
                                    name = p.Name, 
                                    ping = p.Ping, 
                                    slot = (byte)p.Id, 
                                    verified = (sbyte)(p.Verified ? 1 : 0), 
                                    first_seen = now, 
                                    last_seen = now, 
                                    online = 1
                                });
                    }

                    db.SaveChanges();
                    scope.Complete();
                }

                // ~trans()
            }

            // ~db()
        }


        private void LogSql(DbContext dbcontext, string command)
        {
            // http://www.codeproject.com/Articles/499902/Profiling-Entity-Framework-5-in-code
            // call HookSaveChanges extension method
            this.Log.Trace(command);
        }
    }
}
