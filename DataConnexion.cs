using Microsoft.Data.Sqlite;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;

namespace PKServ
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
            SELECT Id, Pseudo, Stream, Platform, PokeName, CountNormal, CountShiny, DataLastCatch, DataFirstCatch
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

                            entriesByPseudo.Add(new Entrie(id, pseudo, stream, platform, pokeName, countNormal, countShiny, last, first));
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
            using (var connection = new SqliteConnection($"Data Source={this.dataFilePath}"))
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
            using (var connection = new SqliteConnection($"Data Source={this.dataFilePath}"))
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

            using (var connection = new SqliteConnection($"Data Source={this.dataFilePath}"))
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

            using (var connection = new SqliteConnection($"Data Source={this.dataFilePath}"))
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

            using (var connection = new SqliteConnection($"Data Source={this.dataFilePath}"))
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

        internal string GetPseudoByPlatformCodeUser(User item)
        {
            string platform = item.Platform;
            string codeUser = item.Code_user;
            string pseudo = null;
            using (var connection = new SqliteConnection($"Data Source={this.dataFilePath}"))
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
            using (var connection = new SqliteConnection($"Data Source={this.dataFilePath}"))
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

            using (var connection = new SqliteConnection($"Data Source={this.dataFilePath}"))
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

            using (var connection = new SqliteConnection($"Data Source={this.dataFilePath}"))
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

            using (var connection = new SqliteConnection($"Data Source={this.dataFilePath}"))
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

            using (var connection = new SqliteConnection($"Data Source={this.dataFilePath}"))
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
                        Stat_MoneySpent = @MoneySpent
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
    }
}