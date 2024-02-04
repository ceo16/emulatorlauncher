using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using Microsoft.Win32;
using Newtonsoft.Json;

namespace EmulatorLauncher.Common.Launchers.Epic
{
    public class EpicClient
    {
        private static readonly string AllUsersPath = Path.Combine(Environment.ExpandEnvironmentVariables("%PROGRAMDATA%"), "Epic");
        private static readonly string LauncherExePath = GetExecutablePath();
        private static readonly string MetadataPath = GetMetadataPath();

        public static bool IsInstalled => File.Exists(LauncherExePath);

        public static void Open()
        {
            Process.Start(LauncherExePath);
        }

        public static void Shutdown()
        {
            var mainProc = Process.GetProcessesByName("EpicGamesLauncher").FirstOrDefault();
            if (mainProc == null)
            {
                return;
            }

            var procRes = ProcessStarter.StartProcessWait(CmdLineTools.TaskKill, <span class="math-inline">"/f /pid \{mainProc\.Id\}", null, out var stdOut, out var stdErr\);
if \(procRes \!\= 0\)
\{
Console\.WriteLine\(</span>"Errore durante la chiusura del client Epic: {procRes}, {stdErr}");
            }
        }

        public static string GetIcon()
        {
            return Path.Combine(Path.GetDirectoryName(LauncherExePath), "Engine\\Binaries\\Win64\\UE4Editor-Cmd.exe");
        }

        public static LauncherGameInfo[] GetInstalledGames()
        {
            if (!IsInstalled)
            {
                return new LauncherGameInfo[0];
            }

            var appList = GetInstalledAppList();
            var manifests = GetInstalledManifests();

            if (appList == null || manifests == null)
            {
                return new LauncherGameInfo[0];
            }

            var games = new List<LauncherGameInfo>();

            foreach (var app in appList)
            {
                if (app.AppName.StartsWith("UE_"))
                {
                    continue;
                }

                var manifest = manifests.FirstOrDefault(a => a.AppName == app.AppName);
                if (manifest == null)
                {
                    continue;
                }

                // Skip DLCs
                if (manifest.AppName != manifest.MainGameAppName)
                {
                    continue;
                }

                // Skip Plugins
                if (manifest.AppCategories != null && manifest.AppCategories.Any(a => a == "plugins" || a == "plugins/engine"))
                {
                    continue;
                }

                var gameName = manifest.DisplayName ?? Path.GetFileName(app.InstallLocation);
                var installLocation = manifest.InstallLocation ?? app.InstallLocation;

                if (string.IsNullOrEmpty(installLocation))
                {
                    continue;
                }

                games.Add(new LauncherGameInfo
                {
                    Id = app.AppName,
                    Name = gameName,
                    LauncherUrl = string.Format(GameLaunchUrl, manifest.AppName),
                    InstallDirectory = Path.GetFullPath(installLocation),
                    ExecutableName = manifest.LaunchExecutable,
                    Launcher = GameLauncherType.Epic
                });
            }

            return games.ToArray();
        }

        public static string GetEpicGameExecutableName(Uri uri)
        {
            var shorturl = uri.LocalPath.ExtractString("/", ":");

            if (string.IsNullOrEmpty(MetadataPath))
            {
                throw new ApplicationException("Impossibile trovare la cartella dei metadati di Epic Games.");
            }

            if (!Directory.Exists(MetadataPath))
            {
                throw new ApplicationException("Impossibile trovare la cartella dei metadati di Epic Games.");
            }

            foreach (var manifest in GetInstalledManifests())
            {
                if (shorturl.Equals(manifest.CatalogNamespace))
                {
                    return manifest.LaunchExecutable;
                }
            }

            throw new ApplicationException("Il gioco non è installato.");
        }

        private static List<LauncherInstalled.InstalledApp> GetInstalledAppList()
        {
            var installListPath = Path.Combine(AllUsersPath, "UnrealEngineLauncher", "LauncherInstalled.dat");

            if (!File.Exists(installListPath))
            {
                return new List<LauncherInstalled.InstalledApp>();
            }

            var list = JsonConvert.DeserializeObject<
