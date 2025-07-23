﻿using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Diagnostics;
using EmulatorLauncher.Common;
using System;
using EmulatorLauncher.Common.FileFormats;

namespace EmulatorLauncher
{
    partial class ShadPS4Generator : Generator
    {
        public override System.Diagnostics.ProcessStartInfo Generate(string system, string emulator, string core, string rom, string playersControllers, ScreenResolution resolution)
        {
            SimpleLogger.Instance.Info("[Generator] Getting " + emulator + " path and executable name.");

            string path = AppConfig.GetFullPath(emulator);
            if (!Directory.Exists(path))
                return null;

            string exe = Path.Combine(path, "shadPS4.exe");
            if (!File.Exists(exe))
                return null;

            string targetRom = "";

            if (Directory.Exists(rom))
            {
                rom = Directory.GetFiles(rom, "eboot.bin", SearchOption.AllDirectories).FirstOrDefault();

                if (!File.Exists(rom))
                    throw new ApplicationException("Unable to find any game in the provided folder");
            }

            else if (Path.GetExtension(rom).ToLower() == ".m3u")
            {
                string romPath = Path.GetDirectoryName(rom);
                string romSubPath = File.ReadAllText(rom);
                rom = Path.Combine(romPath, romSubPath);
            }
            
            else if (Path.GetExtension(rom).ToLower() == ".lnk")
            {
                targetRom = FileTools.GetShortcutArgswsh(rom);
            }

            bool fullscreen = !IsEmulationStationWindowed() || SystemConfig.getOptBoolean("forcefullscreen");

            //settings
            SetupConfiguration(path, rom, fullscreen, resolution);

            var commandArray = new List<string>();

            if (SystemConfig.getOptBoolean("shadps4_gui"))
                commandArray.Add("-s");

            if (Path.GetExtension(rom).ToLower() == ".lnk")
            {
                commandArray.Add(targetRom);
            }
            else
            {
                //commandArray.Add("-g");
                commandArray.Add("\"" + rom + "\"");
            }

            string args = string.Join(" ", commandArray);

            return new ProcessStartInfo()
            {
                FileName = exe,
                Arguments = args,
                WorkingDirectory = path,
            };
        }

