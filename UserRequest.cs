using PKServ.Configuration;
using PKServ.Entity;
using System;
using System.Collections.Generic;
using System.Linq;

namespace PKServ
{
    public class UserRequest
    {
        public string UserName;
        public string Platform;
        public string UserCode;
        public string TriggerName;
        public string ChannelSource;
        public int Price;
        private bool? skip;
        public string avatarUrl;

        public UserRequest(string userName, string platform, string triggerName, string channelSource, int price, string userCode = "", string avatarUrl = null)
        {
            UserName = userName;
            Platform = platform;
            TriggerName = triggerName;
            ChannelSource = channelSource;
            Price = price;
            UserCode = userCode;
            this.avatarUrl = avatarUrl;
            bool? skip = false;

            if (UserName == null && platform == null && triggerName == null && channelSource == null)
            {
                skip = true;
            }

            if (!skip.Value && userCode == "" && platform.ToLower() != "interface" && platform.ToLower() != "system" && !userName.StartsWith('+'))
            {
                DataConnexion data = new DataConnexion();
                User u = new User(userName, platform);
                string grabbedCode = data.GetCodeUserByPlatformPseudo(u);
                if (grabbedCode != null && grabbedCode != "" && grabbedCode != "unset" && grabbedCode != "unset in UserRequest")
                {
                    UserCode = grabbedCode;
                }
                else
                {
                    try
                    {
                        string code = data.GetEntriesByPseudo(UserName, platform).Where(grab => grab.code != null && grab.code != "" && grab.code != "unset" && grab.code != "unset in UserRequest").FirstOrDefault().code;
                        if (code != null)
                        {
                            UserCode = grabbedCode;
                            data.SetCodeUserByPlatformPseudo(u);
                        }
                    }
                    catch { }
                }
            }
        }
    }

    public class GetPokeStats
    {
        public User User { get; set; }
        public string Name { get; set; }

        // Constructeur par défaut requis pour la désérialisation
        public GetPokeStats()
        { }

        public GetPokeStats(User User, string Name)
        {
            this.User = User;
            this.Name = Name.ToLower().Replace("_", " ");
        }
    }

    public class BackgroundChange
    {
        public User User { get; set; }
        public string Name { get; set; }

        // Constructeur par défaut requis pour la désérialisation
        public BackgroundChange()
        { }

        public BackgroundChange(User User, string Name)
        {
            this.User = User;
            this.Name = Name.ToLower().Replace("_", " ");
        }

        public bool IsValide(DataConnexion data, AppSettings appSettings)
        {
            if (!appSettings.TrainerCardsBackgrounds.Where(x => x.Name.ToLower() == Name.Replace("_", " ").ToLower()).Any())
                return false;
            else
            {
                Background bg = appSettings.TrainerCardsBackgrounds.Where(x => x.Name.ToLower().Replace("_", " ") == Name.Replace("_", " ").ToLower()).FirstOrDefault();
                User.generateStats();
                return bg.IsUnlocked(User);
            }
        }

        public string DoResult(AppSettings appSettings)
        {
            Name = Name.ToLower().Replace("_", " ");
            User.ChangeBackground(appSettings.TrainerCardsBackgrounds.Where(x => x.Name.ToLower().Replace("_", " ") == Name).FirstOrDefault().Url);
            return $"The background has been changed to {Name}.";
        }
    }

    public class FavoriteCreatureRequest
    {
        public User User { get; set; }
        public string Mode { get; set; }
        public string Name { get; set; }

        public FavoriteCreatureRequest(User User, string Name, string Mode)
        {
            this.User = User;
            this.Name = Name.ToLower().Replace("_", " ");
            this.Mode = Mode.ToLower();
        }

        public bool IsValide(DataConnexion data, AppSettings appSettings)
        {
            bool Exist = appSettings.pokemons.Where(x => Commun.isSamePoke(x, Name)).Any();
            if (!Exist)
            {
                Console.WriteLine($"The pokemon {Name} does not exist in the json.");
                return false;
            }
            return data.GetEntriesByPseudo(User.Pseudo, User.Platform).Where(registeredEntrie => Commun.StringifyChange(registeredEntrie.PokeName) == Commun.StringifyChange(Name) &&
                                                                          (registeredEntrie.CountShiny > 0 && this.Mode.StartsWith('s') || registeredEntrie.CountNormal > 0 && this.Mode.StartsWith('n'))).Any();
        }

        internal string Set(User user, string name, string mode, DataConnexion data)
        {
            data.UpdateFavCreature(user, $"{name}#{mode[0]}");
            return $"The creature {name} has been set to your favorites.";
        }
    }
}