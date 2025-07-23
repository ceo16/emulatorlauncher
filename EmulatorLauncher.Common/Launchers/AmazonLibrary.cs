using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Data.SQLite;
using EmulatorLauncher.Common.FileFormats;
using Microsoft.Win32;

namespace EmulatorLauncher.Common.Launchers
{
    public static class AmazonLibrary
    {
        static AmazonLibrary()
        {
            SQLiteInteropManager.InstallSQLiteInteropDll();
        }

        const string GameLaunchUrl = @"amazon-games://play/{0}";

        /// <summary>
        /// Metodo robusto per trovare il percorso completo di Amazon Games.exe
        /// leggendo il Registro di Sistema di Windows, esattamente come fa Playnite.
        /// </summary>
public static string GetAmazonClientExePath()
{
    SimpleLogger.Instance.Info("[AmazonLibrary] Ricerca di Amazon Games.exe nel Registro di Sistema (HKLM & HKCU).");
    string installPath = null;
    
    // Funzione helper per cercare in un ramo specifico del registro
    Func<RegistryKey, string> findInHive = (hive) =>
    {
        using (var key = hive.OpenSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall") ??
                         hive.OpenSubKey(@"SOFTWARE\WOW6432Node\Microsoft\Windows\CurrentVersion\Uninstall"))
        {
            if (key == null) return null;

            foreach (var subkeyName in key.GetSubKeyNames())
            {
                using (var subkey = key.OpenSubKey(subkeyName))
                {
                    var displayName = subkey.GetValue("DisplayName") as string;
                    var uninstallString = subkey.GetValue("UninstallString") as string;

                    if (!string.IsNullOrEmpty(displayName) && displayName.Contains("Amazon Games") &&
                        !string.IsNullOrEmpty(uninstallString) && uninstallString.Contains("Uninstall Amazon Games.exe"))
                    {
                        return subkey.GetValue("InstallLocation") as string;
                    }
                }
            }
        }
        return null;
    };

    try
    {
        // 1. Cerca in HKEY_LOCAL_MACHINE (per tutti gli utenti)
        SimpleLogger.Instance.Info("[AmazonLibrary] Controllo in HKEY_LOCAL_MACHINE...");
        using (var hklm = RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, RegistryView.Registry64))
        {
            installPath = findInHive(hklm);
        }

        // 2. Se non trovato, cerca in HKEY_CURRENT_USER (per utente singolo)
        if (string.IsNullOrEmpty(installPath))
        {
            SimpleLogger.Instance.Info("[AmazonLibrary] Non trovato in HKLM. Controllo in HKEY_CURRENT_USER...");
            using (var hkcu = RegistryKey.OpenBaseKey(RegistryHive.CurrentUser, RegistryView.Registry64))
            {
                installPath = findInHive(hkcu);
            }
        }
    }
    catch (Exception ex)
    {
        SimpleLogger.Instance.Error("[AmazonLibrary] Errore durante la lettura del registro: " + ex.Message);
        return null;
    }

    if (string.IsNullOrEmpty(installPath))
    {
        SimpleLogger.Instance.Error("[AmazonLibrary] Impossibile trovare la cartella di installazione di Amazon Games da nessuna posizione del registro.");
        return null;
    }

    SimpleLogger.Instance.Info($"[AmazonLibrary] Percorso di installazione trovato: '{installPath}'");
    string exePath = Path.Combine(installPath, "Amazon Games.exe");
    if (!File.Exists(exePath))
    {
        SimpleLogger.Instance.Error($"[AmazonLibrary] L'eseguibile non esiste nel percorso trovato: {exePath}");
        return null;
    }
    
    return exePath;
}

        public static LauncherGameInfo[] GetInstalledGames()
        {
            var games = new List<LauncherGameInfo>();

            if (!IsInstalled)
                return games.ToArray();

            using (var db = new SQLiteConnection("Data Source = " + GetDatabasePath()))
            {
                db.Open();

                var cmd = db.CreateCommand();
                cmd.CommandText = "SELECT * FROM DbSet WHERE Installed = 1;";

                var reader = cmd.ExecuteReader();

                var list = reader.ReadObjects<AmazonInstalledGameInfo>();
                if (list != null)
                {
                    foreach (var app in list)
                    {
                        if (!Directory.Exists(app.InstallDirectory))
                            continue;

                        var game = new LauncherGameInfo()
                        {
                            Id = app.Id,
                            Name = app.ProductTitle,
                            InstallDirectory = Path.GetFullPath(app.InstallDirectory),
                            LauncherUrl = string.Format(GameLaunchUrl, app.Id),    
                            ExecutableName = GetAmazonGameExecutable(app.InstallDirectory),
                            Launcher = GameLauncherType.Amazon
                        };

                        games.Add(game);
                    }
                }
                
                db.Close();
            }
            
            return games.ToArray();
        }

