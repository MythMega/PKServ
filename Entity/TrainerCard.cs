using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PKServ.Entity
{
    public class TrainerCard
    {
        public List<Background> backgrounds { get; set; }
        public List<Badge> badges { get; set; } = [];

        public TrainerCard()
        {
            backgrounds = new List<Background>();
            badges = new List<Badge>();
        }

        public TrainerCard(List<Requirement> requirements, List<Background> backgrounds, List<Badge> badges)
        {
            this.backgrounds = backgrounds;
            this.badges = badges ?? new List<Badge>();
        }
    }

    public class Background
    {
        public List<Requirement> requirements { get; set; }
        public string Name { get; set; }
        public string Url { get; set; }
        public bool Exclusive { get; set; }
        public string Group { get; set; }
        public List<string> Usercodes { get; set; } = new List<string>();

        public Background(List<Requirement> requirements, string name, string url, bool exclusive, List<string> usercodes)
        {
            this.requirements = requirements;
            Name = name;
            Url = url;
            Exclusive = exclusive;
            Usercodes = usercodes;
        }

        public Background()
        {
        }

        public bool IsUnlocked(User value)
        {
            foreach (Requirement requirement in requirements)
            {
                if (!requirement.IsConditionValid(value))
                {
                    return false;
                }
            }

            if (!IsUsercodeAllowed(value))
            {
                return false;
            }

            return true;
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        public bool IsUsercodeAllowed(User value)
        {
            if (Exclusive)
            {
                return Usercodes.Any(code => code.ToLower() == value.Code_user.ToLower());
            }

            return true;
        }
    }

    public class Requirement
    {
        public string Type { get; set; }
        public int Value { get; set; }

        public Requirement(string type, int value)
        {
            Type = type;
            Value = value;
        }

        public bool IsConditionValid(User value)
        {
            switch (Type)
            {
                case "TotalCatch":
                    return value.Stats.pokeCaught >= Value;

                case "ShinyCatch":
                    return value.Stats.shinyCaught >= Value;

                case "TotalRegistered":
                    return value.Stats.dexCount >= Value;

                case "ShinyRegistered":
                    return value.Stats.shinydex >= Value;

                case "BallLaunched":
                    return value.Stats.ballLaunched >= Value;

                case "MoneySpent":
                    return value.Stats.moneySpent >= Value;

                case "LengendariesRegistered":
                    return value.Stats.LengendariesRegistered >= Value;

                case "CustomRegistered":
                    return value.Stats.CustomRegistered >= Value;

                case "TotalRaid":
                    return value.Stats.RaidCount >= Value;

                case "TotalRaidDamages":
                    return value.Stats.RaidTotalDmg >= Value;

                case "TotalTade":
                    return value.Stats.TradeCount >= Value;

                case "Level":
                    return value.Stats.level >= Value;

                default:
                    return false;
            }
        }
    }
}