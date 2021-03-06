namespace DirkSarodnick.TS3_Bot.Core.Repository
{
    using Entity;
    using System;
    using System.Collections.Generic;
    using System.Configuration;
    using System.Data;
    using System.Data.SQLite;
    using System.IO;
    using System.Linq;
    using DirkSarodnick.TS3_Bot.Core.Helper;
    using TS3QueryLib.Core.Server.Entities;
    using TS3QueryLib.Core.Server.Responses;

    public class DataContainer : IDisposable
    {
        private bool disposed;
        private readonly string instanceName;

        /// <summary>
        /// Initializes a new instance of the <see cref="DataContainer"/> class.
        /// </summary>
        /// <param name="name">The name.</param>
        public DataContainer(string name)
        {
            this.instanceName = name;
        }

        #region Specified Data

        private SQLiteConnection databaseConnection;
        internal SQLiteConnection DatabaseConnection
        {
            get
            {
                lock (lockDatabase)
                {
                    if (databaseConnection == null)
                    {
                        if (!Directory.Exists(".\\Data\\")) Directory.CreateDirectory(".\\Data\\");
                        if (!File.Exists(".\\Data\\" + instanceName + ".s3db")) File.Copy(".\\BotDatabase.s3db", ".\\Data\\" + instanceName + ".s3db");

                        databaseConnection = new SQLiteConnection(ConfigurationManager.ConnectionStrings["BotDatabase"].ConnectionString.Replace(".\\BotDatabase.s3db", ".\\Data\\" + instanceName + ".s3db"));
                        databaseConnection.Open();
                    }

                    return this.databaseConnection;
                }
            }
        }

        internal Dictionary<uint, ClientServerGroupList> ClientServerGroupList = new Dictionary<uint, ClientServerGroupList>();
        internal List<ClientWarningEntity> ClientWarningList = new List<ClientWarningEntity>();

        #endregion

        #region Basic Data

        internal DateTime Now;

        internal int LastConnectionWaiting = 1;
        internal DateTime LastConnectionError = DateTime.UtcNow;

        internal List<ClientListEntry> ClientList;
        internal Dictionary<uint, ClientInfoResponse> ClientInfoList = new Dictionary<uint, ClientInfoResponse>();
        internal List<ChannelListEntry> ChannelList;
        internal Dictionary<uint, ChannelInfoResponse> ChannelInfoList = new Dictionary<uint, ChannelInfoResponse>();
        internal Dictionary<uint, ClientDbEntry> ClientDatabaseList = new Dictionary<uint, ClientDbEntry>();
        internal List<FileEntity> FileList = new List<FileEntity>();
        internal List<ComplainListEntry> CompliantList;
        internal List<ServerListItem> ServerList;
        internal Dictionary<Guid, DateTime> LastIntervalList = new Dictionary<Guid, DateTime>();

        #endregion

        #region Locks

        internal readonly object lockNow = new object();
        internal readonly object lockDatabase = new object();
        internal readonly object lockGetClientInfo = new object();
        internal readonly object lockGetClientList = new object();
        internal readonly object lockGetClientsFromDatabase = new object();
        internal readonly object lockGetClientFromDatabase = new object();
        internal readonly object lockGetRawClientsFromDatabase = new object();
        internal readonly object lockGetChannelInfo = new object();
        internal readonly object lockGetChannelList = new object();
        internal readonly object lockGetClientServerGroups = new object();
        internal readonly object lockClientWarningList = new object();

        internal readonly object lockClientLastChannelList = new object();
        internal readonly object lockStickyClientList = new object();
        internal readonly object lockVotedClientList = new object();
        internal readonly object lockSeenClientList = new object();
        internal readonly object lockModeratedClientList = new object();
        internal readonly object lockTimeClientList = new object();
        internal readonly object lockPreviousServerGroupsList = new object();

        internal readonly object lockFileList = new object();
        internal readonly object lockGetCompliantList = new object();
        internal readonly object lockGetServerList = new object();
        internal readonly object lockGetServerGroup = new object();
        internal readonly object lockLastEventList = new object();

        #endregion

        #region Public Methods

        /// <summary>
        /// Releases unmanaged and - optionally - managed resources
        /// </summary>
        public void Clean()
        {
            lock (lockNow) Now = default(DateTime);
            lock (lockGetClientList) ClientList = null;
            lock (lockGetChannelList) ChannelList = null;
            lock (lockGetCompliantList) CompliantList = null;
            lock (lockGetServerList) ServerList = null;
            lock (lockGetClientInfo) ClientInfoList.Clear();
            lock (lockGetChannelInfo) ChannelInfoList.Clear();

            lock (lockClientWarningList) ClientWarningList.RemoveAll(m => m.Creation.AddMinutes(5) < DateTime.UtcNow);

            lock (lockDatabase)
            {
                try
                {
                    using (var command = new SQLiteCommand(this.DatabaseConnection))
                    {
                        command.CommandText = "DELETE FROM Sticky WHERE CreationDate + (StickMinutes * 60) < " + DateTime.UtcNow.ToTimeStamp();
                        command.ExecuteNonQuery();
                    }

                    using (var command = new SQLiteCommand(this.DatabaseConnection))
                    {
                        command.CommandText = "DELETE FROM Vote WHERE CreationDate + (1 * 60 * 60) < " + DateTime.UtcNow.ToTimeStamp();
                        command.ExecuteNonQuery();
                    }
                }
                catch (Exception exception)
                {
                    throw exception.InnerException ?? exception;
                }
            }
        }

        /// <summary>
        /// Releases unmanaged and - optionally - managed resources
        /// </summary>
        public void SlowClean()
        {
            lock (lockGetClientsFromDatabase) ClientDatabaseList.Clear();
        }

        #endregion

        /// <summary>
        /// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
        /// </summary>
        public void Dispose()
        {
            if (disposed) return;
            disposed = true;

            DatabaseConnection.Close();
            DatabaseConnection.Dispose();
            ClientList = null;
            ClientInfoList = null;
            ChannelList = null;
            ChannelInfoList = null;
            ClientDatabaseList = null;
            FileList = null;
            CompliantList = null;
            ServerList = null;
            LastIntervalList = null;
            ClientServerGroupList = null;
            ClientWarningList = null;

            GC.SuppressFinalize(this);
        }
    }
}