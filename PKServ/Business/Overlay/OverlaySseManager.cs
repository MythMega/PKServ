using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Net;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace PKServ.Business.Overlay
{
    /// <summary>
    /// Gestionnaire SSE générique multi-canaux pour tous les overlays temps réel.
    ///
    /// Chaque "canal" est identifié par un nom (voir OverlaySseChannels).
    /// Plusieurs clients peuvent être connectés sur le même canal simultanément
    /// (ex: OBS + navigateur de test).
    ///
    /// Principe de fonctionnement :
    ///   1. Le client HTML ouvre un EventSource sur GET /overlay/{name}/stream.
    ///   2. Program.cs appelle RegisterClientAsync → connexion maintenue indéfiniment.
    ///   3. À chaque événement métier (catch, giveaway, etc.), le contrôleur appelle
    ///      BroadcastChannel(channelName, payload) → tous les clients du canal reçoivent l'event.
    ///   4. La déconnexion est détectée à la prochaine écriture (IOException), pas via InputStream.Read.
    ///
    /// Thread-safety : ConcurrentDictionary à deux niveaux (canal → clients).
    /// </summary>
    public static class OverlaySseManager
    {
        // Structure : canal → { clientId → response }
        private static readonly ConcurrentDictionary<string, ConcurrentDictionary<Guid, HttpListenerResponse>>
            _channels = new(StringComparer.OrdinalIgnoreCase);

        // ── Enregistrement d'un client ────────────────────────────────────────

        /// <summary>
        /// Enregistre un client SSE sur le canal donné et maintient sa connexion ouverte.
        /// Envoie immédiatement le payload initial fourni par <paramref name="initialPayloadFn"/>.
        /// Bloque jusqu'à la déconnexion ou l'annulation.
        /// </summary>
        public static async Task RegisterClientAsync(
            string channel,
            HttpListenerResponse response,
            Func<string> initialPayloadFn,
            CancellationToken cancellationToken = default)
        {
            Guid id = Guid.NewGuid();

            // Headers SSE obligatoires
            response.ContentType = "text/event-stream";
            response.AddHeader("Cache-Control", "no-cache");
            response.AddHeader("X-Accel-Buffering", "no");

            // Crée le dictionnaire du canal si absent
            var clients = _channels.GetOrAdd(channel, _ => new ConcurrentDictionary<Guid, HttpListenerResponse>());
            clients[id] = response;

            try
            {
                // Envoi immédiat de l'état courant au nouveau client
                await SendAsync(response, initialPayloadFn());

                // Heartbeat toutes les 15s pour garder la connexion vivante côté proxy/navigateur.
                // La boucle se termine naturellement sur IOException (client déconnecté).
                while (!cancellationToken.IsCancellationRequested)
                {
                    await Task.Delay(15_000, cancellationToken).ConfigureAwait(false);
                    await SendAsync(response, ": heartbeat\n\n");
                }
            }
            catch (OperationCanceledException) { /* arrêt propre */ }
            catch { /* client déconnecté → nettoyage dans finally */ }
            finally
            {
                clients.TryRemove(id, out _);
                try { response.Close(); } catch { }
            }
        }

        // ── Diffusion sur un canal ────────────────────────────────────────────

        /// <summary>
        /// Pousse <paramref name="data"/> à tous les clients connectés sur <paramref name="channel"/>.
        /// Le payload doit déjà être un objet sérialisable ; cette méthode le sérialise en JSON
        /// et le formate en event SSE.
        /// </summary>
        public static void BroadcastChannel(string channel, object data)
        {
            string json  = JsonSerializer.Serialize(data);
            string frame = $"data: {json}\n\n";
            BroadcastRaw(channel, frame);
        }

        /// <summary>
        /// Pousse une chaîne SSE brute (déjà formatée "data: ...\n\n") à tous les clients du canal.
        /// </summary>
        public static void BroadcastRaw(string channel, string sseFrame)
        {
            if (!_channels.TryGetValue(channel, out var clients)) return;

            foreach (var (id, response) in clients)
            {
                _ = Task.Run(async () =>
                {
                    try   { await SendAsync(response, sseFrame); }
                    catch { clients.TryRemove(id, out _); try { response.Close(); } catch { } }
                });
            }
        }

        /// <summary>Nombre de clients connectés sur un canal (debug).</summary>
        public static int ClientCount(string channel)
            => _channels.TryGetValue(channel, out var c) ? c.Count : 0;

        // ── Utilitaire d'écriture ─────────────────────────────────────────────

        private static async Task SendAsync(HttpListenerResponse response, string ssePayload)
        {
            byte[] bytes = Encoding.UTF8.GetBytes(ssePayload);
            lock (response)
            {
                response.OutputStream.WriteAsync(bytes, 0, bytes.Length).GetAwaiter().GetResult();
                response.OutputStream.FlushAsync().GetAwaiter().GetResult();
            }
            await Task.CompletedTask;
        }
    }
}