        public static bool IsInstalled
        {
            get
            {
                return File.Exists(GetDatabasePath());
            }
        }

        static string GetDatabasePath()
        {
            string appData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            string amazonDB = Path.Combine(appData, "Amazon Games", "Data", "Games", "Sql", "GameInstallInfo.sqlite");
            if (File.Exists(amazonDB))
                return amazonDB;

            return null;
        }

        public static string GetAmazonGameExecutableName(Uri uri)
        {
            if (!IsInstalled)
                return null;

            string gameId = uri.Segments.Last();

            using (var db = new SQLiteConnection("Data Source = " + GetDatabasePath()))
            {
                db.Open();

                var cmd = db.CreateCommand();
                cmd.CommandText = "SELECT installDirectory FROM DbSet WHERE Id = @gameId;";
                cmd.Parameters.Add(new SQLiteParameter("@gameId", gameId));

                var reader = cmd.ExecuteReader();

                if (!reader.HasRows)
                {
                    db.Close();
                    SimpleLogger.Instance.Warning($"[AmazonLibrary] Gioco con ID '{gameId}' non trovato nel database Amazon Games.");
                    return null; 
                }

                string gameInstallPath = null;
                while (reader.Read())
                    gameInstallPath = reader.GetString(0);

                db.Close();

                if (string.IsNullOrEmpty(gameInstallPath))
                {
                    SimpleLogger.Instance.Warning($"[AmazonLibrary] Percorso di installazione vuoto per il gioco con ID '{gameId}'.");
                    return null;
                }

                string executableFullPath = GetAmazonGameExecutable(gameInstallPath);
                if (string.IsNullOrEmpty(executableFullPath))
                {
                    SimpleLogger.Instance.Warning($"[AmazonLibrary] Eseguibile del gioco non trovato nella directory '{gameInstallPath}'.");
                    return null;
                }

                return Path.GetFileNameWithoutExtension(executableFullPath);
            }
        }

        private static string GetAmazonGameExecutable(string installDirectory)
        {
            string fuelFile = Path.Combine(installDirectory, "fuel.json");
            string gameExeRelativePath = null;

            if (!File.Exists(fuelFile))
            {
                SimpleLogger.Instance.Warning($"[AmazonLibrary] File 'fuel.json' non trovato in '{installDirectory}'.");
                string firstExe = Directory.EnumerateFiles(installDirectory, "*.exe", SearchOption.TopDirectoryOnly).FirstOrDefault();
                if (firstExe != null)
                {
                    return firstExe;
                }
                return null;
            }

            try
            {
                var json = DynamicJson.Load(fuelFile);
                var jsonMain = json.GetObject("Main");
                if (jsonMain == null)
                {
                    SimpleLogger.Instance.Warning($"[AmazonLibrary] Sezione 'Main' non trovata in 'fuel.json' in '{installDirectory}'.");
                    return null;
                }

                gameExeRelativePath = jsonMain["Command"];

                if (!string.IsNullOrEmpty(gameExeRelativePath))
                {
                    string fullPath = Path.Combine(installDirectory, gameExeRelativePath);
                    if (File.Exists(fullPath))
                    {
                        return fullPath;
                    }
                    else
                    {
                        SimpleLogger.Instance.Warning($"[AmazonLibrary] Eseguibile specificato in 'fuel.json' ('{gameExeRelativePath}') non trovato in '{installDirectory}'.");
                    }
                }
            }
            catch (Exception ex)
            {
                SimpleLogger.Instance.Error($"[AmazonLibrary] Errore durante la lettura di 'fuel.json' in '{installDirectory}': {ex.Message}", ex);
            }
            
            string fallbackExe = Directory.EnumerateFiles(installDirectory, "*.exe", SearchOption.TopDirectoryOnly).FirstOrDefault();
            if (fallbackExe != null)
            {
                return fallbackExe;
            }

            return null;
        }
    }

    public class AmazonInstalledGameInfo
    {
        public string Id { get; set; }
        public string InstallDirectory { get; set; }
        public int Installed { get; set; }
        public string ProductTitle { get; set; }
        public string ProductAsin { get; set; }
        public string ManifestSignature { get; set; }
        public string ManifestSignatureKeyId { get; set; }
    }
}