using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PKServ.Binding
{
    public static class CreatureRarity
    {
        public const string COMMON = "COMMON";
        public const string UNCOMMON = "UNCOMMON";
        public const string RARE = "RARE";
        public const string EPIC = "EPIC";
        public const string LEGENDARY = "LEGENDARY";
        public const string MYTHICAL = "MYTHICAL";

        public const string _ANY = "ANY"; // uniquement lorsqu on veut ignorer la rareté

        public static readonly int COMMON_CHANCE = 1;
        public static readonly int UNCOMMON_CHANCE = 2;
        public static readonly int RARE_CHANCE = 3;
        public static readonly int EPIC_CHANCE = 4;
        public static readonly int LEGENDARY_CHANCE = 5;
        public static readonly int MYTHICAL_CHANCE = 6;

        public static bool ValidateSelection(string rarity)
        {
            Random random = new Random();
            int rollStrength = 1;
            switch (rarity.ToUpper())
            {
                // 100 %
                case COMMON:
                    rollStrength = COMMON_CHANCE;
                    break;

                // 80 %
                case UNCOMMON:
                    rollStrength = UNCOMMON_CHANCE;
                    break;

                // 50 %
                case RARE:
                    rollStrength = RARE_CHANCE;
                    break;

                // 36.36 %
                case EPIC:
                    rollStrength = EPIC_CHANCE;
                    break;

                // 28.57 %
                case LEGENDARY:
                    rollStrength = LEGENDARY_CHANCE;
                    break;

                // 23.53 %
                case MYTHICAL:
                    rollStrength = MYTHICAL_CHANCE;
                    break;

                default:
                    return true;
            }
            return random.Next(1, rollStrength * 3) < 5;
        }

        public static int GetDivider(string rarity)
        {
            return rarity.ToUpper() switch
            {
                COMMON => COMMON_CHANCE,
                UNCOMMON => UNCOMMON_CHANCE,
                RARE => RARE_CHANCE,
                EPIC => EPIC_CHANCE,
                LEGENDARY => LEGENDARY_CHANCE,
                MYTHICAL => MYTHICAL_CHANCE,
                _ => 1, // Default case if rarity is not recognized
            };
        }

        public static int GetReducedCatchrate(int initialCatchRate, string rarity)
        {
            int divider = GetDivider(rarity);
            if (divider <= 0) return initialCatchRate; // Avoid division by zero
            return (int)Math.Ceiling((double)initialCatchRate / divider);
        }

        public static string IconLink(string rarity)
        {
            return $"https://raw.githubusercontent.com/MythMega/PkServData/refs/heads/master/img/sprite/rarity/{rarity.ToLower()}.png";
        }

        public static string IconHTML(string rarity, IconSize iconSize)
        {
            switch (iconSize)
            {
                case IconSize.Small:
                    return $"<img src={IconLink(rarity)} style='width:32px; height:8px;'>";

                case IconSize.Medium:
                    return $"<img src={IconLink(rarity)} style='width:64px; height:16px;'>";

                case IconSize.Large:
                    return $"<img src={IconLink(rarity)} style='width:128px; height:32px;'>";

                case IconSize.Huge:
                    return $"<img src={IconLink(rarity)} style='width:256px; height:64px;'>";

                default:
                    return "";
            }
        }
    }

    public enum IconSize
    {
        Small,
        Medium,
        Large,
        Huge
    }
}