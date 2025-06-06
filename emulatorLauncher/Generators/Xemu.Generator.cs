﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Diagnostics;
using System.Drawing;
using EmulatorLauncher.PadToKeyboard;
using EmulatorLauncher.Common;
using EmulatorLauncher.Common.Joysticks;
using EmulatorLauncher.Common.EmulationStation;
using EmulatorLauncher.Common.FileFormats;

namespace EmulatorLauncher
{
    class XEmuGenerator : Generator
    {
        public XEmuGenerator()
        {
            DependsOnDesktopResolution = true;
        }

        private SdlVersion _sdlVersion = SdlVersion.SDL2_0_X;
        private ScreenResolution _resolution;
        private BezelFiles _bezelFileInfo;
        private Rectangle _windowRect = Rectangle.Empty;

        public override System.Diagnostics.ProcessStartInfo Generate(string system, string emulator, string core, string rom, string playersControllers, ScreenResolution resolution)
        {
            SimpleLogger.Instance.Info("[Generator] Getting " + emulator + " path and executable name.");

            string path = AppConfig.GetFullPath("xemu");
            if (string.IsNullOrEmpty(path))
                return null;

            string exe = Path.Combine(path, "xemu.exe");
            if (!File.Exists(exe))
                exe = Path.Combine(path, "xemuw.exe");
            
            if (!File.Exists(exe))
                return null;

            bool fullscreen = !IsEmulationStationWindowed() || SystemConfig.getOptBoolean("forcefullscreen");

            //Applying bezels
            if (!ReshadeManager.Setup(ReshadeBezelType.opengl, ReshadePlatform.x64, system, rom, path, resolution, emulator))
                _bezelFileInfo = BezelFiles.GetBezelFiles(system, rom, resolution, emulator);

            _resolution = resolution;

            // Extract SDL2 version info
            SimpleLogger.Instance.Info("[Generator] Getting SDL version from: " + exe);
            _sdlVersion = SdlJoystickGuidManager.GetSdlVersionFromStaticBinary(exe, SdlVersion.SDL2_0_X);

            try
            {
                // Define Paths
                string eepromPath = null;
                string hddPath = null;
                string bootRom = null;

                if (!string.IsNullOrEmpty(AppConfig["saves"]) && Directory.Exists(AppConfig.GetFullPath("saves")))
                {
                    string savePath = Path.Combine(AppConfig.GetFullPath("saves"), system);
                    if (!Directory.Exists(savePath)) try { Directory.CreateDirectory(savePath); }
                        catch { }

                    // Copy eeprom file from resources if file does not exist yet
                    if (!File.Exists(Path.Combine(savePath, "eeprom.bin")))
                    {
                        SimpleLogger.Instance.Info("[Generator] eeprom.bin not found, copying from template.");
                        File.WriteAllBytes(Path.Combine(savePath, "eeprom.bin"), Properties.Resources.eeprom);
                    }

                    // Unzip and Copy hdd image file from resources if file does not exist yet
                    if (!File.Exists(Path.Combine(savePath, "xbox_hdd.qcow2")))
                    {
                        SimpleLogger.Instance.Info("[Generator] xbox_hdd.qcow2 not found, copying from template.");
                        string zipFile = Path.Combine(savePath, "xbox_hdd.qcow2.zip");
                        File.WriteAllBytes(zipFile, Properties.Resources.xbox_hdd_qcow2);

                        string unzip = Path.Combine(Path.GetDirectoryName(typeof(XEmuGenerator).Assembly.Location), "unzip.exe");
                        if (File.Exists(unzip))
                        {
                            Process.Start(new ProcessStartInfo()
                            {
                                FileName = unzip,
                                Arguments = "-o \"" + zipFile + "\" -d \"" + savePath + "\"",
                                WorkingDirectory = savePath,
                                WindowStyle = ProcessWindowStyle.Hidden,
                                UseShellExecute = true
                            })
                            .WaitForExit();
                        }

                        File.Delete(zipFile);
                    }

                    if (File.Exists(Path.Combine(savePath, "eeprom.bin")))
                        eepromPath = Path.Combine(savePath, "eeprom.bin");

                    if (File.Exists(Path.Combine(savePath, "xbox_hdd.qcow2")))
                        hddPath = Path.Combine(savePath, "xbox_hdd.qcow2");
                }

                // BIOS file paths
                if (!string.IsNullOrEmpty(AppConfig["bios"]) && Directory.Exists(AppConfig.GetFullPath("bios")))
                {
                    if (File.Exists(Path.Combine(AppConfig.GetFullPath("bios"), "mcpx_1.0.bin")))
                        bootRom = Path.Combine(AppConfig.GetFullPath("bios"), "mcpx_1.0.bin");
                }

                // Settings
                SetupTOMLConfiguration(path, system, eepromPath, hddPath, bootRom);
            }
            catch { }

            // Command line arguments
            List<string> commandArray = new List<string>();

            if (IsEmulationStationWindowed(out Rectangle emulationStationBounds, true) && !SystemConfig.getOptBoolean("forcefullscreen"))
            {
                _windowRect = emulationStationBounds;
                _bezelFileInfo = null;
            }
            else if (fullscreen)
                commandArray.Add("-full-screen");

            commandArray.Add("-dvd_path");
            commandArray.Add("\"" + rom + "\"");

            string args = string.Join(" ", commandArray);

            // Disable bezel if is widescreen
            if (!SystemConfig.isOptSet("bezel") && SystemConfig["scale"] == "stretch")
            {
                SystemConfig["forceNoBezel"] = "1";
                ReshadeManager.Setup(ReshadeBezelType.opengl, ReshadePlatform.x64, system, rom, path, resolution, emulator);
            }

            // Launch emulator
            return new ProcessStartInfo()
            {
                FileName = exe,
                Arguments = args,
                WorkingDirectory = path,
            };
        }

