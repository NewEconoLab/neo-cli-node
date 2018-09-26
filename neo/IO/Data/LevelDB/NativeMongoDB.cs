using MongoDB.Bson;
using MongoDB.Driver;
using System.Collections.Generic;
using System.Linq;

namespace Neo.IO.Data.LevelDB
{
    class NativeMongoDB
    {
        public static void mongodb_destroy()
        {

        }
        public static void clear()
        {

        }
        public static void mongodb_del(Slice key)
        {
            MongoDBHelper.InnerDB.leveldb_DEL(key.buffer.ToHexString());
        }
        public static void mongodb_put(Slice key, Slice value)
        {
            MongoDBHelper.InnerDB.leveldb_SET(key.buffer.ToHexString(), value.buffer.ToHexString());
        }
        public static Slice mongodb_get(Slice key)
        {
            string valueStr = MongoDBHelper.InnerDB.leveldb_GET(key.buffer.ToHexString());

            return new Slice { buffer = valueStr.HexToBytes() };
        }
        public static bool mongodb_tryGet(Slice key, out Slice value)
        {
            string valueStr = MongoDBHelper.InnerDB.leveldb_GET(key.buffer.ToHexString());
            if (valueStr != null)
            {
                value = new Slice { buffer = valueStr.HexToBytes() };
                return true;
            }
            value = default(Slice);
            return false;
        }

        // 批量操作
        public void mongodb_batch_del(Slice key)
        {
            itemCache.Add(new Item { op = ItemOp.DEL, key = key });
        }
        public void mongodb_batch_put(Slice key, Slice value)
        {
            itemCache.Add(new Item { op = ItemOp.PUT, key = key, value = value });
        }
        public void commit()
        {
            int retry = 0;
            while (++retry < 4)
            {
                try
                {
                    foreach (Item item in itemCache)
                    {
                        switch (item.op)
                        {
                            case ItemOp.DEL:
                                MongoDBHelper.InnerDB.leveldb_DEL(item.key.buffer.ToHexString());
                                break;
                            case ItemOp.PUT:
                                MongoDBHelper.InnerDB.leveldb_SET(item.key.buffer.ToHexString(), item.value.buffer.ToHexString());
                                break;
                        }
                    }
                    break;
                }
                catch (System.Exception ex)
                {
                    System.IO.File.AppendAllText("mongodb.log", ex.Message + "\r\n");
                }
            }
            itemCache.Clear();
        }

        class Item
        {
            public ItemOp op { get; set; }
            public Slice key { get; set; }
            public Slice value { get; set; }
        }
        enum ItemOp : byte
        {
            PUT = 0x01,
            DEL = 0x02,
            GET = 0x03,
        }
        static List<Item> itemCache = new List<Item>();

    }
    public class MongoDBHelper
    {
        public static class InnerDB
        {
            public static string dbConnStr;
            public static string dbDatabase;
            public static string leveldbCol;
            private static bool hasIndexFlag = false;

            public static bool setIndex()
            {
                return MongoDBHelper.setIndex(dbConnStr, dbDatabase, leveldbCol, "i_key", "{'key':1}");
            }


