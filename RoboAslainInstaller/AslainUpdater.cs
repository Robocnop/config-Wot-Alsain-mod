using System;
using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;

namespace RoboAslainInstaller
{
    public class AslainUpdater
    {
        private readonly AppConfig _config;
        private readonly Logger _logger;
        private const string ASLAIN_DOWNLOAD_URL = "https://aslain.com/index.php?/topic/13-download/"; // URL de base

        // HttpClient partagé (timeout long pour le gros fichier installateur)
        private static readonly HttpClient _httpClient = CreateClient();

        private static HttpClient CreateClient()
        {
            var client = new HttpClient { Timeout = TimeSpan.FromMinutes(30) };
            client.DefaultRequestHeaders.Add("User-Agent", "RoboAslainInstaller/2.0");
            return client;
        }

        public AslainUpdater(AppConfig config, Logger logger)
        {
            _config = config;
            _logger = logger;
        }

        /// <summary>
        /// Vérifie et télécharge la dernière version d'Aslain
        /// </summary>
        public async Task<OperationResult<string>> DownloadLatestAslainAsync(string? downloadUrl = null)
        {
            _logger.Info("🔄 Recherche de la dernière version d'Aslain...");

            // Utiliser l'URL fournie ou celle par défaut
            var url = downloadUrl ?? _config.AslainDownloadUrl ?? ASLAIN_DOWNLOAD_URL;
            
            try
            {
                // Si c'est juste l'URL de la page, informer l'utilisateur
                if (url.Contains("aslain.com") && !url.EndsWith(".exe"))
                {
                    _logger.Warning("⚠️  URL de téléchargement non directe détectée.");
                    _logger.Info("   Veuillez copier le lien direct du .exe depuis le site Aslain");
                    _logger.Info($"   Site: {url}");
                    
                    Console.WriteLine("\n📝 Pour télécharger automatiquement:");
                    Console.WriteLine("   1. Visitez le site Aslain");
                    Console.WriteLine("   2. Copiez le lien DIRECT du fichier .exe");
                    Console.WriteLine("   3. Relancez avec: RoboAslainInstaller.exe --update-aslain <URL>");
                    
                    // Ouvrir le navigateur
                    Process.Start(new ProcessStartInfo
                    {
                        FileName = url,
                        UseShellExecute = true
                    });
                    
                    return OperationResult<string>.Fail(
                        "URL de téléchargement direct requise",
                        "Le navigateur a été ouvert sur la page de téléchargement Aslain"
                    );
                }

                var tempPath = Path.Combine(Path.GetTempPath(), "Aslains_WoT_Modpack_Installer_Latest.exe");
                
                _logger.Info($"📥 Téléchargement depuis: {url}");
                _logger.Info("   Cela peut prendre plusieurs minutes...");

                // Téléchargement avec progression
                using (var response = await _httpClient.GetAsync(url, HttpCompletionOption.ResponseHeadersRead))
                {
                    response.EnsureSuccessStatusCode();

                    var totalBytes = response.Content.Headers.ContentLength ?? -1;
                    var downloadedBytes = 0L;

                    using (var contentStream = await response.Content.ReadAsStreamAsync())
                    using (var fileStream = new FileStream(tempPath, FileMode.Create, FileAccess.Write, FileShare.None, 8192, true))
                    {
                        var buffer = new byte[8192];
                        var lastPercent = 0;

                        while (true)
                        {
                            var bytesRead = await contentStream.ReadAsync(buffer, 0, buffer.Length);
                            if (bytesRead == 0) break;

                            await fileStream.WriteAsync(buffer, 0, bytesRead);
                            downloadedBytes += bytesRead;

                            if (totalBytes > 0)
                            {
                                var percent = (int)((downloadedBytes * 100) / totalBytes);
                                if (percent > lastPercent && percent % 10 == 0)
                                {
                                    Console.Write($"\r   Progression: {percent}% ({FormatFileSize(downloadedBytes)} / {FormatFileSize(totalBytes)})   ");
                                    lastPercent = percent;
                                }
                            }
                        }

                        Console.WriteLine($"\r   Progression: 100% ({FormatFileSize(downloadedBytes)})   ");
                    }
                }

                // Vérifier le fichier
                if (!File.Exists(tempPath))
                {
                    return OperationResult<string>.Fail("Le fichier n'a pas été téléchargé");
                }

                var fileInfo = new FileInfo(tempPath);
                _logger.Success($"✅ Aslain téléchargé ({FormatFileSize(fileInfo.Length)})");

                return OperationResult<string>.Ok(
                    "Dernière version téléchargée",
                    tempPath,
                    $"Taille: {FormatFileSize(fileInfo.Length)}"
                );
            }
            catch (HttpRequestException ex)
            {
                _logger.Error("Erreur réseau lors du téléchargement", ex);
                return OperationResult<string>.Fail(
                    "Impossible de télécharger Aslain",
                    $"Vérifiez votre connexion et l'URL: {url}",
                    ex
                );
            }
            catch (Exception ex)
            {
                _logger.Error("Erreur lors du téléchargement", ex);
                return OperationResult<string>.Fail(
                    "Erreur lors du téléchargement d'Aslain",
                    ex.Message,
                    ex
                );
            }
        }

