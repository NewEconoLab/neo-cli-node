using Microsoft.Extensions.Configuration;
using Neo.Network.P2P;
using System.Reflection;

namespace Neo.Plugins
{
    internal class Settings
    {
        public string Path { get; }
        public DBInfoSettings dbInfo { get; }

        public static Settings Default { get; }

        static Settings()
        {
            Default = new Settings(Assembly.GetExecutingAssembly().GetConfiguration());
        }

        public Settings(IConfigurationSection section)
        {
            this.Path = string.Format(section.GetSection("Path").Value, Message.Magic.ToString("X8"));
            this.dbInfo = new DBInfoSettings(section.GetSection("DBInfo"));
        }
    }
    public class DBInfoSettings
    {
        public string dbConnStr { get; }
        public string dbDataBase { get; }
        //public string leveldbCol { get; }
        public string blockCol { get; }
        public string txCol { get; }
        public string notifyCol { get; }

        public DBInfoSettings(IConfigurationSection section)
        {
            //if (section.Value != null)
            {
                this.dbConnStr = section.GetSection("mongodbConnStr").Value;
                this.dbDataBase = section.GetSection("mongodbDataBase").Value;
                //this.leveldbCol = section.GetSection("leveldbCol").Value;
                this.blockCol = section.GetSection("blockCol").Value;
                this.txCol = section.GetSection("txCol").Value;
                this.notifyCol = section.GetSection("notifyCol").Value;
            }
        }
    }
}
