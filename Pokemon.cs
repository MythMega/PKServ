namespace PKServ
{
    public class Pokemon
    {
        /// <summary>
        /// Name, in french
        /// </summary>
        public string Name_FR;

        /// <summary>
        /// Name, in english
        /// </summary>
        public string Name_EN;

        /// <summary>
        /// sprite link in shiny
        /// if not specified, will grab from pokemondb, the sprite from Pokémon Home
        /// except if the poke is custom
        /// </summary>
        public string Sprite_Shiny;

        /// <summary>
        /// sprite link in normal
        /// if not specified, will grab from pokemondb, the sprite from Pokémon Home
        /// except if the poke is custom
        /// </summary>
        public string Sprite_Normal;

        /// <summary>
        /// is that poke custom
        /// </summary>
        public bool isCustom;

        /// <summary>
        /// is that poke locked ?
        /// if locked, that poke won't be available to catch
        /// </summary>
        public bool isLock;

        /// <summary>
        /// is that poke legendary ?
        /// if legendary, a special message will be returned
        /// </summary>
        public bool isLegendary;

        public string Type1 { get; set; }

        public string Type2 { get; set; }

        public bool isShiny = false;

        public bool isShinyLock;

        public bool enabled { get; set; }

        public int? valueNormal { get; set; }
        public int? valueShiny { get; set; }
        public int? priceNormal { get; set; }
        public int? priceShiny { get; set; }
        public int? rarity { get; set; }

        public string? AltName { get; set; }

        public bool AltNameForced { get; set; } = true;



        /// <summary>
        /// constructor
        /// </summary>
        /// <param name="name_FR"></param>
        /// <param name="name_EN"></param>
        /// <param name="sprite_Shiny">generated if = ""</param>
        /// <param name="sprite_Normal">generated if = ""</param>
        /// <param name="isCustom">false by default</param>
        /// <param name="isLock">false by default</param>
        /// <param name="isLegendary">false by default</param>
        public Pokemon(string name_FR, string name_EN, string sprite_Shiny, string sprite_Normal, bool isCustom = false, bool isLock = false, bool isLegendary = false, bool isShinyLock = false, int? valueNormal = null, int? valueShiny = null, int? priceNormal = null, int? priceShiny = null, int? rarity = 1, string AltName = null)
        {
            Name_FR = name_FR;
            Name_EN = name_EN;
            Sprite_Shiny = string.IsNullOrEmpty(sprite_Shiny) && !isCustom ? $"https://img.pokemondb.net/sprites/black-white/anim/shiny/{name_EN.ToLower()}.gif" : sprite_Shiny;
            Sprite_Normal = string.IsNullOrEmpty(sprite_Normal) && !isCustom ? $"https://img.pokemondb.net/sprites/black-white/anim/normal/{name_EN.ToLower()}.gif" : sprite_Normal;
            this.isCustom = isCustom;
            this.isLock = isLock;
            this.isLegendary = isLegendary;
            this.isShinyLock = isShinyLock;
            this.valueNormal = valueNormal;
            this.valueShiny = valueShiny;
            this.priceNormal = priceNormal;
            this.priceShiny = priceShiny;
            this.rarity = rarity;
            if(AltName is null)
            {
                this.AltNameForced = true;
                this.AltName = Name_FR;
            }
            else
            {
                this.AltNameForced = false;
                this.AltName = AltName;
            }
        }
    }
}