        /// <summary>
        /// Installe la version téléchargée d'Aslain
        /// </summary>
        public OperationResult InstallAslainUpdate(string installerPath, AslainLocation? aslainLocation = null)
        {
            _logger.Info("📦 Installation de la mise à jour Aslain...");

            try
            {
                if (!File.Exists(installerPath))
                {
                    return OperationResult.Fail("Fichier d'installation introuvable");
                }

                // Si on a un emplacement Aslain, sauvegarder l'ancien installateur
                if (aslainLocation != null && File.Exists(aslainLocation.InstallerPath))
                {
                    var backupPath = aslainLocation.InstallerPath.Replace(".exe", "_backup.exe");
                    _logger.Info($"💾 Sauvegarde de l'ancien installateur: {Path.GetFileName(backupPath)}");
                    
                    try
                    {
                        File.Copy(aslainLocation.InstallerPath, backupPath, overwrite: true);
                    }
                    catch (Exception ex)
                    {
                        _logger.Warning($"⚠️  Impossible de créer la sauvegarde: {ex.Message}");
                    }

                    // Copier le nouvel installateur
                    _logger.Info("📋 Installation du nouvel installateur...");
                    File.Copy(installerPath, aslainLocation.InstallerPath, overwrite: true);
                    
                    _logger.Success("✅ Installateur mis à jour !");
                    _logger.Info($"   Emplacement: {aslainLocation.Path}");
                }

                // Lancer l'installateur
                _logger.Info("🚀 Lancement de l'installateur Aslain...");
                
                var startInfo = new ProcessStartInfo
                {
                    FileName = installerPath,
                    UseShellExecute = true
                };

                var process = Process.Start(startInfo);
                
                if (process == null)
                {
                    return OperationResult.Fail("Impossible de démarrer l'installateur");
                }

                _logger.Success("✅ Installateur lancé avec succès !");
                
                return OperationResult.Ok(
                    "Mise à jour lancée",
                    $"PID: {process.Id}"
                );
            }
            catch (Exception ex)
            {
                _logger.Error("Erreur lors de l'installation", ex);
                return OperationResult.Fail(
                    "Erreur lors de l'installation",
                    ex.Message,
                    ex
                );
            }
        }

        /// <summary>
        /// Télécharge ET installe la dernière version
        /// </summary>
        public async Task<OperationResult> UpdateAslainAsync(AslainLocation? aslainLocation = null, string? downloadUrl = null)
        {
            // Télécharger
            var downloadResult = await DownloadLatestAslainAsync(downloadUrl);
            if (!downloadResult.Success || downloadResult.Data == null)
            {
                return OperationResult.Fail(downloadResult.Message, downloadResult.Details);
            }

            // Installer
            var installResult = InstallAslainUpdate(downloadResult.Data, aslainLocation);
            
            // Nettoyer le fichier temporaire si installé avec succès
            if (installResult.Success)
            {
                try
                {
                    File.Delete(downloadResult.Data);
                }
                catch
                {
                    // Ignorer les erreurs de nettoyage
                }
            }

            return installResult;
        }

        private static string FormatFileSize(long bytes)
        {
            string[] sizes = { "B", "KB", "MB", "GB" };
            double len = bytes;
            int order = 0;

            while (len >= 1024 && order < sizes.Length - 1)
            {
                order++;
                len /= 1024;
            }

            return $"{len:0.##} {sizes[order]}";
        }
    }
}
