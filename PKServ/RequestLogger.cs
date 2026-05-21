using System;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Concurrent;

namespace PKServ
{
    /// <summary>
    /// Logger de requêtes HTTP thread-safe, à écriture asynchrone dans un fichier de log rotatif.
    /// Chaque ligne est au format :
    ///   [yyyy-MM-dd HH:mm:ss.fff] [VERB ] [ENTER|DONE |BGDONE|BGERR ] [durée ms ou ---    ] path | détail
    ///
    /// Le fichier actif est logs/requests_YYYY-MM-DD.log.
    /// Un nouveau fichier est créé automatiquement à minuit.
    /// </summary>
    public static class RequestLogger
    {
        private static readonly ConcurrentQueue<string> _queue = new();
        private static readonly SemaphoreSlim _signal = new(0);
        private static string _logDir = "logs";
        private static bool _running = false;

        // ── Démarrage ────────────────────────────────────────────────────────────

        /// <summary>Démarre le writer de log en arrière-plan. À appeler une seule fois au démarrage.</summary>
        public static void Start(string logDirectory = "logs")
        {
            if (_running) return;
            _running = true;
            _logDir = logDirectory;
            Directory.CreateDirectory(_logDir);

            _ = Task.Run(async () =>
            {
                while (true)
                {
                    await _signal.WaitAsync();
                    while (_queue.TryDequeue(out string line))
                    {
                        try
                        {
                            // Fichier rotatif par jour dans le dossier logs/
                            string dayFile = Path.Combine(_logDir, $"requests_{DateTime.Now:yyyy-MM-dd}.log");
                            await File.AppendAllTextAsync(dayFile, line + "\n", Encoding.UTF8);

                            // logs.txt en temps réel à côté de l'exe (AppDomain.CurrentDomain.BaseDirectory
                            // pointe toujours vers le répertoire de l'exe, même si le CWD diffère)
                            string liveFile = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "logs.txt");
                            await File.AppendAllTextAsync(liveFile, line + "\n", Encoding.UTF8);
                        }
                        catch { /* ne jamais planter le writer */ }
                    }
                }
            });
        }

        // ── API publique ─────────────────────────────────────────────────────────

        /// <summary>Enregistre l'arrivée d'une requête. Retourne un Stopwatch démarré + un id court pour le DONE.</summary>
        public static (Stopwatch sw, string id) Enter(string method, string path, string body = null)
        {
            var sw = Stopwatch.StartNew();
            string id = NewId();
            string bodyPreview = body is null ? "" : TruncateBody(body);
            Write(method, "ENTER", "---     ", id, path, bodyPreview);
            return (sw, id);
        }

        /// <summary>Enregistre la fin d'une requête traitée sur le thread principal.</summary>
        public static void Done(string method, string path, Stopwatch sw, string id, string response = null)
        {
            sw.Stop();
            string preview = response is null ? "" : TruncateBody(response);
            string flag = sw.ElapsedMilliseconds >= 1000 ? "DONE ⚠" : "DONE  ";
            Write(method, flag, $"{sw.ElapsedMilliseconds,7}ms", id, path, preview);
        }

        /// <summary>Enregistre la fin d'une tâche background (export, overlay, scheduled task…).</summary>
        public static void BackgroundDone(string label, Stopwatch sw, string detail = null)
        {
            sw.Stop();
            string flag = sw.ElapsedMilliseconds >= 5000 ? "BGDONE⚠" : "BGDONE ";
            Write("BG  ", flag, $"{sw.ElapsedMilliseconds,7}ms", "-", label, detail ?? "");
        }

        /// <summary>Enregistre une erreur dans une tâche background.</summary>
        public static void BackgroundError(string label, Exception ex)
        {
            Write("BG  ", "BGERR  ", "---     ", "-", label, $"{ex.GetType().Name}: {ex.Message}");
        }

        /// <summary>Log libre — pour tout autre événement remarquable.</summary>
        public static void Info(string message)
        {
            Write("    ", "INFO   ", "---     ", "-", message, "");
        }

        // ── Implémentation interne ───────────────────────────────────────────────

        private static void Write(string method, string status, string duration, string id, string path, string detail)
        {
            // Format : [2025-01-15 14:32:07.421] [POST ] [DONE   ] [ 1234ms] #a3f | CatchPoke | {"UserName":"..."}
            string line = $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}] " +
                          $"[{method,-4}] " +
                          $"[{status,-7}] " +
                          $"[{duration}] " +
                          $"#{id} | {path}" +
                          (string.IsNullOrEmpty(detail) ? "" : $" | {detail}");

            _queue.Enqueue(line);
            _signal.Release();
        }

        private static int _idCounter = 0;
        private static string NewId()
        {
            int n = Interlocked.Increment(ref _idCounter);
            return n.ToString("x4"); // ex: "00a3"
        }

        private static string TruncateBody(string s, int max = 200)
        {
            if (s.Length <= max) return s.Replace('\n', ' ').Replace('\r', ' ');
            return s[..max].Replace('\n', ' ').Replace('\r', ' ') + "…";
        }
    }
}
