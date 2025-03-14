using PKServ.Configuration;
using System;
using System.Collections.Generic;

namespace PKServ.Entity
{
    public class Giveaway
    {
        public string Code { get; set; }
        public List<Pokemon> Pokemons { get; set; } = [];
        public List<GiveawayCreatures> pokeList { get; set; }
        public int Money { get; set; }
        public string Mode { get; set; }
        public DateTime Start { get; set; }
        public DateTime End { get; set; }

        public Giveaway(string code, List<GiveawayCreatures> pokeList, int money, string mode, DateTime start, DateTime end)
        {
            Code = code;
            Money = money;
            Mode = mode;
            Start = start;
            End = end;
            this.pokeList = pokeList;
        }

        public bool IsValide()
        {
            return
                DateTime.Now > Start &&
                DateTime.Now < End;
        }

        public string Attribute(GlobalAppSettings globalAppSettings, User user)
        {
            string result = String.Empty;
            if (!IsValide())
            {
                return globalAppSettings.Texts.TranslationGiveaway.AlreadyClaimed
                    .Replace("[USER]", $"{user.Pseudo}")
                    .Replace("[CODE]", $"{this.Code}");
            }
            else
            {
                if (Money > 0)
                {
                    user.generateStats();
                    user.Stats.CustomMoney = Money;
                }

                switch (Mode)
                {
                    case GiveawayMode.All:
                        break;

                    case GiveawayMode.RandomOne:
                        break;
                }

                Pokemons.ForEach(p =>
                {
                    new Work(null, null, null, null).ObtainPoke(user, p);
                });

                return result;
            }
        }
    }

    public static class GiveawayMode
    {
        public const string All = "All";
        public const string RandomOne = "Random";
    }

    public class GiveawayCreatures
    {
        public string Name { get; set; }
        public bool shiny { get; set; }
    }

    public class GiveawayClaim
    {
        public string Code { get; set; }
        public User User { get; set; }
        public string ChannelName { get; set; }
        public DataConnexion DataConnexion { get; set; } = new DataConnexion();

        public GiveawayClaim(string Code, User User, string ChannelName)
        {
            this.Code = Code;
            this.User = User;
            this.ChannelName = ChannelName;
        }
    }
}