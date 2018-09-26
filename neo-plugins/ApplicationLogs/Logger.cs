using Akka.Actor;
using Neo.IO;
using Neo.IO.Data.LevelDB;
using Neo.IO.Json;
using Neo.Ledger;
using Neo.VM;
using System;
using System.Linq;

namespace Neo.Plugins
{
    internal class Logger : UntypedActor
    {
        private readonly DB db;
        private readonly MongoDB mongodb;

        public Logger(IActorRef blockchain, DB db, MongoDB mongodb)
        {
            this.db = db;
            this.mongodb = mongodb;
            blockchain.Tell(new Blockchain.Register());
        }

        protected override void OnReceive(object message)
        {
            if (message is Blockchain.ApplicationExecuted e)
            {
                // 入库Block和Tx
                if (mongodb.checkBlockAndTx(e)) return;
                //
                JObject json = new JObject();
                json["txid"] = e.Transaction.Hash.ToString();
                json["executions"] = e.ExecutionResults.Select(p =>
                {
                    JObject execution = new JObject();
                    execution["trigger"] = p.Trigger;
                    execution["contract"] = p.ScriptHash.ToString();
                    execution["vmstate"] = p.VMState;
                    execution["gas_consumed"] = p.GasConsumed.ToString();
                    try
                    {
                        execution["stack"] = p.Stack.Select(q => q.ToParameter().ToJson()).ToArray();
                    }
                    catch (InvalidOperationException)
                    {
                        execution["stack"] = "error: recursive reference";
                    }
                    execution["notifications"] = p.Notifications.Select(q =>
                    {
                        JObject notification = new JObject();
                        notification["contract"] = q.ScriptHash.ToString();
                        try
                        {
                            notification["state"] = q.State.ToParameter().ToJson();
                        }
                        catch (InvalidOperationException)
                        {
                            notification["state"] = "error: recursive reference";
                        }
                        return notification;
                    }).ToArray();
                    return execution;
                }).ToArray();
                db.Put(WriteOptions.Default, e.Transaction.Hash.ToArray(), json.ToString());
                // 入库notify
                json["blockindex"] = e.ExecutionResults[0].blockindex;
                mongodb.checkNotify(json.ToString(), e.Transaction.Hash.ToString());
            }
        }

        public static Props Props(IActorRef blockchain, DB db, MongoDB mongodb)
        {
            return Akka.Actor.Props.Create(() => new Logger(blockchain, db, mongodb));
        }
    }
}