        /// <summary>
        /// Configure emulator, write to .toml file.
        /// </summary>
        /// <param name="path"></param>
        /// <param name="eepromPath"></param>
        /// <param name="hddPath"></param>
        /// <param name="bootRom"></param>
        private void SetupTOMLConfiguration(string path, string system, string eepromPath, string hddPath, string bootRom)
        {
            using (IniFile ini = new IniFile(Path.Combine(path, "xemu.toml"), IniOptions.KeepEmptyLines | IniOptions.UseSpaces))
            {
                SimpleLogger.Instance.Info("[Generator] Writing settings to 'xemu.toml' file.");

                // Force settings
                ini.WriteValue("general", "show_welcome", "false");
                ini.WriteValue("general.updates", "check", "false");

                // Skip Boot anim
                BindBoolIniFeatureAuto(ini, "general", "skip_boot_anim", "show_boot", "true", "false", "true");
                BindBoolIniFeature(ini, "display.ui", "show_notifications", "xemu_notifications", "true", "false");

                // Controllers
                if (!SystemConfig.getOptBoolean("disableautocontrollers"))
                {
                    for (int i = 0; i < 16; i++)
                        ini.Remove("input.bindings", "port" + i);

                    int port = 1;
                    foreach (var ctl in Controllers)
                    {
                        if (ctl.Name == "Keyboard")
                            ini.WriteValue("input.bindings", "port" + port, "'keyboard'");
                        else if (ctl.Config != null)
                            ini.WriteValue("input.bindings", "port" + port, "'" + ctl.GetSdlGuid(_sdlVersion, true).ToLowerInvariant() + "'");

                        port++;
                    }
                }

                // Renderer
                if (SystemConfig.isOptSet("xemu_renderer") && !string.IsNullOrEmpty(SystemConfig["xemu_renderer"]))
                    ini.WriteValue("display", "renderer", "'" + SystemConfig["xemu_renderer"] + "'");
                else if (Features.IsSupported("xemu_renderer"))
                    ini.Remove("display", "renderer");

                // Resolution
                BindIniFeature(ini, "display.quality", "surface_scale", "render_scale", "1");

                // Aspect Ratio and scaling
                if (SystemConfig.isOptSet("xemu_scale") && !string.IsNullOrEmpty(SystemConfig["xemu_scale"]))
                    ini.WriteValue("display.ui", "fit", "'" + SystemConfig["xemu_scale"] + "'");                
                else if (Features.IsSupported("xemu_scale"))
                    ini.WriteValue("display.ui", "fit", "'scale'");

                if (SystemConfig.isOptSet("xemu_ratio") && !string.IsNullOrEmpty(SystemConfig["xemu_ratio"]))
                    ini.WriteValue("display.ui", "aspect_ratio", "'" + SystemConfig["xemu_ratio"] + "'");
                else
                    ini.Remove("display.ui", "aspect_ratio");

                // Menu Bar
                if (SystemConfig.isOptSet("menubar") && SystemConfig.getOptBoolean("menubar"))
                    ini.WriteValue("display.ui", "show_menubar", "true");
                else if (Features.IsSupported("menubar"))
                    ini.WriteValue("display.ui", "show_menubar", "false");


                // sys options
                if (SystemConfig.isOptSet("system_memory") && !string.IsNullOrEmpty(SystemConfig["system_memory"]))
                    ini.WriteValue("sys", "mem_limit", "'" + SystemConfig["system_memory"] + "'");
                else
                    ini.WriteValue("sys", "mem_limit", "'128'");

                if (SystemConfig.isOptSet("xemu_avpack") && !string.IsNullOrEmpty(SystemConfig["xemu_avpack"]))
                    ini.WriteValue("sys", "avpack", "'" + SystemConfig["xemu_avpack"] + "'");
                else
                    ini.WriteValue("sys", "avpack", "'HDTV'");

                // Vsync
                BindBoolIniFeatureOn(ini, "display.window", "vsync", "vsync", "true", "false");

                //¨Paths
                string screenshotPath = Path.Combine(AppConfig.GetFullPath("screenshots"), "xemu");
                if (Directory.Exists(screenshotPath))
                    ini.WriteValue("general", "screenshot_dir", "'" + screenshotPath + "'");

                if (!string.IsNullOrEmpty(eepromPath))
                    ini.WriteValue("sys.files", "eeprom_path", "'" + eepromPath + "'");

                if (!string.IsNullOrEmpty(hddPath))
                    ini.WriteValue("sys.files", "hdd_path", "'" + hddPath + "'");

                string flashromPath = Path.Combine(AppConfig.GetFullPath("bios"));
                if (SystemConfig.isOptSet("xemu_flashrom") && !string.IsNullOrEmpty(SystemConfig["xemu_flashrom"]))
                    ini.WriteValue("sys.files", "flashrom_path", "'" + Path.Combine(flashromPath, SystemConfig["xemu_flashrom"] + "'"));
                else
                    ini.WriteValue("sys.files", "flashrom_path", system == "chihiro" ? "'" + Path.Combine(flashromPath, "Cerbios.bin") + "'" : "'" + Path.Combine(flashromPath, "Complex_4627.bin") + "'");

                if (!string.IsNullOrEmpty(bootRom))
                    ini.WriteValue("sys.files", "bootrom_path", "'" + bootRom + "'");

                // dvd_path by command line is enough and in newer versions, if put in toml, it brakes the loading
                //ini.WriteValue("sys.files", "dvd_path", "'" + rom + "'");

                //audio
                BindBoolIniFeature(ini, "audio", "use_dsp", "xemu_dsp", "true", "false");
            }

            // Write xbox bios settings in eeprom.bin file
            WriteXboxEEPROM(eepromPath);
        }

