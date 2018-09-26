using Neo.IO.Data.LevelDB;
using Neo.IO.Json;
using Neo.Ledger;
using Neo.Network.P2P.Payloads;
using System.Linq;

namespace Neo.Plugins
{
    class MongoDB
    {
        public static MongoDB Create(string dbConnStr, string dbDatabase, string blockCol, string txCol, string notifyCol)
        {
            return new MongoDB() {
                dbConnStr = dbConnStr,
                dbDatabase = dbDatabase,
                blockCol = blockCol,
                txCol = txCol,
                notifyCol = notifyCol
            };
        }
        private string dbConnStr;
        private string dbDatabase;
        private string blockCol;
        private string txCol;
        private string notifyCol;

        public void checkNotify(string data, string txid)
        {
            // 入库Notify
            //if(isNotEmpty(notifyCol) && MongoDBHelper.GetDataCount(dbConnStr, dbDatabase, notifyCol, "{txid:'"+txid+"'}") <= 0)
            {
                MongoDBHelper.PutData(dbConnStr, dbDatabase, notifyCol, data);
            }
        }
        public bool checkBlockAndTx(Blockchain.ApplicationExecuted e)
        {
            string data = null;
            if (e.block != null)
            {
                long index = e.block.Index;
                data = e.block.ToJson().ToString();
                
                // 入库Block
                if (isNotEmpty(blockCol) && MongoDBHelper.GetDataCount(dbConnStr, dbDatabase, blockCol, "{'index':" + e.block.Index + "}") <= 0)
                {
                    MongoDBHelper.PutData(dbConnStr, dbDatabase, blockCol, data);
                }
                // 入库Tx
                if (isNotEmpty(txCol) && MongoDBHelper.GetDataCount(dbConnStr, dbDatabase, txCol, "{'blockindex':" + e.block.Index + "}") <= 0)
                {
                    MongoDBHelper.PutData(dbConnStr, dbDatabase, txCol, toJArray(e.block.Transactions, e.block.Index));
                }
                return true;
            }
            return false;
        }
        private bool isNotEmpty(string str)
        {
            return str != null && str.Trim().Length != 0;
        }
        private string[] toJArray(Transaction[] Transactions, long blockindex)
        {
            return Transactions.Select(p => {
                JObject jo = p.ToJson();
                jo["blockindex"] = blockindex;
                return jo.ToString();
            }).ToArray();
        }
    }
}
