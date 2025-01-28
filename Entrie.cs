using Microsoft.Data.Sqlite;
using PKServ.Configuration;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace PKServ
{
    public class Entrie
    {
        public int id;
        public string code;
        public string Pseudo;
        public string Stream;
        public string Platform;
        public string PokeName;
        public int CountNormal;
        public int CountShiny;
        public DateTime dateLastCatch;
        public DateTime dateFirstCatch;
        public int? entryPokeID;

        public Entrie(int id, string Pseudo, string Stream, string Platform, string PokeName, int Normal, int Shiny, DateTime lastcatch, DateTime firstcatch, string code = "unset by code")
        {
            this.id = id;
            this.Pseudo = Pseudo;
            this.Stream = Stream;
            this.Platform = Platform;
            this.PokeName = PokeName;
            CountNormal = Normal;
            CountShiny = Shiny;
            dateLastCatch = lastcatch;
            dateFirstCatch = firstcatch;
            this.code = code;
        }

        public Entrie(string Pseudo, string Stream, string Platform, string PokeName)
        {
            this.Pseudo = Pseudo;
            this.Stream = Stream;
            this.Platform = Platform;
            this.PokeName = PokeName;
            CountNormal = 0;
            CountShiny = 0;
            dateLastCatch = DateTime.Now;
            dateFirstCatch = DateTime.Now;
        }

        public void PreValidate(DataConnexion cnx)
        {
            Validate(NeedNewLine(cnx));
        }

        public bool NeedNewLine(DataConnexion cnx)
        {
            List<Entrie> entriesByPseudo = cnx.GetEntriesByPseudo(Pseudo, Platform);
            return !entriesByPseudo.Where(x => x.PokeName == PokeName).Any();
        }

        internal void Validate(bool NewLine)
        {
            string path = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "database.sqlite");

            if (code is null || code == "unset" || code == "unset in UserRequest")
            {
                try
                {
                    code = new DataConnexion().GetCodeUserByPlatformPseudo(new User { Pseudo = Pseudo, Platform = Platform });
                }
                catch (Exception e) { Console.WriteLine("Cannot create code user - " + e.Message); }
            }

            using (SqliteConnection connection = new SqliteConnection($"Data Source={path}"))
            {
                connection.Open();

                string query;
                if (NewLine)
                {
                    query = @"
        INSERT INTO Entrees (Pseudo, CODE_USER, Stream, Platform, PokeName, CountNormal, CountShiny, DataLastCatch, DataFirstCatch)
        VALUES (@Pseudo, @CODE_USER, @Stream, @Platform, @PokeName, @CountNormal, @CountShiny, @DataLastCatch, @DataFirstCatch)";
                }
                else
                {
                    query = @"
        UPDATE Entrees SET
            Pseudo = @Pseudo,
            CODE_USER = @CODE_USER,
            Stream = @Stream,
            Platform = @Platform,
            PokeName = @PokeName,
            CountNormal = @CountNormal,
            CountShiny = @CountShiny,
            DataLastCatch = @DataLastCatch
        WHERE Id = @Id";
                }

                using (SqliteCommand command = new SqliteCommand(query, connection))
                {
                    if (!NewLine)
                    {
                        command.Parameters.AddWithValue("@Id", id);
                    }

                    command.Parameters.AddWithValue("@Pseudo", Pseudo);
                    command.Parameters.AddWithValue("@CODE_USER", code); // Ajout de la colonne CODE_USER avec valeur par défaut
                    command.Parameters.AddWithValue("@Stream", Stream);
                    command.Parameters.AddWithValue("@Platform", Platform);
                    command.Parameters.AddWithValue("@PokeName", PokeName);
                    command.Parameters.AddWithValue("@CountNormal", CountNormal);
                    command.Parameters.AddWithValue("@CountShiny", CountShiny);
                    command.Parameters.AddWithValue("@DataLastCatch", dateLastCatch);

                    if (NewLine)
                    {
                        command.Parameters.AddWithValue("@DataFirstCatch", dateFirstCatch);
                    }

                    command.ExecuteNonQuery();
                }
            }
        }

        internal void setIDPoke(AppSettings appSettings)
        {
            entryPokeID = appSettings.GetIdPokeByName(PokeName);
        }

        internal void DeleteEntrie()
        {
            new DataConnexion().DeleteEntrie(this);
        }

        internal bool IsLinkedWithThatCreatureName(string name)
        {
            return this.PokeName.ToLower() == name.ToLower().Replace("_", " ");
        }

        internal bool IsLinkedWithThatCreature(Pokemon Poke)
        {
            return this.PokeName.ToLower() == Poke.AltName ||
                this.PokeName.ToLower() == Poke.Name_EN ||
                this.PokeName.ToLower() == Poke.Name_FR;
        }
    }
}