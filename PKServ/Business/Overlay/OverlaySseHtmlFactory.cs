using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace PKServ.Business.Overlay
{
    /// <summary>
    /// Génère les fichiers HTML statiques pour chaque overlay SSE.
    ///
    /// Chaque fichier est produit UNE SEULE FOIS (si absent) dans StreamOverlays/.
    /// Le HTML embarque un EventSource qui se connecte sur /overlay/{channel}/stream.
    /// Le serveur pousse les mises à jour via OverlaySseManager.BroadcastChannel().
    ///
    /// L'overlay polling original (OverlayGeneration.cs) n'est PAS modifié :
    /// les deux systèmes cohabitent sur des URLs et fichiers distincts.
    /// </summary>
    public static class OverlaySseHtmlFactory
    {
        private static readonly string Folder = Path.Combine(AppContext.BaseDirectory, "StreamOverlays");

        /// <summary>Génère tous les overlays SSE manquants.</summary>
        public static async Task GenerateAllAsync(GlobalAppSettings gas)
        {
            Directory.CreateDirectory(Folder);

            await WriteIfMissingAsync(OverlaySseChannels.Raid,             BuildRaidHtml(gas));
            await WriteIfMissingAsync(OverlaySseChannels.BallThrowResume,  BuildBallThrowResumeHtml(gas));
            await WriteIfMissingAsync(OverlaySseChannels.LastCaughtSprite, BuildLastCaughtSpriteHtml(gas));
            await WriteIfMissingAsync(OverlaySseChannels.GlobalDex,        BuildProgressBarHtml(gas, OverlaySseChannels.GlobalDex,        "Global Dex",           "or"));
            await WriteIfMissingAsync(OverlaySseChannels.GlobalShinyDex,   BuildProgressBarHtml(gas, OverlaySseChannels.GlobalShinyDex,   "Global Shiny Dex",     "cyan"));
            await WriteIfMissingAsync(OverlaySseChannels.GlobalTotalCaught,BuildProgressBarHtml(gas, OverlaySseChannels.GlobalTotalCaught,"Total Captures Global","or"));
            await WriteIfMissingAsync(OverlaySseChannels.GlobalShinyCaught,BuildProgressBarHtml(gas, OverlaySseChannels.GlobalShinyCaught,"Shinies Global",       "cyan"));
            await WriteIfMissingAsync(OverlaySseChannels.GlobalMoneySpent, BuildProgressBarHtml(gas, OverlaySseChannels.GlobalMoneySpent, "Argent depense global","gold"));
            await WriteIfMissingAsync(OverlaySseChannels.SessionParticipants, BuildProgressBarHtml(gas, OverlaySseChannels.SessionParticipants,"Participants session","or"));
            await WriteIfMissingAsync(OverlaySseChannels.SessionTotalCaught,  BuildProgressBarHtml(gas, OverlaySseChannels.SessionTotalCaught, "Captures session",   "or"));
            await WriteIfMissingAsync(OverlaySseChannels.SessionShinyCaught,  BuildProgressBarHtml(gas, OverlaySseChannels.SessionShinyCaught, "Shinies session",    "cyan"));
            await WriteIfMissingAsync(OverlaySseChannels.SessionMoneySpent,   BuildProgressBarHtml(gas, OverlaySseChannels.SessionMoneySpent,  "Argent session",     "gold"));
        }

        // ── Helpers ──────────────────────────────────────────────────────────

        private static async Task WriteIfMissingAsync(string channel, string html)
        {
            string path = Path.Combine(Folder, channel + ".html");
            if (!File.Exists(path))
                await File.WriteAllTextAsync(path, html, System.Text.Encoding.UTF8);
        }

        // ── HTML raid (identique visuellement à current_raid.html) ────────────

        private static string BuildRaidHtml(GlobalAppSettings gas)
        {
            int port = gas.ServerPort;
            return
"<!DOCTYPE html>\n" +
"<html lang=\"fr\"><head><meta charset=\"UTF-8\">\n" +
"<title>Raid Overlay SSE</title>\n" +
"<style>\n" +
"body{display:flex;flex-direction:column;align-items:center;}\n" +
".hidden{display:none;}\n" +
".image-container{position:absolute;width:256px;height:256px;overflow:hidden;z-index:0;}\n" +
".image-container img{position:absolute;top:0;left:0;width:100%;height:100%;object-fit:contain;}\n" +
".progress-bar{padding-top:236px;width:256px;height:20px;border-radius:10px;overflow:hidden;transition:background-color 0.5s ease;}\n" +
".progress-bar.common{background-color:rgba(220,220,220,0.4);}\n" +
".progress-bar.uncommon{background-color:rgba(144,238,144,0.4);}\n" +
".progress-bar.rare{background-color:rgba(173,216,230,0.4);}\n" +
".progress-bar.epic{background-color:rgba(186,85,211,0.4);}\n" +
".progress-bar.legendary{background-color:rgba(255,215,0,0.4);}\n" +
".progress-bar.mythical{background-color:rgba(255,182,193,0.4);}\n" +
".progress-fill{position:relative;overflow:hidden;height:100%;width:0%;transition:width 0.5s ease,background-color 0.5s ease;}\n" +
".progress-fill::before{content:\"\";position:absolute;top:0;left:-150%;width:50%;height:100%;background:rgba(255,255,255,0.4);transform:skewX(-20deg);animation:shine 2s ease-in-out infinite;}\n" +
"@keyframes shine{0%{left:-150%}50%{left:150%}100%{left:150%}}\n" +
".progress-fill.low{background-color:#e74c3c;}\n" +
".progress-fill.medium{background-color:#e67e22;}\n" +
".progress-fill.high{background-color:#2ecc71;}\n" +
"#image-overlay{z-index:2;}#image-creature{z-index:1;}\n" +
".dmg-container{position:absolute;width:256px;height:256px;pointer-events:none;z-index:10;}\n" +
".dmg-float{position:absolute;font-size:32px;font-weight:bold;color:#fff;" +
"text-shadow:-2px -2px 0 #000,2px -2px 0 #000,-2px 2px 0 #000,2px 2px 0 #000;" +
"animation:floatUp 1.6s ease-out forwards;white-space:nowrap;}\n" +
".dmg-float.crit{color:#FFD700;font-size:40px;}\n" +
".dmg-float.heal{color:#2ecc71;}\n" +
"@keyframes floatUp{0%{transform:translateY(0);opacity:1;}80%{transform:translateY(-90px);opacity:1;}100%{transform:translateY(-110px);opacity:0;}}\n" +
"</style></head><body>\n" +
"<div id=\"content\" class=\"hidden\"><center>\n" +
"<div class=\"image-container\">\n" +
"<img id=\"image-creature\" src=\"\" alt=\"\">\n" +
"<img id=\"image-overlay\" src=\"\" alt=\"\">\n" +
"<div class=\"dmg-container\" id=\"dmg-container\"></div>\n" +
"</div>\n" +
"<div class=\"progress-bar\" id=\"progress-bar\"><div class=\"progress-fill\" id=\"progress-fill\"></div></div>\n" +
"</center></div>\n" +
"<script>\n" +
$"const source = new EventSource('http://localhost:{port}/overlay/{OverlaySseChannels.Raid}/stream');\n" +
"source.onmessage = function(e) {\n" +
"  const d = JSON.parse(e.data), c = document.getElementById('content');\n" +
"  if (!d.active) { c.classList.add('hidden'); return; }\n" +
"  c.classList.remove('hidden');\n" +
"  document.getElementById('image-creature').src = d.Url_Creature;\n" +
"  document.getElementById('image-overlay').src  = d.Url_Overlay;\n" +
"  const pct = (d.Bar_CurrentValue / d.Bar_Max) * 100;\n" +
"  const f = document.getElementById('progress-fill');\n" +
"  f.style.width = pct + '%';\n" +
"  f.classList.remove('low','medium','high');\n" +
"  if (pct < 15) f.classList.add('low'); else if (pct < 50) f.classList.add('medium'); else f.classList.add('high');\n" +
"  const b = document.getElementById('progress-bar');\n" +
"  b.classList.remove('common','uncommon','rare','epic','legendary','mythical');\n" +
"  b.classList.add(d.Rarity.toLowerCase());\n" +
"  if (d.Damages && d.Damages.length > 0) showDamages(d.Damages);\n" +
"};\n" +
"function showDamages(damages) {\n" +
"  const container = document.getElementById('dmg-container');\n" +
"  damages.forEach(function(dmg, i) {\n" +
"    setTimeout(function() {\n" +
"      const el = document.createElement('span');\n" +
"      el.classList.add('dmg-float');\n" +
"      if (dmg.endsWith('!'))   el.classList.add('crit');\n" +
"      if (dmg.startsWith('+')) el.classList.add('heal');\n" +
"      el.textContent = dmg;\n" +
"      el.style.left = Math.floor(Math.random() * 180) + 'px';\n" +
"      el.style.top  = Math.floor(60 + Math.random() * 120) + 'px';\n" +
"      container.appendChild(el);\n" +
"      el.addEventListener('animationend', function() { el.remove(); });\n" +
"    }, i * 120);\n" +
"  });\n" +
"}\n" +
"source.onerror = function() { console.warn('SSE: reconnexion en cours...'); };\n" +
"</script></body></html>\n";
        }

        // ── HTML résumé lancer de pokéball ────────────────────────────────────

        private static string BuildBallThrowResumeHtml(GlobalAppSettings gas)
        {
            int port = gas.ServerPort;
            return
"<!DOCTYPE html>\n" +
"<html lang=\"fr\"><head><meta charset=\"UTF-8\">\n" +
"<title>Ball Throw Resume SSE</title>\n" +
"<style>\n" +
"body{background:black;display:flex;justify-content:center;align-items:center;height:100vh;margin:0;font-family:Arial,sans-serif;color:white;}\n" +
".display-container{display:flex;align-items:center;gap:15px;opacity:0;transition:opacity 0.5s ease-in-out;}\n" +
".display-container.visible{opacity:1;}\n" +
".time,.username{font-size:96px;font-weight:bold;height:128px;display:flex;align-items:center;}\n" +
".time{color:#FFD700;}.username{color:#87CEEB;}\n" +
"img{height:128px;min-height:128px;width:auto;image-rendering:pixelated;}\n" +
".separator{font-size:32px;height:128px;display:flex;align-items:center;}\n" +
"</style></head><body>\n" +
"<div class=\"display-container\" id=\"disp\">\n" +
"<span class=\"time\" id=\"tDisplay\"></span>\n" +
"<img id=\"platIcon\" src=\"\" alt=\"\" style=\"display:none;\"/>\n" +
"<span class=\"username\" id=\"uname\"></span>\n" +
"<span class=\"separator\">-</span>\n" +
"<img id=\"pokeImg\" src=\"\" alt=\"\" style=\"display:none;\"/>\n" +
"<img id=\"shinyIcon\" src=\"\" alt=\"\" style=\"display:none;\"/>\n" +
"<img id=\"catchIcon\" src=\"\" alt=\"\" style=\"display:none;\"/>\n" +
"</div>\n" +
"<script>\n" +
"const SHINY_ICON  = 'https://cdn-icons-png.flaticon.com/256/2267/2267359.png';\n" +
"const CAUGHT_ICON = 'https://upload.wikimedia.org/wikipedia/commons/thumb/5/53/Pok%C3%A9_Ball_icon.svg/960px-Pok%C3%A9_Ball_icon.svg.png';\n" +
"const MISS_ICON   = 'https://cdn-icons-png.flaticon.com/512/6659/6659895.png';\n" +
"function fmtTime(t) { return t && t.length===6 ? '['+t.slice(0,2)+':'+t.slice(2,4)+':'+t.slice(4,6)+']' : t; }\n" +
"let hideTimer = null;\n" +
$"const source = new EventSource('http://localhost:{port}/overlay/{OverlaySseChannels.BallThrowResume}/stream');\n" +
"source.onmessage = function(e) {\n" +
"  const d = JSON.parse(e.data);\n" +
"  const c = document.getElementById('disp');\n" +
"  if (!d || !d.imageUrl) { c.classList.remove('visible'); return; }\n" +
"  // Annule le masquage automatique précédent si un nouveau lancer arrive avant 5s\n" +
"  if (hideTimer) { clearTimeout(hideTimer); hideTimer = null; }\n" +
"  c.classList.remove('visible');\n" +
"  setTimeout(() => {\n" +
"    document.getElementById('tDisplay').textContent = fmtTime(d.time);\n" +
"    document.getElementById('uname').textContent = d.userName;\n" +
"    const pi = document.getElementById('platIcon');\n" +
"    if (d.userPlateformIcon) { pi.src=d.userPlateformIcon; pi.style.display='block'; } else pi.style.display='none';\n" +
"    const img = document.getElementById('pokeImg');\n" +
"    if (d.imageUrl) { img.src=d.imageUrl; img.style.display='block'; } else img.style.display='none';\n" +
"    const si = document.getElementById('shinyIcon');\n" +
"    if (d.isShiny) { si.src=SHINY_ICON; si.style.display='block'; } else si.style.display='none';\n" +
"    const ci = document.getElementById('catchIcon');\n" +
"    ci.src = d.isCaught ? CAUGHT_ICON : MISS_ICON; ci.style.display='block';\n" +
"    c.classList.add('visible');\n" +
"    // Masquage automatique après 5 secondes d'affichage\n" +
"    hideTimer = setTimeout(() => { c.classList.remove('visible'); hideTimer = null; }, 5000);\n" +
"  }, 250);\n" +
"};\n" +
"source.onerror = function() { console.warn('SSE: reconnexion...'); };\n" +
"</script></body></html>\n";
        }

        // ── HTML dernier sprite attrapé ───────────────────────────────────────

        private static string BuildLastCaughtSpriteHtml(GlobalAppSettings gas)
        {
            int port = gas.ServerPort;
            return
"<!DOCTYPE html>\n" +
"<html lang=\"fr\"><head><meta charset=\"UTF-8\">\n" +
"<title>Last Caught Sprite SSE</title>\n" +
"<style>\n" +
"body{background:black;display:flex;flex-direction:column;justify-content:center;align-items:center;height:100vh;margin:0;}\n" +
"@keyframes fadeInOut{0%{opacity:0;}5%{opacity:1;}80%{opacity:1;}100%{opacity:0;}}\n" +
".sprite-wrap{opacity:0;}\n" +
".sprite-wrap.playing{animation:fadeInOut 5s ease forwards;}\n" +
"img{width:64px;height:auto;}\n" +
"#username{display:block;margin-top:10px;font-size:28px;color:white;text-align:center;" +
"text-shadow:-2px -2px 0 black,2px -2px 0 black,-2px 2px 0 black,2px 2px 0 black;}\n" +
"</style></head><body>\n" +
"<div class=\"sprite-wrap\" id=\"wrap\">\n" +
"<img id=\"pokeImg\" src=\"\" alt=\"Sprite\"/>\n" +
"<span id=\"username\"></span>\n" +
"</div>\n" +
"<script>\n" +
"const wrap = document.getElementById('wrap');\n" +
$"const source = new EventSource('http://localhost:{port}/overlay/{OverlaySseChannels.LastCaughtSprite}/stream');\n" +
"source.onmessage = function(e) {\n" +
"  const d = JSON.parse(e.data);\n" +
"  if (!d || !d.imageUrl) return;\n" +
"  // Redémarre l'animation en retirant puis en réajoutant la classe\n" +
"  wrap.classList.remove('playing');\n" +
"  void wrap.offsetWidth; // force reflow pour reset l'animation\n" +
"  document.getElementById('pokeImg').src = d.imageUrl;\n" +
"  document.getElementById('username').textContent = d.userName || '';\n" +
"  wrap.classList.add('playing');\n" +
"};\n" +
"source.onerror = function() { console.warn('SSE: reconnexion...'); };\n" +
"</script></body></html>\n";
        }

        // ── HTML barre de progression générique ───────────────────────────────

        private static string BuildProgressBarHtml(GlobalAppSettings gas, string channel, string title, string color)
        {
            int port = gas.ServerPort;
            return
"<!DOCTYPE html>\n" +
"<html lang=\"fr\"><head><meta charset=\"UTF-8\">\n" +
$"<title>{title} SSE</title>\n" +
"<style>\n" +
"body{background:none;}\n" +
".progress{--progress:0%;width:500px;height:50px;margin:0;border:1px solid #fff;padding:12px 10px;box-shadow:0 0 10px #aaa;}\n" +
".progress .bar{position:relative;width:var(--progress);height:100%;background:linear-gradient(gold,#c85,gold);background-repeat:repeat;box-shadow:0 0 10px 0px orange;animation:shine 4s ease-in infinite,end 1s ease-out 1;transition:width 3s ease;}\n" +
".progress .bar span{position:absolute;top:50%;left:50%;transform:translate(-50%,-50%);color:white;font-size:40px;font-weight:bold;}\n" +
"@property --progress{syntax:\"<length>\";initial-value:0%;inherits:true;}\n" +
"@keyframes shine{0%{background-position:0 0}100%{background-position:0 50px}}\n" +
"@keyframes end{0%,100%{box-shadow:0 0 10px 0px orange}50%{box-shadow:0 0 15px 5px orange}}\n" +
"</style></head><body>\n" +
"<div class=\"progress\"><div class=\"bar\"><span id=\"txt\">0/0</span></div></div>\n" +
"<script>\n" +
$"const source = new EventSource('http://localhost:{port}/overlay/{channel}/stream');\n" +
"source.onmessage = function(e) {\n" +
"  const d = JSON.parse(e.data);\n" +
"  if (!d || d.progress === undefined) return;\n" +
"  const pct = (d.progress / d.total) * 100;\n" +
"  document.querySelector('.progress').style.setProperty('--progress', pct + '%');\n" +
"  document.getElementById('txt').textContent = d.progress + '/' + d.total;\n" +
"};\n" +
"source.onerror = function() { console.warn('SSE: reconnexion...'); };\n" +
"</script></body></html>\n";
        }
    }
}
