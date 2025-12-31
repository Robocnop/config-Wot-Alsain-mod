using System;
using System.Linq;
using System.Threading.Tasks;

namespace RoboAslainInstaller
{
    class Program
    {
        static async Task<int> Main(string[] args)
        {
            Console.OutputEncoding = System.Text.Encoding.UTF8;
            Console.Title = "Robo Aslain Config Installer";

            ShowBanner();

            var config = new AppConfig();
            var verboseMode = args.Contains("--verbose") || args.Contains("-v");

            using (var logger = new Logger(verboseMode))
            {
                try
                {
                    // Vérifier les arguments de ligne de commande
                    if (args.Length > 0)
                    {
                        var command = args[0].ToLower();

                        // Commande de mise à jour Aslain
                        if (command == "--update-aslain" || command == "-u")
                        {
                            return await HandleAslainUpdate(args, config, logger);
                        }

                        // Afficher l'aide
                        if (command == "--help" || command == "-h" || command == "/?")
                        {
                            ShowHelp();
                            return 0;
                        }
                    }

                    // Mode normal : Installation de la config
                    return await InstallConfigAsync(config, logger);
                }
                catch (Exception ex)
                {
                    logger.Error("❌ Erreur inattendue", ex);
                    return ExitWithLog(logger, 99);
                }
            }
        }

        static async Task<int> HandleAslainUpdate(string[] args, AppConfig config, Logger logger)
        {
            logger.Info("Mode: Mise à jour Aslain\n");

            var updater = new AslainUpdater(config, logger);
            var finder = new AslainFinder(config, logger);

            // Essayer de trouver le dossier Aslain pour la mise à jour en place
            AslainLocation? aslainLocation = null;
            
            logger.Info("🔍 Recherche du dossier Aslain...");
            var findResult = await finder.FindAslainFolderAsync();
            
            if (findResult.Success && findResult.Data != null)
            {
                aslainLocation = findResult.Data;
                logger.Success($"✅ Dossier trouvé: {aslainLocation.Path}\n");
            }
            else
            {
                logger.Warning("⚠️  Dossier Aslain non trouvé, téléchargement uniquement.\n");
            }

            // URL personnalisée fournie en paramètre
            string? downloadUrl = null;
            if (args.Length > 1)
            {
                downloadUrl = args[1];
                logger.Info($"URL personnalisée: {downloadUrl}\n");
            }

            // Effectuer la mise à jour
            var updateResult = await updater.UpdateAslainAsync(aslainLocation, downloadUrl);

            if (!updateResult.Success)
            {
                logger.Error($"❌ {updateResult.Message}");
                if (!string.IsNullOrEmpty(updateResult.Details))
                {
                    logger.Info($"   {updateResult.Details}");
                }
                return ExitWithLog(logger, 5);
            }

            logger.Success($"✅ {updateResult.Message}\n");
            Console.WriteLine("\n🎉 Mise à jour terminée !");
            Console.WriteLine("L'installateur Aslain a été lancé.");
            Console.WriteLine("Suivez les instructions à l'écran pour compléter l'installation.");

            return ExitWithLog(logger, 0);
        }

        static async Task<int> InstallConfigAsync(AppConfig config, Logger logger)
        {
            logger.Info("Mode: Installation de configuration\n");

            var finder = new AslainFinder(config, logger);
            var downloader = new ConfigDownloader(config, logger);
            var installer = new ConfigInstaller(config, logger);

            // Étape 1: Trouver le dossier Aslain
            Console.WriteLine("🔍 Recherche du dossier Aslain...");
            var findResult = await finder.FindAslainFolderAsync();

            if (!findResult.Success || findResult.Data == null)
            {
                logger.Error($"❌ {findResult.Message}");
                return ExitWithLog(logger, 1);
            }

            var aslainLocation = findResult.Data;
            logger.Success($"✅ Dossier trouvé: {aslainLocation.Path}\n");

            // Étape 2: Télécharger la configuration
            Console.WriteLine("📥 Téléchargement de la configuration...");
            var downloadResult = await downloader.DownloadConfigAsync();

            if (!downloadResult.Success || downloadResult.Data == null)
            {
                logger.Error($"❌ {downloadResult.Message}");
                return ExitWithLog(logger, 2);
            }

            var configPath = downloadResult.Data;
            logger.Success("✅ Configuration téléchargée\n");

            // Étape 3: Installer la configuration
            Console.WriteLine("📋 Installation de la configuration...");
            var installResult = installer.InstallConfig(configPath, aslainLocation);

            if (!installResult.Success)
            {
                logger.Error($"❌ {installResult.Message}");
                return ExitWithLog(logger, 3);
            }

            logger.Success("✅ Configuration installée\n");

            // Nettoyage
            if (System.IO.File.Exists(configPath))
                System.IO.File.Delete(configPath);

            // Étape 4: Lancer l'installateur
            Console.WriteLine("🚀 Lancement de l'installateur Aslain...");
            var launchResult = installer.LaunchInstaller(aslainLocation);

            if (!launchResult.Success)
            {
                logger.Error($"❌ {launchResult.Message}");
                return ExitWithLog(logger, 4);
            }

            logger.Success("✅ Installateur lancé avec succès!\n");
            Console.WriteLine("\n🎉 Installation terminée !");
            Console.WriteLine("L'installateur Aslain a été lancé avec votre configuration.");

            return ExitWithLog(logger, 0);
        }

        static void ShowBanner()
        {
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine(@"
╔═══════════════════════════════════════════════════════════╗
║                                                           ║
║         ROBO ASLAIN CONFIG INSTALLER v2.0                 ║
║         Par Robocnop                                      ║
║                                                           ║
╚═══════════════════════════════════════════════════════════╝
");
            Console.ResetColor();
        }

        static void ShowHelp()
        {
            Console.WriteLine(@"
UTILISATION:
  RoboAslainInstaller.exe [options]

OPTIONS:
  (aucune)              Installation normale avec votre config GitHub
  
  --update-aslain, -u   Télécharger et installer la dernière version d'Aslain
                        Exemple: RoboAslainInstaller.exe --update-aslain
  
  --update-aslain <URL> Télécharger depuis une URL spécifique
                        Exemple: RoboAslainInstaller.exe --update-aslain https://...
  
  --verbose, -v         Mode verbose (logs détaillés)
  
  --help, -h, /?        Afficher cette aide

EXEMPLES:
  # Installation normale de votre config
  RoboAslainInstaller.exe
  
  # Mise à jour d'Aslain (ouvre le site de téléchargement)
  RoboAslainInstaller.exe --update-aslain
  
  # Mise à jour avec URL directe
  RoboAslainInstaller.exe --update-aslain https://example.com/aslain.exe
  
  # Mode verbose pour debugging
  RoboAslainInstaller.exe --verbose

WORKFLOW RECOMMANDÉ:
  1. Première installation: Téléchargez Aslain manuellement depuis https://aslain.com/
  2. Lancez RoboAslainInstaller.exe pour appliquer votre config
  3. Pour les mises à jour: RoboAslainInstaller.exe --update-aslain
");
        }

        static int ExitWithLog(Logger logger, int exitCode)
        {
            Console.WriteLine($"\n📄 Log disponible: {logger.GetLogFilePath()}");

            if (exitCode != 0)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("\nAppuyez sur une touche pour quitter...");
                Console.ResetColor();
            }
            else
            {
                Console.WriteLine("\nAppuyez sur une touche pour quitter...");
            }

            Console.ReadKey(true);
            return exitCode;
        }
    }
}
