using PKServ.Configuration;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace PKServ
{
    public class Trade
    {
        public User trader1;
        public User trader2;
        public Pokemon pokemon1;
        public Pokemon pokemon2;
        public DataConnexion cnx;
        public GlobalAppSettings globalAppSettings;
        public string Channel;
        public bool complete = true;
        public int price = 0;

        public Trade()
        {
        }

        [JsonConstructor]
        public Trade(User trader1, User trader2, Pokemon pokemon1, Pokemon pokemon2, string channel)
        {
            this.trader1 = trader1;
            this.trader2 = trader2;
            this.pokemon1 = pokemon1;
            this.pokemon2 = pokemon2;
            cnx = new DataConnexion();
            this.Channel = channel;
            this.price = 0;
        }

        public Trade(TradeRequest request)
        {
            this.trader1 = request.UserWhoAccepted;
            this.trader2 = request.UserWhoMadeRequest;
            this.pokemon2 = request.CreatureSent;
            this.pokemon1 = request.CreatureRequested;
            this.Channel = request.ChannelSource;
            this.price = request.Price;
            cnx = new DataConnexion();
        }

        public void SetEnv(GlobalAppSettings globalAppSettings)
        {
            this.globalAppSettings = globalAppSettings;
        }

        /// <summary>
        /// méthode principale, retourne le string de retours
        /// </summary>
        /// <returns></returns>
        internal string DoWork(bool paid = false)
        {
            string result = "";

            List<Entrie> entrie_User1 = cnx.GetEntriesByPseudo(trader1.Pseudo, trader1.Platform, false);
            List<Entrie> entrie_User2 = cnx.GetEntriesByPseudo(trader2.Pseudo, trader2.Platform, false);

            bool trader1FillCondition = CheckCondition(entrie_User1, pokemon1);
            bool trader2FillCondition = CheckCondition(entrie_User2, pokemon2);

            Entrie PokemonLeaveJ1 = entrie_User1.Where(x => x.PokeName.ToLower() == pokemon1.Name_EN.ToLower() || x.PokeName.ToLower() == pokemon1.Name_FR.ToLower() || x.PokeName.ToLower() == pokemon1.AltName.ToLower()).FirstOrDefault();
            Entrie PokemonLeaveJ2 = entrie_User2.Where(x => x.PokeName.ToLower() == pokemon2.Name_EN.ToLower() || x.PokeName.ToLower() == pokemon2.Name_FR.ToLower() || x.PokeName.ToLower() == pokemon2.AltName.ToLower()).FirstOrDefault();
            bool J1NeedNewLine = false;
            bool J2NeedNewLine = false;
            Entrie PokemonJoinJ1 = null;
            Entrie PokemonJoinJ2 = null;

            // l'entrée du pokémon recu par joueur 1 existe
            if (entrie_User1.Where(x => x.PokeName.ToLower() == pokemon2.Name_EN.ToLower() || x.PokeName.ToLower() == pokemon2.Name_FR.ToLower() || x.PokeName.ToLower() == pokemon2.AltName.ToLower()).Any())
            {
                PokemonJoinJ1 = entrie_User1.Where(x => x.PokeName.ToLower() == pokemon2.Name_EN.ToLower() || x.PokeName.ToLower() == pokemon2.Name_FR.ToLower() || x.PokeName.ToLower() == pokemon2.AltName.ToLower()).FirstOrDefault();
            }
            // l'entrée du pokémon recu par joueur 1 n'existe pas
            else
            {
                PokemonJoinJ1 = new Entrie(-1, trader1.Pseudo, Channel, trader1.Platform, pokemon2.Name_FR, 0, 0, DateTime.Now, DateTime.Now, trader1.Code_user);
                J1NeedNewLine = true;
            }

            // l'entrée du pokémon recu par joueur 2 existe
            if (entrie_User2.Where(x => x.PokeName.ToLower() == pokemon1.Name_EN.ToLower() || x.PokeName.ToLower() == pokemon1.Name_FR.ToLower() || x.PokeName.ToLower() == pokemon1.AltName.ToLower()).Any())
            {
                PokemonJoinJ2 = entrie_User2.Where(x => x.PokeName.ToLower() == pokemon1.Name_EN.ToLower() || x.PokeName.ToLower() == pokemon1.Name_FR.ToLower() || x.PokeName.ToLower() == pokemon1.AltName.ToLower()).FirstOrDefault();
            }
            // l'entrée du pokémon recu par joueur 2 n'existe pas
            else
            {
                PokemonJoinJ2 = new Entrie(-1, trader2.Pseudo, Channel, trader2.Platform, pokemon1.Name_FR, 0, 0, DateTime.Now, DateTime.Now, trader2.Code_user);
                J2NeedNewLine = true;
            }

            // on enleve les pokémon qui partent de leur entrée respective
            if (pokemon1.isShiny)
                PokemonLeaveJ1.CountShiny--;
            else
                PokemonLeaveJ1.CountNormal--;

            if (pokemon2.isShiny)
                PokemonLeaveJ2.CountShiny--;
            else
                PokemonLeaveJ2.CountNormal--;

            // on ajoute les pokémon qui arrivent à leurs entrées respectives
            if (pokemon1.isShiny)
                PokemonJoinJ2.CountShiny++;
            else
                PokemonJoinJ2.CountNormal++;

            if (pokemon2.isShiny)
                PokemonJoinJ1.CountShiny++;
            else
                PokemonJoinJ1.CountNormal++;

            // on valide les données en base de données
            // le NewLine peut etre true/false en fonction de si l'entrée existait déjà pour l'utilisateur dans le cas d'une reception
            // il ne peut pas etre true, dans le cas d'un pokémon qui part, donc sera toujours a faux
            PokemonJoinJ1.Validate(NewLine: J1NeedNewLine);
            PokemonLeaveJ1.Validate(NewLine: false);
            PokemonJoinJ2.Validate(NewLine: J2NeedNewLine);
            PokemonLeaveJ2.Validate(NewLine: false);

            if (paid)
            {
                this.trader1.Stats.CustomMoney -= this.price;
                this.trader2.Stats.CustomMoney -= this.price;
            }

            this.trader1.ValidateStatsBDD();
            this.trader2.ValidateStatsBDD();

            result = "échange réussi";

            this.complete = true;

            return result;
        }

        private bool CheckCondition(List<Entrie> entries, Pokemon pokemon)
        {
            return pokemon.isShiny
                ? entries.Any(p => p.PokeName == pokemon.Name_FR && p.CountShiny >= 1)
                : entries.Any(p => p.PokeName == pokemon.Name_FR && p.CountNormal >= 1);
        }
    }

    public class TradeRequest
    {
        public User UserWhoMadeRequest { get; set; }
        public User? UserWhoAccepted { get; set; }
        public Pokemon CreatureSent { get; set; }
        public Pokemon CreatureRequested { get; set; }
        public bool Completed { get; set; } = false;
        public int Price { get; set; } = 0;

        [JsonInclude]
        private string PokeSent { get; set; }

        [JsonInclude]
        private string PokeWanted { get; set; }

        [JsonInclude]
        private string ShinySent { get; set; }

        [JsonInclude]
        private string ShinyWanted { get; set; }

        [JsonInclude]
        public string ID { get; set; }

        [JsonInclude]
        public string ChannelSource { get; set; }

        public TradeRequest()
        {
        }

        [JsonConstructor]
        public TradeRequest(User UserWhoMadeRequest, string PokeSent, string ShinySent, string PokeWanted, string ShinyWanted, string ID, string channelSource)
        {
            this.UserWhoMadeRequest = UserWhoMadeRequest;
            this.ID = ID;
            this.ShinySent = ShinySent;
            this.ShinyWanted = ShinyWanted;
            this.ChannelSource = channelSource;

            this.PokeSent = PokeSent.Replace('_', ' ').ToLower();
            this.PokeWanted = PokeWanted.Replace('_', ' ').ToLower();
            bool sentShiny = (this.ShinySent.ToLower() == "shiny" || this.ShinySent.ToLower() == "chromatique" || this.ShinySent.ToLower() == "s");
            bool requestedShiny = (this.ShinyWanted.ToLower() == "shiny" || this.ShinyWanted.ToLower() == "chromatique" || this.ShinyWanted.ToLower() == "s");

            InitializePokemons(this.PokeSent, this.PokeWanted, sentShiny, requestedShiny);
        }

        public void Complete()
        {
            this.Completed = true;
        }

        private void InitializePokemons(string pokeSent, string pokeWanted, bool sentShiny, bool requestedShiny)
        {
            var options = new JsonSerializerOptions
            {
                IncludeFields = true
            };

            List<Pokemon> pokeAvailable = JsonSerializer.Deserialize<List<Pokemon>>(File.ReadAllText("./pokemons.json"), options).Where(p => p.enabled).ToList();
            pokeAvailable.AddRange(JsonSerializer.Deserialize<List<Pokemon>>(File.ReadAllText("./customPokemons.json"), options).Where(p => p.enabled).ToList());

            this.CreatureSent = pokeAvailable.Where(p => p.Name_EN.ToLower() == pokeSent || p.Name_FR.ToLower() == pokeSent || p.AltName.ToLower() == pokeSent).FirstOrDefault();
            if (this.CreatureSent == null)
            {
                throw new Exception("Poke Sent Not Found");
            }
            this.CreatureRequested = pokeAvailable.Where(p => p.Name_EN.ToLower() == pokeWanted || p.Name_FR.ToLower() == pokeWanted || p.AltName.ToLower() == pokeWanted).FirstOrDefault();
            if (this.CreatureRequested == null)
            {
                throw new Exception("Poke Wanted Not Found");
            }

            this.CreatureSent.isShiny = sentShiny;
            this.CreatureRequested.isShiny = requestedShiny;
        }

        internal void CalculatePrice(GlobalAppSettings globalAppSettings)
        {
            // prix de base
            int price = globalAppSettings.TradeSettings.Prices.BasePrice;

            // shiny
            if (CreatureSent.isShiny)
                price += globalAppSettings.TradeSettings.Prices.PerShinyIncreasement;
            if (CreatureRequested.isShiny)
                price += globalAppSettings.TradeSettings.Prices.PerShinyIncreasement;

            // rarity
            price += globalAppSettings.TradeSettings.Prices.PerRarityIncreasement * ((CreatureRequested.rarity.HasValue ? CreatureRequested.rarity.Value : 0) + (CreatureSent.rarity.HasValue ? CreatureSent.rarity.Value : 0));

            // custom
            if (CreatureSent.isCustom)
                price += globalAppSettings.TradeSettings.Prices.PerCustomIncreasement;
            if (CreatureRequested.isCustom)
                price += globalAppSettings.TradeSettings.Prices.PerCustomIncreasement;

            // legendary
            if (CreatureSent.isLegendary)
                price += globalAppSettings.TradeSettings.Prices.PerLegendaryIncreasement;
            if (CreatureRequested.isLegendary)
                price += globalAppSettings.TradeSettings.Prices.PerLegendaryIncreasement;

            this.Price = price;
        }

        internal string GetMessageCode(GlobalAppSettings globalAppSettings)
        {
            //"tradeRequestCreated": "Échange demandé. [USER], qui donne [CREATURE_SENT] en échange de [CREATURE_REQUESTED]. Prix : [PRICE]. Si intéressé : [COMMAND-TRADE-ACCEPT] [CODE]. Pour annuler : [COMMAND-TRADE-CANCEL].",
            return globalAppSettings.Texts.TranslationTrading.tradeRequestCreated.
                Replace("[USER]", $"{this.UserWhoMadeRequest.Pseudo}").
                Replace("[CREATURE_SENT]", $"{this.CreatureSent.Name_FR} ({this.ShinySent.ToLower()})").
                Replace("[CREATURE_REQUESTED]", $"{this.CreatureRequested.Name_FR} ({this.ShinyWanted.ToLower()})").
                Replace("[PRICE]", $"{this.Price}").
                Replace("[COMMAND-TRADE-ACCEPT]", $"{globalAppSettings.CommandSettings.CmdTradeAccept}").
                Replace("[CODE]", $"{this.ID}").
                Replace("[COMMAND-TRADE-CANCEL]", $"{globalAppSettings.CommandSettings.CmdTradeCancel}");
        }

        internal bool CheckIfCanTradeThisItem()
        {
            bool result = false;

            var entries = new DataConnexion().GetEntriesByPseudo(UserWhoMadeRequest.Pseudo, UserWhoMadeRequest.Platform).Where(e => e.PokeName.ToLower() == CreatureSent.Name_EN.ToLower() || e.PokeName.ToLower() == CreatureSent.Name_FR.ToLower() || e.PokeName.ToLower() == CreatureSent.AltName.ToLower()).FirstOrDefault();
            if (entries != null)
                result = this.CreatureSent.isShiny ? entries.CountShiny > 0 : entries.CountNormal > 0;
            return result;
        }
    }

    public class TradeAccept
    {
        public User UserWhoAccepted { get; set; }

        public string ID { get; set; }

        public TradeAccept()
        {
        }

        public TradeAccept(User user, string ID)
        {
            this.UserWhoAccepted = user;
            this.ID = ID;
        }

        internal bool VerifEligibilityMoney(int money)
        {
            return UserWhoAccepted.Stats.CustomMoney > money;
        }

        internal bool VerifEligibilityCreature(Pokemon poke, DataConnexion data)
        {
            bool result = false;
            var entries = data.GetEntriesByPseudo(this.UserWhoAccepted.Pseudo, this.UserWhoAccepted.Platform).Where(e => e.PokeName.ToLower() == poke.Name_EN.ToLower() || e.PokeName.ToLower() == poke.Name_FR.ToLower() || e.PokeName.ToLower() == poke.AltName.ToLower()).FirstOrDefault();
            if (entries != null)
                result = poke.isShiny ? entries.CountShiny > 0 : entries.CountNormal > 0;
            return result;
        }
    }

    public class TradeCancel
    {
        public User User { get; set; }

        public string ID { get; set; }

        public TradeCancel()
        {
        }

        public TradeCancel(User user, string ID)
        {
            this.User = user;
            this.ID = ID;
        }
    }
}