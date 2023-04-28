using Dapper;
using Microsoft.Data.Sqlite;
using System.ComponentModel;

namespace StarRailTool;

public class DatabaseService
{


    private static readonly Lazy<DatabaseService> _lazy = new Lazy<DatabaseService>(() => new DatabaseService());


    public static DatabaseService Instance => _lazy.Value;



    private readonly string _databasePath;

    private readonly string _connectionString;





    public DatabaseService(string? databasePath = null)
    {
        if (string.IsNullOrWhiteSpace(databasePath))
        {
            _databasePath = Path.Combine(AppContext.BaseDirectory, "Database.db");
        }
        else
        {
            _databasePath = Path.GetFullPath(databasePath);
        }
        _connectionString = $"DataSource={_databasePath};";
        InitializeDatabase();
    }




    public SqliteConnection CreateConnection()
    {
        var con = new SqliteConnection(_connectionString);
        con.Open();
        return con;
    }





    private void InitializeDatabase()
    {
        using var con = CreateConnection();
        var version = con.QueryFirstOrDefault<int>("PRAGMA USER_VERSION;");
        if (version == 0)
        {
            con.Execute("PRAGMA JOURNAL_MODE = WAL;");
        }
        foreach (var sql in StructureSqls.Skip(version))
        {
            con.Execute(sql);
        }
    }



    public void AutoBackupDatabase()
    {
        try
        {
            if (AppConfig.Instance.EnableAutoBackupDatabase)
            {
                var interval = Math.Clamp(AppConfig.Instance.BackupIntervalInDays, 1, int.MaxValue);
                GetValue<string>("AutoBackupDatabase", out var lastTime);
                if ((DateTime.Now - lastTime).TotalDays > interval)
                {
                    var dir = Path.Combine(AppContext.BaseDirectory, "Backup");
                    Directory.CreateDirectory(dir);
                    var file = Path.Combine(dir, $"Database_{DateTime.Now:yyyyMMdd}.db");
                    using var backupCon = new SqliteConnection($"DataSource={file};");
                    backupCon.Open();
                    using var con = CreateConnection();
                    con.Execute("VACUUM;");
                    con.BackupDatabase(backupCon);
                    SetValue("AutoBackupDatabase", file);
                }
            }
        }
        catch { }
    }




    private class KVT
    {
        public KVT(string key, string value, DateTime time)
        {
            Key = key;
            Value = value;
            Time = time;
        }

        public string Key { get; set; }

        public string Value { get; set; }

        public DateTime Time { get; set; }
    }





    public T? GetValue<T>(string key, out DateTime time, T? defaultValue = default)
    {
        time = DateTime.MinValue;
        try
        {
            using var con = CreateConnection();
            var kvt = con.QueryFirstOrDefault<KVT>("SELECT * FROM KVT WHERE Key = @key LIMIT 1;", new { key });
            if (kvt != null)
            {
                time = kvt.Time;
                var converter = TypeDescriptor.GetConverter(typeof(T));
                if (converter == null)
                {
                    return defaultValue;
                }
                return (T?)converter.ConvertFromString(kvt.Value);
            }
            else
            {
                return defaultValue;
            }
        }
        catch
        {
            return defaultValue;
        }
    }



    public bool TryGetValue<T>(string key, out T? result, out DateTime time, T? defaultValue = default)
    {
        result = defaultValue;
        time = DateTime.MinValue;
        try
        {
            using var con = CreateConnection();
            var kvt = con.QueryFirstOrDefault<KVT>("SELECT * FROM KVT WHERE Key = @key LIMIT 1;", new { key });
            if (kvt != null)
            {
                time = kvt.Time;
                var converter = TypeDescriptor.GetConverter(typeof(T));
                if (converter == null)
                {
                    return false;
                }
                result = (T?)converter.ConvertFromString(kvt.Value);
                return true;
            }
            else
            {
                return false;
            }
        }
        catch
        {
            return false;
        }
    }



    public void SetValue<T>(string key, T value, DateTime? time = null)
    {
        try
        {
            using var con = CreateConnection();
            con.Execute("INSERT OR REPLACE INTO KVT (Key, Value, Time) VALUES (@Key, @Value, @Time);", new KVT(key, value?.ToString() ?? "", time ?? DateTime.Now));

        }
        catch { }
    }





    private static List<string> StructureSqls = new() { Structure_v1 };


    private const string Structure_v1 = """
        BEGIN TRANSACTION;

        CREATE TABLE IF NOT EXISTS GachaLogItem
        (
            Uid       INTEGER NOT NULL,
            Id        INTEGER NOT NULL,
            Name      TEXT    NOT NULL,
            Time      TEXT    NOT NULL,
            ItemId    INTEGER NOT NULL,
            ItemType  TEXT    NOT NULL,
            RankType  INTEGER NOT NULL,
            GachaType INTEGER NOT NULL,
            GachaId   INTEGER NOT NULL,
            Count     INTEGER NOT NULL,
            Lang      TEXT,
            PRIMARY KEY (Uid, Id)
        );
        CREATE INDEX IF NOT EXISTS IX_GachaLogItem_Id ON GachaLogItem (Id);
        CREATE INDEX IF NOT EXISTS IX_GachaLogItem_RankType ON GachaLogItem (RankType);
        CREATE INDEX IF NOT EXISTS IX_GachaLogItem_GachaType ON GachaLogItem (GachaType);

        CREATE TABLE IF NOT EXISTS GachaLogUrl
        (
            Uid      INTEGER NOT NULL PRIMARY KEY,
            GachaUrl TEXT    NOT NULL,
            Time     TEXT    NOT NULL
        );

        CREATE TABLE IF NOT EXISTS KVT
        (
            Key   TEXT NOT NULL PRIMARY KEY,
            Value TEXT NOT NULL,
            Time  TEXT NOT NULL
        );

        PRAGMA USER_VERSION = 1;
        COMMIT TRANSACTION;
        """;




}