        /// <summary>
        /// Configure emulator features (user/config.toml)
        /// </summary>
        /// <param name="path"></param>
        /// <param name="rom"></param>
        private void SetupConfiguration(string path, string rom, bool fullscreen, ScreenResolution resolution)
        {
            string userFolder = Path.Combine(path, "user");
            if (!Directory.Exists(userFolder))
                try { Directory.CreateDirectory(userFolder); } catch { }

            string settingsFile = Path.Combine(userFolder, "config.toml");
            string romPath = Path.GetDirectoryName(rom);
            if (Path.GetExtension(romPath).ToLower() == ".ps4")
                romPath = Directory.GetParent(romPath).FullName.Replace("\\", "\\\\");
            else if (Path.GetExtension(romPath).ToLower() == ".m3u")
                romPath = romPath.Replace("\\", "\\\\");

            using (IniFile toml = new IniFile(settingsFile, IniOptions.KeepEmptyLines | IniOptions.UseSpaces))
            {
                // General section
                BindBoolIniFeature(toml, "General", "isPS4Pro", "shadps4_isps4pro", "true", "false");
                BindBoolIniFeature(toml, "General", "enableDiscordRPC", "discord", "true", "false");
                BindBoolIniFeatureOn(toml, "Input", "isMotionControlsEnabled", "shadps4_motion", "true", "false");

                if (SystemConfig.isOptSet("shadps4_username") && !string.IsNullOrEmpty(SystemConfig["shadps4_username"]))
                    toml.WriteValue("General", "userName", "\"" + SystemConfig["shadps4_username"] + "\"");

               // toml.WriteValue("General", "autoUpdate", "false");
                toml.WriteValue("General", "showSplash", "false");

                // GPU section
                if (!fullscreen)
                {
                    toml.WriteValue("GPU", "screenHeight", resolution == null ? ScreenResolution.CurrentResolution.Height.ToString() : resolution.Height.ToString());
                    toml.WriteValue("GPU", "screenWidth", resolution == null ? ScreenResolution.CurrentResolution.Width.ToString() : resolution.Width.ToString());
                }
                BindBoolIniFeature(toml, "GPU", "allowHDR", "enable_hdr", "true", "false");

                if (fullscreen)
                    toml.WriteValue("GPU", "Fullscreen", "true");
                else
                    toml.WriteValue("GPU", "Fullscreen", "false");

                if (SystemConfig.getOptBoolean("exclusivefs"))
                    toml.WriteValue("GPU", "FullscreenMode", "\"Fullscreen\"");
                else
                    toml.WriteValue("GPU", "FullscreenMode", "\"Fullscreen (Borderless)\"");

                // Settings section
                string ps4Lang = Getps4LangFromEnvironment();
                if (SystemConfig.isOptSet("shadps4_lang") && !string.IsNullOrEmpty(SystemConfig["shadps4_lang"]))
                    ps4Lang = SystemConfig["shadps4_lang"];
                toml.WriteValue("Settings", "consoleLanguage", ps4Lang);

                // GUI section
                string currentDirs = toml.GetValue("GUI", "installDirs");
                
                if (currentDirs == null || currentDirs == "[]")
                    toml.WriteValue("GUI", "installDirs", "[\"" + romPath + "\"]");
                else
                {
                    currentDirs = currentDirs.Substring(1, currentDirs.Length - 2);
                    string[] dirs = currentDirs.Split(new char[] { ',' });
                    List<string> newDirs = dirs.Select(dir => dir.TrimStart()).ToList();
                    newDirs = newDirs.Where(s => !string.IsNullOrEmpty(s)).ToList();

                    if (newDirs.Count > 0 && !newDirs.Contains("\"" + romPath + "\""))
                        newDirs.Add("\"" + romPath + "\"");
                    string finalDirList = string.Join(", ", newDirs);
                    toml.WriteValue("GUI", "installDirs", "[" + finalDirList + "]");
                }

                string savePath = AppConfig.GetFullPath("saves");
                string shadSavePath = Path.Combine(savePath, "ps4", "shadps4");
                if (!Directory.Exists(shadSavePath))
                    try { Directory.CreateDirectory(shadSavePath); } catch { }
                toml.WriteValue("GUI", "saveDataPath", "\"" + shadSavePath.Replace("\\", "/") + "\"");

                string dlcPath = Path.Combine(savePath, "ps4", "DLC");
                if (!Directory.Exists(dlcPath))
                    try { Directory.CreateDirectory(dlcPath); } catch { }
                toml.WriteValue("GUI", "addonInstallDir", "\"" + dlcPath.Replace("\\", "\\\\") + "\"");
				
				toml.Save();
            }

            string uiSettingsFile = Path.Combine(userFolder, "qt_ui.ini");

            using (IniFile ini = new IniFile(uiSettingsFile))
            {
                ini.WriteValue("General", "autoUpdate", "false");
                ini.Save();
            }
        }

        private string Getps4LangFromEnvironment()
        {
            SimpleLogger.Instance.Info("[Generator] Getting Language from Lumaca language.");

            var availableLanguages = new Dictionary<string, int>()
            {
                { "ja", 0 },
                { "jp", 0 },
                { "en", 1 },
                { "fr", 2 },
                { "es", 3 },
                { "de", 4 },
                { "it", 5 },
                { "nl", 6 },
                { "pt", 7 },
                { "ru", 8 },
                { "ko", 9 },
                { "zh", 11 },
                { "fi", 12 },
                { "sv", 13 },
                { "nn", 15 },
                { "nb", 15 },
                { "pl", 16 },
                { "tr", 19 },
            };

            // Special case for some variances
            if (SystemConfig["Language"] == "zh_TW")
                return "10";
            else if (SystemConfig["Language"] == "pt_BR")
                return "17";
            else if (SystemConfig["Language"] == "en_GB")
                return "18";
            else if (SystemConfig["Language"] == "cs_CZ")
                return "23";
            else if (SystemConfig["Language"] == "ja_JP")
                return "0";

            var lang = GetCurrentLanguage();
            if (!string.IsNullOrEmpty(lang))
            {
                if (availableLanguages.TryGetValue(lang, out int ret))
                    return ret.ToString();
            }

            return 1.ToString();
        }
    }
}
