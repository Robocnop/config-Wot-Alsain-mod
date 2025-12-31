using System;
using System.IO;
using System.Text;

namespace RoboAslainInstaller
{
    public class Logger : IDisposable
    {
        private readonly string _logFilePath;
        private readonly StreamWriter? _logWriter;  // ← Ajout du ?
        private readonly bool _verboseMode;
        private static readonly object _lock = new object();

        public Logger(bool verboseMode = false)
        {
            _verboseMode = verboseMode;
            _logFilePath = Path.Combine(
                Path.GetTempPath(),
                $"RoboAslainInstaller_{DateTime.Now:yyyyMMdd_HHmmss}.log"
            );

            try
            {
                _logWriter = new StreamWriter(_logFilePath, false, Encoding.UTF8) { AutoFlush = true };
                WriteHeader();
            }
            catch
            {
                _logWriter = null;
            }
        }

        private void WriteHeader()
        {
            WriteLine("=".PadRight(80, '='));
            WriteLine($"Robo Aslain Config Installer - {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
            WriteLine($"OS: {Environment.OSVersion}");
            WriteLine($"User: {Environment.UserName}");
            WriteLine("=".PadRight(80, '='));
            WriteLine();
        }

        public void Info(string message)
        {
            Log("INFO", message, ConsoleColor.White);
        }

        public void Success(string message)
        {
            Log("SUCCESS", message, ConsoleColor.Green);
        }

        public void Warning(string message)
        {
            Log("WARNING", message, ConsoleColor.Yellow);
        }

        public void Error(string message, Exception? ex = null)  // ← Ajout du ?
        {
            Log("ERROR", message, ConsoleColor.Red);
            if (ex != null)
            {
                Log("ERROR", $"Exception: {ex.Message}", ConsoleColor.Red);
                if (_verboseMode)
                {
                    Log("ERROR", $"StackTrace: {ex.StackTrace}", ConsoleColor.Red);
                }
            }
        }

        public void Debug(string message)
        {
            if (_verboseMode)
            {
                Log("DEBUG", message, ConsoleColor.Gray);
            }
        }

        private void Log(string level, string message, ConsoleColor color)
        {
            lock (_lock)
            {
                var timestamp = DateTime.Now.ToString("HH:mm:ss");
                var logMessage = $"[{timestamp}] [{level.PadRight(7)}] {message}";

                // Écrire dans le fichier
                _logWriter?.WriteLine(logMessage);

                // Afficher dans la console
                if (level != "DEBUG" || _verboseMode)
                {
                    var originalColor = Console.ForegroundColor;
                    Console.ForegroundColor = color;
                    Console.WriteLine(message);
                    Console.ForegroundColor = originalColor;
                }
            }
        }

        private void WriteLine(string message = "")
        {
            _logWriter?.WriteLine(message);
        }

        public string GetLogFilePath() => _logFilePath;

        public void Dispose()
        {
            _logWriter?.Dispose();
        }
    }
}
