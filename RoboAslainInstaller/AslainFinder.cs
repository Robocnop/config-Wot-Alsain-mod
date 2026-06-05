using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace RoboAslainInstaller
{
    public class AslainFinder
    {
        private readonly AppConfig _config;
        private readonly Logger _logger;

        public AslainFinder(AppConfig config, Logger logger)
        {
            _config = config;
            _logger = logger;
        }

        public async Task<OperationResult<AslainLocation>> FindAslainFolderAsync()
        {
            _logger.Debug("Vérification des emplacements standards...");

            // Étape 1: Vérifier les emplacements courants
            var commonResult = await Task.Run(() => CheckCommonLocations());
            if (commonResult != null)
            {
                return OperationResult<AslainLocation>.Ok("Dossier trouvé", commonResult);
            }

            // Étape 2: Recherche approfondie
            _logger.Info("⏳ Recherche approfondie sur tous les disques...");
            var deepResult = await Task.Run(() => DeepScanAllDrives());
            if (deepResult != null)
            {
                return OperationResult<AslainLocation>.Ok("Dossier trouvé", deepResult);
            }

            // Étape 3: Demander le chemin manuellement
            _logger.Warning("❌ Dossier introuvable automatiquement.");
            var manualPath = PromptForManualPath();
            
            if (!string.IsNullOrEmpty(manualPath))
            {
                var validationResult = ValidateAslainFolder(manualPath);
                if (validationResult != null)
                {
                    return OperationResult<AslainLocation>.Ok("Chemin manuel validé", validationResult);
                }
            }

            return OperationResult<AslainLocation>.Fail(
                "Impossible de localiser le dossier Aslain Modpack",
                "Assurez-vous que World of Tanks EU et Aslain Modpack sont installés."
            );
        }

        private AslainLocation? CheckCommonLocations()
        {
            var locations = _config.CommonLocations.ToList();

            // Ajouter ProgramFiles
            var programFiles = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86);
            if (!string.IsNullOrEmpty(programFiles))
            {
                locations.Add(Path.Combine(programFiles, _config.WorldOfTanksFolderName, _config.AslainFolderName));
            }

            programFiles = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles);
            if (!string.IsNullOrEmpty(programFiles))
            {
                locations.Add(Path.Combine(programFiles, _config.WorldOfTanksFolderName, _config.AslainFolderName));
            }

            foreach (var location in locations)
            {
                _logger.Debug($"Vérification: {location}");
                var result = ValidateAslainFolder(location);
                if (result != null)
                {
                    _logger.Success($"✓ Trouvé: {location}");
                    return result;
                }
            }

            return null;
        }

        private AslainLocation? DeepScanAllDrives()
        {
            var drives = DriveInfo.GetDrives()
                .Where(d => d.IsReady && d.DriveType == DriveType.Fixed)
                .ToList();

            _logger.Debug($"Disques à scanner: {string.Join(", ", drives.Select(d => d.Name))}");

            foreach (var drive in drives)
            {
                _logger.Info($"   📂 Analyse du disque {drive.Name}...");
                try
                {
                    var result = ScanDrive(drive.Name);
                    if (result != null)
                        return result;
                }
                catch (Exception ex)
                {
                    _logger.Debug($"Erreur scan {drive.Name}: {ex.Message}");
                }
            }

            return null;
        }

        private AslainLocation? ScanDrive(string drivePath)
        {
            try
            {
                var directories = Directory.EnumerateDirectories(
                    drivePath,
                    _config.WorldOfTanksFolderName,
                    new EnumerationOptions
                    {
                        IgnoreInaccessible = true,
                        RecurseSubdirectories = true,
                        MaxRecursionDepth = 5
                    }
                );

                foreach (var wotDir in directories)
                {
                    var aslainPath = Path.Combine(wotDir, _config.AslainFolderName);
                    _logger.Debug($"Vérification: {aslainPath}");

                    var result = ValidateAslainFolder(aslainPath);
                    if (result != null)
                    {
                        _logger.Success($"✓ Trouvé: {aslainPath}");
                        return result;
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.Debug($"Erreur scan {drivePath}: {ex.Message}");
            }

            return null;
        }

        private AslainLocation? ValidateAslainFolder(string path)
        {
            if (!Directory.Exists(path))
                return null;

            var installerPath = Path.Combine(path, _config.InstallerName);
            if (!File.Exists(installerPath))
            {
                _logger.Debug($"Installateur absent: {installerPath}");
                return null;
            }

            var fileInfo = new FileInfo(installerPath);

            return new AslainLocation
            {
                Path = path,
                InstallerPath = installerPath,
                InstallerExists = true,
                LastModified = fileInfo.LastWriteTime,
                InstallerSize = fileInfo.Length
            };
        }

        private string? PromptForManualPath()
        {
            Console.WriteLine("\n📝 Vous pouvez entrer le chemin manuellement:");
            Console.WriteLine("Exemple: C:\\Games\\World_of_Tanks_EU\\Aslain_Modpack");
            Console.Write("Chemin (ou ENTER pour annuler): ");
            
            var path = Console.ReadLine();
            return string.IsNullOrWhiteSpace(path) ? null : path.Trim();
        }
    }
}
