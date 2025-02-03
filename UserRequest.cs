using PKServ.Configuration;
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

        public UserRequest(string userName, string platform, string triggerName, string channelSource, int price, string userCode = "")
        {
            UserName = userName;
            Platform = platform;
            TriggerName = triggerName;
            ChannelSource = channelSource;
            Price = price;
            UserCode = userCode;
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

        public GetPokeStats(User User, string Name)
        {
            this.User = User;
            this.Name = Name.ToLower().Replace("_", " ");
        }
    }
}