﻿using System.Collections.Generic;
using System.IO;
using System.Diagnostics;
using EmulatorLauncher.Common;

namespace EmulatorLauncher
{
    class Pico8Generator : Generator
    {
        public override System.Diagnostics.ProcessStartInfo Generate(string system, string emulator, string core, string rom, string playersControllers, ScreenResolution resolution)
        {
            string path = AppConfig.GetFullPath("pico8");

            string exe = Path.Combine(path, "pico8.exe");
            if (!File.Exists(exe))
                return null;

            bool fullscreen = !IsEmulationStationWindowed() || SystemConfig.getOptBoolean("forcefullscreen");

            List<string> commandArray = new List<string>
            {
                "-run"
            };

            if (fullscreen)
                commandArray.Add("-windowed 0");
            else
                commandArray.Add("-windowed 1");

            commandArray.Add("-home " + '\u0022' + path + '\u0022');
            commandArray.Add("-root_path " + '\u0022' + Path.GetDirectoryName(rom) + '\u0022');
            commandArray.Add("-desktop " + '\u0022' + AppConfig.GetFullPath("screenshots") + "\\pico8" + '\u0022');

            if (Path.GetExtension(rom).ToLower() == ".exe")
            {
                path = Path.GetDirectoryName(rom);
				
	            if (!File.Exists(rom))
                    return null;

                return new ProcessStartInfo()
                {
		            FileName = rom,
		            WorkingDirectory = path
                };

            }
			
            commandArray.Add('\u0022' + rom + '\u0022');

            string args = string.Join(" ", commandArray);
            return new ProcessStartInfo()
            {
                FileName = exe,
                WorkingDirectory = path,
                Arguments = args,
            };
        }
    }
}
