namespace PKServ.Binding
{
    public static class TypeBinding
    {
        public static string GetImageUrl(string type)
        {
            switch (type.ToLower())
            {
                case "normal":
                    return "https://raw.githubusercontent.com/MythMega/PkServData/refs/heads/master/img/sprite/type/banner/sv/140px-NormalIC_SV.png";

                case "fire":
                    return "https://raw.githubusercontent.com/MythMega/PkServData/refs/heads/master/img/sprite/type/banner/sv/140px-FireIC_SV.png";

                case "water":
                    return "https://raw.githubusercontent.com/MythMega/PkServData/refs/heads/master/img/sprite/type/banner/sv/140px-WaterIC_SV.png";

                case "electric":
                    return "https://raw.githubusercontent.com/MythMega/PkServData/refs/heads/master/img/sprite/type/banner/sv/140px-ElectricIC_SV.png";

                case "grass":
                    return "https://raw.githubusercontent.com/MythMega/PkServData/refs/heads/master/img/sprite/type/banner/sv/140px-GrassIC_SV.png";

                case "ice":
                    return "https://raw.githubusercontent.com/MythMega/PkServData/refs/heads/master/img/sprite/type/banner/sv/140px-IceIC_SV.png";

                case "fighting":
                    return "https://raw.githubusercontent.com/MythMega/PkServData/refs/heads/master/img/sprite/type/banner/sv/140px-FightingIC_SV.png";

                case "poison":
                    return "https://raw.githubusercontent.com/MythMega/PkServData/refs/heads/master/img/sprite/type/banner/sv/140px-PoisonIC_SV.png";

                case "ground":
                    return "https://raw.githubusercontent.com/MythMega/PkServData/refs/heads/master/img/sprite/type/banner/sv/140px-GroundIC_SV.png";

                case "flying":
                    return "https://raw.githubusercontent.com/MythMega/PkServData/refs/heads/master/img/sprite/type/banner/sv/140px-FlyingIC_SV.png";

                case "psychic":
                    return "https://raw.githubusercontent.com/MythMega/PkServData/refs/heads/master/img/sprite/type/banner/sv/140px-PsychicIC_SV.png";

                case "bug":
                    return "https://raw.githubusercontent.com/MythMega/PkServData/refs/heads/master/img/sprite/type/banner/sv/140px-BugIC_SV.png";

                case "rock":
                    return "https://raw.githubusercontent.com/MythMega/PkServData/refs/heads/master/img/sprite/type/banner/sv/140px-RockIC_SV.png";

                case "ghost":
                    return "https://raw.githubusercontent.com/MythMega/PkServData/refs/heads/master/img/sprite/type/banner/sv/140px-GhostIC_SV.png";

                case "dragon":
                    return "https://raw.githubusercontent.com/MythMega/PkServData/refs/heads/master/img/sprite/type/banner/sv/140px-DragonIC_SV.png";

                case "dark":
                    return "https://raw.githubusercontent.com/MythMega/PkServData/refs/heads/master/img/sprite/type/banner/sv/140px-DarkIC_SV.png";

                case "steel":
                    return "https://raw.githubusercontent.com/MythMega/PkServData/refs/heads/master/img/sprite/type/banner/sv/140px-SteelIC_SV.png";

                case "fairy":
                    return "https://raw.githubusercontent.com/MythMega/PkServData/refs/heads/master/img/sprite/type/banner/sv/140px-FairyIC_SV.png";

                default:
                    return "https://cdn-icons-png.flaticon.com/512/2748/2748558.png";
            }
        }
    }
}