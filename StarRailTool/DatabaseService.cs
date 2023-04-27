using Dapper;
using Microsoft.Data.Sqlite;

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

        PRAGMA USER_VERSION =1;
        COMMIT TRANSACTION;
        """;




}
