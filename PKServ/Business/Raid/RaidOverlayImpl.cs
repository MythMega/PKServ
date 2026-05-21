using System;
using System.IO;
using System.Threading.Tasks;

namespace PKServ.Business.Raid
{
    public static class RaidOverlayImpl
    {
        // CSS et JS partagés pour les dégâts flottants (injectés dans les deux overlays SSE)
        private const string DmgCss =
"        .dmg-container { position:absolute; width:256px; height:256px; pointer-events:none; z-index:10; }\n" +
"        .dmg-float {\n" +
"            position:absolute;\n" +
"            font-size:32px;\n" +
"            font-weight:bold;\n" +
"            color:#fff;\n" +
"            text-shadow:-2px -2px 0 #000,2px -2px 0 #000,-2px 2px 0 #000,2px 2px 0 #000;\n" +
"            animation: floatUp 1.6s ease-out forwards;\n" +
"            white-space:nowrap;\n" +
"        }\n" +
"        .dmg-float.crit  { color:#FFD700; font-size:40px; }\n" +
"        .dmg-float.heal  { color:#2ecc71; }\n" +
"        @keyframes floatUp {\n" +
"            0%   { transform:translateY(0);     opacity:1; }\n" +
"            80%  { transform:translateY(-90px); opacity:1; }\n" +
"            100% { transform:translateY(-110px);opacity:0; }\n" +
"        }\n";

        private const string DmgJs =
"        function showDamages(damages) {\n" +
"            const container = document.getElementById('dmg-container');\n" +
"            damages.forEach(function(dmg, i) {\n" +
"                setTimeout(function() {\n" +
"                    const el = document.createElement('span');\n" +
"                    el.classList.add('dmg-float');\n" +
"                    if (dmg.endsWith('!'))   el.classList.add('crit');\n" +
"                    if (dmg.startsWith('+')) el.classList.add('heal');\n" +
"                    el.textContent = dmg;\n" +
"                    el.style.left = Math.floor(Math.random() * 180) + 'px';\n" +
"                    el.style.top  = Math.floor(60 + Math.random() * 120) + 'px';\n" +
"                    container.appendChild(el);\n" +
"                    el.addEventListener('animationend', function() { el.remove(); });\n" +
"                }, i * 120);\n" +
"            });\n" +
"        }\n";

        // ── CSS commun barre de progression ──────────────────────────────────
        private const string ProgressCss =
"        body { display:flex; flex-direction:column; align-items:center; }\n" +
"        .hidden { display:none; }\n" +
"        .image-container { position:absolute; width:256px; height:256px; overflow:hidden; z-index:0; }\n" +
"        .image-container img { position:absolute; top:0; left:0; width:100%; height:100%; object-fit:contain; }\n" +
"        .progress-bar { padding-top:236px; width:256px; height:20px; border-radius:10px; overflow:hidden; transition:background-color 0.5s ease; }\n" +
"        .progress-bar.common    { background-color:rgba(220,220,220,0.4); }\n" +
"        .progress-bar.uncommon  { background-color:rgba(144,238,144,0.4); }\n" +
"        .progress-bar.rare      { background-color:rgba(173,216,230,0.4); }\n" +
"        .progress-bar.epic      { background-color:rgba(186,85,211,0.4); }\n" +
"        .progress-bar.legendary { background-color:rgba(255,215,0,0.4); }\n" +
"        .progress-bar.mythical  { background-color:rgba(255,182,193,0.4); }\n" +
"        .progress-fill { position:relative; overflow:hidden; height:100%; width:0%; transition:width 0.5s ease,background-color 0.5s ease; }\n" +
"        .progress-fill::before { content:\"\"; position:absolute; top:0; left:-150%; width:50%; height:100%;\n" +
"            background:rgba(255,255,255,0.4); transform:skewX(-20deg); animation:shine 2s ease-in-out infinite; }\n" +
"        @keyframes shine { 0%{left:-150%} 50%{left:150%} 100%{left:150%} }\n" +
"        .progress-fill.low    { background-color:#e74c3c; }\n" +
"        .progress-fill.medium { background-color:#e67e22; }\n" +
"        .progress-fill.high   { background-color:#2ecc71; }\n" +
"        #image-overlay  { z-index:2; }\n" +
"        #image-creature { z-index:1; }\n";

        // JS commun mise à jour de la barre (sans appel showDamages)
        private const string BarUpdateJs =
"            document.getElementById('image-creature').src = data.Url_Creature;\n" +
"            document.getElementById('image-overlay').src  = data.Url_Overlay;\n" +
"            const percentage = (data.Bar_CurrentValue / data.Bar_Max) * 100;\n" +
"            const fillEl = document.getElementById('progress-fill');\n" +
"            fillEl.style.width = percentage + '%';\n" +
"            fillEl.classList.remove('low','medium','high');\n" +
"            if (percentage < 15) fillEl.classList.add('low');\n" +
"            else if (percentage < 50) fillEl.classList.add('medium');\n" +
"            else fillEl.classList.add('high');\n" +
"            const barEl = document.getElementById('progress-bar');\n" +
"            barEl.classList.remove('common','uncommon','rare','epic','legendary','mythical');\n" +
"            barEl.classList.add(data.Rarity.toLowerCase());\n";

