using Microsoft.Data.Sqlite;
using PKServ.Entity;
using PKServ.Entity._DATA;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;

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

            // ajout table cards dans users
            if (version == 9)
            {
                updated = true;

                string alterTableSql = @"
    ALTER TABLE user
    ADD COLUMN selectedZone TEXT NOT NULL DEFAULT '<void>';
";

                using (var command = new SqliteCommand(alterTableSql, connection))
                {
                    command.ExecuteNonQuery();
                }

                version = 10;
            }

            // OPTIM P3 : ajout de la contrainte UNIQUE(Pseudo, Platform) sur la table user.
            // Cette contrainte est requise par les upserts ON CONFLICT utilisés dans
            // UpdateUserStatsMoneyBall et UpdateUserStatsGiveaway pour remplacer les
            // SELECT COUNT + UPDATE/INSERT en deux allers-retours par une seule requête atomique.
            // CREATE UNIQUE INDEX IF NOT EXISTS est idempotent : sans risque si déjà présent.
            if (version == 10)
            {
                updated = true;

                string alterTableSql = @"
    CREATE UNIQUE INDEX IF NOT EXISTS idx_user_pseudo_platform
    ON user (Pseudo, Platform);
";

                using (var command = new SqliteCommand(alterTableSql, connection))
                {
                    command.ExecuteNonQuery();
                }

                version = 11;
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
            connection.Close();
            Console.WriteLine(result);
        }

        public async Task<List<Entrie>> GetEntriesByPseudoAsync(string pseudoTriggered, string platformTriggered, bool includeDisabled = false)
        {
            string path = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "database.sqlite");
            List<Entrie> entriesByPseudo = new List<Entrie>();

            if (!File.Exists(path))
            {
                return entriesByPseudo;
            }

            using (SqliteConnection connection = new SqliteConnection($"Data Source={path}"))
            {
                await connection.OpenAsync();
                var options = new JsonSerializerOptions
                {
                    IncludeFields = true
                };
                string query = @"
            SELECT Id, Pseudo, Stream, Platform, PokeName, CountNormal, CountShiny, DataLastCatch, DataFirstCatch, CODE_USER
            FROM Entrees
            WHERE Pseudo = @Pseudo AND Platform = @Platform";

                using (SqliteCommand command = new SqliteCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@Pseudo", pseudoTriggered);
                    command.Parameters.AddWithValue("@Platform", platformTriggered);

                    using (SqliteDataReader reader = await command.ExecuteReaderAsync())
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
                // OPTIM P5 : JsonSerializerOptions était alloué ici mais jamais utilisé — supprimé.
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

        public List<Entrie> GetAllEntries()
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
                // OPTIM P5 : JsonSerializerOptions était alloué ici mais jamais utilisé — supprimé.
                string query = @"
            SELECT Id, Pseudo, Stream, Platform, PokeName, CountNormal, CountShiny, DataLastCatch, DataFirstCatch, CODE_USER
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

                            Entrie entrie = new Entrie(id, pseudo, stream, platform, pokeName, countNormal, countShiny, last, first);
                            entrie.code = reader.GetString(9);
                            entriesByPseudo.Add(entrie);
                        }
                    }
                }
            }

            return entriesByPseudo;
        }

        public string GetFirstNonNullStream()
        {
            // Chemin vers la DB
            string path = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "database.sqlite");
            if (!File.Exists(path))
            {
                return null; // ou string.Empty, selon vos besoins
            }

            // On ne sélectionne qu'une seule colonne et la première ligne où Stream n'est pas NULL
            string query = @"
        SELECT Stream
        FROM Entrees
        WHERE Stream IS NOT NULL
        LIMIT 1;
    ";

            using (var connection = new SqliteConnection($"Data Source={path}"))
            {
                connection.Open();
                using (var command = new SqliteCommand(query, connection))
                {
                    // ExecuteScalar renvoie le premier champ de la première ligne
                    object result = command.ExecuteScalar();
                    return result?.ToString();
                    // si result == null (pas de lignes), retourne null
                }
            }
        }

        public async Task CreateUser(string pseudo, string platform, string code_user, string avatarUrl)
        {
            using var connection = new SqliteConnection($"Data Source={dataFilePath}");
            connection.Open();
            // Insérer une nouvelle ligne si l'utilisateur n'existe pas
            string insertQuery = @"
                    INSERT INTO user (CODE_USER, Pseudo, Platform)
                    VALUES (@CODE_USER, @Pseudo, @Platform)";

            using var insertCommand = new SqliteCommand(insertQuery, connection);
            insertCommand.Parameters.AddWithValue("@CODE_USER", code_user);
            insertCommand.Parameters.AddWithValue("@Pseudo", pseudo);
            insertCommand.Parameters.AddWithValue("@Platform", platform);

            await insertCommand.ExecuteNonQueryAsync();
            connection.Close();
        }

        public async Task CreateUser(User user)
        {
            using var connection = new SqliteConnection($"Data Source={dataFilePath}");
            connection.Open();
            // Insérer une nouvelle ligne si l'utilisateur n'existe pas
            string insertQuery = @"
                    INSERT INTO user (CODE_USER, Pseudo, Platform)
                    VALUES (@CODE_USER, @Pseudo, @Platform)";

            using var insertCommand = new SqliteCommand(insertQuery, connection);
            insertCommand.Parameters.AddWithValue("@CODE_USER", user.Code_user);
            insertCommand.Parameters.AddWithValue("@Pseudo", user.Pseudo);
            insertCommand.Parameters.AddWithValue("@Platform", user.Platform);

            await insertCommand.ExecuteNonQueryAsync();
            connection.Close();
            Console.WriteLine("\n\nUTILISATEUR CREE = " + user.Pseudo + "   " + user.Code_user);
        }

        public async Task UpdateUserStatsMoneyBall(string pseudo, string platform, int ballsLaunched, int moneySpent, string CODE_USER)
        {
            using (var connection = new SqliteConnection($"Data Source={dataFilePath}"))
            {
                await connection.OpenAsync();

                // OPTIM P3 : anciennement deux requêtes (SELECT COUNT puis UPDATE ou INSERT).
                // SQLite supporte ON CONFLICT sur une contrainte UNIQUE — on utilise l'upsert
                // pour fusionner les deux requêtes en une seule opération atomique.
                // Pré-requis : la table user doit avoir une contrainte UNIQUE(Pseudo, Platform)
                // (ajoutée via UpdateDatabase si absente — voir migration ci-dessus).
                string upsertQuery = @"
                    INSERT INTO user (CODE_USER, Pseudo, Platform, Stat_BallLaunched, Stat_MoneySpent)
                    VALUES (@CODE_USER, @Pseudo, @Platform, @BallsLaunched, @MoneySpent)
                    ON CONFLICT(Pseudo, Platform) DO UPDATE SET
                        CODE_USER         = excluded.CODE_USER,
                        Stat_BallLaunched = Stat_BallLaunched + excluded.Stat_BallLaunched,
                        Stat_MoneySpent   = Stat_MoneySpent   + excluded.Stat_MoneySpent";

                using (var cmd = new SqliteCommand(upsertQuery, connection))
                {
                    cmd.Parameters.AddWithValue("@CODE_USER",     CODE_USER);
                    cmd.Parameters.AddWithValue("@Pseudo",        pseudo);
                    cmd.Parameters.AddWithValue("@Platform",      platform);
                    cmd.Parameters.AddWithValue("@BallsLaunched", ballsLaunched);
                    cmd.Parameters.AddWithValue("@MoneySpent",    moneySpent);
                    await cmd.ExecuteNonQueryAsync();
                }
            }
        }

        public void UpdateUserStatsGiveaway(string pseudo, string platform, bool isShiny)
        {
            // OPTIM P3 : anciennement deux requêtes (SELECT COUNT puis UPDATE ou INSERT).
            // Remplacé par un upsert atomique ON CONFLICT.
            int toAddNormal = isShiny ? 0 : 1;
            int toAddShiny  = isShiny ? 1 : 0;

            using (var connection = new SqliteConnection($"Data Source={dataFilePath}"))
            {
                connection.Open();

                string upsertQuery = @"
                    INSERT INTO user (Pseudo, Platform, pokeReceived_normal, pokeReceived_shiny)
                    VALUES (@Pseudo, @Platform, @normalAdded, @shinyAdded)
                    ON CONFLICT(Pseudo, Platform) DO UPDATE SET
                        pokeReceived_normal = pokeReceived_normal + excluded.pokeReceived_normal,
                        pokeReceived_shiny  = pokeReceived_shiny  + excluded.pokeReceived_shiny";

                using (var cmd = new SqliteCommand(upsertQuery, connection))
                {
                    cmd.Parameters.AddWithValue("@Pseudo",      pseudo);
                    cmd.Parameters.AddWithValue("@Platform",    platform);
                    cmd.Parameters.AddWithValue("@normalAdded", toAddNormal);
                    cmd.Parameters.AddWithValue("@shinyAdded",  toAddShiny);
                    cmd.ExecuteNonQuery();
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

        // OPTIM P1 : anciennement, GenerateBaseStats ouvrait 12 connexions SQLite séparées pour
        // récupérer chaque stat d'un utilisateur (BallLaunched, MoneySpent, Giveaway×2, Scrap×2,
        // Money, TradeCount, RaidCount, RaidTotalDmg, FavoritePoke).
        // Toutes ces colonnes vivent dans la même table 'user' — une seule requête SELECT suffit.
        // Cette méthode retourne un BDD_USER (structure plate de toutes les colonnes stats) en une
        // connexion, remplaçant les 11 méthodes GetDataUserStats_* pour le cas stats complet.
        public BDD_USER GetUserStatsRow(string pseudo, string platform)
        {
            using var connection = new SqliteConnection($"Data Source={dataFilePath}");
            connection.Open();

            string query = @"
                SELECT
                    Stat_BallLaunched,
                    Stat_MoneySpent,
                    pokeReceived_normal,
                    pokeReceived_shiny,
                    pokeScrapped_normal,
                    pokeScrapped_shiny,
                    customMoney,
                    Stat_tradeCount,
                    Stat_RaidCount,
                    Stat_RaidTotalDmg,
                    favoriteCreature
                FROM user
                WHERE Pseudo = @Pseudo AND Platform = @Platform
                LIMIT 1";

            using var command = new SqliteCommand(query, connection);
            command.Parameters.AddWithValue("@Pseudo", pseudo);
            command.Parameters.AddWithValue("@Platform", platform);

            using var reader = command.ExecuteReader();
            if (reader.Read())
            {
                return new BDD_USER
                {
                    Stat_BallLaunched    = reader.IsDBNull(0)  ? 0    : reader.GetInt32(0),
                    Stat_MoneySpent      = reader.IsDBNull(1)  ? 0    : reader.GetInt32(1),
                    pokeReceived_normal  = reader.IsDBNull(2)  ? 0    : reader.GetInt32(2),
                    pokeReceived_shiny   = reader.IsDBNull(3)  ? 0    : reader.GetInt32(3),
                    pokeScrapped_normal  = reader.IsDBNull(4)  ? 0    : reader.GetInt32(4),
                    pokeScrapped_shiny   = reader.IsDBNull(5)  ? 0    : reader.GetInt32(5),
                    customMoney          = reader.IsDBNull(6)  ? 0    : reader.GetInt32(6),
                    Stat_tradeCount      = reader.IsDBNull(7)  ? 0    : reader.GetInt32(7),
                    Stat_RaidCount       = reader.IsDBNull(8)  ? 0    : reader.GetInt32(8),
                    Stat_RaidTotalDmg    = reader.IsDBNull(9)  ? 0    : reader.GetInt32(9),
                    favoriteCreature     = reader.IsDBNull(10) ? null : reader.GetString(10),
                };
            }

            // Aucune ligne trouvée : on retourne un objet avec des valeurs par défaut à zéro
            // (comportement identique aux anciennes méthodes GetDataUserStats_* qui renvoyaient 0)
            return new BDD_USER();
        }

        // OPTIM P4 : anciennement, Exporter.DoFullExport appelait user.lastCatch() pour chaque
        // utilisateur afin de filtrer ceux à exporter — chaque appel déclenchait une requête SQL
        // complète sur les entrées. Cette méthode récupère les dernières captures de TOUS les
        // utilisateurs en une seule requête groupée, évitant N aller-retours en base.
        public Dictionary<string, DateTime> GetLastCatchPerUser()
        {
            var result = new Dictionary<string, DateTime>(StringComparer.OrdinalIgnoreCase);

            using var connection = new SqliteConnection($"Data Source={dataFilePath}");
            connection.Open();

            // Clé composite Pseudo|Platform pour correspondre à la logique existante
            string query = @"
                SELECT Pseudo, Platform, MAX(DataLastCatch) AS LastCatch
                FROM entrees
                GROUP BY Pseudo, Platform";

            using var command = new SqliteCommand(query, connection);
            using var reader = command.ExecuteReader();
            while (reader.Read())
            {
                if (!reader.IsDBNull(2))
                {
                    string key = $"{reader.GetString(0)}|{reader.GetString(1)}";
                    result[key] = reader.GetDateTime(2);
                }
            }
            return result;
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

        internal string GetPseudoByCodeUser(string codeUser)
        {
            using (var connection = new SqliteConnection($"Data Source={dataFilePath}"))
            {
                connection.Open();
                string query = @"
                SELECT Pseudo
                FROM user
                WHERE CODE_USER = @CodeUser AND Pseudo IS NOT NULL
                LIMIT 1";

                using (var command = new SqliteCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@CodeUser", codeUser);
                    using (var reader = command.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            return reader["Pseudo"].ToString();
                        }
                    }
                }
            }
            return "NOT FOUND";
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
                connection.Close();
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

                connection.Close();
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

            using var connection = new SqliteConnection($"Data Source={dataFilePath}");
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
            connection.Close();
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

        internal void UpdateAvatar(string userCode, string avatarUrl)
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
                command.Parameters.AddWithValue("@CODE_USER", userCode);
                command.Parameters.AddWithValue("@AVATAR_URL", avatarUrl);

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

        public Zone GetZoneUser(string userCode, List<Zone> zones)
        {
            using var connection = new SqliteConnection($"Data Source={dataFilePath}");
            connection.Open();

            string query = @"
            SELECT
                selectedZone
            FROM
                user
            WHERE
                CODE_USER = @CODE_USER
            LIMIT 1";

            using (var command = new SqliteCommand(query, connection))
            {
                command.Parameters.AddWithValue("@CODE_USER", userCode);

                using (var reader = command.ExecuteReader())
                {
                    if (reader.Read())
                    {
                        try
                        {
                            return zones.Where(w => w.Name == reader["selectedZone"].ToString()).First();
                        }
                        catch (Exception)
                        {
                            throw new Exception("Zone not found : " + reader["selectedZone"].ToString());
                        }
                    }
                    else
                    {
                        return null;
                    }
                }
            }
        }

        internal async Task SetUserZone(string code_user, string name)
        {
            using var connection = new SqliteConnection($"Data Source={dataFilePath}");
            await connection.OpenAsync();

            string query = @"
        UPDATE user
        SET selectedZone = @selectedZone
        WHERE CODE_USER = @CODE_USER";

            using (var command = new SqliteCommand(query, connection))
            {
                command.Parameters.AddWithValue("@selectedZone", name);
                command.Parameters.AddWithValue("@CODE_USER", code_user);

                int rowsAffected = await command.ExecuteNonQueryAsync();
                if (rowsAffected == 0)
                {
                    throw new Exception($"Aucun utilisateur trouvé avec le code {code_user}");
                }
            }
        }

        public User GetUserBaseInfo(string userCode, string platform, AppSettings appSettings)
        {
            using var connection = new SqliteConnection($"Data Source={dataFilePath}");
            connection.Open();

            string query = @"
            SELECT
                Pseudo, selectedZone, Platform, CODE_USER, avatarUrl, cardsUrl, favoriteCreature
            FROM
                user
            WHERE
                CODE_USER = @CODE_USER
                AND Platform = @Platform
            LIMIT 1";

            using (var command = new SqliteCommand(query, connection))
            {
                command.Parameters.AddWithValue("@CODE_USER", userCode);
                command.Parameters.AddWithValue("@Platform", platform);

                using (var reader = command.ExecuteReader())
                {
                    if (reader.Read())
                    {
                        try
                        {
                            User utilisateur = new User
                            {
                                Pseudo = reader["Pseudo"].ToString(),
                                Platform = reader["Platform"].ToString(),
                                Code_user = reader["CODE_USER"].ToString(),
                                Location = appSettings.Zones.Where(w => Commun.CompareStrings(w.Name, reader["selectedZone"].ToString())).First() ?? Commun.GetBaseZone(),
                                AvatarUrl = reader["avatarUrl"]?.ToString(),
                                CardBackgroundUrl = reader["cardsUrl"]?.ToString()
                            };
                            try
                            {
                                utilisateur.FavoritePoke = appSettings.pokemons.Where(w => Commun.CompareStrings(w.Name_FR, reader["favoriteCreature"].ToString().Split('#')[0])).First() ??
                                appSettings.pokemons.Where(w => Commun.CompareStrings(w.Name_FR, this.GetEntriesByPseudo(reader["Pseudo"].ToString(), platform)[0].PokeName)).First();
                            }
                            catch
                            {
                            }
                            return utilisateur;
                        }
                        catch (Exception)
                        {
                            throw new KeyNotFoundException($"Dataconnexion.GetUserBaseInfo User not found {userCode} {platform}");
                        }
                    }
                    else
                    {
                        return null;
                    }
                }
            }
        }

        /// <summary>
        /// Update le pseudo d'un utilisateur dans la base de données.
        /// Filtrage where avec le code utilisateur et la plateforme.
        /// </summary>
        /// <param name="user"></param>
        /// <param name="NewUserName"></param>
        /// <returns></returns>
        public async Task UpdateUserPseudo(User user, string NewUserName)
        {
            using var connection = new SqliteConnection($"Data Source={dataFilePath}");
            await connection.OpenAsync();

            string query = @"
        UPDATE user
        SET Pseudo = @Pseudo
        WHERE CODE_USER = @CODE_USER";

            using (var command = new SqliteCommand(query, connection))
            {
                command.Parameters.AddWithValue("@Pseudo", NewUserName);
                command.Parameters.AddWithValue("@CODE_USER", user.Code_user);

                int rowsAffected = await command.ExecuteNonQueryAsync();
                if (rowsAffected == 0)
                {
                    throw new Exception($"Aucun utilisateur trouvé avec le code {user.Code_user}");
                }
            }
        }

        public async Task<List<Entrie>> GetEntrieByCodeUser(string code_user, AppSettings settings)
        {
            string path = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "database.sqlite");
            List<Entrie> entriesByPseudo = new List<Entrie>();

            if (!File.Exists(path))
            {
                return entriesByPseudo;
            }

            using (SqliteConnection connection = new SqliteConnection($"Data Source={path}"))
            {
                await connection.OpenAsync();
                var options = new JsonSerializerOptions
                {
                    IncludeFields = true
                };
                string query = @"
            SELECT Id, Pseudo, Stream, Platform, PokeName, CountNormal, CountShiny, DataLastCatch, DataFirstCatch, CODE_USER
            FROM Entrees
            WHERE CODE_USER = @Usercode";

                using (SqliteCommand command = new SqliteCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@Usercode", code_user);

                    using (SqliteDataReader reader = await command.ExecuteReaderAsync())
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

        public async Task<List<BDD_USER>> GetAllUsersEntitiesAsync()
        {
            var resultats = new List<BDD_USER>();
            var erroredUsersEntries = new List<int>();

            await using var connection = new SqliteConnection($"Data Source={dataFilePath}");
            await connection.OpenAsync();

            const string query = @"
        SELECT *
          FROM user;
    ";

            await using var command = new SqliteCommand(query, connection);
            await using var reader = await command.ExecuteReaderAsync();

            while (await reader.ReadAsync())
            {
                try
                {
                    var user = new BDD_USER
                    {
                        Id = reader.IsDBNull(reader.GetOrdinal("Id"))
                                                 ? null
                                                 : reader.GetInt32(reader.GetOrdinal("Id")),

                        CODE_USER = reader.IsDBNull(reader.GetOrdinal("CODE_USER"))
                                                 ? null
                                                 : reader.GetString(reader.GetOrdinal("CODE_USER")),

                        Pseudo = reader.IsDBNull(reader.GetOrdinal("Pseudo"))
                                                 ? null
                                                 : reader.GetString(reader.GetOrdinal("Pseudo")),

                        Platform = reader.IsDBNull(reader.GetOrdinal("Platform"))
                                                 ? null
                                                 : reader.GetString(reader.GetOrdinal("Platform")),

                        Stat_BallLaunched = reader.GetInt32(reader.GetOrdinal("Stat_BallLaunched")),
                        Stat_MoneySpent = reader.GetInt32(reader.GetOrdinal("Stat_MoneySpent")),
                        pokeReceived_normal = reader.GetInt32(reader.GetOrdinal("pokeReceived_normal")),
                        pokeReceived_shiny = reader.GetInt32(reader.GetOrdinal("pokeReceived_shiny")),
                        pokeScrapped_normal = reader.GetInt32(reader.GetOrdinal("pokeScrapped_normal")),
                        pokeScrapped_shiny = reader.GetInt32(reader.GetOrdinal("pokeScrapped_shiny")),
                        customMoney = reader.GetInt32(reader.GetOrdinal("customMoney")),
                        Stat_tradeCount = reader.GetInt32(reader.GetOrdinal("Stat_tradeCount")),
                        Stat_RaidCount = reader.GetInt32(reader.GetOrdinal("Stat_RaidCount")),
                        Stat_RaidTotalDmg = reader.GetInt32(reader.GetOrdinal("Stat_RaidTotalDmg")),
                        favoriteCreature = reader.IsDBNull(reader.GetOrdinal("favoriteCreature"))
                                                 ? null
                                                 : reader.GetString(reader.GetOrdinal("favoriteCreature")),

                        avatarUrl = reader.IsDBNull(reader.GetOrdinal("avatarUrl"))
                                                 ? null
                                                 : reader.GetString(reader.GetOrdinal("avatarUrl")),

                        cardsUrl = reader.IsDBNull(reader.GetOrdinal("cardsUrl"))
                                                 ? null
                                                 : reader.GetString(reader.GetOrdinal("cardsUrl")),

                        selectedZone = reader.IsDBNull(reader.GetOrdinal("selectedZone"))
                                                 ? Commun.GetBaseZone().Name
                                                 : reader.GetString(reader.GetOrdinal("selectedZone"))
                    };

                    resultats.Add(user);
                }
                catch (Exception ex)
                {
                    // Si l’Id est dispo, on le stocke pour debug
                    if (!reader.IsDBNull(reader.GetOrdinal("Id")))
                    {
                        erroredUsersEntries.Add(reader.GetInt32(reader.GetOrdinal("Id")));
                    }
                    Console.WriteLine(ex.Message);
                }
            }

            return resultats;
        }

        public async Task DeleteListUsersByIds(List<int> toDelete)
        {
            if (toDelete == null || toDelete.Count == 0)
                return;

            await using var connection = new SqliteConnection($"Data Source={dataFilePath}");
            await connection.OpenAsync();

            // Crée autant de paramètres que d'IDs
            var paramNames = toDelete
                .Select((id, idx) => new { Name = $"@id{idx}", Value = id })
                .ToList();

            // DELETE … WHERE Id IN (@id0, @id1, @id2, …)
            var sql = $"DELETE FROM user WHERE Id IN ({string.Join(", ", paramNames.Select(p => p.Name))});";

            await using var cmd = new SqliteCommand(sql, connection);
            foreach (var p in paramNames)
                cmd.Parameters.AddWithValue(p.Name, p.Value);

            await cmd.ExecuteNonQueryAsync();
        }

        public async Task UpdateListUsersByIds(List<BDD_USER> toKeepList)
        {
            if (toKeepList == null || toKeepList.Count == 0)
                return;

            await using var connection = new SqliteConnection($"Data Source={dataFilePath}");
            await connection.OpenAsync();

            // CAST de la transaction en SqliteTransaction
            await using var tx = (SqliteTransaction)await connection.BeginTransactionAsync();

            const string sql = @"
        UPDATE user
           SET CODE_USER           = @CODE_USER,
               Pseudo              = @Pseudo,
               Platform            = @Platform,
               Stat_BallLaunched   = @Stat_BallLaunched,
               Stat_MoneySpent     = @Stat_MoneySpent,
               pokeReceived_normal = @pokeReceived_normal,
               pokeReceived_shiny  = @pokeReceived_shiny,
               pokeScrapped_normal = @pokeScrapped_normal,
               pokeScrapped_shiny  = @pokeScrapped_shiny,
               customMoney         = @customMoney,
               Stat_tradeCount     = @Stat_tradeCount,
               Stat_RaidCount      = @Stat_RaidCount,
               Stat_RaidTotalDmg   = @Stat_RaidTotalDmg,
               favoriteCreature    = @favoriteCreature,
               avatarUrl           = @avatarUrl,
               cardsUrl            = @cardsUrl,
               selectedZone        = @selectedZone
         WHERE Id = @Id;
    ";

            foreach (var user in toKeepList)
            {
                await using var cmd = new SqliteCommand(sql, connection, tx);

                // Les AddWithValue() gèrent DBNull pour les strings nullables
                cmd.Parameters.AddWithValue("@Id", user.Id ?? (object)DBNull.Value);
                cmd.Parameters.AddWithValue("@CODE_USER", user.CODE_USER);
                cmd.Parameters.AddWithValue("@Pseudo", user.Pseudo ?? (object)DBNull.Value);
                cmd.Parameters.AddWithValue("@Platform", user.Platform ?? (object)DBNull.Value);
                cmd.Parameters.AddWithValue("@Stat_BallLaunched", user.Stat_BallLaunched);
                cmd.Parameters.AddWithValue("@Stat_MoneySpent", user.Stat_MoneySpent);
                cmd.Parameters.AddWithValue("@pokeReceived_normal", user.pokeReceived_normal);
                cmd.Parameters.AddWithValue("@pokeReceived_shiny", user.pokeReceived_shiny);
                cmd.Parameters.AddWithValue("@pokeScrapped_normal", user.pokeScrapped_normal);
                cmd.Parameters.AddWithValue("@pokeScrapped_shiny", user.pokeScrapped_shiny);
                cmd.Parameters.AddWithValue("@customMoney", user.customMoney);
                cmd.Parameters.AddWithValue("@Stat_tradeCount", user.Stat_tradeCount);
                cmd.Parameters.AddWithValue("@Stat_RaidCount", user.Stat_RaidCount);
                cmd.Parameters.AddWithValue("@Stat_RaidTotalDmg", user.Stat_RaidTotalDmg);
                cmd.Parameters.AddWithValue("@favoriteCreature", user.favoriteCreature ?? (object)DBNull.Value);
                cmd.Parameters.AddWithValue("@avatarUrl", user.avatarUrl ?? (object)DBNull.Value);
                cmd.Parameters.AddWithValue("@cardsUrl", user.cardsUrl ?? (object)DBNull.Value);
                cmd.Parameters.AddWithValue("@selectedZone", user.selectedZone);

                await cmd.ExecuteNonQueryAsync();
            }

            await tx.CommitAsync();
        }

        public async Task<User?> GetUserByCodeUser(string usercode)
        {
            string path = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "database.sqlite");
            string connectionString = $"Data Source={path}";

            await using var connection = new SqliteConnection(connectionString);
            await connection.OpenAsync();

            string query = @"
        SELECT Id,
               Pseudo,
               CODE_USER,
               Platform
        FROM user
        WHERE CODE_USER = @CODE_USER;";

            await using var command = new SqliteCommand(query, connection);
            command.Parameters.AddWithValue("@CODE_USER", usercode);

            await using var reader = await command.ExecuteReaderAsync();

            // Tenter de lire la première ligne
            if (!await reader.ReadAsync())
            {
                // Aucun enregistrement trouvé pour ce code_user
                return null;
            }

            // Extraction des données
            var user = new User
            {
                Id = reader.IsDBNull(reader.GetOrdinal("Id"))
                               ? null
                               : reader.GetInt32(reader.GetOrdinal("Id")),
                Pseudo = reader.IsDBNull(reader.GetOrdinal("Pseudo"))
                               ? null
                               : reader.GetString(reader.GetOrdinal("Pseudo")),
                Code_user = reader.IsDBNull(reader.GetOrdinal("CODE_USER"))
                               ? null
                               : reader.GetString(reader.GetOrdinal("CODE_USER")),
                Platform = reader.IsDBNull(reader.GetOrdinal("Platform"))
                               ? null
                               : reader.GetString(reader.GetOrdinal("Platform"))
            };

            return user;
        }


        public async Task<User?> GetUserByCodeUserStats(string usercode)
        {
            string path = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "database.sqlite");
            string connectionString = $"Data Source={path}";

            await using var connection = new SqliteConnection(connectionString);
            await connection.OpenAsync();

            string query = @"
        SELECT Id,
               Pseudo,
               CODE_USER,
               Platform
        FROM user
        WHERE CODE_USER = @CODE_USER;";

            await using var command = new SqliteCommand(query, connection);
            command.Parameters.AddWithValue("@CODE_USER", usercode);

            await using var reader = await command.ExecuteReaderAsync();

            if (!await reader.ReadAsync())
            {
                // Aucun utilisateur trouvé
                return null;
            }

            var response = new User
            {
                Id = reader.IsDBNull(reader.GetOrdinal("Id"))
                               ? null
                               : reader.GetInt32(reader.GetOrdinal("Id")),
                Pseudo = reader.IsDBNull(reader.GetOrdinal("Pseudo"))
                               ? null
                               : reader.GetString(reader.GetOrdinal("Pseudo")),
                Code_user = reader.IsDBNull(reader.GetOrdinal("CODE_USER"))
                               ? null
                               : reader.GetString(reader.GetOrdinal("CODE_USER")),
                Platform = reader.IsDBNull(reader.GetOrdinal("Platform"))
                               ? null
                               : reader.GetString(reader.GetOrdinal("Platform")),
                Data = this
            };

            response.generateStats();
            return response;
        }


        public async Task<User?> GetUserByCodeUserFull(
    string usercode,
    AppSettings appSettings,
    GlobalAppSettings globalAppSettings)
        {
            string path = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "database.sqlite");
            string connectionString = $"Data Source={path}";

            await using var connection = new SqliteConnection(connectionString);
            await connection.OpenAsync();

            string query = @"
        SELECT Id,
               Pseudo,
               CODE_USER,
               Platform
        FROM user
        WHERE CODE_USER = @CODE_USER;";

            await using var command = new SqliteCommand(query, connection);
            command.Parameters.AddWithValue("@CODE_USER", usercode);

            await using var reader = await command.ExecuteReaderAsync();

            if (!await reader.ReadAsync())
            {
                // Aucun utilisateur trouvé
                return null;
            }

            var response = new User
            {
                Id = reader.IsDBNull(reader.GetOrdinal("Id"))
                               ? null
                               : reader.GetInt32(reader.GetOrdinal("Id")),
                Pseudo = reader.IsDBNull(reader.GetOrdinal("Pseudo"))
                               ? null
                               : reader.GetString(reader.GetOrdinal("Pseudo")),
                Code_user = reader.IsDBNull(reader.GetOrdinal("CODE_USER"))
                               ? null
                               : reader.GetString(reader.GetOrdinal("CODE_USER")),
                Platform = reader.IsDBNull(reader.GetOrdinal("Platform"))
                               ? null
                               : reader.GetString(reader.GetOrdinal("Platform"))
            };

            response.generateStatsAndAchievements(appSettings, globalAppSettings);
            return response;
        }

    }
}