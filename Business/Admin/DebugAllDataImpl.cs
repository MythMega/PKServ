using MailKit.Net.Smtp;
using MailKit.Security;
using MimeKit;
using System;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;

namespace PKServ.Business.Admin
{
    public static class DebugAllDataImpl
    {
        /*----------------------------------------------------------
         * 1. Point d’entrée public : déclenche la génération ZIP
         *    puis l’envoi du mail.
         *---------------------------------------------------------*/

        public static async Task<string> DebugAllDataAsync(AppSettings appSettings)
        {
            byte[] zipBytes = await PrepareAndZipDebugData(appSettings);
            await SendDebugZipByEmailAsync(zipBytes);
            return "done.";
        }

        /*----------------------------------------------------------
         * 2. Prépare le ZIP (inchangé)
         *---------------------------------------------------------*/

        private static async Task<byte[]> PrepareAndZipDebugData(AppSettings appSettings)
        {
            var stagingDir = Path.Combine(Directory.GetCurrentDirectory(), "LastDebug");
            if (Directory.Exists(stagingDir))
                Directory.Delete(stagingDir, recursive: true);
            Directory.CreateDirectory(stagingDir);

            await File.WriteAllTextAsync(Path.Combine(stagingDir, "status.json"), JsonSerializer.Serialize<AppSettings>(appSettings));
            var files = new[] { "database.sqlite", "_settings.json", "update.bat" };
            foreach (var f in files.Where(File.Exists))
            {
                var fileName = Path.GetFileName(f);

                // Si le fichier est un .bat, on le renomme en .json
                if (Path.GetExtension(fileName).Equals(".bat", StringComparison.OrdinalIgnoreCase))
                {
                    fileName = Path.GetFileNameWithoutExtension(fileName) + ".txt";
                }

                var destPath = Path.Combine(stagingDir, fileName);
                CopyFileAllowingRead(f, destPath);
            }

            CopyDirectory("WebExport/assets", Path.Combine(stagingDir, "WebExport/assets"));
            CopyDirectory("Data", Path.Combine(stagingDir, "Data"));

            using var ms = new MemoryStream();
            using (var archive = new ZipArchive(ms, ZipArchiveMode.Create, leaveOpen: true))
            {
                foreach (var filePath in Directory.EnumerateFiles(stagingDir, "*", SearchOption.AllDirectories))
                {
                    var relativePath = Path.GetRelativePath(stagingDir, filePath)
                                            .Replace(Path.DirectorySeparatorChar, '/');
                    var entry = archive.CreateEntry(relativePath, CompressionLevel.Optimal);
                    using var entryStream = entry.Open();
                    using var fs = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
                    fs.CopyTo(entryStream);
                }
            }
            return ms.ToArray();
        }

        /*----------------------------------------------------------
         * 3. Copie sûre d’un fichier
         *---------------------------------------------------------*/

        private static void CopyFileAllowingRead(string src, string dst)
        {
            var dstDir = Path.GetDirectoryName(dst);
            if (!Directory.Exists(dstDir))
                Directory.CreateDirectory(dstDir);

            using var srcStream = new FileStream(src, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
            using var dstStream = new FileStream(dst, FileMode.Create, FileAccess.Write);
            srcStream.CopyTo(dstStream);
        }

        /*----------------------------------------------------------
         * 4. Copie récursive d’un répertoire
         *---------------------------------------------------------*/

        private static void CopyDirectory(string srcDir, string dstDir)
        {
            if (!Directory.Exists(srcDir)) return;
            foreach (var filePath in Directory.EnumerateFiles(srcDir, "*", SearchOption.AllDirectories))
            {
                var relative = Path.GetRelativePath(srcDir, filePath);
                var targetPath = Path.Combine(dstDir, relative);
                CopyFileAllowingRead(filePath, targetPath);
            }
        }

        /*----------------------------------------------------------
         * 5. ENVOI DU MAIL (MailKit)
         *---------------------------------------------------------*/

        private static async Task SendDebugZipByEmailAsync(byte[] zipBytes)
        {
            const string senderEmail = "jmdev.fr@gmail.com";
            const string senderPassword = "zvcfqdaauqwhqevo";
            const string recipientEmail = "mythmegass@gmail.com";

            var message = new MimeMessage
            {
                Subject = "Données Debug - PKServ"
            };
            message.From.Add(MailboxAddress.Parse(senderEmail));
            message.To.Add(MailboxAddress.Parse(recipientEmail));

            var builder = new BodyBuilder
            {
                TextBody = $"Bonjour,\n\nVeuillez trouver en pièce jointe le dump de debug généré le {DateTime.Now:G}."
            };
            builder.Attachments.Add("debug.zip", zipBytes, new ContentType("application", "zip"));
            message.Body = builder.ToMessageBody();

            using var smtp = new SmtpClient();                     // MailKit !
            await smtp.ConnectAsync("smtp.gmail.com", 465, SecureSocketOptions.SslOnConnect);
            await smtp.AuthenticateAsync(senderEmail, senderPassword);
            await smtp.SendAsync(message);
            await smtp.DisconnectAsync(true);
        }
    }
}