        // ── Overlay existant (polling toutes les 10s) ─────────────────────────
        public static async Task WriteOverlay(GlobalAppSettings gas)
        {
            string fileContent =
"<!DOCTYPE html>\n" +
"<html lang=\"fr\">\n" +
"<head>\n" +
"    <meta charset=\"UTF-8\">\n" +
"    <title>Raid Progress</title>\n" +
"    <style>\n" +
ProgressCss +
"    </style>\n" +
"</head>\n" +
"<body>\n" +
"    <div id=\"content\">\n" +
"        <center>\n" +
"            <div class=\"image-container\">\n" +
"                <img id=\"image-creature\" src=\"\" alt=\"\">\n" +
"                <img id=\"image-overlay\"  src=\"\" alt=\"\">\n" +
"            </div>\n" +
"            <div class=\"progress-bar\" id=\"progress-bar\">\n" +
"                <div class=\"progress-fill\" id=\"progress-fill\"></div>\n" +
"            </div>\n" +
"        </center>\n" +
"    </div>\n" +
"    <script>\n" +
"        async function updateContent() {\n" +
"            try {\n" +
$"                const resp = await fetch('http://localhost:{gas.ServerPort}/GetRaidInfos');\n" +
"                const data = await resp.json();\n" +
"                const contentDiv = document.getElementById('content');\n" +
"                if (Object.keys(data).length === 0) { contentDiv.classList.add('hidden'); return; }\n" +
"                contentDiv.classList.remove('hidden');\n" +
BarUpdateJs +
"            } catch(error) {\n" +
"                console.error('Erreur lors de la recuperation des donnees :', error);\n" +
"                document.getElementById('content').classList.add('hidden');\n" +
"            }\n" +
"        }\n" +
"        setInterval(updateContent, 10000);\n" +
"        updateContent();\n" +
"    </script>\n" +
"</body>\n" +
"</html>\n";

            string folderLocation = System.IO.Path.Combine(AppContext.BaseDirectory, "StreamOverlays");
            if (!Directory.Exists(folderLocation))
                Directory.CreateDirectory(folderLocation);

            await File.WriteAllTextAsync(System.IO.Path.Combine(folderLocation, "raidOverlay.html"), fileContent);
        }

        // ── Overlay SSE legacy (/current_raid_stream) ─────────────────────────
        public static async Task WriteRealtimeOverlay(GlobalAppSettings gas)
        {
            string fileContent =
"<!DOCTYPE html>\n" +
"<html lang=\"fr\">\n" +
"<head>\n" +
"    <meta charset=\"UTF-8\">\n" +
"    <title>Raid Progress (Temps reel)</title>\n" +
"    <style>\n" +
ProgressCss +
DmgCss +
"    </style>\n" +
"</head>\n" +
"<body>\n" +
"    <div id=\"content\" class=\"hidden\">\n" +
"        <center>\n" +
"            <div class=\"image-container\">\n" +
"                <img id=\"image-creature\" src=\"\" alt=\"\">\n" +
"                <img id=\"image-overlay\"  src=\"\" alt=\"\">\n" +
"                <div class=\"dmg-container\" id=\"dmg-container\"></div>\n" +
"            </div>\n" +
"            <div class=\"progress-bar\" id=\"progress-bar\">\n" +
"                <div class=\"progress-fill\" id=\"progress-fill\"></div>\n" +
"            </div>\n" +
"        </center>\n" +
"    </div>\n" +
"    <script>\n" +
"        // SSE - EventSource ouvre une connexion persistante vers /current_raid_stream.\n" +
"        // Le serveur pousse un event JSON a chaque changement d'etat du raid.\n" +
$"        const source = new EventSource('http://localhost:{gas.ServerPort}/current_raid_stream');\n" +
"\n" +
"        source.onmessage = function(event) {\n" +
"            const data       = JSON.parse(event.data);\n" +
"            const contentDiv = document.getElementById('content');\n" +
"            if (!data.active) { contentDiv.classList.add('hidden'); return; }\n" +
"            contentDiv.classList.remove('hidden');\n" +
BarUpdateJs +
"            if (data.Damages && data.Damages.length > 0) showDamages(data.Damages);\n" +
"        };\n" +
"\n" +
DmgJs +
"\n" +
"        source.onerror = function() {\n" +
"            // Connexion SSE perdue : l'overlay reste visible, reconnexion automatique.\n" +
"            console.warn('SSE: connexion perdue, reconnexion en cours...');\n" +
"        };\n" +
"    </script>\n" +
"</body>\n" +
"</html>\n";

            string folderLocation = System.IO.Path.Combine(AppContext.BaseDirectory, "StreamOverlays");
            if (!Directory.Exists(folderLocation))
                Directory.CreateDirectory(folderLocation);

            // Génération unique : supprimez current_raid.html pour forcer la regénération
            string targetPath = System.IO.Path.Combine(folderLocation, "current_raid.html");
            if (!File.Exists(targetPath))
                await File.WriteAllTextAsync(targetPath, fileContent);
        }
    }
}
