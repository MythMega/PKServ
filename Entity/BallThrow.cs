using PKServ.Configuration;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace PKServ.Entity
{
    public class BallThrow
    {
        public string ChannelSource { get; set; }
        public int Price { get; set; }
    }

    public class BallThrowRequest : BallThrow
    {
        public string UserName { get; set; }
        public string Platform { get; set; }
        public string UserCode { get; set; }
        public string avatarUrl { get; set; }
        public string BallName { get; set; }

        [JsonConstructor]
        public BallThrowRequest(string UserName, string Platform, string UserCode, string ChannelSource, int Price, string BallName, string AvatarUrl = null)
        {
            this.UserName = UserName;
            this.Platform = Platform;
            this.UserCode = UserCode;
            this.ChannelSource = ChannelSource;
            this.Price = Price;
            this.BallName = BallName;
            this.avatarUrl = AvatarUrl;
        }
    }

    public class BallThrowTreatement : BallThrow
    {
        public Pokeball Ball { get; set; }
        public User User { get; set; }

        public BallThrowTreatement(string ChannelSource, int Price, Pokeball Ball, User User)
        {
            this.ChannelSource = ChannelSource;
            this.Price = Price;
            this.Ball = Ball;
            this.User = User;
        }

        public BallThrowTreatement()
        {
            // Default constructor for deserialization or other purposes
        }

        public async Task InitializeAsync(BallThrowRequest ballThrowRequest, AppSettings appSettings, DataConnexion data, GlobalAppSettings globalAppSettings)
        {
            this.ChannelSource = ballThrowRequest.ChannelSource;
            this.Price = ballThrowRequest.Price;
            this.Ball = appSettings.pokeballs.FirstOrDefault(w => Commun.CompareStrings(w.Name, ballThrowRequest.BallName)) ?? throw new KeyNotFoundException("BallThrow.BallThrowTreatement this.Ball not found");
            try
            {
                this.User = data.GetUserBaseInfo(ballThrowRequest.UserCode, ballThrowRequest.Platform, appSettings);
                if (this.User.AvatarUrl != ballThrowRequest.avatarUrl)
                {
                    data.UpdateAvatar(this.User.Code_user, ballThrowRequest.avatarUrl);
                    this.User.AvatarUrl = ballThrowRequest.avatarUrl;
                }
                if (this.User.Pseudo != ballThrowRequest.UserName)
                {
                    await data.UpdateUserPseudo(this.User, ballThrowRequest.UserName);
                    this.User.Pseudo = ballThrowRequest.UserName;
                }
            }
            catch
            {
                this.User = new User
                {
                    Pseudo = ballThrowRequest.UserName,
                    Platform = ballThrowRequest.Platform,
                    Code_user = ballThrowRequest.UserCode,
                    AvatarUrl = ballThrowRequest.avatarUrl,
                    Location = Commun.GetBaseZone(),
                };
                await data.CreateUser(this.User);
            }
        }

        public async Task<string> ProcessAsync(AppSettings appSettings, GlobalAppSettings globalAppSettings, DataConnexion dataConnexion)
        {
            Business.CatchingImpl catchingImpl = new(appSettings, globalAppSettings, dataConnexion);
            return await catchingImpl.Capture(this);
        }
    }
}