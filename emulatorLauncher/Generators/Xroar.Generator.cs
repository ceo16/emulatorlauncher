using System.Collections.Generic;
using System.IO;
using System.Diagnostics;
using EmulatorLauncher.Common;
using EmulatorLauncher.Common.FileFormats;
using EmulatorLauncher.Common.Joysticks;
using System.Linq;

namespace EmulatorLauncher
{
    partial class XroarGenerator : Generator
    {
        public XroarGenerator()
        {
            DependsOnDesktopResolution = true;
        }

        private BezelFiles _bezelFileInfo;
        private ScreenResolution _resolution;

        public override System.Diagnostics.ProcessStartInfo Generate(string system, string emulator, string core, string rom, string playersControllers, ScreenResolution resolution)
        {
            SimpleLogger.Instance.Info("[Generator] Getting " + emulator + " path and executable name.");

            string path = AppConfig.GetFullPath(emulator);
            if (!Directory.Exists(path))
                return null;

            string exe = Path.Combine(path, "xroar.exe");
            if (!File.Exists(exe))
                return null;

            // Configuration file
            string confFile = Path.Combine(path, "xroar.conf");
            if (!File.Exists(confFile))
            {
                string templateConfFile = Path.Combine(AppConfig.GetFullPath("lumaca"), "system", "templates", "xroar", "xroar.conf");
                if (File.Exists(templateConfFile))
                    try { File.Copy(templateConfFile, confFile); } catch { }
            }
            
            if (!File.Exists(confFile))
                File.WriteAllText(confFile, "");

            bool fullscreen = !IsEmulationStationWindowed() || SystemConfig.getOptBoolean("forcefullscreen");

            // Bezels
            if (fullscreen)
                _bezelFileInfo = BezelFiles.GetBezelFiles(system, rom, resolution, emulator);

            _resolution = resolution;

            string setupPath = Path.Combine(path, "xroar.conf");
            var commandArray = new List<string>();

            commandArray.Add("-c");
            commandArray.Add("xroar.conf");

            if (SystemConfig.isOptSet("xroar_machine") && !string.IsNullOrEmpty(SystemConfig["xroar_machine"]))
            {
                commandArray.Add("-m");
                commandArray.Add(SystemConfig["xroar_machine"]);
            }

            if (SystemConfig.isOptSet("xroar_cart") && !string.IsNullOrEmpty(SystemConfig["xroar_cart"]))
            {
                commandArray.Add("-cart");
                commandArray.Add(SystemConfig["xroar_cart"]);
            }

            commandArray.Add("-rompath");
            string romPath = Path.Combine(AppConfig.GetFullPath("bios"), "dragon");
            commandArray.Add("\"" + romPath + "\"");

            if (fullscreen)
                commandArray.Add("-fs");

            SetupConfiguration(setupPath, commandArray);

            // Check if additional command line arguments are set
            string commandFile = Path.Combine(Path.GetDirectoryName(rom), Path.GetFileNameWithoutExtension(rom) + ".commands");
            string defaultCommands = Path.Combine(path, "default.commands");
            if (File.Exists(commandFile))
            {
                string[] lines = File.ReadAllLines(commandFile);
                foreach (string line in lines)
                {
                    if (line.StartsWith("#"))
                        continue;

                    commandArray.Add(line);
                }
            }
            else if (File.Exists(defaultCommands))
            {
                string[] lines = File.ReadAllLines(defaultCommands);
                foreach (string line in lines)
                {
                    if (line.StartsWith("#"))
                        continue;

                    commandArray.Add(line);
                }
            }

            ConfigureControls(path, commandArray, setupPath);

            commandArray.Add("\"" + rom + "\"");

            string args = string.Join(" ", commandArray);

            return new ProcessStartInfo()
            {
                FileName = exe,
                WorkingDirectory = path,
                Arguments = args,
            };
        }

        //Manage config.xroar file settings
        private void SetupConfiguration(string setupPath, List<string> commandArray)
        {
            bool rompathExists = false;
            bool defaultmachine = false;
            string rompath = "rompath \".\\\\..\\\\..\\\\bios\\\\dragon\"";

            string defaultMachine = null;

            if (SystemConfig.isOptSet("xroar_machine") && !string.IsNullOrEmpty(SystemConfig["xroar_machine"]))
                defaultMachine = "default-machine " + SystemConfig["xroar_machine"];

            if (File.Exists(setupPath))
            {
                var lines = File.ReadAllLines(setupPath).ToList();

                for (int i = 0; i < lines.Count; i++)
                {
                    if (lines[i].StartsWith("rompath "))
                    {
                        lines[i] = rompath;
                        rompathExists = true;
                    }
                    if (lines[i].StartsWith("default-machine ") && defaultMachine != null)
                    {
                        lines[i] = defaultMachine;
                        defaultmachine = true;
                    }
                }

                if (!rompathExists)
                    lines.Add(rompath);
                if (!defaultmachine && defaultMachine != null)
                    lines.Add(defaultMachine);

                File.WriteAllLines(setupPath, lines);
            }
        }

