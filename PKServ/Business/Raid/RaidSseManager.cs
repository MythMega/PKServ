using System;
using System.Collections.Concurrent;
using System.Net;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace PKServ.Business.Raid
{
    /// <summary>
    /// Gestionnaire SSE (Server-Sent Events) pour l'overlay de raid en temps réel.
    ///
    /// Principe SSE :
    ///   - Le client ouvre une connexion HTTP GET longue durée sur /current_raid_stream.
    ///   - Le serveur ne ferme PAS la réponse : il envoie des lignes "data: ...\n\n" au fil
    ///     du temps, sans que le client n'ait à ré-interroger.
    ///   - Le navigateur expose ces events via l'API EventSource (standard HTML5).
    ///   - Avantage vs polling : zéro latence, zéro requête inutile.
    ///
    /// Cohabitation avec le polling existant (raidOverlay.html) :
    ///   - L'ancien overlay continue de fonctionner via GET /GetRaidInfos (toutes les 10s).
    ///   - Ce manager ne touche pas à cette route.
    ///   - Les deux overlays peuvent être affichés simultanément sans interférence.
    ///
    /// Thread-safety :
    ///   - _clients est un ConcurrentDictionary → ajout/suppression thread-safe.
    ///   - L'écriture dans chaque stream se fait dans un lock(client) pour éviter
    ///     les écritures concurrentes sur le même OutputStream.
    /// </summary>
    public static class RaidSseManager
    {
        // Chaque client connecté est identifié par un Guid et stocke sa réponse HTTP ouverte
        private static readonly ConcurrentDictionary<Guid, HttpListenerResponse> _clients = new();

        // ── Connexion d'un nouveau client ─────────────────────────────────────

        /// <summary>
        /// Enregistre un nouveau client SSE et maintient sa connexion ouverte.
        /// À appeler depuis le handler HTTP dès qu'une requête GET /current_raid_stream arrive.
        /// Cette méthode est async et bloque jusqu'à la déconnexion du client.
        ///
        /// La déconnexion est détectée par une IOException lors de l'écriture sur le stream :
        /// on NE lit PAS request.InputStream (un GET sans body retourne 0 immédiatement
        /// et déclencherait une fausse annulation).
        /// </summary>
        public static async Task RegisterClientAsync(
            HttpListenerResponse response,
            AppSettings settings,
            GlobalAppSettings globalSettings,
            CancellationToken cancellationToken = default)
        {
            Guid id = Guid.NewGuid();

            // Headers SSE obligatoires
            response.ContentType  = "text/event-stream";
            response.AddHeader("Cache-Control", "no-cache");
            response.AddHeader("X-Accel-Buffering", "no"); // désactive le buffering Nginx si proxy

            _clients[id] = response;

            try
            {
                // On envoie immédiatement l'état courant du raid au client qui vient de se connecter
                // (évite une page vide jusqu'au prochain event)
                await SendToClientAsync(response, BuildRaidEvent(settings, globalSettings, new System.Collections.Generic.List<string>()));

                // Maintient la connexion ouverte avec un heartbeat toutes les 15 secondes.
                // Sans heartbeat, certains proxies ou navigateurs ferment la connexion inactive.
                // La boucle se termine quand l'écriture échoue (IOException = client déconnecté)
                // ou quand le CancellationToken est annulé (arrêt du serveur).
                while (!cancellationToken.IsCancellationRequested)
                {
                    await Task.Delay(15_000, cancellationToken).ConfigureAwait(false);
                    // Commentaire SSE : commence par ":" → ignoré par EventSource mais garde la connexion vivante
                    await SendToClientAsync(response, ": heartbeat\n\n");
                }
            }
            catch (OperationCanceledException) { /* arrêt propre du serveur */ }
            catch { /* IOException : le client a coupé la connexion — nettoyage ci-dessous */ }
            finally
            {
                _clients.TryRemove(id, out _);
                try { response.Close(); } catch { }
            }
        }

        // ── Diffusion d'un event à tous les clients ───────────────────────────

        /// <summary>
        /// Pousse l'état courant du raid à TOUS les clients SSE connectés.
        /// À appeler après chaque AttackAsync, GivePoke, lancement ou fin de raid.
        /// </summary>
        public static void BroadcastRaidState(AppSettings settings, GlobalAppSettings globalSettings)
            => BroadcastRaidStateWithDamages(settings, globalSettings, new System.Collections.Generic.List<string>());

        public static void BroadcastRaidStateWithDamages(AppSettings settings, GlobalAppSettings globalSettings, System.Collections.Generic.List<string> damages)
        {
            string eventData = BuildRaidEvent(settings, globalSettings, damages);

            foreach (var (id, response) in _clients)
            {
                // Fire-and-forget par client : un client lent ne bloque pas les autres
                _ = Task.Run(async () =>
                {
                    try
                    {
                        await SendToClientAsync(response, eventData);
                    }
                    catch
                    {
                        // Le client s'est déconnecté entre-temps → on le retire
                        _clients.TryRemove(id, out _);
                        try { response.Close(); } catch { }
                    }
                });
            }
        }

        // ── Construction du payload SSE ───────────────────────────────────────

        /// <summary>
        /// Construit la chaîne SSE à envoyer.
        /// Format SSE : "data: {json}\n\n"
        ///   - "data:" est le champ reconnu par EventSource.
        ///   - Le double \n\n marque la fin de l'event.
        ///
        /// Si aucun raid actif → on envoie { "active": false } pour que le client
        /// masque l'overlay sans avoir besoin de timeout côté client.
        /// </summary>
        private static string BuildRaidEvent(AppSettings settings, GlobalAppSettings globalSettings, System.Collections.Generic.List<string> damages)
        {
            object payload;

            if (settings.ActiveRaid is null)
            {
                // Raid terminé ou pas encore commencé
                payload = new { active = false };
            }
            else
            {
                var raid = settings.ActiveRaid;
                payload = new
                {
                    active         = true,
                    Url_Creature   = raid.DisplayShiny ? raid.Boss.Sprite_Shiny : raid.Boss.Sprite_Normal,
                    Url_Overlay    = raid.PV > 0
                        ? "https://upload.wikimedia.org/wikipedia/commons/thumb/8/89/HD_transparent_picture.png/1280px-HD_transparent_picture.png"
                        : globalSettings.RaidSettings.PictureOverlayWhenCreatureFainted,
                    Bar_Max          = raid.PVMax,
                    Bar_CurrentValue = raid.PV,
                    Rarity           = raid.Boss.Rarity,
                    Damages          = damages
                };
            }

            // Sérialisation compacte (pas de retours à la ligne dans le JSON → obligatoire pour SSE)
            string json = JsonSerializer.Serialize(payload);
            return $"data: {json}\n\n";
        }

        // ── Utilitaire d'écriture ─────────────────────────────────────────────

        private static async Task SendToClientAsync(HttpListenerResponse response, string ssePayload)
        {
            byte[] bytes = Encoding.UTF8.GetBytes(ssePayload);
            // lock sur la réponse pour éviter des écritures concurrentes sur le même stream
            lock (response)
            {
                response.OutputStream.WriteAsync(bytes, 0, bytes.Length).GetAwaiter().GetResult();
                response.OutputStream.FlushAsync().GetAwaiter().GetResult();
            }
            await Task.CompletedTask;
        }

        /// <summary>Nombre de clients SSE actuellement connectés (utile pour le debug).</summary>
        public static int ConnectedClients => _clients.Count;
    }
}
