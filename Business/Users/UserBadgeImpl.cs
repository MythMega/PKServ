using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PKServ.Business.Users
{
    public static class UserBadgeImpl
    {
        public static string GetUserBadgeHTML(AppSettings appSettings, GlobalAppSettings globalAppSettings, PKServ.User user)
        {
            string data = "";
            string wip = "";
            try
            {
                data += $"<p>Level {user.Stats.level}</p><br><p>{user.Stats.currentXP} XP/{user.Stats.MaxXPLevel} XP</p><br><p>{user.Stats.totalXP} XP Totale</p><br>";

                List<string> GroupsBadges = user.Stats.badges.Select(element => element.Group).Distinct().ToList();
                foreach (string group in GroupsBadges)
                {
                    List<Badge> badgesOfThisGroup = user.Stats.badges.Where(g => g.Group == group).ToList();
                    List<string> SubGroupsBadges = badgesOfThisGroup.Select(element => element.SubGroup).Distinct().ToList();
                    data += $"<br><br><br><h2 class=\"col-12\" style=\"margin-top:25px\"><b>{group.ToString()} [{badgesOfThisGroup.Where(x => x.Obtained).Count().ToString()}/{badgesOfThisGroup.Count}]</b></h2>";
                    data += "<div class=\"row\">";
                    foreach (string subgroup in SubGroupsBadges)
                    {
                        List<Badge> badgeOfThisSubgroup = badgesOfThisGroup.Where(element => element.SubGroup == subgroup).ToList();
                        data += $"  <br><br><h4 class=\"col-12\" style=\"margin-top:15px\"><b>{subgroup.ToString()} [{badgeOfThisSubgroup.Where(x => x.Obtained).Count().ToString()}/{badgeOfThisSubgroup.Count}]</b></h4>";
                        data += "   <div class=\"row\">";

                        foreach (Badge badge in badgeOfThisSubgroup)
                        {
                            wip = badge.Obtained ? badge.Description : "????";
                            wip += $" [+{badge.XP}XP]";
                            data += $@"
                            <div style=""width: 29vw;  margin-left: 1vw; margin-bottom: 15px;"">
                                <div class=""card {badge.Rarity.ToLower()}"" style=""background-color: #222222;  height: 220px;"">
                                  <center><br><img src=""{badge.IconUrl}"" class=""card-img-top trophy-{badge.Obtained}"" alt=""..."" style=""height: 96px; width: auto;""></center>
                                  <div class=""card-body"">
                                    <h5 class=""card-title"">{badge.Title}</h5>
                                    <p class=""card-text"">{wip}</p>
                                  </div>
                                </div>
                            </div>";
                        }
                        data += "   </div>";
                    }
                    data += "</div>";
                }
            }
            catch (Exception)
            {
            }
            return data;
        }
    }
}