        /// <summary>
        /// Get XBOX language to write to eeprom, value from features or default language of ES.
        /// </summary>
        private int GetXboxLangFromEnvironment()
        {
            SimpleLogger.Instance.Info("[Generator] Getting Language from Lumaca language.");

            var availableLanguages = new Dictionary<string, int>()
            {
                { "en", 1 },
                { "jp", 2 },
                { "ja", 2 },
                { "de", 3 },
                { "fr", 4 },
                { "es", 5 },
                { "it", 6 },
                { "ko", 7 },
                { "zh", 8 },
                { "pt", 9 }
            };

            var lang = GetCurrentLanguage();
            if (!string.IsNullOrEmpty(lang))
            {
                if (availableLanguages.TryGetValue(lang, out int ret))
                    return ret;
            }

            return 1;
        }

        /// <summary>
        /// Write data to XboX eeprom (language).
        /// </summary>
        /// <param name="path"></param>
        private void WriteXboxEEPROM(string path)
        {
            if (!File.Exists(path))
                return;

            SimpleLogger.Instance.Info("[Generator] Writing language in XBOX eeprom.");

            int langId;

            if (SystemConfig.isOptSet("xbox_language") && !string.IsNullOrEmpty(SystemConfig["xbox_language"]))
                langId = SystemConfig["xbox_language"].ToInteger();
            else
                langId = GetXboxLangFromEnvironment();

            // Read eeprom file
            byte[] bytes = File.ReadAllBytes(path);

            var toSet = new byte[] { (byte)langId };
            for (int i = 0; i < toSet.Length; i++)
                bytes[144] = toSet[i];

            uint UserSectionChecksum = ~ChecksumCalculate(bytes, 0x64, 0x5C);

            byte[] userchecksum = BitConverter.GetBytes(UserSectionChecksum);
            for (int i = 0; i < userchecksum.Length; i++)
                bytes[96 + i] = userchecksum[i];

            File.WriteAllBytes(path, bytes);
        }

