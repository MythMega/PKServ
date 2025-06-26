namespace PKServ.Binding
{
    public static class ShinyBinding
    {
        public static string GetIcon(bool isShiny)
        {
            return isShiny ? "https://raw.githubusercontent.com/MythMega/PkServData/refs/heads/master/img/sprite/others/isshiny.png" : "https://raw.githubusercontent.com/MythMega/PkServData/refs/heads/master/img/sprite/others/notshiny.png";
        }
    }
}