using PKServ.Entity;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PKServ.Admin
{
    internal class TempFix
    {
        public static async Task<BallThrowRequest> FixUserNameYoutube(BallThrowRequest ballThrowRequest, Configuration.DataConnexion data)
        {
            if (ballThrowRequest.Platform.ToString() == "youtube" && ballThrowRequest.UserName == "…loading…")
            {
                var a = await data.GetUserByCodeUser(ballThrowRequest.UserCode);
                ballThrowRequest.UserName = a.Pseudo;
            }
            return ballThrowRequest;
        }
    }
}