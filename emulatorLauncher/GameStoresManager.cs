﻿﻿using EmulatorLauncher.Common;
using EmulatorLauncher.Common.Launchers;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace EmulatorLauncher
{
    class GameStoresManager
    {
        public static void UpdateGames()
        {
            Parallel.Invoke(
               () => ImportStore("amazon", AmazonLibrary.GetInstalledGames),
               () => ImportStore("eagames", EaGamesLibrary.GetInstalledGames),
               () => ImportStore("epic", EpicLibrary.GetInstalledGames),
               () => ImportStore("gog", GogLibrary.GetInstalledGames),
               () => ImportStore("steam", SteamLibrary.GetInstalledGames));
        }

        private static void ImportStore(string name, Func<LauncherGameInfo[]> getInstalledGames)
        {
            try
            {
                var roms = Program.AppConfig.GetFullPath("roms");

                var dir = Path.Combine(roms, name);
                Directory.CreateDirectory(dir);

                var files = new HashSet<string>(new[] { "*.url", "*.lnk" }.SelectMany(ext => Directory.GetFiles(dir, ext)));
           
                dynamic shell = null;

                foreach (var game in getInstalledGames())
                {
                    try
                    {
                        Uri uri = new Uri(game.LauncherUrl);

                        string gameName = RemoveInvalidFileNameChars(game.Name);

                        string path = Path.Combine(dir, gameName + ".url");
                        if (uri.Scheme == "file")
                            path = Path.Combine(dir, gameName + ".lnk");

                        if (files.Contains(path))
                        {
                            files.Remove(path);
                            continue;
                        }

                        if (uri.Scheme == "file")
                        {
                            try
                            {
                                if (shell == null)
                                    shell = Activator.CreateInstance(Type.GetTypeFromProgID("WScript.Shell"));

                                dynamic shortcut = shell.CreateShortcut(Path.Combine(dir, gameName + ".lnk"));
                                shortcut.TargetPath = game.LauncherUrl;
                                shortcut.Arguments = game.Parameters;
                                shortcut.WorkingDirectory = game.InstallDirectory;
                                shortcut.Save();

                                System.Runtime.InteropServices.Marshal.FinalReleaseComObject(shortcut);
                                continue;
                            }
                            catch { }
                        }

                        File.WriteAllText(path, "[InternetShortcut]\r\nURL=" + game.LauncherUrl);
                    }
                    catch (Exception ex) { SimpleLogger.Instance.Error("[ImportStore] " + name + " : " + ex.Message, ex); }
                }

                if (shell != null)
                    System.Runtime.InteropServices.Marshal.FinalReleaseComObject(shell);

                if (!Program.SystemConfig.getOptBoolean("storekeep"))
                {
                    foreach (var file in files)
                        FileTools.TryDeleteFile(file);
                }
            }

            catch (Exception ex) { SimpleLogger.Instance.Error("[ImportStore] " + name + " : " + ex.Message, ex); }
        }

        private static string RemoveInvalidFileNameChars(string x)
        {
            var invalidChars = Path.GetInvalidFileNameChars();
            return new string(x.Where(c => !invalidChars.Contains(c)).ToArray());
        }
    }
}