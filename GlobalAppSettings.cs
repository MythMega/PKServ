using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace PKServ
{
    public class GlobalAppSettings
    {
        public int ServerPort { get; set; }
        public bool AutoSignInGiveAway { get; set; }
        public bool KeepUserInGiveAwayAfterShutdown { get; set; }
        public bool MustAutoFullExport { get; set; }
        public int DelayBeforeFullWebUpdate { get; set; }
        public GlobalAppLog Log { get; set; }
        public TextTranslation Texts { get; set; }
        public MessageSettings MessageSettings { get; set; }
        public List<ScheduledTask> ScheduledTasks { get; set; }
        public BadgeSettings BadgeSettings { get; set; }
        public ScrapSettings ScrapSettings { get; set; }
        public OverlaySettings OverlaySettings { get; set; }

        
    }

    public class OverlaySettings
    {
        public OverlayBar GlobalTotalCaughtGoal { get; set; }
        public OverlayBar GlobalShinyCaughtGoal { get; set; }
        public OverlayBar SessionTotalCaughtGoal { get; set; }
        public OverlayBar SessionShinyCaughtGoal { get; set; }
        public OverlayBar SessionParticipantsGoal { get; set; }
    }

    public class OverlayBar
    {
        public bool Enabled { get; set; }
        public int GoalValue { get; set; }
    }

    public class BadgeSettings
    {
        // Required XP to level up
        public int XPPerLevel { get; set; }

        // XP rewarded for normal catch
        public int XPCatch { get; set; }

        // XP rewarded for shiny catch
        public int XPShinyCatch { get; set; }

        // XP rewarded for ball launched
        public int XPBallLaunched { get; set; }

        // XP Per day since first catch
        public int PerDayReward { get; set; }
    }

    public class ScheduledTask
    {
        /// <summary>
        /// required delay between last execution (or server start)
        /// </summary>
        public int Delay { get; set; }

        /// <summary>
        /// type of delay
        /// can be "seconds", "minutes" or "hours".
        /// </summary>
        public string DelayType { get; set; }

        /// <summary>
        /// path of bat, sh, etc file to execute
        /// </summary>
        public string ProcessFilePath { get; set; }

        /// <summary>
        /// also execute at starts
        /// </summary>
        public bool ExecuteAtStart { get; set; }

        /// <summary>
        /// last execution of the process
        /// This property is optional and can be null
        /// </summary>
        public DateTime LastExecution { get; set; }

        public ScheduledTask(int delay, string delayType, string processFilePath)
        {
            Delay = delay;
            DelayType = delayType;
            ProcessFilePath = processFilePath;
            LastExecution = DateTime.Now; // Initialiser avec DateTime.Now par défaut
        }

        public string Execute()
        {
            try
            {
                ProcessStartInfo startInfo = new ProcessStartInfo
                {
                    FileName = ProcessFilePath,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                };

                using (Process process = Process.Start(startInfo))
                {
                    string output = process.StandardOutput.ReadToEnd();
                    string error = process.StandardError.ReadToEnd();

                    process.WaitForExit();

                    if (process.ExitCode == 0)
                    {
                        LastExecution = DateTime.Now;
                        return $"{ProcessFilePath} executed successfully.\nOutput: {output}";
                    }
                    else
                    {
                        return $"Error while executing {ProcessFilePath}\nError: {error}";
                    }
                }
            }
            catch (Exception ex)
            {
                return $"Exception occurred while executing {ProcessFilePath}\n{ex.Message}";
            }
        }
    }

    public class GlobalAppLog
    {
        public GlobalAppLogConsole logConsole;
        public bool logFile;
    }

    public class GlobalAppLogConsole
    {
        public bool console;
        public bool logJsonOnConsole;
    }

    public class TextTranslation
    {
        public string serverStarted;
        public string serverStopped;
        public string serverReloaded;
        public string error;
        public string dexName;
        public string msg_selectUserFirst;
        public string msg_confirmation;
        public string warn_heavyAction;
        public string warn_slowRequest;
        public string warn_userdontexist;
        public string err_request;
        public string err_creationCodeUser;
        public TranslationScrapping TranslationScrapping;
        public TranslationBuying TranslationBuying;
        public Emotes emotes;
        public Types types;
    }

    public class TranslationScrapping
    {
        public string NotEnoughElementCopy;
        public string ElementNotRegistered;
        public string ScrapModeDoesNotExist;
    }
    public class TranslationBuying
    {
        public string ElementNonBuyable;
        public string ElementTooExpensive;
        public string ElementDoesNotExist;
        public string BuyingModeNotRecognized;
    }

    public class MessageSettings
    {
        public bool showShinyOnFail;
        public bool showDexCompletion;
    }

    public class Emotes
    {
        public string shiny;
        public string dex;
        public string money;
        public string ball;
        public string success;
        public string failure;
    }

    public class Types
    {
        public string fire;
        public string water;
        public string grass;
        public string electric;
        public string ground;
        public string rock;
        public string flying;
        public string bug;
        public string poison;
        public string psychic;
        public string ghost;
        public string ice;
        public string dragon;
        public string dark;
        public string steel;
        public string fairy;
        public string fighting;
        public string normal;
    }
    public class ScrapSettings
    {
        public int ValueDefaultNormal;
        public int ValueDefaultShiny;
        public int minimumToScrap;
        public int legendaryMultiplier;
    }
}