using System;
using System.Diagnostics;
using System.IO;

namespace RoboAslainInstaller
{
    public class ConfigInstaller
    {
        private readonly AppConfig _config;
        private readonly Logger _logger;

        public ConfigInstaller(AppConfig config, Logger logger)
        {
            _config = config;
            _logger = logger;
        }

        public OperationResult InstallConfig(string configPath, AslainLocation aslainLocation)
        {
            try
            {
                var targetPath = Path.Combine(aslainLocation.Path, _config.ConfigFileName);

                // Créer une sauvegarde si demandé
                if (_config.CreateBackup && File.Exists(targetPath))
                {
                    var backupResult = CreateBackup(targetPath);
                    if (!backupResult.Success)
                    {
                        _logger.Warning($"⚠️  Impossible de créer la sauvegarde: {backupResult.Message}");
                    }
                }

                // Copier la nouvelle configuration
                _logger.Debug($"Copie: {configPath} -> {targetPath}");
                File.Copy(configPath, targetPath, overwrite: true);

                // Vérifier que le fichier a été copié
                if (!File.Exists(targetPath))
                {
                    return OperationResult.Fail(
                        "La configuration n'a pas été copiée",
                        $"Chemin cible: {targetPath}"
                    );
                }

                var fileInfo = new FileInfo(targetPath);
                _logger.Debug($"Fichier installé: {targetPath}");

                return OperationResult.Ok(
                    "Configuration installée avec succès",
                    $"Emplacement: {targetPath}"
                );
            }
            catch (UnauthorizedAccessException ex)
            {
                _logger.Error("Accès refusé lors de l'installation", ex);
                return OperationResult.Fail(
                    "Permissions insuffisantes",
                    "Essayez de relancer le programme en tant qu'administrateur",
                    ex
                );
            }
            catch (IOException ex)
            {
                _logger.Error("Erreur d'entrée/sortie lors de l'installation", ex);
                return OperationResult.Fail(
                    "Impossible d'écrire le fichier de configuration",
                    ex.Message,
                    ex
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

        public OperationResult LaunchInstaller(AslainLocation aslainLocation)
        {
            try
            {
                var installerPath = aslainLocation.InstallerPath;
                var workingDirectory = aslainLocation.Path;
                var arguments = $"/LOADINF={_config.ConfigFileName}";

                _logger.Debug($"Exécutable: {installerPath}");
                _logger.Debug($"Répertoire: {workingDirectory}");
                _logger.Debug($"Arguments: {arguments}");

                var startInfo = new ProcessStartInfo
                {
                    FileName = installerPath,
                    Arguments = arguments,
                    WorkingDirectory = workingDirectory,
                    UseShellExecute = true
                };

                var process = Process.Start(startInfo);

                if (process == null)
                {
                    return OperationResult.Fail(
                        "Impossible de démarrer l'installateur",
                        "Le processus n'a pas pu être créé"
                    );
                }

                _logger.Info("   Suivez les instructions à l'écran pour terminer l'installation.");

                return OperationResult.Ok(
                    "Installateur lancé avec succès",
                    $"PID: {process.Id}"
                );
            }
            catch (Exception ex)
            {
                _logger.Error("Erreur lors du lancement de l'installateur", ex);
                return OperationResult.Fail(
                    "Impossible de lancer l'installateur",
                    ex.Message,
                    ex
                );
            }
        }

        private OperationResult CreateBackup(string configPath)
        {
            try
            {
                var timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
                var directory = Path.GetDirectoryName(configPath) ?? string.Empty;
                var backupName = $"{Path.GetFileNameWithoutExtension(configPath)}_backup_{timestamp}.inf";
                var backupPath = Path.Combine(directory, backupName);

                _logger.Debug($"Création de la sauvegarde: {backupPath}");
                File.Copy(configPath, backupPath, overwrite: false);

                _logger.Info($"💾 Sauvegarde créée: {backupName}");

                return OperationResult.Ok("Sauvegarde créée", backupPath);
            }
            catch (Exception ex)
            {
                _logger.Debug($"Erreur lors de la sauvegarde: {ex.Message}");
                return OperationResult.Fail("Impossible de créer la sauvegarde", ex.Message, ex);
            }
        }
    }
}
