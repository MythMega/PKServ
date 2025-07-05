using PKServ.Configuration;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PKServ.Business
{
    public static class TrainerCardImpl
    {
        public static string GetTrainerCardHtml(User user, DataConnexion dataConnexion, AppSettings appSettings, GlobalAppSettings globalAppSettings)
        {
            string data = "";
            User utilisateur = user;
            utilisateur.Code_user = dataConnexion.GetCodeUserByPlatformPseudo(utilisateur);

            string urlAvatar = dataConnexion.GetAvatarUrl(utilisateur);
            string urlSpritePokeFav = dataConnexion.GetSpriteFavoriteCreature(utilisateur, appSettings);

            utilisateur.generateStats();
            utilisateur.generateStatsAchievement(appSettings, globalAppSettings);

            List<Badge> badgeToShow = utilisateur.Stats.badges.Where(x => x.Rarity == "exotic" && x.Obtained).ToList();
            badgeToShow.AddRange(utilisateur.Stats.badges.Where(x => x.Rarity == "legendary" && x.Obtained).ToList());
            badgeToShow.AddRange(utilisateur.Stats.badges.Where(x => x.Rarity == "epic" && x.Obtained).ToList());
            badgeToShow.AddRange(utilisateur.Stats.badges.Where(x => x.Rarity == "rare" && x.Obtained).ToList());
            badgeToShow.AddRange(utilisateur.Stats.badges.Where(x => x.Rarity == "uncommon" && x.Obtained).ToList());
            badgeToShow.AddRange(utilisateur.Stats.badges.Where(x => x.Rarity == "common" && x.Obtained).ToList());

            badgeToShow = badgeToShow.Take(8).ToList();

            string badgePart = "";
            int count = 1;
            foreach (Badge badge in badgeToShow)
            {
                badgePart += $@"
<div class=""col"">
    <img
        src=""{badge.IconUrl}""
        alt=""Badge {count}""
        style=""height: 48px; width: 48px;""
        class=""img-badge img-badge-{badge.Rarity}""
        title=""{badge.Description}"">
    <p style=""font-size: 12px; margin-top: 4px"" class=""textShadow"">{badge.Title}</p>
</div>";
                count++;
            }

            data = @$"
<style>
    .generatedCard {{
      color: white;
      border: 1px solid #ccc;
      padding: 20px;
      width: 856px;
      height: 540px;
      border-radius: 10px;
      background-image: url(""{utilisateur.GetBackground()}"");
    }}
    .img-badge {{
      position: relative;
      display: inline-block;
      transition: transform 0.2s ease-in-out;
    }}

    .img-badge-common {{filter: drop-shadow(0 0 10px white) drop-shadow(0 0 20px white);
            }}
.img-badge-uncommon {{filter: drop-shadow(0 0 10px green) drop-shadow(0 0 20px green);
            }}
.img-badge-rare
            {{filter: drop-shadow(0 0 10px blue) drop-shadow(0 0 20px blue);
}}
.img-badge-epic
            {{filter: drop-shadow(0 0 10px purple) drop-shadow(0 0 20px purple);
            }}
.img-badge-legendary {{filter: drop-shadow(0 0 10px yellow) drop-shadow(0 0 20px yellow);
}}
.img-badge-exotic {{filter: drop-shadow(0 0 10px pink) drop-shadow(0 0 20px pink);
}}

    .img-badge:hover {{transform: scale(1.25) rotate(360deg);
    }}

    #downloadBtn {{
      margin-top: 35px;
      font-size: large;
      border-radius: 3px;
      border: 5px;
      padding: 15px;
      box-shadow: 0px 0px 38px 0px rgba(0,0,0,0.5);
-webkit-box-shadow: 0px 0px 38px 0px rgba(0,0,0,0.5);
-moz-box-shadow: 0px 0px 38px 0px rgba(0,0,0,0.5);
    }}
    .textShadow {{ text-shadow: 0px 0px 11px #000, 0px 0px 11px #000, 0px 0px 20px #000;
font-weight: bolder;
            }}
    .stars {{
  width: 1px;
  height: 1px;
  position: absolute;
  background: white;
  box-shadow: 2vw 5vh 2px white, 10vw 8vh 2px white, 15vw 15vh 1px white,
    22vw 22vh 1px white, 28vw 12vh 2px white, 32vw 32vh 1px white,
    38vw 18vh 2px white, 42vw 35vh 1px white, 48vw 25vh 2px white,
    53vw 42vh 1px white, 58vw 15vh 2px white, 63vw 38vh 1px white,
    68vw 28vh 2px white, 73vw 45vh 1px white, 78vw 32vh 2px white,
    83vw 48vh 1px white, 88vw 20vh 2px white, 93vw 52vh 1px white,
    98vw 35vh 2px white, 5vw 60vh 1px white, 12vw 65vh 2px white,
    18vw 72vh 1px white, 25vw 78vh 2px white, 30vw 85vh 1px white,
    35vw 68vh 2px white, 40vw 82vh 1px white, 45vw 92vh 2px white,
    50vw 75vh 1px white, 55vw 88vh 2px white, 60vw 95vh 1px white,
    65vw 72vh 2px white, 70vw 85vh 1px white, 75vw 78vh 2px white,
    80vw 92vh 1px white, 85vw 82vh 2px white, 90vw 88vh 1px white,
    95vw 75vh 2px white;
  animation: twinkle 8s infinite linear;
}}
@keyframes twinkle {{
  0%,
  100% {{
    opacity: 0.8;
  }}
  50% {{
    opacity: 0.4;
  }}
}}
  </style>
</head>
<body>
  <center>
  <div class=""generatedCard"" style=""background-color: rgba(0, 0, 0, 0.5);"">
  <!-- Partie titre -->
    <div class=""stars""></div>
    <h1 class=""textShadow"">Dresseur : {utilisateur.Pseudo}</h1>
    <h3 class=""textShadow"" style=""margin-bottom: 50px;"">ID : {utilisateur.Code_user}</h3>
    <!-- Partie Corp -->
    <div class=""container"">
      <!-- Photo -->
      <div class=""row"">
        <div class=""col"">
          <img src=""{urlAvatar}"" class=""img-thumbnail"" alt=""userprofile picture"" style=""width: 192px;"" crossorigin=""anonymous"">
        </div>
        <!-- Stats -->
        <div class=""col"">
            <div>
          <p class=""textShadow"">Global dex : {utilisateur.Stats.dexCount}</p>
          <p class=""textShadow"">Shiny dex : {utilisateur.Stats.shinydex}</p>
          <p class=""textShadow"">Trainer since : {utilisateur.Stats.firstCatch.ToString("d MMM. yy", CultureInfo.InvariantCulture)}</p>
          <p class=""textShadow"">Platform : {user.Platform} <img src=""https://raw.githubusercontent.com/MythMega/PkServData/refs/heads/master/img/platform/{user.Platform.ToLower()}.png"" style=""height: 16px; width: 16px;""></p>
          <p class=""textShadow"">Level : {utilisateur.Stats.level}</p>
          <p class=""textShadow"">Captures : {utilisateur.Stats.pokeCaught}</p>
            </div>
        </div>
        <!-- Favorite creature -->
        <div class=""col"">
          <h6 class=""textShadow"">Creature Favorite :</h6>
          <p class=""textShadow"">{Commun.FullInfoShinyNormal(Commun.CapitalizePhrase(utilisateur.Stats.favoritePoke))}</p>
          <img src=""{urlSpritePokeFav}"" alt=""userprofile fav creature"" style=""width: 128px;"">
        </div>
      </div>
    </div>
    <!-- Badges -->
    <div class=""container"">
      <div class=""row"">
        {badgePart}
      </div>
    </div>
  </div>

  <button id=""downloadBtn"">Télécharger sa carte</button>
</center>
<script src=""https://cdnjs.cloudflare.com/ajax/libs/dom-to-image/2.6.0/dom-to-image.min.js""></script>
<script>
  document.getElementById(""downloadBtn"").addEventListener(""click"", function(){{
    var cardElement = document.querySelector("".generatedCard"");
    domtoimage.toPng(cardElement)
      .then(function(dataUrl){{
        var downloadLink = document.createElement('a');
        downloadLink.href = dataUrl;
        downloadLink.download = 'ma-carte.png';
        downloadLink.click();
      }})
      .catch(function(error) {{
        console.error(""Une erreur est survenue :"", error);
      }});
  }});
</script>
";

            return data;
        }
    }
}