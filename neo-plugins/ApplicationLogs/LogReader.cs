using Microsoft.AspNetCore.Http;
using Neo.IO.Data.LevelDB;
using Neo.IO.Json;
using Neo.Network.RPC;

namespace Neo.Plugins
{
    public class LogReader : Plugin, IRpcPlugin
    {
        private readonly DB db = DB.Open(Settings.Default.Path, new Options { CreateIfMissing = true });
        private readonly MongoDB mongodb = MongoDB.Create(
            Settings.Default.dbInfo.dbConnStr,
            Settings.Default.dbInfo.dbDataBase,
            Settings.Default.dbInfo.blockCol,
            Settings.Default.dbInfo.txCol,
            Settings.Default.dbInfo.notifyCol);
        public override string Name => "ApplicationLogs";

        public LogReader()
        {
            System.ActorSystem.ActorOf(Logger.Props(System.Blockchain, db, mongodb));
        }

        public JObject OnProcess(HttpContext context, string method, JArray _params)
        {
            if (method != "getapplicationlog") return null;
            UInt256 hash = UInt256.Parse(_params[0].AsString());
            if (!db.TryGet(ReadOptions.Default, hash.ToArray(), out Slice value))
                throw new RpcException(-100, "Unknown transaction");
            return JObject.Parse(value.ToString());
        }
    }
}
