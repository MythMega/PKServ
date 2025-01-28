using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace PKServ
{
    internal class Debug
    {
        private string DATA { get; set; }
        public string Request { get; set; }
        private string folderCurrentDebug { get; set; }
        public List<User> UsersHere { get; set; }
        public AppSettings AppSettings { get; set; }
        public GlobalAppSettings GlobalAppSettings { get; set; }

        public Debug(string DATA)
        {
            this.DATA = DATA;
        }

        public Debug()
        {
        }

        public async Task<string> DoDebug()
        {
            StringBuilder log = new StringBuilder();
            folderCurrentDebug = "debug" + DateTime.Now.ToString("yyyyMMdd_HHmmss");
            log.AppendLine($"Dossier {folderCurrentDebug}");
            log.AppendLine($"\n\n\nDebug Export {DateTime.Now.ToString("f")}");
            if (DATA == "all" || true)
            {
                string sourceDirectory = Directory.GetCurrentDirectory();
                string targetDirectory = Path.Combine("ExportsSimple", "DEBUG", folderCurrentDebug);
                string targetDirectoryFullPath = Path.GetFullPath(targetDirectory);

                // Créer les dossiers s'ils n'existent pas
                Directory.CreateDirectory(targetDirectory);

                log.AppendLine("Dossier créé.");

                try
                {
                    log.AppendLine($"génération des fichiers json.\n");
                    // Copier les fichiers JSON
                    foreach (string file in Directory.EnumerateFiles(sourceDirectory, "*.json"))
                    {
                        List<string> files = new List<string> { "balls.json", "badges.json", "customOverlays.json", "customPokemons.json", "Triggers.json", "pokemons.json", "_settings.json" };
                        if (files.Contains(Path.GetFileName(file)))
                        {
                            try
                            {
                                string fileName = Path.GetFileName(file);
                                string targetFilePath = Path.Combine(targetDirectory, fileName);
                                File.Copy(file, targetFilePath, overwrite: true);
                                log.AppendLine($"fichier {fileName} créé.\n");
                            }
                            catch (Exception e)
                            {
                                log.AppendLine($"fichier {Path.GetFileName(file)} échech lors de la copie. \n{e.Message}\n{e.Source}\n{e.Data}\n");
                            }
                        }
                        else
                        {
                            log.AppendLine($"fichier {Path.GetFileName(file)} ignoré.\n");
                        }
                    }

                    // full info
                    try
                    {
                        log.AppendLine("\ngénération du fichier fullInfoEnvironnement.json ... ");
                        File.WriteAllText(Path.Combine(targetDirectory, "fullInfoEnvironnement.json"), JsonSerializer.Serialize(AppSettings, Commun.GetJsonSerializerOptions()));
                        log.AppendLine("\ngénération réussie.");
                    }
                    catch (Exception e)
                    {
                        log.AppendLine($"Erreur lors de la génération du fichier fullInfoEnvironnement.json : \n{e.Message}\n{e.Source}\n{e.Data}\n");
                    }

                    // request
                    try
                    {
                        log.AppendLine("\ngénération du fichier request.json ... ");
                        File.WriteAllText(Path.Combine(targetDirectory, "request.json"), JsonSerializer.Serialize(Request, Commun.GetJsonSerializerOptions()));
                        log.AppendLine("\ngénération réussie.");
                    }
                    catch (Exception e)
                    {
                        log.AppendLine($"Erreur lors de la génération du fichier request.json : \n{e.Message}\n{e.Source}\n{e.Data}\n");
                    }

                    // userhere
                    try
                    {
                        log.AppendLine("\ngénération du fichier userhere.json ... ");
                        File.WriteAllText(Path.Combine(targetDirectory, "userhere.json"), JsonSerializer.Serialize(UsersHere, Commun.GetJsonSerializerOptions()));
                        log.AppendLine("\ngénération réussie.");
                    }
                    catch (Exception e)
                    {
                        log.AppendLine($"Erreur lors de la génération du fichier userhere.json : \n{e.Message}\n{e.Source}\n{e.Data}\n");
                    }

                    // base de données
                    try
                    {
                        log.AppendLine("\ncopie de la base de donnée ... ");
                        File.Copy("database.sqlite", Path.Combine(targetDirectory, "database_backup.sqlite"));
                        log.AppendLine("\ngénération réussie.");
                    }
                    catch (Exception e)
                    {
                        log.AppendLine($"Erreur lors de la copie de la base de donnée : \n{e.Message}\n{e.Source}\n{e.Data}\n");
                    }

                    string zipFilePath = Path.Combine(targetDirectoryFullPath, $"debug{DateTime.Now.ToString("yyyyMMdd_HHmmss")}.zip");
                    try
                    {
                        log.AppendLine("\ncreation du proccess de zipping ... ");
                        File.WriteAllText(Path.Combine(targetDirectory, "zipper.bat"), @$"@echo off
setlocal

:: Définir le nom du fichier ZIP
set zipFile=""{zipFilePath}""

:: Supprimer le fichier ZIP s'il existe déjà
if exist ""{zipFilePath}"" (
    del ""{zipFilePath}""
)

:: Créer un fichier ZIP en utilisant tar
tar -a -c -f ""{zipFilePath}"" --exclude=zipper.bat *

:: Vérifier si le fichier ZIP a été créé avec succès
if exist ""{zipFilePath}"" (
    echo Les fichiers ont été zippés avec succès dans ""{zipFilePath}"".
) else (
    echo Erreur lors de la création de l'archive ZIP.
    exit /b 1
)

endlocal
exit /b 0
");

                        log.AppendLine("\ncreation reussie ... ");
                    }
                    catch (Exception e)
                    {
                        log.AppendLine($"Erreur lors de la creation du script de zipping : \n{e.Message}\n{e.Source}\n{e.Data}\n");
                    }
                    File.WriteAllText(Path.Combine(targetDirectory, "log.txt"), log.ToString());
                    string ProcessFilePath = Path.Combine(targetDirectoryFullPath, "zipper.bat");

                    Process process = new Process();
                    process.StartInfo.FileName = ProcessFilePath;
                    process.StartInfo.WorkingDirectory = targetDirectoryFullPath;
                    process.StartInfo.RedirectStandardOutput = true;
                    process.StartInfo.RedirectStandardError = true;
                    process.StartInfo.UseShellExecute = false;
                    process.StartInfo.CreateNoWindow = true;

                    process.Start();

                    // Capture standard output and error output
                    string output = await process.StandardOutput.ReadToEndAsync();
                    string error = await process.StandardError.ReadToEndAsync();

                    await process.WaitForExitAsync();

                    Console.WriteLine(output);
                    Console.WriteLine(error);

                    if (process.ExitCode == 0)
                    {
                        Console.WriteLine($"\ntask {Path.GetFileName(ProcessFilePath)} success.");
                        return await Commun.UploadFileAsync(zipFilePath, globalAppSettings: GlobalAppSettings);
                    }
                    else
                    {
                        throw new Exception($"Script batch failed with exit code {process.ExitCode}.");
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Erreur: {ex.Message}");
                    return "null : " + ex.Message;
                }
            }
            else
            {
                return "null : invalid arg";
            }
        }

        internal void SetEnv(List<User> users, AppSettings settings, string request, GlobalAppSettings globalAppSettings)
        {
            UsersHere = users;
            AppSettings = settings;
            Request = request;
            GlobalAppSettings = globalAppSettings;
        }
    }
}