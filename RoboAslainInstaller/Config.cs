using System;
using System.Collections.Generic;

namespace RoboAslainInstaller
{
    public class AppConfig
    {
        public string GitHubUser { get; set; } = "Robocnop";
        public string GitHubRepo { get; set; } = "config-Wot-Alsain-mod";
        public string ConfigFileName { get; set; } = "robo_configv3.inf";  // ← Changé pour v3
        public string InstallerName { get; set; } = "Aslains_WoT_Modpack_Installer.exe";
        public string WorldOfTanksFolderName { get; set; } = "World_of_Tanks_EU";
        public string AslainFolderName { get; set; } = "Aslain_Modpack";
        public bool CreateBackup { get; set; } = true;
        
       
        public string? AslainDownloadUrl { get; set; } = null;

        public List<string> CommonLocations { get; set; } = new List<string>
        {
            @"C:\Games\World_of_Tanks_EU\Aslain_Modpack",
            @"D:\Games\World_of_Tanks_EU\Aslain_Modpack",
            @"E:\Games\World_of_Tanks_EU\Aslain_Modpack"
        };

        public string GetRawUrl()
        {
            return $"https://raw.githubusercontent.com/{GitHubUser}/{GitHubRepo}/main/{ConfigFileName}";
        }
    }

    public class OperationResult
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public string? Details { get; set; }
        public Exception? Exception { get; set; }

        public static OperationResult Ok(string message, string? details = null)
        {
            return new OperationResult { Success = true, Message = message, Details = details };
        }

        public static OperationResult Fail(string message, string? details = null, Exception? exception = null)
        {
            return new OperationResult { Success = false, Message = message, Details = details, Exception = exception };
        }
    }

    public class OperationResult<T> where T : class
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public string? Details { get; set; }
        public T? Data { get; set; }
        public Exception? Exception { get; set; }

        public static OperationResult<T> Ok(string message, T data, string? details = null)
        {
            return new OperationResult<T> { Success = true, Message = message, Data = data, Details = details };
        }

        public static OperationResult<T> Fail(string message, string? details = null, Exception? exception = null)
        {
            return new OperationResult<T> { Success = false, Message = message, Details = details, Exception = exception };
        }
    }

    public class AslainLocation
    {
        public string Path { get; set; } = string.Empty;
        public string InstallerPath { get; set; } = string.Empty;
        public bool InstallerExists { get; set; }
        public DateTime? LastModified { get; set; }
        public long? InstallerSize { get; set; }
    }
}
