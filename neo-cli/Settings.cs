﻿using Microsoft.Extensions.Configuration;
using Neo.Network;

namespace Neo
{
    internal class Settings
    {
        public PathsSettings Paths { get; }
        public P2PSettings P2P { get; }
        public RPCSettings RPC { get; }
        public UnlockWalletSettings UnlockWallet { get; set; }
        public DBInfoSettings dbInfo { get; set; }

        public static Settings Default { get; }

        static Settings()
        {
            IConfigurationSection section = new ConfigurationBuilder().AddJsonFile("config.json").Build().GetSection("ApplicationConfiguration");
            Default = new Settings(section);
        }

        public Settings(IConfigurationSection section)
        {
            this.Paths = new PathsSettings(section.GetSection("Paths"));
            this.P2P = new P2PSettings(section.GetSection("P2P"));
            this.RPC = new RPCSettings(section.GetSection("RPC"));
            this.UnlockWallet = new UnlockWalletSettings(section.GetSection("UnlockWallet"));
            this.dbInfo = new DBInfoSettings(section.GetSection("DBInfo"));
        }
    }

    internal class PathsSettings
    {
        public string Chain { get; }
        public string ApplicationLogs { get; }

        public PathsSettings(IConfigurationSection section)
        {
            this.Chain = string.Format(section.GetSection("Chain").Value, Message.Magic.ToString("X8"));
            this.ApplicationLogs = string.Format(section.GetSection("ApplicationLogs").Value, Message.Magic.ToString("X8"));
        }
    }

    internal class P2PSettings
    {
        public ushort Port { get; }
        public ushort WsPort { get; }

        public P2PSettings(IConfigurationSection section)
        {
            this.Port = ushort.Parse(section.GetSection("Port").Value);
            this.WsPort = ushort.Parse(section.GetSection("WsPort").Value);
        }
    }

    internal class RPCSettings
    {
        public ushort Port { get; }
        public string SslCert { get; }
        public string SslCertPassword { get; }

        public RPCSettings(IConfigurationSection section)
        {
            this.Port = ushort.Parse(section.GetSection("Port").Value);
            this.SslCert = section.GetSection("SslCert").Value;
            this.SslCertPassword = section.GetSection("SslCertPassword").Value;
        }
    }

    public class UnlockWalletSettings
    {
        public string Path { get; }
        public string Password { get; }
        public bool StartConsensus { get; }
        public bool IsActive { get; }

        public UnlockWalletSettings(IConfigurationSection section)
        {
            if (section.Value != null)
            {
                this.Path = section.GetSection("WalletPath").Value;
                this.Password = section.GetSection("WalletPassword").Value;
                this.StartConsensus = bool.Parse(section.GetSection("StartConsensus").Value);
                this.IsActive = bool.Parse(section.GetSection("IsActive").Value);
            }
        }
    }

    public class DBInfoSettings
    {
        public string dbConnStr { get; }
        public string dbDataBase { get; }
        public string leveldbCol { get; }
        public string blockCol { get; }
        public string txCol { get; }
        public string notifyCol { get; }
        
        public DBInfoSettings(IConfigurationSection section)
        {
            //if (section.Value != null)
            {
                this.dbConnStr = section.GetSection("mongodbConnStr").Value;
                this.dbDataBase = section.GetSection("mongodbDataBase").Value;
                this.leveldbCol = section.GetSection("leveldbCol").Value;
                this.blockCol =section.GetSection("blockCol").Value;
                this.txCol =section.GetSection("txCol").Value;
                this.notifyCol = section.GetSection("notifyCol").Value;
            }
        }
    }
}
