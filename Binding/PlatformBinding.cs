namespace PKServ.Binding
{
    public static class PlatformBinding
    {
        public const string PLATFORM_YOUTUBE = "YOUTUBE";
        public const string PLATFORM_TWITCH = "TWITCH";
        public const string PLATFORM_TIKTOK = "TIKTOK";

        /// <summary>
        /// Retourne L'URL de l'îcone
        /// </summary>
        /// <param name="plaform"></param>
        /// <returns></returns>
        public static string IconLink(string plaform)
        {
            plaform = plaform.ToUpper();
            switch (plaform)
            {
                case PLATFORM_YOUTUBE:
                    return "https://raw.githubusercontent.com/MythMega/PkServData/refs/heads/master/img/platform/youtube.png";

                case PLATFORM_TWITCH:
                    return "https://raw.githubusercontent.com/MythMega/PkServData/refs/heads/master/img/platform/twitch.png";

                case PLATFORM_TIKTOK:
                    return "https://raw.githubusercontent.com/MythMega/PkServData/refs/heads/master/img/platform/tiktok.png";

                default:
                    return "https://raw.githubusercontent.com/MythMega/PkServData/refs/heads/master/img/platform/system.png";
            }
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
}