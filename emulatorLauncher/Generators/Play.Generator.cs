﻿using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Diagnostics;
using System.Xml.Linq;
using EmulatorLauncher.Common;

namespace EmulatorLauncher
{
    partial class PlayGenerator : Generator
    {
        public PlayGenerator()
        {
            DependsOnDesktopResolution = false;
        }

        private bool _isArcade = false;
        private BezelFiles _bezelFileInfo;
        private ScreenResolution _resolution;
        private string _romName;

        public override System.Diagnostics.ProcessStartInfo Generate(string system, string emulator, string core, string rom, string playersControllers, ScreenResolution resolution)
        {
            SimpleLogger.Instance.Info("[Generator] Getting " + emulator + " path and executable name.");

            string path = AppConfig.GetFullPath("play");
            if (!Directory.Exists(path))
                return null;

            string exe = Path.Combine(path, "Play.exe");
            if (!File.Exists(exe))
                return null;

            // Create portable file if it does not exist
            string portableFile = Path.Combine(path, "portable.txt");
            if (!File.Exists(portableFile))
                File.WriteAllText(portableFile, "");

            _isArcade = system == "namco2x6";
            bool fullscreen = !IsEmulationStationWindowed() || SystemConfig.getOptBoolean("forcefullscreen");
            _romName = Path.GetFileNameWithoutExtension(rom);

            //settings
            SetupConfiguration(path, rom);
            ConfigureControllers(path);

            //Applying bezels
            if (!fullscreen)
            {
                SystemConfig["forceNoBezel"] = "1"; // Force no bezel in windowed mode
            }
            
            if (!ReshadeManager.Setup(ReshadeBezelType.opengl, ReshadePlatform.x64, system, rom, path, resolution, emulator))
                _bezelFileInfo = BezelFiles.GetBezelFiles(system, rom, resolution, emulator);

            _resolution = resolution;

            var commandArray = new List<string>();

            if (_isArcade)
            {
                commandArray.Add("--arcade");
                commandArray.Add(Path.GetFileNameWithoutExtension(rom));
            }

            else
            {
                commandArray.Add("--disc");
                commandArray.Add("\"" + rom + "\"");
            }

            if (fullscreen)
             commandArray.Add("--fullscreen");

            string args = string.Join(" ", commandArray);

            return new ProcessStartInfo()
            {
                FileName = exe,
                Arguments = args,
                WorkingDirectory = path,
            };
        }