            public static void leveldb_SET(string key, string value)
            {
                var client = new MongoClient(dbConnStr);
                var database = client.GetDatabase(dbDatabase);
                var collection = database.GetCollection<KeyValue>(leveldbCol);
                if (collection.Find(BsonDocument.Parse(toFindson(key))).CountDocuments() == 0)
                {
                    collection.InsertOne(new KeyValue { key = key, value = value });
                }
                else
                {
                    collection.ReplaceOne(BsonDocument.Parse(toFindson(key)), new KeyValue { key = key, value = value });
                }

                client = null;

                // 自动设置索引
                if (!hasIndexFlag && setIndex())
                {
                    hasIndexFlag = true;
                }
            }
            public static string leveldb_GET(string key)
            {
                var client = new MongoClient(dbConnStr);
                var database = client.GetDatabase(dbDatabase);
                var collection = database.GetCollection<KeyValue>(leveldbCol);

                string value = null;
                List<KeyValue> query = collection.Find(BsonDocument.Parse(toFindson(key))).ToList();
                if (query != null && query.Count > 0)
                {
                    value = query[0].value;
                }
                client = null;

                return value;
            }
            public static void leveldb_DEL(string key)
            {
                var client = new MongoClient(dbConnStr);
                var database = client.GetDatabase(dbDatabase);
                var collection = database.GetCollection<KeyValue>(leveldbCol);

                collection.DeleteOne(BsonDocument.Parse(toFindson(key)));

                client = null;
            }
            private static string toFindson(string key)
            {
                return "{'key':'" + key + "'}";
            }
            private static string toDocument(string key, string value)
            {
                return "{'key':'" + key + "','value':'" + value + "'}";
            }

            class KeyValue
            {
                public string key { get; set; }
                public string value { get; set; }
            }
        }


        public static long GetDataCount(string mongodbConnStr, string mongodbDatabase, string coll, string findson = "{}")
        {
            var client = new MongoClient(mongodbConnStr);
            var database = client.GetDatabase(mongodbDatabase);
            var collection = database.GetCollection<BsonDocument>(coll);

            var txCount = collection.Find(BsonDocument.Parse(findson)).CountDocuments();

            client = null;

            return txCount;
        }

        public static void GetData(string mongodbConnStr, string mongodbDatabase, string coll, string findson = "{}")
        {
            //

        }

        public static void PutData(string mongodbConnStr, string mongodbDatabase, string coll, string data, bool isAync = false)
        {
            var client = new MongoClient(mongodbConnStr);
            var database = client.GetDatabase(mongodbDatabase);
            var collection = database.GetCollection<BsonDocument>(coll);
            if (isAync)
            {
                collection.InsertOneAsync(BsonDocument.Parse(data));
            }
            else
            {
                collection.InsertOne(BsonDocument.Parse(data));
            }

            collection = null;
        }
        public static void PutData(string mongodbConnStr, string mongodbDatabase, string coll, string[] Jdata)
        {
            var client = new MongoClient(mongodbConnStr);
            var database = client.GetDatabase(mongodbDatabase);
            var collection = database.GetCollection<BsonDocument>(coll);

            collection.InsertMany(Jdata.Select(p => BsonDocument.Parse(p)).ToArray());

            client = null;
        }

        public static void DeleteData(string mongodbConnStr, string mongodbDatabase, string coll, string deleteBson)
        {
            var client = new MongoClient(mongodbConnStr);
            var database = client.GetDatabase(mongodbDatabase);
            var collection = database.GetCollection<BsonDocument>(coll);

            collection.DeleteOne(BsonDocument.Parse(deleteBson));
            client = null;

        }

        public static bool setIndex(string mongodbConnStr, string mongodbDatabase, string coll, string indexName, string indexBson)
        {
            var client = new MongoClient(mongodbConnStr);
            var database = client.GetDatabase(mongodbDatabase);
            var collection = database.GetCollection<BsonDocument>(coll);

            //检查是否已有设置idnex
            bool isSet = false;
            using (var cursor = collection.Indexes.List())
            {
                Newtonsoft.Json.Linq.JArray JAindexs = Newtonsoft.Json.Linq.JArray.Parse(cursor.ToList().ToJson());
                var query = JAindexs.Children().Where(index => (string)index["name"] == indexName);
                if (query.Count() > 0) isSet = true;
            }
            if (!isSet)
            {
                try
                {
                    var options = new CreateIndexOptions { Name = indexName, Unique = true };
                    collection.Indexes.CreateOne(new CreateIndexModel<BsonDocument>(indexBson, options));
                    return true;
                }
                catch { }
            }

            client = null;
            return false;
        }
    }
}
