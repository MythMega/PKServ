using Microsoft.Data.Sqlite;
using PKServ.Entity;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Text.Json;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace PKServ.Configuration
{
    public class DataConnexion
    {
        public string dataFilePath = "./database.sqlite";

        internal void Initialize()
        {
            if (!File.Exists(dataFilePath))
            {
                CreateDatabase();
            }
            UpdateDatabase();
        }

        private void CreateDatabase()
        {
            using var connection = new SqliteConnection($"Data Source={dataFilePath}");
            connection.Open();

            string createEntreesTable = @"
CREATE TABLE IF NOT EXISTS entrees (
    Id INTEGER PRIMARY KEY,
    Pseudo TEXT,
    CODE_USER TEXT NOT NULL DEFAULT 'unset',
    Stream TEXT,
    Platform TEXT,
    Pokename TEXT,
    CountNormal INTEGER NOT NULL DEFAULT 0,
    CountShiny INTEGER NOT NULL DEFAULT 0,
    DataLastCatch DATETIME,
    DataFirstCatch DATETIME
)";

            string createUserTable = @"
CREATE TABLE IF NOT EXISTS user (
    Id INTEGER PRIMARY KEY,
    CODE_USER TEXT NOT NULL DEFAULT 'unset',
    Pseudo TEXT,
    Platform TEXT,
    Stat_BallLaunched INTEGER NOT NULL DEFAULT 0,
    Stat_MoneySpent INTEGER NOT NULL DEFAULT 0,
    pokeReceived_normal INTEGER NOT NULL DEFAULT 0,
    pokeReceived_shiny INTEGER NOT NULL DEFAULT 0,
    pokeScrapped_normal INTEGER NOT NULL DEFAULT 0,
    pokeScrapped_shiny INTEGER NOT NULL DEFAULT 0,
    customMoney INTEGER NOT NULL DEFAULT 0
)";
            string createGlobalInfoTable = @"
CREATE TABLE IF NOT EXISTS info (
    data TEXT NOT NULL DEFAULT '',
    value TEXT NOT NULL DEFAULT ''
)";
            string createVersionLine = @"
INSERT INTO info (data, value)
VALUES ('SQLVersion', '1');";

            using (var command = new SqliteCommand(createEntreesTable, connection))
            {
                command.ExecuteNonQuery();
            }

            using (var command = new SqliteCommand(createUserTable, connection))
            {
                command.ExecuteNonQuery();
            }

            using (var command = new SqliteCommand(createGlobalInfoTable, connection))
            {
                command.ExecuteNonQuery();
            }

            using (var command = new SqliteCommand(createVersionLine, connection))
            {
                command.ExecuteNonQuery();
            }
        }

        private void UpdateDatabase()
        {
            int version = 0;
            bool updated = false;
            string currentVersion = "SQLVersion";
            string newVersion = "NewSQLVersion"; // Remplacez par la nouvelle version que vous souhaitez définir

            using var connection = new SqliteConnection($"Data Source={dataFilePath}");
            connection.Open();

            // Récupérer la version actuelle
            string selectSql = "SELECT value FROM info WHERE data = @data";
            using (var selectCommand = new SqliteCommand(selectSql, connection))
            {
                selectCommand.Parameters.AddWithValue("@data", currentVersion);
                using (var reader = selectCommand.ExecuteReader())
                {
                    if (reader.Read())
                    {
                        version = int.Parse(reader.GetString(0));
                        Console.WriteLine($"Current version: {version}");
                    }
                }
            }

            // ajout colonne Stat_tradeCount
            if (version == 1)
            {
                updated = true;
                connection.Open();

                string alterTableSql = @"
        ALTER TABLE user
        ADD COLUMN Stat_tradeCount INTEGER NOT NULL DEFAULT 0;
    ";

                using (var command = new SqliteCommand(alterTableSql, connection))
                {
                    command.ExecuteNonQuery();
                }

                version = 2;
            }

            // ajout colonne Stat_RaidCount
            if (version == 2)
            {
                updated = true;
                connection.Open();

                string alterTableSql = @"
        ALTER TABLE user
ADD COLUMN Stat_RaidCount INTEGER NOT NULL DEFAULT 0;

ALTER TABLE user
ADD COLUMN Stat_RaidTotalDmg INTEGER NOT NULL DEFAULT 0;
;
    ";

                using (var command = new SqliteCommand(alterTableSql, connection))
                {
                    command.ExecuteNonQuery();
                }

                version = 3;
            }

            // ajout colonne Stat_RaidCount
            if (version == 3)
            {
                updated = true;
                connection.Open();

                string alterTableSql = @"
    CREATE TABLE giveaway (
        ID INTEGER PRIMARY KEY,
        Usercode TEXT NOT NULL,
        Giveawaycode TEXT NOT NULL
    );
";

                using (var command = new SqliteCommand(alterTableSql, connection))
                {
                    command.ExecuteNonQuery();
                }

                version = 4;
            }
            // ajout colonne Stat_RaidCount
            if (version == 4)
            {
                updated = true;
                connection.Open();

                string alterTableSql = @"
        ALTER TABLE user
ADD COLUMN favoriteCreature TEXT NULL;
    ";

                using (var command = new SqliteCommand(alterTableSql, connection))
                {
                    command.ExecuteNonQuery();
                }

                version = 5;
            }
            // ajout colonne date dans giveaway
            if (version == 5)
            {
                updated = true;
                connection.Open();

                string alterTableSql = @"
        ALTER TABLE giveaway
ADD COLUMN date DATETIME NOT NULL;
    ";

                using (var command = new SqliteCommand(alterTableSql, connection))
                {
                    command.ExecuteNonQuery();
                }

                version = 6;
            }
            // ajout colonne avatar dans users
            if (version == 6)
            {
                updated = true;
                connection.Open();

                string alterTableSql = @"
        ALTER TABLE user
ADD COLUMN avatarUrl TEXT NULL;
    ";

                using (var command = new SqliteCommand(alterTableSql, connection))
                {
                    command.ExecuteNonQuery();
                }

                version = 7;
            }
            // ajout colonne cards dans users
            if (version == 7)
            {
                updated = true;
                connection.Open();

                string alterTableSql = @"
        ALTER TABLE user
ADD COLUMN cardsUrl TEXT NULL;
    ";

                using (var command = new SqliteCommand(alterTableSql, connection))
                {
                    command.ExecuteNonQuery();
                }

                version = 8;
            }

            // ajout table cards dans users
            if (version == 8)
            {
                updated = true;
                connection.Open();

                string alterTableSql = @"
    CREATE TABLE records (
        ID INTEGER PRIMARY KEY,
        CreatureName TEXT NOT NULL,
        Statut TEXT NOT NULL,
        Type TEXT NOT NULL,
         Date DATETIME NOT NULL
    );
    ";

                using (var command = new SqliteCommand(alterTableSql, connection))
                {
                    command.ExecuteNonQuery();
                }

                version = 9;
            }

            // Mettre à jour la version dans la base de données
            newVersion = $"{version}";
            string updateSql = "UPDATE info SET value = @newValue WHERE data = @data";
            using (var updateCommand = new SqliteCommand(updateSql, connection))
            {
                updateCommand.Parameters.AddWithValue("@newValue", newVersion);
                updateCommand.Parameters.AddWithValue("@data", currentVersion);
                updateCommand.ExecuteNonQuery();
            }
            string result = updated ? $"Database updated successfully to version {version}" : "Database already up-to-date";
            Console.WriteLine(result);
        }

        private static bool ColumnExists(SqliteConnection connection, string tableName, string columnName)
        {
            using (var command = new SqliteCommand($"PRAGMA table_info({tableName})", connection))
            {
                using (var reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        if (reader["name"].ToString() == columnName)
                        {
                            return true;
                        }
                    }
                }
            }
            return false;
        }

        public List<Entrie> GetEntriesByPseudo(string pseudoTriggered, string platformTriggered, bool includeDisabled = false)
        {
            string path = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "database.sqlite");
            List<Entrie> entriesByPseudo = new List<Entrie>();

            if (!File.Exists(path))
            {
                return entriesByPseudo;
            }

            using (SqliteConnection connection = new SqliteConnection($"Data Source={path}"))
            {
                connection.Open();
                var options = new JsonSerializerOptions
                {
                    IncludeFields = true
                };
                List<Pokemon> pokemonsEnabled = JsonSerializer.Deserialize<List<Pokemon>>(File.ReadAllText("./pokemons.json"), options).Where(w => w.enabled).ToList();
                string query = @"
            SELECT Id, Pseudo, Stream, Platform, PokeName, CountNormal, CountShiny, DataLastCatch, DataFirstCatch, CODE_USER
            FROM Entrees
            WHERE Pseudo = @Pseudo AND Platform = @Platform";

                using (SqliteCommand command = new SqliteCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@Pseudo", pseudoTriggered);
                    command.Parameters.AddWithValue("@Platform", platformTriggered);

                    using (SqliteDataReader reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            int id = reader.GetInt32(0);
                            string pseudo = reader.GetString(1);
                            string stream = reader.GetString(2);
                            string platform = reader.GetString(3);
                            string pokeName = reader.GetString(4);
                            int countNormal = reader.GetInt32(5);
                            int countShiny = reader.GetInt32(6);
                            DateTime last = reader.GetDateTime(7);
                            DateTime first = reader.GetDateTime(8);
                            string Code_User = reader.GetString(9);

                            entriesByPseudo.Add(new Entrie(id, pseudo, stream, platform, pokeName, countNormal, countShiny, last, first, Code_User));
                        }
                    }
                }
            }

            return entriesByPseudo;
        }

        public List<Entrie> GetAllEntries(bool includeDisabled = false)
        {
            string path = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "database.sqlite");
            List<Entrie> entriesByPseudo = new List<Entrie>();

            if (!File.Exists(path))
            {
                return entriesByPseudo;
            }

            using (SqliteConnection connection = new SqliteConnection($"Data Source={path}"))
            {
                connection.Open();
                var options = new JsonSerializerOptions
                {
                    IncludeFields = true
                };
                List<Pokemon> pokemonsEnabled = JsonSerializer.Deserialize<List<Pokemon>>(File.ReadAllText("./pokemons.json"), options).Where(w => w.enabled).ToList();
                string query = @"
            SELECT Id, Pseudo, Stream, Platform, PokeName, CountNormal, CountShiny, DataLastCatch, DataFirstCatch
            FROM Entrees";

                using (SqliteCommand command = new SqliteCommand(query, connection))
                {
                    using (SqliteDataReader reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            int id = reader.GetInt32(0);
                            string pseudo = reader.GetString(1);
                            string stream = reader.GetString(2);
                            string platform = reader.GetString(3);
                            string pokeName = reader.GetString(4);
                            int countNormal = reader.GetInt32(5);
                            int countShiny = reader.GetInt32(6);
                            DateTime last = reader.GetDateTime(7);
                            DateTime first = reader.GetDateTime(8);

                            entriesByPseudo.Add(new Entrie(id, pseudo, stream, platform, pokeName, countNormal, countShiny, last, first));
                        }
                    }
                }
            }

            return entriesByPseudo;
        }

        public void UpdateUserStatsMoneyBall(string pseudo, string platform, int ballsLaunched, int moneySpent, string CODE_USER)
        {
            using (var connection = new SqliteConnection($"Data Source={dataFilePath}"))
            {
                connection.Open();

                // Vérifier si l'utilisateur existe
                string checkQuery = @"
            SELECT COUNT(1)
            FROM user
            WHERE Pseudo = @Pseudo AND Platform = @Platform";

                using (var checkCommand = new SqliteCommand(checkQuery, connection))
                {
                    checkCommand.Parameters.AddWithValue("@Pseudo", pseudo);
                    checkCommand.Parameters.AddWithValue("@Platform", platform);

                    int userExists = Convert.ToInt32(checkCommand.ExecuteScalar());

                    if (userExists > 0)
                    {
                        // Mettre à jour les statistiques si l'utilisateur existe
                        string updateQuery = @"
                    UPDATE user
                    SET
                        CODE_USER = @CODE_USER,
                        Stat_BallLaunched = Stat_BallLaunched + @BallsLaunched,
                        Stat_MoneySpent = Stat_MoneySpent + @MoneySpent
                    WHERE Pseudo = @Pseudo AND Platform = @Platform";

                        using (var updateCommand = new SqliteCommand(updateQuery, connection))
                        {
                            updateCommand.Parameters.AddWithValue("@Pseudo", pseudo);
                            updateCommand.Parameters.AddWithValue("@CODE_USER", CODE_USER);
                            updateCommand.Parameters.AddWithValue("@Platform", platform);
                            updateCommand.Parameters.AddWithValue("@BallsLaunched", ballsLaunched);
                            updateCommand.Parameters.AddWithValue("@MoneySpent", moneySpent);

                            updateCommand.ExecuteNonQuery();
                        }
                    }
                    else
                    {
                        // Insérer une nouvelle ligne si l'utilisateur n'existe pas
                        string insertQuery = @"
                    INSERT INTO user (CODE_USER, Pseudo, Platform, Stat_BallLaunched, Stat_MoneySpent)
                    VALUES (@CODE_USER, @Pseudo, @Platform, @BallsLaunched, @MoneySpent)";

                        using (var insertCommand = new SqliteCommand(insertQuery, connection))
                        {
                            insertCommand.Parameters.AddWithValue("@CODE_USER", CODE_USER);
                            insertCommand.Parameters.AddWithValue("@Pseudo", pseudo);
                            insertCommand.Parameters.AddWithValue("@Platform", platform);
                            insertCommand.Parameters.AddWithValue("@BallsLaunched", ballsLaunched);
                            insertCommand.Parameters.AddWithValue("@MoneySpent", moneySpent);

                            insertCommand.ExecuteNonQuery();
                        }
                    }
                }
            }
        }

        public void UpdateUserStatsGiveaway(string pseudo, string platform, bool isShiny)
        {
            int toAddShiny = 0;
            int toAddNormal = 0;
            if (isShiny)
            {
                toAddShiny = 1;
            }
            else
            {
                toAddNormal = 1;
            }

            using (var connection = new SqliteConnection($"Data Source={dataFilePath}"))
            {
                connection.Open();

                // Vérifier si l'utilisateur existe
                string checkQuery = @"
            SELECT COUNT(1)
            FROM user
            WHERE Pseudo = @Pseudo AND Platform = @Platform";

                using (var checkCommand = new SqliteCommand(checkQuery, connection))
                {
                    checkCommand.Parameters.AddWithValue("@Pseudo", pseudo);
                    checkCommand.Parameters.AddWithValue("@Platform", platform);

                    int userExists = Convert.ToInt32(checkCommand.ExecuteScalar());

                    if (userExists > 0)
                    {
                        // Mettre à jour les statistiques si l'utilisateur existe
                        string updateQuery = @"
                    UPDATE user
                    SET
                        pokeReceived_normal = pokeReceived_normal + @normalAdded,
                        pokeReceived_shiny = pokeReceived_shiny + @shinyAdded
                    WHERE Pseudo = @Pseudo AND Platform = @Platform";

                        using (var updateCommand = new SqliteCommand(updateQuery, connection))
                        {
                            updateCommand.Parameters.AddWithValue("@Pseudo", pseudo);
                            updateCommand.Parameters.AddWithValue("@Platform", platform);
                            updateCommand.Parameters.AddWithValue("@normalAdded", toAddNormal);
                            updateCommand.Parameters.AddWithValue("@shinyAdded", toAddShiny);

                            Console.WriteLine(updateCommand.CommandText);

                            updateCommand.ExecuteNonQuery();
                        }
                    }
                    else
                    {
                        // Insérer une nouvelle ligne si l'utilisateur n'existe pas
                        string insertQuery = @"
                    INSERT INTO user (Pseudo, Platform, pokeReceived_normal, pokeReceived_shiny)
                    VALUES (@Pseudo, @Platform, @normalAdded, @shinyAdded)";

                        using (var insertCommand = new SqliteCommand(insertQuery, connection))
                        {
                            insertCommand.Parameters.AddWithValue("@Pseudo", pseudo);
                            insertCommand.Parameters.AddWithValue("@Platform", platform);
                            insertCommand.Parameters.AddWithValue("@normalAdded", toAddNormal);
                            insertCommand.Parameters.AddWithValue("@shinyAdded", toAddShiny);

                            insertCommand.ExecuteNonQuery();
                        }
                    }
                }
            }
        }

        public List<User> GetAllUserPlatforms()
        {
            List<User> userPlatforms = new List<User>();

            using (SqliteConnection connection = new SqliteConnection($"Data Source={dataFilePath}"))
            {
                connection.Open();

                string query = @"
            SELECT Pseudo, Platform, CODE_USER
            FROM user";

                using (SqliteCommand command = new SqliteCommand(query, connection))
                {
                    using (SqliteDataReader reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            string pseudo = reader.GetString(0);
                            string platform = reader.GetString(1);
                            string code = reader.GetString(2);
                            userPlatforms.Add(new User(unPseudo: pseudo, platform: platform, code_user: code, data: this));
                        }
                    }
                }
            }

            return userPlatforms;
        }

        public int GetDataUserStats_BallLaunched(string pseudo, string platform)
        {
            using (var connection = new SqliteConnection($"Data Source={dataFilePath}"))
            {
                connection.Open();

                string query = @"
            SELECT
                Stat_BallLaunched
            FROM
                user
            WHERE
                Pseudo = @Pseudo AND Platform = @Platform
            LIMIT 1";

                using (var command = new SqliteCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@Pseudo", pseudo);
                    command.Parameters.AddWithValue("@Platform", platform);

                    using (var reader = command.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            return int.Parse($"{reader["Stat_BallLaunched"]}");
                        }
                        else
                        {
                            return 0;
                        }
                    }
                }
            }
        }

        public int GetDataUserStats_MoneySpent(string pseudo, string platform)
        {
            using (var connection = new SqliteConnection($"Data Source={dataFilePath}"))
            {
                connection.Open();

                string query = @"
            SELECT
                Stat_MoneySpent
            FROM
                user
            WHERE
                Pseudo = @Pseudo AND Platform = @Platform
            LIMIT 1";

                using (var command = new SqliteCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@Pseudo", pseudo);
                    command.Parameters.AddWithValue("@Platform", platform);

                    using (var reader = command.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            return int.Parse($"{reader["Stat_MoneySpent"]}");
                        }
                        else
                        {
                            return 0;
                        }
                    }
                }
            }
        }

        public int GetDataUserStats_Giveaway(string pseudo, string platform, bool shiny)
        {
            string info = shiny ? "pokeReceived_shiny" : "pokeReceived_normal";

            using (var connection = new SqliteConnection($"Data Source={dataFilePath}"))
            {
                connection.Open();

                string query = $@"
            SELECT
                {info}
            FROM
                user
            WHERE
                Pseudo = @Pseudo AND Platform = @Platform
            LIMIT 1";

                using (var command = new SqliteCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@Pseudo", pseudo);
                    command.Parameters.AddWithValue("@Platform", platform);

                    using (var reader = command.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            return int.Parse($"{reader[info]}");
                        }
                        else
                        {
                            return 0;
                        }
                    }
                }
            }
        }

        public int GetDataUserStats_Scrap(string pseudo, string platform, bool shiny)
        {
            string info = shiny ? "pokeScrapped_shiny" : "pokeScrapped_normal";

            using (var connection = new SqliteConnection($"Data Source={dataFilePath}"))
            {
                connection.Open();

                string query = $@"
            SELECT
                {info}
            FROM
                user
            WHERE
                Pseudo = @Pseudo AND Platform = @Platform
            LIMIT 1";

                using (var command = new SqliteCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@Pseudo", pseudo);
                    command.Parameters.AddWithValue("@Platform", platform);

                    using (var reader = command.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            return int.Parse($"{reader[info]}");
                        }
                        else
                        {
                            return 0;
                        }
                    }
                }
            }
        }

        public int GetDataUserStats_Money(string pseudo, string platform)
        {
            string info = "customMoney";

            using (var connection = new SqliteConnection($"Data Source={dataFilePath}"))
            {
                connection.Open();

                string query = $@"
            SELECT
                {info}
            FROM
                user
            WHERE
                Pseudo = @Pseudo AND Platform = @Platform
            LIMIT 1";

                using (var command = new SqliteCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@Pseudo", pseudo);
                    command.Parameters.AddWithValue("@Platform", platform);

                    using (var reader = command.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            return int.Parse($"{reader[info]}");
                        }
                        else
                        {
                            return 0;
                        }
                    }
                }
            }
        }

        public int GetDataUserStats_TradeCount(User user)
        {
            string info = "Stat_tradeCount";

            using (var connection = new SqliteConnection($"Data Source={dataFilePath}"))
            {
                connection.Open();

                string query = $@"
            SELECT
                {info}
            FROM
                user
            WHERE
                Pseudo = @Pseudo AND Platform = @Platform
            LIMIT 1";

                using (var command = new SqliteCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@Pseudo", user.Pseudo);
                    command.Parameters.AddWithValue("@Platform", user.Platform);

                    using (var reader = command.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            return int.Parse($"{reader[info]}");
                        }
                        else
                        {
                            return 0;
                        }
                    }
                }
            }
        }

        public string GetDataUserStats_FavoritePoke(User user)
        {
            string info = "favoriteCreature";

            using (var connection = new SqliteConnection($"Data Source={dataFilePath}"))
            {
                connection.Open();

                string query = $@"
            SELECT
                {info}
            FROM
                user
            WHERE
                Pseudo = @Pseudo AND Platform = @Platform
            LIMIT 1";

                using (var command = new SqliteCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@Pseudo", user.Pseudo);
                    command.Parameters.AddWithValue("@Platform", user.Platform);

                    using (var reader = command.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            return reader["favoriteCreature"].ToString();
                        }
                        else
                        {
                            return "N/A";
                        }
                    }
                }
            }
        }

        public int GetDataUserStats_RaidCount(User user)
        {
            string info = "Stat_RaidCount";

            using (var connection = new SqliteConnection($"Data Source={dataFilePath}"))
            {
                connection.Open();

                string query = $@"
            SELECT
                {info}
            FROM
                user
            WHERE
                Pseudo = @Pseudo AND Platform = @Platform
            LIMIT 1";

                using (var command = new SqliteCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@Pseudo", user.Pseudo);
                    command.Parameters.AddWithValue("@Platform", user.Platform);

                    using (var reader = command.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            return int.Parse($"{reader[info]}");
                        }
                        else
                        {
                            return 0;
                        }
                    }
                }
            }
        }

        public int GetDataUserStats_RaidTotalDmg(User user)
        {
            string info = "Stat_RaidTotalDmg";

            using (var connection = new SqliteConnection($"Data Source={dataFilePath}"))
            {
                connection.Open();

                string query = $@"
            SELECT
                {info}
            FROM
                user
            WHERE
                Pseudo = @Pseudo AND Platform = @Platform
            LIMIT 1";

                using (var command = new SqliteCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@Pseudo", user.Pseudo);
                    command.Parameters.AddWithValue("@Platform", user.Platform);

                    using (var reader = command.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            return int.Parse($"{reader[info]}");
                        }
                        else
                        {
                            return 0;
                        }
                    }
                }
            }
        }

        internal string GetPseudoByPlatformCodeUser(User item)
        {
            string platform = item.Platform;
            string codeUser = item.Code_user;
            string pseudo = null;
            using (var connection = new SqliteConnection($"Data Source={dataFilePath}"))
            {
                connection.Open();
                string query = @"
                SELECT Pseudo
                FROM user
                WHERE Platform = @Platform AND CODE_USER = @CodeUser AND Pseudo IS NOT NULL
                LIMIT 1";

                using (var command = new SqliteCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@Platform", platform);
                    command.Parameters.AddWithValue("@CodeUser", codeUser);
                    using (var reader = command.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            pseudo = reader["Pseudo"].ToString();
                        }
                    }
                }
            }
            return pseudo;
        }

        internal string GetCodeUserByPlatformPseudo(User item)
        {
            string platform = item.Platform;
            string pseudo = item.Pseudo;
            string codeUser = null;
            using (var connection = new SqliteConnection($"Data Source={dataFilePath}"))
            {
                connection.Open();
                string query = @"
SELECT CODE_USER
FROM user
WHERE Platform = @Platform AND Pseudo = @Pseudo AND CODE_USER IS NOT NULL LIMIT 1";
                using (var command = new SqliteCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@Platform", platform);
                    command.Parameters.AddWithValue("@Pseudo", pseudo);
                    using (var reader = command.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            codeUser = reader["CODE_USER"].ToString();
                        }
                    }
                }
            }
            return codeUser;
        }

        internal void SetCodeUserByPlatformPseudo(User item)
        {
            string platform = item.Platform;
            string pseudo = item.Pseudo;
            string codeUser = item.Code_user;

            using (var connection = new SqliteConnection($"Data Source={dataFilePath}"))
            {
                connection.Open();
                string query = @"
        UPDATE user
        SET CODE_USER = @CodeUser
        WHERE Platform = @Platform AND Pseudo = @Pseudo";

                using (var command = new SqliteCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@CodeUser", codeUser);
                    command.Parameters.AddWithValue("@Platform", platform);
                    command.Parameters.AddWithValue("@Pseudo", pseudo);

                    command.ExecuteNonQuery();
                }
            }
        }

        internal void DeleteUser(User user)
        {
            // dans la table user
            // on essaie de trouver une ligne correspondant a un utilisateur avec ce pseudo && code user && platform
            // si ça n'en trouve aucun, trouver une ligne correspondant a un utilisateur avec juste pseudo && platform
            // supprimer la ligne qui corresponds

            string pseudo = user.Pseudo;
            string codeUser = user.Code_user ?? "unset";
            string platform = user.Platform;

            using (var connection = new SqliteConnection($"Data Source={dataFilePath}"))
            {
                connection.Open();
                string query = @"
        DELETE FROM user
        WHERE (Pseudo = @Pseudo AND CODE_USER = @CodeUser AND Platform = @Platform)
           OR (Pseudo = @Pseudo AND Platform = @Platform AND NOT EXISTS (
               SELECT 1 FROM user WHERE Pseudo = @Pseudo AND CODE_USER = @CodeUser AND Platform = @Platform))";

                using (var command = new SqliteCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@Pseudo", pseudo);
                    command.Parameters.AddWithValue("@CodeUser", codeUser);
                    command.Parameters.AddWithValue("@Platform", platform);
                    command.ExecuteNonQuery();
                }
            }
        }

        internal void DeleteAllEntries(User user)
        {
            // dans la table user
            // on essaie de trouver une ligne correspondant a un utilisateur avec ce pseudo && code user && platform
            // si ça n'en trouve aucun, trouver une ligne correspondant a un utilisateur avec juste pseudo && platform
            // supprimer la ligne qui corresponds

            string pseudo = user.Pseudo;
            string codeUser = user.Code_user ?? "unset";
            string platform = user.Platform;

            using (var connection = new SqliteConnection($"Data Source={dataFilePath}"))
            {
                connection.Open();
                string query = @"
        DELETE FROM entrees
        WHERE (Pseudo = @Pseudo AND CODE_USER = @CodeUser AND Platform = @Platform)
           OR (Pseudo = @Pseudo AND Platform = @Platform AND NOT EXISTS (
               SELECT 1 FROM entrees WHERE Pseudo = @Pseudo AND CODE_USER = @CodeUser AND Platform = @Platform))";

                using (var command = new SqliteCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@Pseudo", pseudo);
                    command.Parameters.AddWithValue("@CodeUser", codeUser);
                    command.Parameters.AddWithValue("@Platform", platform);
                    command.ExecuteNonQuery();
                }
            }
        }

        internal void DeleteEntrie(Entrie entrie)
        {
            // dans la table entrees
            // on essaie de trouver une ligne correspondant a un utilisateur avec ce pseudo && code user && platform && pokeName
            // si ça n'en trouve aucun, trouver une ligne correspondant a un utilisateur avec juste pseudo && platform && pokeName
            // supprimer la ligne qui corresponds

            string pseudo = entrie.Pseudo;
            string codeUser = entrie.code ?? "unset";
            string platform = entrie.Platform;
            string pokeName = entrie.PokeName;

            using (var connection = new SqliteConnection($"Data Source={dataFilePath}"))
            {
                connection.Open();
                string query = @"
        DELETE FROM entrees
        WHERE (Pseudo = @Pseudo AND CODE_USER = @CodeUser AND Platform = @Platform AND Pokename = @Pokename)
           OR (Pseudo = @Pseudo AND Platform = @Platform AND Pokename = @Pokename AND NOT EXISTS (
               SELECT 1 FROM entrees WHERE Pseudo = @Pseudo AND CODE_USER = @CodeUser AND Platform = @Platform AND Pokename = @Pokename))";

                using (var command = new SqliteCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@Pseudo", pseudo);
                    command.Parameters.AddWithValue("@CodeUser", codeUser);
                    command.Parameters.AddWithValue("@Platform", platform);
                    command.Parameters.AddWithValue("@Pokename", pokeName);
                    command.ExecuteNonQuery();
                }
            }
        }

        internal void UpdateUserStatsMoney(int moneyEarned, User user, string mode = "update")
        {
            using (var connection = new SqliteConnection($"Data Source={dataFilePath}"))
            {
                connection.Open();

                // Vérifier si l'utilisateur existe
                string checkQuery = @"
            SELECT COUNT(1)
            FROM user
            WHERE Pseudo = @Pseudo AND Platform = @Platform";

                using (var checkCommand = new SqliteCommand(checkQuery, connection))
                {
                    checkCommand.Parameters.AddWithValue("@Pseudo", user.Pseudo);
                    checkCommand.Parameters.AddWithValue("@Platform", user.Platform);

                    int userExists = Convert.ToInt32(checkCommand.ExecuteScalar());

                    if (userExists > 0)
                    {
                        // Mettre à jour les statistiques si l'utilisateur existe
                        string updateQuery = "";

                        switch (mode.ToLower())
                        {
                            case "add":
                                updateQuery = @"
                    UPDATE user
                    SET
                        CODE_USER = @CODE_USER,
                        customMoney = customMoney + @customMoney
                    WHERE Pseudo = @Pseudo AND Platform = @Platform";
                                break;

                            case "remove":
                                updateQuery = @"
                    UPDATE user
                    SET
                        CODE_USER = @CODE_USER,
                        customMoney = customMoney - @customMoney
                    WHERE Pseudo = @Pseudo AND Platform = @Platform";
                                break;

                            case "update":
                                updateQuery = @"
                    UPDATE user
                    SET
                        CODE_USER = @CODE_USER,
                        customMoney = @customMoney
                    WHERE Pseudo = @Pseudo AND Platform = @Platform";
                                break;
                        }

                        using (var updateCommand = new SqliteCommand(updateQuery, connection))
                        {
                            updateCommand.Parameters.AddWithValue("@Pseudo", user.Pseudo);
                            updateCommand.Parameters.AddWithValue("@CODE_USER", user.Code_user);
                            updateCommand.Parameters.AddWithValue("@Platform", user.Platform);
                            updateCommand.Parameters.AddWithValue("@customMoney", moneyEarned);

                            updateCommand.ExecuteNonQuery();
                        }
                    }
                    else
                    {
                        // Insérer une nouvelle ligne si l'utilisateur n'existe pas
                        string insertQuery = @"
                    INSERT INTO user (CODE_USER, Pseudo, Platform, customMoney)
                    VALUES (@CODE_USER, @Pseudo, @Platform, @customMoney)";

                        using (var insertCommand = new SqliteCommand(insertQuery, connection))
                        {
                            insertCommand.Parameters.AddWithValue("@Pseudo", user.Pseudo);
                            insertCommand.Parameters.AddWithValue("@CODE_USER", user.Code_user);
                            insertCommand.Parameters.AddWithValue("@Platform", user.Platform);
                            insertCommand.Parameters.AddWithValue("@customMoney", moneyEarned);

                            insertCommand.ExecuteNonQuery();
                        }
                    }
                }
            }
        }

        internal void UpdateUserStatsScrapCount(int normal, int shiny, User user)
        {
            using (var connection = new SqliteConnection($"Data Source={dataFilePath}"))
            {
                connection.Open();

                // Vérifier si l'utilisateur existe
                string checkQuery = @"
            SELECT COUNT(1)
            FROM user
            WHERE Pseudo = @Pseudo AND Platform = @Platform";

                using (var checkCommand = new SqliteCommand(checkQuery, connection))
                {
                    checkCommand.Parameters.AddWithValue("@Pseudo", user.Pseudo);
                    checkCommand.Parameters.AddWithValue("@Platform", user.Platform);

                    int userExists = Convert.ToInt32(checkCommand.ExecuteScalar());

                    if (userExists > 0)
                    {
                        // Mettre à jour les statistiques si l'utilisateur existe
                        string updateQuery = @"
                    UPDATE user
                    SET
                        CODE_USER = @CODE_USER,
                        pokeScrapped_normal = pokeScrapped_normal + @pokeScrapped_normal,
                        pokeScrapped_shiny = pokeScrapped_shiny + @pokeScrapped_shiny
                    WHERE Pseudo = @Pseudo AND Platform = @Platform";

                        using (var updateCommand = new SqliteCommand(updateQuery, connection))
                        {
                            updateCommand.Parameters.AddWithValue("@Pseudo", user.Pseudo);
                            updateCommand.Parameters.AddWithValue("@CODE_USER", user.Code_user);
                            updateCommand.Parameters.AddWithValue("@Platform", user.Platform);
                            updateCommand.Parameters.AddWithValue("@pokeScrapped_shiny", shiny);
                            updateCommand.Parameters.AddWithValue("@pokeScrapped_normal", normal);

                            updateCommand.ExecuteNonQuery();
                        }
                    }
                    else
                    {
                        // Insérer une nouvelle ligne si l'utilisateur n'existe pas
                        string insertQuery = @"
                    INSERT INTO user (CODE_USER, Pseudo, Platform, pokeScrapped_shiny, pokeScrapped_normal)
                    VALUES (@CODE_USER, @Pseudo, @Platform, @pokeScrapped_shiny, @pokeScrapped_normal)";

                        using (var insertCommand = new SqliteCommand(insertQuery, connection))
                        {
                            insertCommand.Parameters.AddWithValue("@Pseudo", user.Pseudo);
                            insertCommand.Parameters.AddWithValue("@CODE_USER", user.Code_user);
                            insertCommand.Parameters.AddWithValue("@Platform", user.Platform);
                            insertCommand.Parameters.AddWithValue("@pokeScrapped_shiny", normal);
                            insertCommand.Parameters.AddWithValue("@pokeScrapped_normal", shiny);

                            insertCommand.ExecuteNonQuery();
                        }
                    }
                }
            }
        }

        internal void UpdateUserAllStats(User user)
        {
            using (var connection = new SqliteConnection($"Data Source={dataFilePath}"))
            {
                connection.Open();

                // Vérifier si l'utilisateur existe
                string checkQuery = @"
            SELECT COUNT(1)
            FROM user
            WHERE Pseudo = @Pseudo AND Platform = @Platform";

                using (var checkCommand = new SqliteCommand(checkQuery, connection))
                {
                    checkCommand.Parameters.AddWithValue("@Pseudo", user.Pseudo);
                    checkCommand.Parameters.AddWithValue("@Platform", user.Platform);

                    int userExists = Convert.ToInt32(checkCommand.ExecuteScalar());

                    if (userExists > 0)
                    {
                        // Mettre à jour les statistiques si l'utilisateur existe
                        string updateQuery = @"
                    UPDATE user
                    SET
                        pokeReceived_normal = @normalAdded,
                        pokeReceived_shiny = @shinyAdded,
                        Stat_BallLaunched = @ballLaunched,
                        pokeScrapped_normal = @normalScrapped,
                        pokeScrapped_shiny = @shinyScrapped,
                        customMoney = @customMoney,
                        Stat_MoneySpent = @MoneySpent,
                        Stat_tradeCount = @Stat_tradeCount,
                        Stat_RaidCount = @Stat_RaidCount,
                        favoriteCreature = @favoriteCreature,
                        Stat_RaidTotalDmg = @Stat_RaidTotalDmg
                    WHERE Pseudo = @Pseudo AND Platform = @Platform";

                        using (var updateCommand = new SqliteCommand(updateQuery, connection))
                        {
                            updateCommand.Parameters.AddWithValue("@Pseudo", user.Pseudo);
                            updateCommand.Parameters.AddWithValue("@Platform", user.Platform);
                            updateCommand.Parameters.AddWithValue("@normalAdded", user.Stats.giveawayNormal);
                            updateCommand.Parameters.AddWithValue("@shinyAdded", user.Stats.giveawayShiny);
                            updateCommand.Parameters.AddWithValue("@ballLaunched", user.Stats.ballLaunched);
                            updateCommand.Parameters.AddWithValue("@MoneySpent", user.Stats.moneySpent);
                            updateCommand.Parameters.AddWithValue("@normalScrapped", user.Stats.scrappedNormal);
                            updateCommand.Parameters.AddWithValue("@shinyScrapped", user.Stats.scrappedShiny);
                            updateCommand.Parameters.AddWithValue("@customMoney", user.Stats.CustomMoney);
                            updateCommand.Parameters.AddWithValue("@Stat_tradeCount", user.Stats.TradeCount);
                            updateCommand.Parameters.AddWithValue("@Stat_RaidTotalDmg", user.Stats.RaidTotalDmg);
                            updateCommand.Parameters.AddWithValue("@Stat_RaidCount", user.Stats.RaidCount);
                            updateCommand.Parameters.AddWithValue("@favoriteCreature", user.Stats.favoritePoke);

                            Console.WriteLine(updateCommand.CommandText);

                            updateCommand.ExecuteNonQuery();
                        }
                    }
                    else
                    {
                        Console.WriteLine("ERROR WHILE MERGING IN DATABASE");
                        throw new Exception("ERROR WHILE MERGING IN DATABASE");
                    }
                }
            }
        }

        public List<Giveaway> GetGiveawayUser(AppSettings settings, User user)
        {
            List<string> codeList = new List<string>();
            List<Giveaway> gives = new List<Giveaway>();

            using (var connection = new SqliteConnection($"Data Source={dataFilePath}"))
            {
                connection.Open();
                string query = @"
SELECT Giveawaycode
FROM giveaway
WHERE Usercode = @Usercode";

                using (var command = new SqliteCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@Usercode", user.Code_user);

                    using (var reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            codeList.Add(reader["Giveawaycode"].ToString());
                        }
                    }
                }
            }

            foreach (string code in codeList)
            {
                Giveaway gAway = settings.giveaways.FirstOrDefault(x => x.Code == code);
                if (gAway != null)
                {
                    gives.Add(gAway);
                }
            }

            return gives;
        }

        internal void RegisterGiveaway(GiveawayClaim giveawayInfos)
        {
            using var connection = new SqliteConnection($"Data Source={dataFilePath}");
            connection.Open();

            string alterTableSql = @"
        INSERT INTO giveaway (Usercode, Giveawaycode, date)
        VALUES (@Usercode, @Giveawaycode, datetime('now'));
    ";

            using (var command = new SqliteCommand(alterTableSql, connection))
            {
                command.Parameters.AddWithValue("@Usercode", giveawayInfos.User.Code_user);
                command.Parameters.AddWithValue("@Giveawaycode", giveawayInfos.Code);

                command.ExecuteNonQuery();
            }

            connection.Close();
        }

        internal void UpdateAvatar(UserRequest ctx)
        {
            using var connection = new SqliteConnection($"Data Source={dataFilePath}");
            connection.Open();

            string updateQuery = @"
                    UPDATE user
                    SET
                        avatarUrl = @AVATAR_URL
                    WHERE CODE_USER = @CODE_USER";

            using (var command = new SqliteCommand(updateQuery, connection))
            {
                command.Parameters.AddWithValue("@CODE_USER", ctx.UserCode);
                command.Parameters.AddWithValue("@AVATAR_URL", ctx.avatarUrl);

                command.ExecuteNonQuery();
            }

            connection.Close();
        }

        internal void UpdateCardBackground(User user, string url)
        {
            using var connection = new SqliteConnection($"Data Source={dataFilePath}");
            connection.Open();

            string updateQuery = @"
                    UPDATE user
                    SET
                        cardsUrl = @CARD_URL
                    WHERE CODE_USER = @CODE_USER";

            using (var command = new SqliteCommand(updateQuery, connection))
            {
                command.Parameters.AddWithValue("@CODE_USER", user.Code_user);
                command.Parameters.AddWithValue("@CARD_URL", url);

                command.ExecuteNonQuery();
            }

            connection.Close();
        }

        internal void UpdateFavCreature(User user, string pokename)
        {
            using var connection = new SqliteConnection($"Data Source={dataFilePath}");
            connection.Open();

            string updateQuery = @"
                    UPDATE user
                    SET
                        favoriteCreature = @FAV_CREATURE
                    WHERE CODE_USER = @CODE_USER";

            using (var command = new SqliteCommand(updateQuery, connection))
            {
                command.Parameters.AddWithValue("@CODE_USER", user.Code_user);
                command.Parameters.AddWithValue("@FAV_CREATURE", pokename);

                command.ExecuteNonQuery();
            }

            connection.Close();
        }

        public string GetAvatarUrl(User user)
        {
            using var connection = new SqliteConnection($"Data Source={dataFilePath}");
            connection.Open();

            string query = @"
            SELECT
                avatarUrl
            FROM
                user
            WHERE
                CODE_USER = @CODE_USER
            LIMIT 1";

            using (var command = new SqliteCommand(query, connection))
            {
                command.Parameters.AddWithValue("@CODE_USER", user.Code_user);

                using (var reader = command.ExecuteReader())
                {
                    if (reader.Read())
                    {
                        return reader["avatarUrl"].ToString();
                    }
                    else
                    {
                        return null;
                    }
                }
            }
        }

        public string? GetCardBackgroundUrl(User user)
        {
            using var connection = new SqliteConnection($"Data Source={dataFilePath}");
            connection.Open();

            string query = @"
            SELECT
                cardsUrl
            FROM
                user
            WHERE
                CODE_USER = @CODE_USER
            LIMIT 1";

            using (var command = new SqliteCommand(query, connection))
            {
                command.Parameters.AddWithValue("@CODE_USER", user.Code_user);

                using (var reader = command.ExecuteReader())
                {
                    if (reader.Read())
                    {
                        return reader["cardsUrl"].ToString();
                    }
                    else
                    {
                        return null;
                    }
                }
            }
        }

        internal string GetSpriteFavoriteCreature(User user, AppSettings settings)
        {
            using var connection = new SqliteConnection($"Data Source={dataFilePath}");
            connection.Open();

            string query = @"
            SELECT
                favoriteCreature
            FROM
                user
            WHERE
                CODE_USER = @CODE_USER
            LIMIT 1";

            string data = string.Empty;

            using (var command = new SqliteCommand(query, connection))
            {
                command.Parameters.AddWithValue("@CODE_USER", user.Code_user);

                using (var reader = command.ExecuteReader())
                {
                    if (reader.Read())
                    {
                        if (reader["favoriteCreature"].ToString() is null || reader["favoriteCreature"].ToString().Length < 2)
                            return "";
                        data = reader["favoriteCreature"].ToString();
                        if (data.Split('#').Count() < 2)
                        {
                            return null;
                        }
                        bool shiny = data.Split('#')[1] == "s";
                        data = data.Split('#')[0];
                        Pokemon poke = settings.pokemons.FirstOrDefault(x => Commun.isSamePoke(x, data));
                        if (poke != null)
                        {
                            if (shiny)
                            {
                                return poke.Sprite_Shiny;
                            }
                            else
                            {
                                return poke.Sprite_Normal;
                            }
                        }
                        else
                        {
                            return null;
                        }
                    }
                    else
                    {
                        return null;
                    }
                }
            }
        }

        public List<Records> GetRecords()
        {
            List<Records> records = new List<Records>();

            using (SqliteConnection connection = new SqliteConnection($"Data Source={dataFilePath}"))
            {
                connection.Open();

                string query = @"
            SELECT ID, CreatureName, Statut, Type, Date
            FROM records";

                using (SqliteCommand command = new SqliteCommand(query, connection))
                {
                    using (SqliteDataReader reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            int ID = reader.GetInt32(0);
                            string CreatureName = reader.GetString(1);
                            string Statut = reader.GetString(2);
                            string Type = reader.GetString(3);
                            DateTime Date = reader.GetDateTime(4);
                            records.Add(new Records(ID: ID, creatureName: CreatureName, statut: Statut, type: Type, date: Date));
                        }
                    }
                }
            }

            return records;
        }

        public void AddRecord(Records record)
        {
            using (var connection = new SqliteConnection($"Data Source={dataFilePath}"))
            {
                connection.Open();

                string insertQuery = @"
                    INSERT INTO records (CreatureName, Statut, Type, Date)
                    VALUES (@CreatureName, @Statut, @Type, @Date)";

                using (var insertCommand = new SqliteCommand(insertQuery, connection))
                {
                    insertCommand.Parameters.AddWithValue("@CreatureName", record.CreatureName);
                    insertCommand.Parameters.AddWithValue("@Statut", record.Statut);
                    insertCommand.Parameters.AddWithValue("@Type", record.Type);
                    insertCommand.Parameters.AddWithValue("@Date", record.Date);

                    insertCommand.ExecuteNonQuery();
                }
            }
        }
    }
}