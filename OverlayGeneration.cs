using PKServ.Configuration;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

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

        public void BuildOverlay(bool firstLaunch)
        {
            try
            {
                // required var
                var data_newTrainer = data.GetAllUserPlatforms().Where(user => user.Stats.firstCatch > startTime).ToList();
                List<Entrie> allentries = data.GetAllEntries();

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
            catch (Exception e)
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


            // Last Poké Caught
            files["raidOverlay.html"] = @$"
<!DOCTYPE html>
<html lang=""fr"">
<head>
    <meta charset=""UTF-8"">
    <title>Raid Progress</title>
    <style>
        body {{
            display: flex;
            flex-direction: column;
            align-items: center;
        }}

        /* Classe pour masquer tout le contenu */
        .hidden {{
            display: none;
        }}

        .image-container {{
            position: relative;
            width: 128px;
            height: auto;
        }}

        .image-container img {{
            width: 128px;
            height: 128px;
            position: absolute;
            top: 0;
            left: 0;
        }}

        .progress-bar {{
			padding-top: 140px;
            width: 256px;
            height: 20px;
            background-color: rgba(0, 90, 45, 0.2);
            border-radius: 10px;
            overflow: hidden;
        }}

        .progress-fill {{
            height: 100%;
            background-color: #16f7a0;
            width: 0%;
            transition: width 0.5s ease;
        }}
    </style>
</head>
<body>

    <div id=""content"">
	<center>
        <div class=""image-container"">
            <img id=""image-creature"" src="""" alt=""Créature"">
            <img id=""image-overlay"" src="""" alt=""Overlay"">
        </div>

        <div class=""progress-bar"">
            <div class=""progress-fill"" id=""progress-fill""></div>
        </div>
	</center>
    </div>

    <script>
        function updateContent() {{
            fetch('http://localhost:{globalAppSettings.ServerPort}/GetRaidInfos')
                .then(response => response.json())
                .then(data => {{
                    const contentDiv = document.getElementById('content');
					console.log(contentDiv);
					console.log(""Element"");
                    // Vérifier si le JSON est vide
                    if (Object.keys(data).length === 0) {{
                        // Masquer tout le contenu avec la classe 'hidden'
                        contentDiv.classList.add('hidden');
                    }} else {{
					
						console.log(data);
                        // Afficher le contenu si masqué
                        contentDiv.classList.remove('hidden');

                        // Mettre à jour les images
                        document.getElementById('image-creature').src = data.Url_Creature;
                        document.getElementById('image-overlay').src = data.Url_Overlay;

                        // Facultatif : Gérer le cache des images
                        /*
                        const timestamp = new Date().getTime();
                        document.getElementById('image-creature').src = data.Url_Creature + '?t=' + timestamp;
                        document.getElementById('image-overlay').src = data.Url_Overlay + '?t=' + timestamp;
                        */

                        // Mettre à jour la barre de progression
                        const maxValue = data.Bar_Max;
                        const currentValue = data.Bar_CurrentValue;
                        const percentage = (currentValue / maxValue) * 100;
                        document.getElementById('progress-fill').style.width = percentage + '%';
                    }}
                }})
                .catch(error => {{
                    console.error('Erreur lors de la récupération des données :', error);
                    // En cas d'erreur, masquer le contenu
                    document.getElementById('content').classList.add('hidden');
                }});
        }}

        // Actualiser les informations toutes les 10 secondes
        setInterval(updateContent, 10000);

        // Appel initial pour charger les données au démarrage
        updateContent();
    </script>
</body>
</html>
";

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
            files["everyonehere.count.txt"] = (usersHere.Count - 1).ToString();
            files["everyonehere.lastJoined.txt"] = usersHere.Last().ToString();

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