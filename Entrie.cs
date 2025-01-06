using Microsoft.Data.Sqlite;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace PKServ
{
    public class Entrie
    {
        public int id;
        public string? code;
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
            this.CountNormal = Normal;
            this.CountShiny = Shiny;
            this.dateLastCatch = lastcatch;
            this.dateFirstCatch = firstcatch;
            this.code = code;
        }

        public Entrie(string Pseudo, string Stream, string Platform, string PokeName)
        {
            this.Pseudo = Pseudo;
            this.Stream = Stream;
            this.Platform = Platform;
            this.PokeName = PokeName;
            this.CountNormal = 0;
            this.CountShiny = 0;
            this.dateLastCatch = DateTime.Now;
            this.dateFirstCatch = DateTime.Now;
        }

        public void PreValidate(DataConnexion cnx)
        {
            this.Validate(NeedNewLine(cnx));
        }

        public bool NeedNewLine(DataConnexion cnx)
        {
            List<Entrie> entriesByPseudo = cnx.GetEntriesByPseudo(this.Pseudo, this.Platform);
            return !entriesByPseudo.Where(x => x.PokeName == this.PokeName).Any();
        }

        internal void Validate(bool NewLine)
        {
            string path = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "database.sqlite");

            if(this.code is null || this.code == "unset" || this.code == "unset in UserRequest")
            {
                try
                {
                    this.code = new DataConnexion().GetCodeUserByPlatformPseudo(new User { Pseudo = this.Pseudo, Platform = this.Platform });
                }
                catch(Exception e) { Console.WriteLine("Cannot create code user - " + e.Message); }
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
                        command.Parameters.AddWithValue("@Id", this.id);
                    }

                    command.Parameters.AddWithValue("@Pseudo", this.Pseudo);
                    command.Parameters.AddWithValue("@CODE_USER", this.code); // Ajout de la colonne CODE_USER avec valeur par défaut
                    command.Parameters.AddWithValue("@Stream", this.Stream);
                    command.Parameters.AddWithValue("@Platform", this.Platform);
                    command.Parameters.AddWithValue("@PokeName", this.PokeName);
                    command.Parameters.AddWithValue("@CountNormal", this.CountNormal);
                    command.Parameters.AddWithValue("@CountShiny", this.CountShiny);
                    command.Parameters.AddWithValue("@DataLastCatch", this.dateLastCatch);

                    if (NewLine)
                    {
                        command.Parameters.AddWithValue("@DataFirstCatch", this.dateFirstCatch);
                    }

                    command.ExecuteNonQuery();
                }
            }
        }

        internal void setIDPoke(AppSettings appSettings)
        {
            this.entryPokeID = appSettings.GetIdPokeByName(this.PokeName);
        }

        internal void DeleteEntrie()
        {
            new DataConnexion().DeleteEntrie(this);
        }
    }
}