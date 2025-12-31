using System;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;

namespace RoboAslainInstaller
{
    public class ConfigDownloader
    {
        private readonly AppConfig _config;
        private readonly Logger _logger;

        public ConfigDownloader(AppConfig config, Logger logger)
        {
            _config = config;
            _logger = logger;
        }

        public async Task<OperationResult<string>> DownloadConfigAsync()
        {
            var url = _config.GetRawUrl();
            var tempPath = Path.Combine(Path.GetTempPath(), _config.ConfigFileName);

            _logger.Debug($"URL: {url}");
            _logger.Debug($"Destination: {tempPath}");

            try
            {
                using (var client = new HttpClient())
                {
                    client.DefaultRequestHeaders.Add("User-Agent", "RoboAslainInstaller/2.0");
                    client.Timeout = TimeSpan.FromMinutes(2);

                    // Téléchargement
                    Console.Write("   Téléchargement en cours");
                    var response = await client.GetAsync(url);
                    response.EnsureSuccessStatusCode();

                    var content = await response.Content.ReadAsByteArrayAsync();
                    await File.WriteAllBytesAsync(tempPath, content);
                    Console.WriteLine(" ✓");

                    // Vérifier que le fichier existe
                    if (!File.Exists(tempPath))
                    {
                        return OperationResult<string>.Fail(
                            "Le fichier n'a pas été créé",
                            $"Chemin attendu: {tempPath}"
                        );
                    }

                    var fileInfo = new FileInfo(tempPath);
                    _logger.Debug($"Fichier créé: {tempPath} ({FormatFileSize(fileInfo.Length)})");

                    return OperationResult<string>.Ok(
                        "Configuration téléchargée avec succès",
                        tempPath,
                        $"Taille: {FormatFileSize(fileInfo.Length)}"
                    );
                }
            }
            catch (HttpRequestException ex)
            {
                _logger.Error("Erreur réseau lors du téléchargement", ex);
                return OperationResult<string>.Fail(
                    "Impossible de télécharger la configuration",
                    $"Vérifiez votre connexion internet. URL: {url}",
                    ex
                );
            }
            catch (TaskCanceledException ex)
            {
                _logger.Error("Timeout lors du téléchargement", ex);
                return OperationResult<string>.Fail(
                    "Le téléchargement a pris trop de temps",
                    "Réessayez avec une meilleure connexion internet",
                    ex
                );
            }
            catch (Exception ex)
            {
                _logger.Error("Erreur inattendue lors du téléchargement", ex);
                return OperationResult<string>.Fail(
                    "Erreur lors du téléchargement",
                    ex.Message,
                    ex
                );
            }
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