        private void ConfigureControls(string path, List<string> commandArray, string setupFile)
        {
            if (Program.SystemConfig.isOptSet("disableautocontrollers") && Program.SystemConfig["disableautocontrollers"] == "1")
                return;

            if (!Controllers.Any(c => !c.IsKeyboard))
                return;

            bool switchjoy = SystemConfig.getOptBoolean("xroar_joyswitch");

            string controllerDBFile = Path.Combine(path, "gamecontrollerdb.txt");
            if (File.Exists(controllerDBFile))
            {
                commandArray.Add("-joy-db-file");
                commandArray.Add("\"" + controllerDBFile + "\"");
            }

            Controller c1 = null;
            Controller c2 = null;

            int controllersCount = Controllers.Count(c => !c.IsKeyboard);

            if (controllersCount > 1)
            {
                c1 = Controllers.FirstOrDefault(c => c.PlayerIndex == 1);
                c2 = Controllers.FirstOrDefault(c => c.PlayerIndex == 2);
            }
            else if (controllersCount == 1)
                c1 = Controllers.FirstOrDefault(c => !c.IsKeyboard);
            else
                return;

            int index1 = c1.DeviceIndex;

            if (switchjoy)
                commandArray.Add("-joy-left");
            else
                commandArray.Add("-joy-right");
            commandArray.Add("Lumaca1");

            int index2 = -1;

            if (c2 != null)
            {
                index2 = c2.DeviceIndex;
                if (switchjoy)
                    commandArray.Add("-joy-right");
                else
                    commandArray.Add("-joy-left");
                commandArray.Add("Lumaca2");
            }
            else
            {
                if (switchjoy)
                    commandArray.Add("-joy-right");
                else
                    commandArray.Add("-joy-left");
                commandArray.Add("kjoy0");
            }

            if (File.Exists(setupFile))
            {
                var lines = File.ReadAllLines(setupFile).ToList();
                bool kjoy0Exists = false;
                bool rb1Exists = false;
                bool rb2Exists = false;
                int rb1line = -1;
                int rb2line = -1;

                for (int i = 0; i < lines.Count; i++)
                {
                    if (lines[i].StartsWith("joy kjoy0"))
                        kjoy0Exists = true;
                    if (lines[i].StartsWith("joy Lumaca2"))
                    {
                        rb2Exists = true;
                        rb2line = i;
                    }
                    if (lines[i].StartsWith("joy Lumaca1"))
                    {
                        rb1Exists = true;
                        rb1line = i;
                    }
                }

                if (!kjoy0Exists)
                {
                    lines.Add("joy kjoy0");
                    lines.Add("  joy-desc \"Keyboard: Cursors+Alt_L,Super_L\"");
                    lines.Add("  joy-axis 0=\"keyboard:Left,Right\"");
                    lines.Add("  joy-axis 1=\"keyboard:Up,Down\"");
                    lines.Add("  joy-button 0=keyboard:Alt_L");
                    lines.Add("  joy-button 1=keyboard:Super_L");
                }

                if (rb1Exists)
                {
                    lines[rb1line] = "joy Lumaca1";
                    lines[rb1line + 1] = "  joy-desc \"Lumaca1\"";
                    lines[rb1line + 2] = "  joy-axis X=\"physical:" + index1 + ",0\"";
                    lines[rb1line + 3] = "  joy-axis Y=\"physical:" + index1 + ",1\"";
                    lines[rb1line + 4] = "  joy-button 0=\"physical:" + index1 + ",0\"";
                    lines[rb1line + 5] = "  joy-button 1=\"physical:" + index1 + ",1\"";
                }
                else
                {
                    lines.Add("joy Lumaca1");
                    lines.Add("  joy-desc \"Lumaca1\"");
                    lines.Add("  joy-axis X=\"physical:" + index1 + ",0\"");
                    lines.Add("  joy-axis Y=\"physical:" + index1 + ",1\"");
                    lines.Add("  joy-button 0=\"physical:" + index1 + ",0\"");
                    lines.Add("  joy-button 1=\"physical:" + index1 + ",1\"");
                }

                if (c2 != null)
                {
                    if (rb2Exists)
                    {
                        lines[rb2line] = "joy Lumaca2";
                        lines[rb2line + 1] = "  joy-desc \"Lumaca2\"";
                        lines[rb2line + 2] = "  joy-axis X=\"physical:" + index2 + ",0\"";
                        lines[rb2line + 3] = "  joy-axis Y=\"physical:" + index2 + ",1\"";
                        lines[rb2line + 4] = "  joy-button 0=\"physical:" + index2 + ",0\"";
                        lines[rb2line + 5] = "  joy-button 1=\"physical:" + index2 + ",1\"";
                    }
                    else
                    {
                        lines.Add("joy Lumaca2");
                        lines.Add("  joy-desc \"Lumaca2\"");
                        lines.Add("  joy-axis X=\"physical:" + index2 + ",0\"");
                        lines.Add("  joy-axis Y=\"physical:" + index2 + ",1\"");
                        lines.Add("  joy-button 0=\"physical:" + index2 + ",0\"");
                        lines.Add("  joy-button 1=\"physical:" + index2 + ",1\"");
                    }
                }

                File.WriteAllLines(setupFile, lines);
            }
        }

        public override int RunAndWait(ProcessStartInfo path)
        {
            FakeBezelFrm bezel = null;

            if (_bezelFileInfo != null)
                bezel = _bezelFileInfo.ShowFakeBezel(_resolution);

            int ret = base.RunAndWait(path);

            bezel?.Dispose();

            if (ret == 1)
            {
                return 0;
            }

            return ret;
        }
    }
}