        /// <summary>
        /// Configure emulator features (config.xml)
        /// </summary>
        /// <param name="path"></param>
        /// <param name="rom"></param>
        private void SetupConfiguration(string path, string rom)
        {
            string settingsFile = Path.Combine(path, "Play Data Files", "config.xml");
            string romPath = Path.GetDirectoryName(rom).Replace("\\", "/");
            string templateConfigFile = Path.Combine(AppConfig.GetFullPath("templates"), "play", "config.xml");

            if (!File.Exists(settingsFile) && !File.Exists(templateConfigFile))
            {
                SimpleLogger.Instance.Info("ERROR: settings file does not exist");
                return;
            }

            else if (!File.Exists(settingsFile) && File.Exists(templateConfigFile))
                File.Copy(templateConfigFile, settingsFile);

            try
            {
                XDocument configfile = XDocument.Load(settingsFile);

                XElement exitConfirmation = configfile.Descendants("Preference").Where(x => (string)x.Attribute("Name") == "ui.showexitconfirmation").FirstOrDefault();
                exitConfirmation.SetAttributeValue("Value", "false");

                // Write path to arcade roms
                if (_isArcade)
                {
                    XElement arcadeRomPath = configfile.Descendants("Preference").Where(x => (string)x.Attribute("Name") == "ps2.arcaderoms.directory").FirstOrDefault();
                    arcadeRomPath.SetAttributeValue("Value", romPath);
                }

                // Language
                XElement language = configfile.Descendants("Preference").Where(x => (string)x.Attribute("Name") == "system.language").FirstOrDefault();

                if (SystemConfig.isOptSet("play_language") && !string.IsNullOrEmpty(SystemConfig["play_language"]))
                    language.SetAttributeValue("Value", SystemConfig["play_language"]);
                else
                    language.SetAttributeValue("Value", "1");

                // Limit framerate
                XElement limitframerate = configfile.Descendants("Preference").Where(x => (string)x.Attribute("Name") == "ps2.limitframerate").FirstOrDefault();

                if (SystemConfig.isOptSet("play_limitframerate") && !SystemConfig.getOptBoolean("play_limitframerate"))
                    limitframerate.SetAttributeValue("Value", "false");
                else
                    limitframerate.SetAttributeValue("Value", "true");

                // Resolution
                XElement resolution = configfile.Descendants("Preference").Where(x => (string)x.Attribute("Name") == "renderer.opengl.resfactor").FirstOrDefault();

                if (SystemConfig.isOptSet("play_resolution") && !string.IsNullOrEmpty(SystemConfig["play_resolution"]))
                    resolution.SetAttributeValue("Value", SystemConfig["play_resolution"]);
                else
                    resolution.SetAttributeValue("Value", "1");

                // Aspect ratio
                XElement aspect = configfile.Descendants("Preference").Where(x => (string)x.Attribute("Name") == "renderer.presentationmode").FirstOrDefault();

                if (SystemConfig.isOptSet("play_presentation") && !string.IsNullOrEmpty(SystemConfig["play_presentation"]))
                    aspect.SetAttributeValue("Value", SystemConfig["play_presentation"]);
                else
                    aspect.SetAttributeValue("Value", "1");

                // Bilinear filtering
                XElement forcebilineartextures = configfile.Descendants("Preference").Where(x => (string)x.Attribute("Name") == "renderer.opengl.forcebilineartextures").FirstOrDefault();

                if (SystemConfig.isOptSet("smooth") && SystemConfig.getOptBoolean("smooth"))
                    forcebilineartextures.SetAttributeValue("Value", "true");
                else
                    forcebilineartextures.SetAttributeValue("Value", "false");

                // Video driver
                XElement videodriver = configfile.Descendants("Preference").Where(x => (string)x.Attribute("Name") == "video.gshandler").FirstOrDefault();

                if (SystemConfig.isOptSet("play_renderer") && !string.IsNullOrEmpty(SystemConfig["play_renderer"]))
                    videodriver.SetAttributeValue("Value", SystemConfig["play_renderer"]);
                else
                    videodriver.SetAttributeValue("Value", "0");

                // Widescreen
                XElement widescreen = configfile.Descendants("Preference").Where(x => (string)x.Attribute("Name") == "renderer.widescreen").FirstOrDefault();

                if (SystemConfig.isOptSet("play_widescreen") && SystemConfig.getOptBoolean("play_widescreen"))
                    widescreen.SetAttributeValue("Value", "true");
                else
                    widescreen.SetAttributeValue("Value", "false");

                // Manage Input profiles
                XElement padProfile = configfile.Descendants("Preference").Where(x => (string)x.Attribute("Name") == "input.pad1.profile").FirstOrDefault();
                string inputProfilePath = Path.Combine(path, "Play Data Files", "inputprofiles");
                if (!Directory.Exists(inputProfilePath)) try { Directory.CreateDirectory(inputProfilePath); }
                    catch { }

                if (!SystemConfig.isOptSet("disableautocontrollers") || SystemConfig["disableautocontrollers"] != "1")
                {
                    padProfile.SetAttributeValue("Value", "Retrobat");
                }

                else
                {
                    string romName = Path.GetFileNameWithoutExtension(rom);
                    string inputProfile = Path.Combine(inputProfilePath, romName + ".xml");

                    if (File.Exists(inputProfile))
                        padProfile.SetAttributeValue("Value", romName);
                    else
                        padProfile.SetAttributeValue("Value", "default");
                }
                
                //save file
                configfile.Save(settingsFile);
            }

            catch { }
        }

        public override int RunAndWait(ProcessStartInfo path)
        {
            FakeBezelFrm bezel = null;

            if (_bezelFileInfo != null)
                bezel = _bezelFileInfo.ShowFakeBezel(_resolution);

            int ret = base.RunAndWait(path);

            if (bezel != null)
                bezel.Dispose();

            ReshadeManager.UninstallReshader(ReshadeBezelType.opengl, path.WorkingDirectory);

            return ret;
        }
    }
}