        /// <summary>
        /// Calculates the EEPROM data checksum of specified offset and size.
        /// Original code by Ernegien (https://github.com/Ernegien/XboxEepromEditor)
        /// </summary>
        /// <param name="data"></param>
        /// <param name="offset"></param>
        /// <param name="size"></param>
        private static uint ChecksumCalculate(byte[] data, int offset, int size)
        {
            if (data == null)
                throw new ArgumentNullException("data");

            if (size % sizeof(uint) > 0)
                throw new ArgumentException("Size must be a multiple of four.", "size");

            if (offset + size > data.Length)
                throw new ArgumentOutOfRangeException();

            // high and low parts of the internal checksum
            uint high = 0, low = 0;

            for (int i = 0; i < size / sizeof(uint); i++)
            {
                uint val = BitConverter.ToUInt32(data, offset + i * sizeof(uint));
                ulong sum = ((ulong)high << 32) | low;

                high = (uint)((sum + val) >> 32);
                low += val;
            }

            return high + low;
        }

        /// <summary>
        /// Add KILL XEMU to padtokey (hotkey + START).
        /// </summary> 
        public override PadToKey SetupCustomPadToKeyMapping(PadToKey mapping)
        {
            return PadToKey.AddOrUpdateKeyMapping(mapping, "xemu", InputKey.hotkey | InputKey.start, "(%{CLOSE})");
        }

        public override int RunAndWait(ProcessStartInfo path)
        {
            FakeBezelFrm bezel = null;

            if (_bezelFileInfo != null)
                bezel = _bezelFileInfo.ShowFakeBezel(_resolution);

            int ret = 0;

            if (_windowRect.IsEmpty)
                ret = base.RunAndWait(path);
            else
            {
                var process = Process.Start(path);

                while (process != null)
                {
                    try
                    {
                        var hWnd = process.MainWindowHandle;
                        if (hWnd != IntPtr.Zero)
                        {
                            User32.SetWindowPos(hWnd, IntPtr.Zero, _windowRect.Left, _windowRect.Top, _windowRect.Width, _windowRect.Height, SWP.NOZORDER);
                            break;
                        }
                    }
                    catch { }

                    if (process.WaitForExit(1))
                    {
                        try { ret = process.ExitCode; }
                        catch { }
                        process = null;
                        break;
                    }

                }

                if (process != null)
                {
                    process.WaitForExit();
                    try { ret = process.ExitCode; }
                    catch { }
                }
            }

            bezel?.Dispose();

            ReshadeManager.UninstallReshader(ReshadeBezelType.opengl, path.WorkingDirectory);

            return ret;
        }
    }
}
