using PKServ.Business.Raid;
using PKServ.Configuration;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace PKServ
{
    public class CustomOverlay()
    {
        private DataConnexion data;
        private AppSettings settings;
        private GlobalAppSettings globalAppSettings;
        private List<User> usersHere;
        private DateTime startTime = DateTime.Now;
        private readonly string folderLocation = "StreamOverlays";
        private Dictionary<string, string> textVariables = [];
        public string Filename { get; set; }
        public string Content { get; set; }

        public void SetEnv(DataConnexion data, AppSettings settings, GlobalAppSettings globalAppSettings, List<User> users)
        {
            this.data = data;
            this.settings = settings;
            this.globalAppSettings = globalAppSettings;
            usersHere = users;
        }

        public async Task BuildOverlay(bool firstLaunch)
        {
            try
            {
                // required var
                var data_newTrainer = data.GetAllUserPlatforms().Where(user => user.Stats.firstCatch > startTime).ToList();
                List<Entrie> allentries = data.GetAllEntries();
                await RaidOverlayImpl.WriteOverlay(gas: globalAppSettings);

                textVariables = new Dictionary<string, string>
                {
                    { "$userCount", usersHere.Count.ToString()},
                    { "$allPokemonCount", settings.pokemons.Count.ToString()},
                    { "$allPokemonCustomCount", settings.pokemons.Where(x => x.isCustom).Count().ToString()},
                    { "$allPokemonLegendaryCount", settings.pokemons.Where(x => x.isLegendary).Count().ToString()},
                    { "$allBadgesCount", settings.badges.Count.ToString()},
                    { "$sessionCatchCount", settings.catchHistory.Count.ToString()},
                    { "$lastCatchTrainerPseudo", settings.catchHistory.Last().User.Pseudo},
                    { "$lastCatchTrainerPlatform", settings.catchHistory.Last().User.Platform},
                    { "$lastCatchPokemonCaughtNameEN", settings.catchHistory.Last().Pokemon.Name_EN},
                    { "$lastCatchPokemonCaughtNameFR", settings.catchHistory.Last().Pokemon.Name_FR},
                    { "$lastCatchPokemonCaughtIsShiny", settings.catchHistory.Last().shiny ? "shiny" : "normal"},
                    { "$lastCatchPokeballUsedName", settings.catchHistory.Last().Ball.Name},
                    { "$lastCatchPokeballUsedCatchRate", settings.catchHistory.Last().Ball.catchrate.ToString()},
                    { "$lastCatchPokeballUsedShinyRate", settings.catchHistory.Last().Ball.shinyrate.ToString()},
                    { "$lastCatchDateTime", settings.catchHistory.Last().time.ToString("G")},
                    { "$lastCatchTime", settings.catchHistory.Last().time.ToString("t")},
                    { "$lastCatchDate", settings.catchHistory.Last().time.ToString("d")},
                    { "$sessionNewTrainerCount", data_newTrainer.Count.ToString() },
                    { "$sessionNewTrainerLastName", data_newTrainer.Last().Pseudo },
                    { "$sessionNewTrainerLastPlatform", data_newTrainer.Last().Platform },
                };

                foreach (string key in textVariables.Keys)
                {
                    Content = Content.Replace(key, textVariables[key]);
                }

                WriteFile();
            }
            catch
            {
                if (!firstLaunch)
                {
                    Console.WriteLine(Filename + " not yet generated.");
                }
            }
        }

        public void WriteFile()
        {
            // Écrit le contenu de chaque overlay dans un fichier pour chaque
            File.WriteAllText(Path.Combine(folderLocation, Filename), Content);
        }
    }

    public class Overlay
    {
        public string filename { get; set; }
        public string content { get; set; }

        public Overlay(string filename, string content)
        {
            this.filename = filename;
            this.content = content;
        }
    }

    public class OverlayGeneration
    {
        private readonly DataConnexion data;
        private readonly AppSettings settings;
        private readonly GlobalAppSettings globalAppSettings;
        private readonly List<User> usersHere;

        private DateTime lastUpdateTime = DateTime.Now;
        private string folderLocation = "StreamOverlays";
        private Dictionary<string, string> files = new();

        public OverlayGeneration(DataConnexion data, AppSettings settings, GlobalAppSettings globalAppSettings, List<User> usersHere)
        {
            this.data = data;
            this.settings = settings;
            this.globalAppSettings = globalAppSettings;
            this.usersHere = usersHere;

            lastUpdateTime = DateTime.Now;
            folderLocation = "StreamOverlays";
        }

        public void FirstRun()
        {
            SearchValue searcher = new SearchValue();
            searcher.SetEnv(data, settings, globalAppSettings, usersHere);
            int port = globalAppSettings.ServerPort;

            // Crée le dossier "Exports" s'il n'existe pas
            if (!Directory.Exists(folderLocation))
                Directory.CreateDirectory(folderLocation);

            // dex of all viewers combined
            files["progressGlobalDex.html"] = @$"
<!DOCTYPE html>
<html lang=""en"">
<head>
    <meta charset=""UTF-8"">
    <meta name=""viewport"" content=""width=device-width, initial-scale=1.0"">
    <title>Progress Dex</title>
    <style>
        body {{
            background: none;
        }}

        .progress {{
            --progress: 0%;
            width: 500px;
            height: 50px;
            margin: 0 0;
            border: 1px solid #fff;
            padding: 12px 10px;
            box-shadow: 0 0 10px #aaa;
        }}

        .progress .bar {{
            position: relative; /* Pour permettre le positionnement absolu des éléments enfants */
            width: var(--progress);
            height: 100%;
            background: linear-gradient(gold, #c85, gold);
            background-repeat: repeat;
            box-shadow: 0 0 10px 0px orange;
            animation:
                shine 4s ease-in infinite,
                end 1s ease-out 1;
            transition: width 3s ease;
        }}

        .progress .bar span {{
            position: absolute;
            top: 50%;
            left: 50%;
            transform: translate(-50%, -50%);
            color: white; /* Pour que le texte soit visible sur le fond */
            font-size: 40px;
            font-weight: bold;
        }}

        @property --progress {{
            syntax: ""<length>"";
            initial-value: 0%;
            inherits: true;
        }}

        @keyframes shine {{
            0% {{ background-position: 0 0; }}
            100% {{ background-position: 0 50px; }}
        }}

        @keyframes end {{
            0%, 100% {{ box-shadow: 0 0 10px 0px orange; }}
            50% {{ box-shadow: 0 0 15px 5px orange; }}
        }}
    </style>
</head>
<body>
    <div class=""progress"">
        <div class=""bar"">
            <span id=""progress-text"">0/0</span>
            <div class=""progress-value""></div>
        </div>
    </div>

    <script>
        async function fetchProgress() {{
            try {{
                // Appel à l'API pour obtenir la valeur de pokecount
                const pokecountResponse = await fetch('http://localhost:{port}/Get?Value=overlayProgressGlobalDex');
                console.log(pokecountResponse);
				const pokecountData = await pokecountResponse.json();

                const maxProgress = pokecountData.total;
                const currentProgress = pokecountData.progress;

                // Calculer le pourcentage de progression
                const progressPercentage = (currentProgress / maxProgress) * 100;

                // Mettre à jour la progression et le texte affiché
                const progress = document.querySelector("".progress"");
                const progressText = document.getElementById(""progress-text"");

                progress.style.setProperty(""--progress"", `${{progressPercentage}}%`);
                progressText.textContent = `${{currentProgress}}/${{maxProgress}}`;
            }} catch (error) {{
                console.error('Erreur:', error);
            }}
        }}

        // Appelle la fonction toutes les 10 secondes (10000 millisecondes)
        setInterval(fetchProgress, 10000);

        // Appel initial pour mettre à jour la barre de progression dès le chargement de la page
        fetchProgress();
    </script>
</body>
</html>
";

            // shinydex of all viewers combined
            files["progressShinyDex.html"] = @$"
<!DOCTYPE html>
<html lang=""en"">
<head>
    <meta charset=""UTF-8"">
    <meta name=""viewport"" content=""width=device-width, initial-scale=1.0"">
    <title>Progress Dex</title>
    <style>
        body {{
            background: none;
        }}

        .progress {{
            --progress: 0%;
            width: 500px;
            height: 50px;
            margin: 0 0;
            border: 1px solid #fff;
            padding: 12px 10px;
            box-shadow: 0 0 10px #aaa;
        }}

        .progress .bar {{
            position: relative; /* Pour permettre le positionnement absolu des éléments enfants */
            width: var(--progress);
            height: 100%;
            background: linear-gradient(gold, #c85, gold);
            background-repeat: repeat;
            box-shadow: 0 0 10px 0px orange;
            animation:
                shine 4s ease-in infinite,
                end 1s ease-out 1;
            transition: width 3s ease;
        }}

        .progress .bar span {{
            position: absolute;
            top: 50%;
            left: 50%;
            transform: translate(-50%, -50%);
            color: white; /* Pour que le texte soit visible sur le fond */
            font-size: 40px;
            font-weight: bold;
        }}

        @property --progress {{
            syntax: ""<length>"";
            initial-value: 0%;
            inherits: true;
        }}

        @keyframes shine {{
            0% {{ background-position: 0 0; }}
            100% {{ background-position: 0 50px; }}
        }}

        @keyframes end {{
            0%, 100% {{ box-shadow: 0 0 10px 0px orange; }}
            50% {{ box-shadow: 0 0 15px 5px orange; }}
        }}
    </style>
</head>
<body>
    <div class=""progress"">
        <div class=""bar"">
            <span id=""progress-text"">0/0</span>
            <div class=""progress-value""></div>
        </div>
    </div>

    <script>
        async function fetchProgress() {{
            try {{
                // Appel à l'API pour obtenir la valeur de pokecount
                const pokecountResponse = await fetch('http://localhost:{port}/Get?Value=overlayProgressGlobalShinyDex');
                console.log(pokecountResponse);
				const pokecountData = await pokecountResponse.json();

                const maxProgress = pokecountData.total;
                const currentProgress = pokecountData.progress;

                // Calculer le pourcentage de progression
                const progressPercentage = (currentProgress / maxProgress) * 100;

                // Mettre à jour la progression et le texte affiché
                const progress = document.querySelector("".progress"");
                const progressText = document.getElementById(""progress-text"");

                progress.style.setProperty(""--progress"", `${{progressPercentage}}%`);
                progressText.textContent = `${{currentProgress}}/${{maxProgress}}`;
            }} catch (error) {{
                console.error('Erreur:', error);
            }}
        }}

        // Appelle la fonction toutes les 10 secondes (10000 millisecondes)
        setInterval(fetchProgress, 10000);

        // Appel initial pour mettre à jour la barre de progression dès le chargement de la page
        fetchProgress();
    </script>
</body>
</html>
";

            // Last Poké Caught
            files["lastCaughtPokeSprite.html"] = @$"
<!DOCTYPE html>
<html lang=""en"">
<head>
    <meta charset=""UTF-8"">
    <meta name=""viewport"" content=""width=device-width, initial-scale=1.0"">
    <title>Image API Update</title>
    <style>
        body {{
            background: black;
            display: flex;
            justify-content: center;
            align-items: center;
            height: 100vh;
            margin: 0;
        }}

        .image-container {{
            position: relative;
        }}

        img {{
            width: 64px;
            height: auto;
            opacity: 1;
            animation: fadeOut 5s infinite;
        }}

        @keyframes fadeOut {{
            0% {{
                opacity: 1;
            }}
            5% {{
                opacity: 1;
            }}
            80% {{
                opacity: 0;
            }}
            100% {{
                opacity: 0;
            }}
        }}

        /* Classe pour forcer le redémarrage de l'animation */
        .restart-animation {{
            animation: none;
        }}
    </style>
</head>
<body>
    <div class=""image-container"">
        <img id=""pokeImage"" src="""" alt=""Pokémon Sprite"">
    </div>

    <script>
        async function fetchImage() {{
            try {{
                const response = await fetch('http://localhost:{port}/Get?Value=lastPokeCaughtSprite');
                const data = await response.json();  // Assuming the API returns a JSON object
                console.log(data);
                const imageUrl = data.imageUrl;  // Adjust based on your actual JSON structure

                const img = document.getElementById('pokeImage');

                // Change the source of the image without altering its animation state
                img.src = imageUrl;

                // Forcer le redémarrage de l'animation
                img.classList.add('restart-animation');
                void img.offsetWidth; // Cette ligne force le reflow, nécessaire pour redémarrer l'animation
                img.classList.remove('restart-animation');

                console.log(imageUrl);
            }} catch (error) {{
                console.error('Erreur:', error);
            }}
        }}

        // Initial call to fetch the image
        fetchImage();

        // Call fetchImage every 5 seconds
        setInterval(fetchImage, 5000);
    </script>
</body>
</html>

";

            // Last Poké Caught (new)
            files["lastCaughtPokeSpriteNew.html"] = @$"
<!DOCTYPE html>
<html lang=""en"">
<head>
    <meta charset=""UTF-8""/>
    <meta name=""viewport"" content=""width=device-width, initial-scale=1.0""/>
    <title>PokéSprite Synchro</title>
    <style>
        body {{
            background: black;
            display: flex;
            justify-content: center;
            align-items: center;
            height: 100vh;
            margin: 0;
            flex-direction: column;
        }}

        .image-container {{
            position: relative;
        }}

        @keyframes fadeInOut {{
            0%, 100% {{ opacity: 0; }}
            5%, 80%  {{ opacity: 1; }}
        }}

        img {{
            width: 64px;
            height: auto;
            animation: fadeInOut 5s infinite;
        }}

        .image-container, #username {{
            display: block;
            text-align: center;
        }}


        #username {{
            display: block;
            margin-top: 10px;
            font-size: 28px;
            color: white;
            text-align: center;
            text-shadow:
                -2px -2px 0 black,
                 2px -2px 0 black,
                -2px  2px 0 black,
                 2px  2px 0 black;
        }}
    </style>
</head>
<body>
    <div class=""image-container"">
        <img id=""pokeImage"" src="""" alt=""Pokémon Sprite""/>
    </div>
        <span id=""username""></span>

    <script>
        async function fetchImage() {{
            try {{
                const response = await fetch('http://localhost:{globalAppSettings.ServerPort}/Get?Value=lastPokeCaughtSprite');
                const data = await response.json();
                document.getElementById('pokeImage').src = data.imageUrl;
                document.getElementById('username').textContent = data.userName;
            }} catch (error) {{
                console.error('Erreur fetch sprite :', error);
            }}
        }}

        // Chargement initial
        fetchImage();

        // À chaque fin de cycle d'animation (opacité = 0) on met à jour le sprite
        document
          .getElementById('pokeImage')
          .addEventListener('animationiteration', fetchImage);
    </script>
</body>
</html>
";

            // Last Poké Caught (last throw result)
            files["barBallThrowResume.html"] = @$"
<!DOCTYPE html>
<html lang=""en"">
<head>
    <meta charset=""UTF-8""/>
    <meta name=""viewport"" content=""width=device-width, initial-scale=1.0""/>
    <title>Pokémon Catch Display</title>
    <style>
        body {{
            background: black;
            display: flex;
            justify-content: center;
            align-items: center;
            height: 100vh;
            margin: 0;
            font-family: Arial, sans-serif;
            color: white;
        }}

        .display-container {{
            display: flex;
            align-items: center;
            gap: 15px;
            opacity: 0;
            transition: opacity 0.5s ease-in-out;
        }}

        .display-container.visible {{
            opacity: 1;
        }}

        .time, .username {{
            font-size: 96px;
            font-weight: bold;
            height: 128px;
            display: flex;
            align-items: center;
        }}

        .time {{
            color: #FFD700;
        }}

        .username {{
            color: #87CEEB;
        }}

        img {{
            height: 128px;
            min-height: 128px;
            width: auto;
            image-rendering: pixelated;
        }}

        .separator {{
            font-size: 32px;
            height: 128px;
            display: flex;
            align-items: center;
        }}
    </style>
</head>
<body>
    <div class=""display-container"" id=""displayContainer"">
        <span class=""time"" id=""timeDisplay""></span>
        <img id=""platformIcon"" src="""" alt=""Platform"" style=""display: none;""/>
        <span class=""username"" id=""username""></span>
        <span class=""separator"">-</span>
        <img id=""pokeImage"" src="""" alt=""Pokémon"" style=""display: none;""/>
        <img id=""shinyIcon"" src="""" alt=""Shiny"" style=""display: none;""/>
        <img id=""catchIcon"" src="""" alt=""Catch Result"" style=""display: none;""/>
    </div>

    <script>
        let nullCount = 0;
        let currentData = null;

        const SHINY_ICON = 'https://cdn-icons-png.flaticon.com/256/2267/2267359.png';
        const CAUGHT_ICON = 'https://upload.wikimedia.org/wikipedia/commons/thumb/5/53/Pok%C3%A9_Ball_icon.svg/960px-Pok%C3%A9_Ball_icon.svg.png';
        const NOT_CAUGHT_ICON = 'https://cdn-icons-png.flaticon.com/512/6659/6659895.png';

        function formatTime(timeString) {{
            // timeString est au format ""HHmmss""
            if (timeString && timeString.length === 6) {{
                const hours = timeString.substring(0, 2);
                const minutes = timeString.substring(2, 4);
                const seconds = timeString.substring(4, 6);
                return `[${{hours}}:${{minutes}}:${{seconds}}]`;
            }}
            return timeString;
        }}

        function updateDisplay(data) {{
            const container = document.getElementById('displayContainer');

            // Fade out
            container.classList.remove('visible');

            setTimeout(() => {{
                // Update content
                document.getElementById('timeDisplay').textContent = formatTime(data.time);
                document.getElementById('username').textContent = data.userName;

                // Platform icon
                const platformIcon = document.getElementById('platformIcon');
                if (data.userPlateformIcon) {{
                    platformIcon.src = data.userPlateformIcon;
                    platformIcon.style.display = 'block';
                }} else {{
                    platformIcon.style.display = 'none';
                }}

                // Pokemon image
                const pokeImage = document.getElementById('pokeImage');
                if (data.imageUrl) {{
                    pokeImage.src = data.imageUrl;
                    pokeImage.style.display = 'block';
                }} else {{
                    pokeImage.style.display = 'none';
                }}

                // Shiny icon
                const shinyIcon = document.getElementById('shinyIcon');
                if (data.isShiny === 'true' || data.isShiny === true) {{
                    shinyIcon.src = SHINY_ICON;
                    shinyIcon.style.display = 'block';
                }} else {{
                    shinyIcon.style.display = 'none';
                }}

                // Catch result icon
                const catchIcon = document.getElementById('catchIcon');
                if (data.isCaught === 'true' || data.isCaught === true) {{
                    catchIcon.src = CAUGHT_ICON;
                    catchIcon.style.display = 'block';
                }} else if (data.isCaught === 'false' || data.isCaught === false) {{
                    catchIcon.src = NOT_CAUGHT_ICON;
                    catchIcon.style.display = 'block';
                }} else {{
                    catchIcon.style.display = 'none';
                }}

                // Fade in
                container.classList.add('visible');
            }}, 250);
        }}

        function hideDisplay() {{
            const container = document.getElementById('displayContainer');
            container.classList.remove('visible');
        }}

        async function fetchData() {{
            try {{
                const response = await fetch('http://localhost:{globalAppSettings.ServerPort}/Get?Value=lastpokecaughtcatchresume');

                if (!response.ok) {{
                    throw new Error('Network response was not ok');
                }}
                console.log(response);

                const data = await response.json();
                // Vérifier si le JSON est valide et contient des données
                if (data && typeof data === 'object' && Object.keys(data).length > 0) {{
                    nullCount = 0;
                    currentData = data;
                    updateDisplay(data);
                }} else {{
                    // JSON null ou vide
                    nullCount++;
                    if (nullCount >= 5) {{
                        hideDisplay();
                    }}
                    // Sinon, on garde l'affichage actuel
                }}
            }} catch (error) {{
                console.error('Erreur lors de la récupération des données:', error);
                nullCount++;
                if (nullCount >= 5) {{
                    hideDisplay();
                }}
                // En cas d'erreur, on garde l'affichage actuel si nullCount < 5
            }}
        }}

        // Chargement initial
        fetchData();

        // Rafraîchissement toutes les 3 secondes
        setInterval(fetchData, 3000);
    </script>
</body>
</html>
";

            if (globalAppSettings.OverlaySettings.GlobalTotalCaughtGoal.Enabled)
            {
                files["GlobalTotalCaughtGoal.html"] = @$"
<!DOCTYPE html>
<html lang=""en"">
<head>
    <meta charset=""UTF-8"">
    <meta name=""viewport"" content=""width=device-width, initial-scale=1.0"">
    <title>Progress Dex</title>
    <style>
        body {{
            background: none;
        }}

        .progress {{
            --progress: 0%;
            width: 500px;
            height: 50px;
            margin: 0 0;
            border: 1px solid #fff;
            padding: 12px 10px;
            box-shadow: 0 0 10px #aaa;
        }}

        .progress .bar {{
            position: relative; /* Pour permettre le positionnement absolu des éléments enfants */
            width: var(--progress);
            height: 100%;
            background: linear-gradient(gold, #c85, gold);
            background-repeat: repeat;
            box-shadow: 0 0 10px 0px orange;
            animation:
                shine 4s ease-in infinite,
                end 1s ease-out 1;
            transition: width 3s ease;
        }}

        .progress .bar span {{
            position: absolute;
            top: 50%;
            left: 50%;
            transform: translate(-50%, -50%);
            color: white; /* Pour que le texte soit visible sur le fond */
            font-size: 40px;
            font-weight: bold;
        }}

        @property --progress {{
            syntax: ""<length>"";
            initial-value: 0%;
            inherits: true;
        }}

        @keyframes shine {{
            0% {{ background-position: 0 0; }}
            100% {{ background-position: 0 50px; }}
        }}

        @keyframes end {{
            0%, 100% {{ box-shadow: 0 0 10px 0px orange; }}
            50% {{ box-shadow: 0 0 15px 5px orange; }}
        }}
    </style>
</head>
<body>
    <div class=""progress"">
        <div class=""bar"">
            <span id=""progress-text"">0/0</span>
            <div class=""progress-value""></div>
        </div>
    </div>

    <script>
        async function fetchProgress() {{
            try {{
                // Appel à l'API pour obtenir la valeur de pokecount
                const pokecountResponse = await fetch('http://localhost:{port}/Get?Value=GlobalTotalCaughtGoal');
                console.log(pokecountResponse);
				const pokecountData = await pokecountResponse.json();

                const maxProgress = pokecountData.total;
                const currentProgress = pokecountData.progress;

                // Calculer le pourcentage de progression
                const progressPercentage = (currentProgress / maxProgress) * 100;

                // Mettre à jour la progression et le texte affiché
                const progress = document.querySelector("".progress"");
                const progressText = document.getElementById(""progress-text"");

                progress.style.setProperty(""--progress"", `${{progressPercentage}}%`);
                progressText.textContent = `${{currentProgress}}/${{maxProgress}}`;
            }} catch (error) {{
                console.error('Erreur:', error);
            }}
        }}

        // Appelle la fonction toutes les 10 secondes (10000 millisecondes)
        setInterval(fetchProgress, 10000);

        // Appel initial pour mettre à jour la barre de progression dès le chargement de la page
        fetchProgress();
    </script>
</body>
</html>
";
            }

            if (globalAppSettings.OverlaySettings.GlobalShinyCaughtGoal.Enabled)
            {
                files["GlobalShinyCaughtGoal.html"] = @$"
<!DOCTYPE html>
<html lang=""en"">
<head>
    <meta charset=""UTF-8"">
    <meta name=""viewport"" content=""width=device-width, initial-scale=1.0"">
    <title>Progress Dex</title>
    <style>
        body {{
            background: none;
        }}

        .progress {{
            --progress: 0%;
            width: 500px;
            height: 50px;
            margin: 0 0;
            border: 1px solid #fff;
            padding: 12px 10px;
            box-shadow: 0 0 10px #aaa;
        }}

        .progress .bar {{
            position: relative; /* Pour permettre le positionnement absolu des éléments enfants */
            width: var(--progress);
            height: 100%;
            background: linear-gradient(gold, #c85, gold);
            background-repeat: repeat;
            box-shadow: 0 0 10px 0px orange;
            animation:
                shine 4s ease-in infinite,
                end 1s ease-out 1;
            transition: width 3s ease;
        }}

        .progress .bar span {{
            position: absolute;
            top: 50%;
            left: 50%;
            transform: translate(-50%, -50%);
            color: white; /* Pour que le texte soit visible sur le fond */
            font-size: 40px;
            font-weight: bold;
        }}

        @property --progress {{
            syntax: ""<length>"";
            initial-value: 0%;
            inherits: true;
        }}

        @keyframes shine {{
            0% {{ background-position: 0 0; }}
            100% {{ background-position: 0 50px; }}
        }}

        @keyframes end {{
            0%, 100% {{ box-shadow: 0 0 10px 0px orange; }}
            50% {{ box-shadow: 0 0 15px 5px orange; }}
        }}
    </style>
</head>
<body>
    <div class=""progress"">
        <div class=""bar"">
            <span id=""progress-text"">0/0</span>
            <div class=""progress-value""></div>
        </div>
    </div>

    <script>
        async function fetchProgress() {{
            try {{
                // Appel à l'API pour obtenir la valeur de pokecount
                const pokecountResponse = await fetch('http://localhost:{port}/Get?Value=GlobalShinyCaughtGoal');
                console.log(pokecountResponse);
				const pokecountData = await pokecountResponse.json();

                const maxProgress = pokecountData.total;
                const currentProgress = pokecountData.progress;

                // Calculer le pourcentage de progression
                const progressPercentage = (currentProgress / maxProgress) * 100;

                // Mettre à jour la progression et le texte affiché
                const progress = document.querySelector("".progress"");
                const progressText = document.getElementById(""progress-text"");

                progress.style.setProperty(""--progress"", `${{progressPercentage}}%`);
                progressText.textContent = `${{currentProgress}}/${{maxProgress}}`;
            }} catch (error) {{
                console.error('Erreur:', error);
            }}
        }}

        // Appelle la fonction toutes les 10 secondes (10000 millisecondes)
        setInterval(fetchProgress, 10000);

        // Appel initial pour mettre à jour la barre de progression dès le chargement de la page
        fetchProgress();
    </script>
</body>
</html>
";
            }

            if (globalAppSettings.OverlaySettings.SessionParticipantsGoal.Enabled)
            {
                files["SessionParticipantsGoal.html"] = @$"
<!DOCTYPE html>
<html lang=""en"">
<head>
    <meta charset=""UTF-8"">
    <meta name=""viewport"" content=""width=device-width, initial-scale=1.0"">
    <title>Progress Dex</title>
    <style>
        body {{
            background: none;
        }}

        .progress {{
            --progress: 0%;
            width: 500px;
            height: 50px;
            margin: 0 0;
            border: 1px solid #fff;
            padding: 12px 10px;
            box-shadow: 0 0 10px #aaa;
        }}

        .progress .bar {{
            position: relative; /* Pour permettre le positionnement absolu des éléments enfants */
            width: var(--progress);
            height: 100%;
            background: linear-gradient(gold, #c85, gold);
            background-repeat: repeat;
            box-shadow: 0 0 10px 0px orange;
            animation:
                shine 4s ease-in infinite,
                end 1s ease-out 1;
            transition: width 3s ease;
        }}

        .progress .bar span {{
            position: absolute;
            top: 50%;
            left: 50%;
            transform: translate(-50%, -50%);
            color: white; /* Pour que le texte soit visible sur le fond */
            font-size: 40px;
            font-weight: bold;
        }}

        @property --progress {{
            syntax: ""<length>"";
            initial-value: 0%;
            inherits: true;
        }}

        @keyframes shine {{
            0% {{ background-position: 0 0; }}
            100% {{ background-position: 0 50px; }}
        }}

        @keyframes end {{
            0%, 100% {{ box-shadow: 0 0 10px 0px orange; }}
            50% {{ box-shadow: 0 0 15px 5px orange; }}
        }}
    </style>
</head>
<body>
    <div class=""progress"">
        <div class=""bar"">
            <span id=""progress-text"">0/0</span>
            <div class=""progress-value""></div>
        </div>
    </div>

    <script>
        async function fetchProgress() {{
            try {{
                // Appel à l'API pour obtenir la valeur de pokecount
                const pokecountResponse = await fetch('http://localhost:{port}/Get?Value=SessionParticipantsGoal');
                console.log(pokecountResponse);
				const pokecountData = await pokecountResponse.json();

                const maxProgress = pokecountData.total;
                const currentProgress = pokecountData.progress;

                // Calculer le pourcentage de progression
                const progressPercentage = (currentProgress / maxProgress) * 100;

                // Mettre à jour la progression et le texte affiché
                const progress = document.querySelector("".progress"");
                const progressText = document.getElementById(""progress-text"");

                progress.style.setProperty(""--progress"", `${{progressPercentage}}%`);
                progressText.textContent = `${{currentProgress}}/${{maxProgress}}`;
            }} catch (error) {{
                console.error('Erreur:', error);
            }}
        }}

        // Appelle la fonction toutes les 10 secondes (10000 millisecondes)
        setInterval(fetchProgress, 10000);

        // Appel initial pour mettre à jour la barre de progression dès le chargement de la page
        fetchProgress();
    </script>
</body>
</html>
";
            }

            if (globalAppSettings.OverlaySettings.SessionShinyCaughtGoal.Enabled)
            {
                files["SessionShinyCaughtGoal.html"] = @$"
<!DOCTYPE html>
<html lang=""en"">
<head>
    <meta charset=""UTF-8"">
    <meta name=""viewport"" content=""width=device-width, initial-scale=1.0"">
    <title>Progress Dex</title>
    <style>
        body {{
            background: none;
        }}

        .progress {{
            --progress: 0%;
            width: 500px;
            height: 50px;
            margin: 0 0;
            border: 1px solid #fff;
            padding: 12px 10px;
            box-shadow: 0 0 10px #aaa;
        }}

        .progress .bar {{
            position: relative; /* Pour permettre le positionnement absolu des éléments enfants */
            width: var(--progress);
            height: 100%;
            background: linear-gradient(gold, #c85, gold);
            background-repeat: repeat;
            box-shadow: 0 0 10px 0px orange;
            animation:
                shine 4s ease-in infinite,
                end 1s ease-out 1;
            transition: width 3s ease;
        }}

        .progress .bar span {{
            position: absolute;
            top: 50%;
            left: 50%;
            transform: translate(-50%, -50%);
            color: white; /* Pour que le texte soit visible sur le fond */
            font-size: 40px;
            font-weight: bold;
        }}

        @property --progress {{
            syntax: ""<length>"";
            initial-value: 0%;
            inherits: true;
        }}

        @keyframes shine {{
            0% {{ background-position: 0 0; }}
            100% {{ background-position: 0 50px; }}
        }}

        @keyframes end {{
            0%, 100% {{ box-shadow: 0 0 10px 0px orange; }}
            50% {{ box-shadow: 0 0 15px 5px orange; }}
        }}
    </style>
</head>
<body>
    <div class=""progress"">
        <div class=""bar"">
            <span id=""progress-text"">0/0</span>
            <div class=""progress-value""></div>
        </div>
    </div>

    <script>
        async function fetchProgress() {{
            try {{
                // Appel à l'API pour obtenir la valeur de pokecount
                const pokecountResponse = await fetch('http://localhost:{port}/Get?Value=SessionShinyCaughtGoal');
                console.log(pokecountResponse);
				const pokecountData = await pokecountResponse.json();

                const maxProgress = pokecountData.total;
                const currentProgress = pokecountData.progress;

                // Calculer le pourcentage de progression
                const progressPercentage = (currentProgress / maxProgress) * 100;

                // Mettre à jour la progression et le texte affiché
                const progress = document.querySelector("".progress"");
                const progressText = document.getElementById(""progress-text"");

                progress.style.setProperty(""--progress"", `${{progressPercentage}}%`);
                progressText.textContent = `${{currentProgress}}/${{maxProgress}}`;
            }} catch (error) {{
                console.error('Erreur:', error);
            }}
        }}

        // Appelle la fonction toutes les 10 secondes (10000 millisecondes)
        setInterval(fetchProgress, 10000);

        // Appel initial pour mettre à jour la barre de progression dès le chargement de la page
        fetchProgress();
    </script>
</body>
</html>
";
            }

            if (globalAppSettings.OverlaySettings.SessionTotalCaughtGoal.Enabled)
            {
                files["SessionTotalCaughtGoal.html"] = @$"
<!DOCTYPE html>
<html lang=""en"">
<head>
    <meta charset=""UTF-8"">
    <meta name=""viewport"" content=""width=device-width, initial-scale=1.0"">
    <title>Progress Dex</title>
    <style>
        body {{
            background: none;
        }}

        .progress {{
            --progress: 0%;
            width: 500px;
            height: 50px;
            margin: 0 0;
            border: 1px solid #fff;
            padding: 12px 10px;
            box-shadow: 0 0 10px #aaa;
        }}

        .progress .bar {{
            position: relative; /* Pour permettre le positionnement absolu des éléments enfants */
            width: var(--progress);
            height: 100%;
            background: linear-gradient(gold, #c85, gold);
            background-repeat: repeat;
            box-shadow: 0 0 10px 0px orange;
            animation:
                shine 4s ease-in infinite,
                end 1s ease-out 1;
            transition: width 3s ease;
        }}

        .progress .bar span {{
            position: absolute;
            top: 50%;
            left: 50%;
            transform: translate(-50%, -50%);
            color: white; /* Pour que le texte soit visible sur le fond */
            font-size: 40px;
            font-weight: bold;
        }}

        @property --progress {{
            syntax: ""<length>"";
            initial-value: 0%;
            inherits: true;
        }}

        @keyframes shine {{
            0% {{ background-position: 0 0; }}
            100% {{ background-position: 0 50px; }}
        }}

        @keyframes end {{
            0%, 100% {{ box-shadow: 0 0 10px 0px orange; }}
            50% {{ box-shadow: 0 0 15px 5px orange; }}
        }}
    </style>
</head>
<body>
    <div class=""progress"">
        <div class=""bar"">
            <span id=""progress-text"">0/0</span>
            <div class=""progress-value""></div>
        </div>
    </div>

    <script>
        async function fetchProgress() {{
            try {{
                // Appel à l'API pour obtenir la valeur de pokecount
                const pokecountResponse = await fetch('http://localhost:{port}/Get?Value=SessionTotalCaughtGoal');
                console.log(pokecountResponse);
				const pokecountData = await pokecountResponse.json();

                const maxProgress = pokecountData.total;
                const currentProgress = pokecountData.progress;

                // Calculer le pourcentage de progression
                const progressPercentage = (currentProgress / maxProgress) * 100;

                // Mettre à jour la progression et le texte affiché
                const progress = document.querySelector("".progress"");
                const progressText = document.getElementById(""progress-text"");

                progress.style.setProperty(""--progress"", `${{progressPercentage}}%`);
                progressText.textContent = `${{currentProgress}}/${{maxProgress}}`;
            }} catch (error) {{
                console.error('Erreur:', error);
            }}
        }}

        // Appelle la fonction toutes les 10 secondes (10000 millisecondes)
        setInterval(fetchProgress, 10000);

        // Appel initial pour mettre à jour la barre de progression dès le chargement de la page
        fetchProgress();
    </script>
</body>
</html>
";
            }

            if (globalAppSettings.OverlaySettings.SessionMoneySpentGoal.Enabled)
            {
                files["SessionMoneySpentGoal.html"] = @$"
<!DOCTYPE html>
<html lang=""en"">
<head>
    <meta charset=""UTF-8"">
    <meta name=""viewport"" content=""width=device-width, initial-scale=1.0"">
    <title>Progress Dex</title>
    <style>
        body {{
            background: none;
        }}

        .progress {{
            --progress: 0%;
            width: 500px;
            height: 50px;
            margin: 0 0;
            border: 1px solid #fff;
            padding: 12px 10px;
            box-shadow: 0 0 10px #aaa;
        }}

        .progress .bar {{
            position: relative; /* Pour permettre le positionnement absolu des éléments enfants */
            width: var(--progress);
            height: 100%;
            background: linear-gradient(gold, #c85, gold);
            background-repeat: repeat;
            box-shadow: 0 0 10px 0px orange;
            animation:
                shine 4s ease-in infinite,
                end 1s ease-out 1;
            transition: width 3s ease;
        }}

        .progress .bar span {{
            position: absolute;
            top: 50%;
            left: 50%;
            transform: translate(-50%, -50%);
            color: white; /* Pour que le texte soit visible sur le fond */
            font-size: 40px;
            font-weight: bold;
        }}

        @property --progress {{
            syntax: ""<length>"";
            initial-value: 0%;
            inherits: true;
        }}

        @keyframes shine {{
            0% {{ background-position: 0 0; }}
            100% {{ background-position: 0 50px; }}
        }}

        @keyframes end {{
            0%, 100% {{ box-shadow: 0 0 10px 0px orange; }}
            50% {{ box-shadow: 0 0 15px 5px orange; }}
        }}
    </style>
</head>
<body>
    <div class=""progress"">
        <div class=""bar"">
            <span id=""progress-text"">0/0</span>
            <div class=""progress-value""></div>
        </div>
    </div>

    <script>
        async function fetchProgress() {{
            try {{
                // Appel à l'API pour obtenir la valeur de pokecount
                const pokecountResponse = await fetch('http://localhost:{port}/Get?Value=sessionmoneygoal');
                console.log(pokecountResponse);
				const pokecountData = await pokecountResponse.json();

                const maxProgress = pokecountData.total;
                const currentProgress = pokecountData.progress;

                // Calculer le pourcentage de progression
                const progressPercentage = (currentProgress / maxProgress) * 100;

                // Mettre à jour la progression et le texte affiché
                const progress = document.querySelector("".progress"");
                const progressText = document.getElementById(""progress-text"");

                progress.style.setProperty(""--progress"", `${{progressPercentage}}%`);
                progressText.textContent = `${{currentProgress}}/${{maxProgress}}`;
            }} catch (error) {{
                console.error('Erreur:', error);
            }}
        }}

        // Appelle la fonction toutes les 10 secondes (10000 millisecondes)
        setInterval(fetchProgress, 10000);

        // Appel initial pour mettre à jour la barre de progression dès le chargement de la page
        fetchProgress();
    </script>
</body>
</html>
";
            }

            if (globalAppSettings.OverlaySettings.GlobalMoneySpentGoal.Enabled)
            {
                files["GlobalMoneySpentGoal.html"] = @$"
<!DOCTYPE html>
<html lang=""en"">
<head>
    <meta charset=""UTF-8"">
    <meta name=""viewport"" content=""width=device-width, initial-scale=1.0"">
    <title>Progress Dex</title>
    <style>
        body {{
            background: none;
        }}

        .progress {{
            --progress: 0%;
            width: 500px;
            height: 50px;
            margin: 0 0;
            border: 1px solid #fff;
            padding: 12px 10px;
            box-shadow: 0 0 10px #aaa;
        }}

        .progress .bar {{
            position: relative; /* Pour permettre le positionnement absolu des éléments enfants */
            width: var(--progress);
            height: 100%;
            background: linear-gradient(gold, #c85, gold);
            background-repeat: repeat;
            box-shadow: 0 0 10px 0px orange;
            animation:
                shine 4s ease-in infinite,
                end 1s ease-out 1;
            transition: width 3s ease;
        }}

        .progress .bar span {{
            position: absolute;
            top: 50%;
            left: 50%;
            transform: translate(-50%, -50%);
            color: white; /* Pour que le texte soit visible sur le fond */
            font-size: 40px;
            font-weight: bold;
        }}

        @property --progress {{
            syntax: ""<length>"";
            initial-value: 0%;
            inherits: true;
        }}

        @keyframes shine {{
            0% {{ background-position: 0 0; }}
            100% {{ background-position: 0 50px; }}
        }}

        @keyframes end {{
            0%, 100% {{ box-shadow: 0 0 10px 0px orange; }}
            50% {{ box-shadow: 0 0 15px 5px orange; }}
        }}
    </style>
</head>
<body>
    <div class=""progress"">
        <div class=""bar"">
            <span id=""progress-text"">0/0</span>
            <div class=""progress-value""></div>
        </div>
    </div>

    <script>
        async function fetchProgress() {{
            try {{
                // Appel à l'API pour obtenir la valeur de pokecount
                const pokecountResponse = await fetch('http://localhost:{port}/Get?Value=globalmoneygoal');
                console.log(pokecountResponse);
				const pokecountData = await pokecountResponse.json();

                const maxProgress = pokecountData.total;
                const currentProgress = pokecountData.progress;

                // Calculer le pourcentage de progression
                const progressPercentage = (currentProgress / maxProgress) * 100;

                // Mettre à jour la progression et le texte affiché
                const progress = document.querySelector("".progress"");
                const progressText = document.getElementById(""progress-text"");

                progress.style.setProperty(""--progress"", `${{progressPercentage}}%`);
                progressText.textContent = `${{currentProgress}}/${{maxProgress}}`;
            }} catch (error) {{
                console.error('Erreur:', error);
            }}
        }}

        // Appelle la fonction toutes les 10 secondes (10000 millisecondes)
        setInterval(fetchProgress, 10000);

        // Appel initial pour mettre à jour la barre de progression dès le chargement de la page
        fetchProgress();
    </script>
</body>
</html>
";
            }
            TextsUpdate();

            writeFile();
        }

        private void writeFile()
        {
            // Écrit le contenu de chaque overlay dans un fichier pour chaque
            foreach (string item in files.Keys)
            {
                File.WriteAllText(Path.Combine(folderLocation, item), files[item]);
            }
        }

        public void TextsUpdate()
        {
            List<Entrie> allentries = data.GetAllEntries();

            // texts

            // everyonehere
            files["everyonehere.count.txt"] = (usersHere.Count).ToString();
            files["everyonehere.lastJoined.txt"] = usersHere.Any() ? usersHere.Last().ToString() : "";

            // this session
            files["session.pokecaughtTotal.Count.txt"] = settings.catchHistory.Count.ToString();
            files["session.pokecaughtShiny.Count.txt"] = settings.catchHistory.Where(h => h.shiny).Count().ToString();
            try
            {
                files["session.pokecaught.Last.FR.txt"] = settings.catchHistory.Last().Pokemon.Name_FR;
                files["session.pokecaught.Last.EN.txt"] = settings.catchHistory.Last().Pokemon.Name_EN;

                files["session.trainer.LastCaught.txt"] = settings.catchHistory.Last().User?.ToString();
            }
            catch { }

            var mostCatchUserGroup = settings.catchHistory
                .GroupBy(x => x.User.Code_user)
                .OrderByDescending(g => g.Count())
                .FirstOrDefault();
            files["session.trainer.MostCatch.txt"] = mostCatchUserGroup?.FirstOrDefault()?.User?.Pseudo ?? ""; ;

            var mostCaughtUserGroup = settings.catchHistory
                .GroupBy(x => x.Pokemon.Name_EN)
                .OrderByDescending(g => g.Count())
                .FirstOrDefault();
            files["session.pokemon.MostCaught.EN.txt"] = mostCaughtUserGroup?.FirstOrDefault()?.Pokemon?.Name_EN ?? "";
            files["session.pokemon.MostCaught.FR.txt"] = mostCaughtUserGroup?.FirstOrDefault()?.Pokemon?.Name_FR ?? "";

            var mostUsedPokeball = settings.catchHistory
                .GroupBy(x => x.Ball.Name)
                .OrderByDescending(g => g.Count())
                .FirstOrDefault();
            files["session.ball.LastUsed.txt"] = settings.catchHistory.Any() ? settings.catchHistory.Last().Ball?.Name ?? "" : "";
            files["session.ball.MostUsed.txt"] = mostUsedPokeball?.FirstOrDefault().Ball?.Name ?? "";

            files["session.money.spent.txt"] = settings.catchHistory.Sum(x => x.price).ToString() ?? "0";

            files["stat.CreatureEnabled.item.txt"] = settings.pokemons.Count.ToString();
            files["stat.CreatureCatchable.item.txt"] = settings.pokemons.Where(x => !x.isLock).Count().ToString();

            files["stat.pokemons.custom.Count.txt"] = settings.pokemons.Where(x => x.isCustom).Count().ToString();
            files["stat.pokemons.legendary.Count.txt"] = settings.pokemons.Where(x => x.isLegendary).Count().ToString();

            files["stat.trainer.Count.txt"] = data.GetAllUserPlatforms().Count.ToString();

            files["stats.poke.CountAll.txt"] = allentries.Sum(x => x.CountNormal + x.CountShiny).ToString();
            files["stats.poke.CountNormal.txt"] = allentries.Sum(x => x.CountNormal).ToString();
            files["stats.poke.CountShiny.txt"] = allentries.Sum(x => x.CountShiny).ToString();
            try
            {
                files["special.history.lastCaughtResumeFR.txt"] = $"{files["session.trainer.LastCaught.txt"]} ► {files["session.ball.LastUsed.txt"]} = {files["session.pokecaught.Last.FR.txt"]}";
                files["special.history.lastCaughtResumeEN.txt"] = $"{files["session.trainer.LastCaught.txt"]} ► {files["session.ball.LastUsed.txt"]} = {files["session.pokecaught.Last.EN.txt"]}";
            }
            catch { files["special.history.lastCaughtResumeFR.txt"] = ""; files["special.history.lastCaughtResumeEN.txt"] = ""; }

            try
            {
                TimeSpan span = DateTime.Now - allentries.First().dateFirstCatch;
                files["stats.system.daysCountSinceFirstCatch.txt"] = span.Days.ToString();
            }
            catch
            {
                files["stats.system.daysCountSinceFirstCatch.txt"] = "0";
            }

            writeFile();
        }
    }
}