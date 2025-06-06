﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Globalization;
using EmulatorLauncher.Common;
using EmulatorLauncher.Common.EmulationStation;
using EmulatorLauncher.Common.FileFormats;
using EmulatorLauncher.Common.Joysticks;
using EmulatorLauncher.Common.Lightguns;

namespace EmulatorLauncher
{
    class DolphinControllers
    {
        private static int _p1sdlindex = 0;
        /// <summary>
        /// Cf. https://github.com/dolphin-emu/dolphin/blob/master/Source/Core/InputCommon/ControllerInterface/SDL/SDL.cpp#L191
        /// </summary>
        private static void UpdateSdlControllersWithHints()
        {
            var hints = new List<string>
            {
                "SDL_HINT_JOYSTICK_HIDAPI_PS4_RUMBLE = 1"
            };

            SdlGameController.ReloadWithHints(string.Join(",", hints));
            Program.Controllers.ForEach(c => c.ResetSdlController());
        }

        public static bool WriteControllersConfig(string path, string system, string rom, bool triforce, out bool sindenSoft)
        {
            sindenSoft = false;

            if (Program.SystemConfig.isOptSet("disableautocontrollers") && Program.SystemConfig["disableautocontrollers"] == "1")
            {
                SimpleLogger.Instance.Info("[INFO] Auto controller configuration disabled.");
                return false;
            }

            SimpleLogger.Instance.Info("[INFO] Creating controller configuration for Dolphin");

            UpdateSdlControllersWithHints();

            if (system == "wii")
            {
                // Guns
                if (Program.SystemConfig.getOptBoolean("use_guns"))
                {
                    GenerateControllerConfig_wiilightgun(path, "WiimoteNew.ini", "Wiimote", sindenSoft);
                    return true;
                }

                // Emulated wiimotes
                else if (Program.SystemConfig.isOptSet("emulatedwiimotes") && Program.SystemConfig.getOptBoolean("emulatedwiimotes"))
                {
                    GenerateControllerConfig_emulatedwiimotes(path, rom);
                    RemoveControllerConfig_gamecube(path, "Dolphin.ini"); // because pads will already be used as emulated wiimotes
                    return true;
                }

                // use real wiimote as emulated
                else if (Program.SystemConfig.isOptSet("emulatedwiimotes") && Program.SystemConfig["emulatedwiimotes"] != "0" && Program.SystemConfig["emulatedwiimotes"] != "1")
                {
                    GenerateControllerConfig_realEmulatedwiimotes(path, "WiimoteNew.ini", "Wiimote");
                    return true;
                }

                // Real wiimotes (default)
                else
                {
                    GenerateControllerConfig_realwiimotes(path, "WiimoteNew.ini", "Wiimote");
                }

                // Additionnaly : configure gamecube pad (except if wii_gamecube is forced to OFF)
                if (Program.SystemConfig.isOptSet("wii_gamecube") && Program.SystemConfig["wii_gamecube"] == "0")
                    RemoveControllerConfig_gamecube(path, "Dolphin.ini");

                GenerateControllerConfig_gamecube(path,gamecubeMapping);
            }
            // Special mapping for triforce games to remove Z button from R1 (as this is used to access service menu and will be mapped to R3+L3)
            else if (triforce)
                GenerateControllerConfig_gamecube(path,triforceMapping, triforce);
            else
                GenerateControllerConfig_gamecube(path,gamecubeMapping);
            return true;
        }

        static readonly InputKeyMapping gamecubeMapping = new InputKeyMapping()
        {
            { InputKey.l3,              "Main Stick/Modifier"},
            { InputKey.r3,              "C-Stick/Modifier"},
            { InputKey.l2,              "Triggers/L-Analog" },
            { InputKey.r2,              "Triggers/R-Analog"},
            { InputKey.y,               "Buttons/Y" },
            { InputKey.b,               "Buttons/B" },
            { InputKey.x,               "Buttons/X" },
            { InputKey.a,               "Buttons/A" },
            { InputKey.start,           "Buttons/Start" },
            { InputKey.pagedown,        "Buttons/Z" },
            { InputKey.l2,              "Triggers/L" },
            { InputKey.r2,              "Triggers/R" },
            { InputKey.up,              "D-Pad/Up" },
            { InputKey.down,            "D-Pad/Down" },
            { InputKey.left,            "D-Pad/Left" },
            { InputKey.right,           "D-Pad/Right" },
            { InputKey.joystick1up,     "Main Stick/Up" },
            { InputKey.joystick1left,   "Main Stick/Left" },
            { InputKey.joystick2up,     "C-Stick/Up" },
            { InputKey.joystick2left,   "C-Stick/Left"},
            { InputKey.hotkey,          "Buttons/Hotkey" },
        };

        static readonly InputKeyMapping gbaMapping = new InputKeyMapping()
        {
            { InputKey.b,               "Buttons/B" },
            { InputKey.a,               "Buttons/A" },
            { InputKey.pageup,          "Buttons/L" },
            { InputKey.pagedown,        "Buttons/R" },
            { InputKey.select,          "Buttons/SELECT"},
            { InputKey.start,           "Buttons/START" },
            { InputKey.up,              "D-Pad/Up" },
            { InputKey.down,            "D-Pad/Down" },
            { InputKey.left,            "D-Pad/Left" },
            { InputKey.right,           "D-Pad/Right" },
            { InputKey.hotkey,          "Buttons/Hotkey" },
        };

        static readonly InputKeyMapping triforceMapping = new InputKeyMapping()
        {
            { InputKey.l2,              "Triggers/L-Analog" },
            { InputKey.r2,              "Triggers/R-Analog"},
            { InputKey.y,               "Buttons/Y" },
            { InputKey.b,               "Buttons/B" },
            { InputKey.select,          "Buttons/X" },
            { InputKey.a,               "Buttons/A" },
            { InputKey.start,           "Buttons/Start" },
            { InputKey.l2,              "Triggers/L" },
            { InputKey.r2,              "Triggers/R" },
            { InputKey.up,              "D-Pad/Up" },
            { InputKey.down,            "D-Pad/Down" },
            { InputKey.left,            "D-Pad/Left" },
            { InputKey.right,           "D-Pad/Right" },
            { InputKey.joystick1up,     "Main Stick/Up" },
            { InputKey.joystick1left,   "Main Stick/Left" },
            { InputKey.joystick2up,     "C-Stick/Up" },
            { InputKey.joystick2left,   "C-Stick/Left"},
            { InputKey.hotkey,          "Buttons/Hotkey" },
        };

        static readonly InputKeyMapping vs4mapping = new InputKeyMapping()
        {
            { InputKey.joystick1left,  "Main Stick/Down" },
            { InputKey.joystick1up,    "Main Stick/Left" },
        };

        static readonly InputKeyMapping reversedButtons = new InputKeyMapping()
        {
            { InputKey.b,               "Buttons/A" },
            { InputKey.a,               "Buttons/B" },
            { InputKey.x,               "Buttons/Y" },
            { InputKey.y,               "Buttons/X" }
        };

        static readonly InputKeyMapping reversedButtonsXY = new InputKeyMapping()
        {
            { InputKey.x,               "Buttons/Y" },
            { InputKey.y,               "Buttons/X" }
        };

        static readonly InputKeyMapping reversedButtonsRotate = new InputKeyMapping()
        {
            { InputKey.b,               "Buttons/A" },
            { InputKey.y,               "Buttons/B" },
            { InputKey.x,               "Buttons/Y" },
            { InputKey.a,               "Buttons/X" }
        };

        static readonly Dictionary<string, string> gamecubeReverseAxes = new Dictionary<string, string>()
        {
            { "Main Stick/Up",   "Main Stick/Down" },
            { "Main Stick/Left", "Main Stick/Right" },
            { "C-Stick/Up",      "C-Stick/Down" },
            { "C-Stick/Left",    "C-Stick/Right" }
        };

        static readonly Dictionary<string, string> vs4ReverseAxes = new Dictionary<string, string>()
        {
            { "Main Stick/Down",   "Main Stick/Up" },
            { "Main Stick/Left",   "Main Stick/Right" },
            { "C-Stick/Up",        "C-Stick/Down" },
            { "C-Stick/Left",      "C-Stick/Right" }
        };

        static readonly InputKeyMapping _wiiMapping = new InputKeyMapping
        {
            { InputKey.x,               "Buttons/2" },
            { InputKey.b,               "Buttons/A" },
            { InputKey.y,               "Buttons/1" },
            { InputKey.a,               "Buttons/B" },
            { InputKey.pageup,          "Buttons/-" },
            { InputKey.pagedown,        "Buttons/+" },
            { InputKey.select,          "Buttons/Home" },
            { InputKey.up,              "D-Pad/Up" },
            { InputKey.down,            "D-Pad/Down" },
            { InputKey.left,            "D-Pad/Left" },
            { InputKey.right,           "D-Pad/Right" },
            { InputKey.joystick1up,     "IR/Up" },
            { InputKey.joystick1left,   "IR/Left" },
            { InputKey.joystick2up,     "Tilt/Forward" },
            { InputKey.joystick2left,   "Tilt/Left" },
            { InputKey.l3,              "IR/Relative Input Hold" },
            { InputKey.r3,              "Tilt/Modifier" }
        };

        static readonly Dictionary<string, string> wiiReverseAxes = new Dictionary<string, string>()
        {
            { "IR/Up",      "IR/Down"},
            { "IR/Left",    "IR/Right"},
            { "Swing/Up",   "Swing/Down"},
            { "Swing/Left", "Swing/Right"},
            { "Tilt/Left",  "Tilt/Right"},
            { "Tilt/Forward", "Tilt/Backward"},
            { "Nunchuk/Stick/Up" ,  "Nunchuk/Stick/Down"},
            { "Nunchuk/Stick/Left", "Nunchuk/Stick/Right"},
            { "Classic/Right Stick/Up" , "Classic/Right Stick/Down"},
            { "Classic/Right Stick/Left" , "Classic/Right Stick/Right"},
            { "Classic/Left Stick/Up" , "Classic/Left Stick/Down"},
            { "Classic/Left Stick/Left" , "Classic/Left Stick/Right" }
        };

        private static void GenerateControllerConfig_emulatedwiimotes(string path, string rom)
        {
            var extraOptions = new Dictionary<string, string>
            {
                ["Source"] = "1"
            };

            string romName = Path.GetFileName(rom);

            var wiiMapping = new InputKeyMapping(_wiiMapping);

            if (Program.SystemConfig["controller_mode"] != "cc" && !romName.Contains(".cc."))
            {
                if (Program.SystemConfig["controller_mode"] == "side" || romName.Contains(".side."))
                {
                    extraOptions["Options/Sideways Wiimote"] = "1";
                    wiiMapping[InputKey.x] = "Buttons/A";
                    wiiMapping[InputKey.y] = "Buttons/1";
                    wiiMapping[InputKey.b] = "Buttons/2";
                    wiiMapping[InputKey.a] = "Buttons/B";
                    wiiMapping[InputKey.l2] = "Shake/X";
                    wiiMapping[InputKey.l2] = "Shake/Y";
                    wiiMapping[InputKey.l2] = "Shake/Z";
                    wiiMapping[InputKey.select] = "Buttons/-";
                    wiiMapping[InputKey.start] = "Buttons/+";
                    wiiMapping[InputKey.pageup] = "Tilt/Left";
                    wiiMapping[InputKey.pagedown] = "Tilt/Right";
                }

                // i: infrared, s: swing, t: tilt, n: nunchuk
                // 12 possible combinations : is si / it ti / in ni / st ts / sn ns / tn nt

                // i
                string[] infraredFirstTags = { ".is.", ".it.", ".in." };
                if (Program.SystemConfig["controller_mode"] == "is" || Program.SystemConfig["controller_mode"] == "it" || Program.SystemConfig["controller_mode"] == "in" || infraredFirstTags.Any(r => romName.Contains(r)))
                {
                    wiiMapping[InputKey.joystick1up] = "IR/Up";
                    wiiMapping[InputKey.joystick1left] = "IR/Left";
                    wiiMapping[InputKey.l3] = "IR/Relative Input Hold";
                }

                string[] infraredLastTags = { ".si.", ".ti.", ".ni." };
                if (Program.SystemConfig["controller_mode"] == "si" || Program.SystemConfig["controller_mode"] == "ti" || Program.SystemConfig["controller_mode"] == "ni" || infraredLastTags.Any(r => romName.Contains(r)))
                {
                    wiiMapping[InputKey.joystick2up] = "IR/Up";
                    wiiMapping[InputKey.joystick2left] = "IR/Left";
                    wiiMapping[InputKey.r3] = "IR/Relative Input Hold";
                }

                // s
                string[] swingFirstTags = { ".si.", ".st.", ".sn." };
                if (Program.SystemConfig["controller_mode"] == "si" || Program.SystemConfig["controller_mode"] == "st" || Program.SystemConfig["controller_mode"] == "sn" || swingFirstTags.Any(r => romName.Contains(r)))
                {
                    wiiMapping[InputKey.joystick1up] = "Swing/Up";
                    wiiMapping[InputKey.joystick1left] = "Swing/Left";
                }

                string[] swingLastTags = { ".is.", ".ts.", ".ns." };
                if (Program.SystemConfig["controller_mode"] == "is" || Program.SystemConfig["controller_mode"] == "ts" || Program.SystemConfig["controller_mode"] == "ns" || swingLastTags.Any(r => romName.Contains(r)))
                {
                    wiiMapping[InputKey.joystick2up] = "Swing/Up";
                    wiiMapping[InputKey.joystick2left] = "Swing/Left";
                }

                // t
                string[] tiltFirstTags = { ".ti.", ".ts.", ".tn." };
                if (Program.SystemConfig["controller_mode"] == "ti" || Program.SystemConfig["controller_mode"] == "ts" || Program.SystemConfig["controller_mode"] == "tn" || tiltFirstTags.Any(r => romName.Contains(r)))
                {
                    wiiMapping[InputKey.joystick1up] = "Tilt/Forward";
                    wiiMapping[InputKey.joystick1left] = "Tilt/Left";
                    wiiMapping[InputKey.l3] = "Tilt/Modifier";
                }

                string[] tiltLastTags = { ".it.", ".st.", ".nt." };
                if (Program.SystemConfig["controller_mode"] == "it" || Program.SystemConfig["controller_mode"] == "st" || Program.SystemConfig["controller_mode"] == "nt" || tiltLastTags.Any(r => romName.Contains(r)))
                {
                    wiiMapping[InputKey.joystick2up] = "Tilt/Forward";
                    wiiMapping[InputKey.joystick2left] = "Tilt/Left";
                    wiiMapping[InputKey.r3] = "Tilt/Modifier";
                }

                // n
                string[] nunchukFirstTags = { ".ni.", ".ns.", ".nt." };
                if (Program.SystemConfig["controller_mode"] == "ni" || Program.SystemConfig["controller_mode"] == "ns" || Program.SystemConfig["controller_mode"] == "nt" || nunchukFirstTags.Any(r => romName.Contains(r)))
                {
                    extraOptions["Extension"] = "Nunchuk";
                    wiiMapping[InputKey.l1] = "Nunchuk/Buttons/C";
                    wiiMapping[InputKey.r1] = "Nunchuk/Buttons/Z";
                    wiiMapping[InputKey.joystick1up] = "Nunchuk/Stick/Up";
                    wiiMapping[InputKey.joystick1left] = "Nunchuk/Stick/Left";
                    wiiMapping[InputKey.l3] = "Nunchuk/Stick/Modifier";
                    wiiMapping[InputKey.select] = "Buttons/-";
                    wiiMapping[InputKey.start] = "Buttons/+";
                    wiiMapping[InputKey.l2] = "Shake/X";
                    wiiMapping[InputKey.l2] = "Shake/Y";
                    wiiMapping[InputKey.l2] = "Shake/Z";
                }

                string[] nunchukLastTags = { ".in.", ".sn.", ".tn." };
                if (Program.SystemConfig["controller_mode"] == "in" || Program.SystemConfig["controller_mode"] == "sn" || Program.SystemConfig["controller_mode"] == "tn" || nunchukLastTags.Any(r => romName.Contains(r)))
                {
                    extraOptions["Extension"] = "Nunchuk";
                    wiiMapping[InputKey.l1] = "Nunchuk/Buttons/C";
                    wiiMapping[InputKey.r1] = "Nunchuk/Buttons/Z";
                    wiiMapping[InputKey.joystick2up] = "Nunchuk/Stick/Up";
                    wiiMapping[InputKey.joystick2left] = "Nunchuk/Stick/Left";
                    wiiMapping[InputKey.r3] = "Nunchuk/Stick/Modifier";
                    wiiMapping[InputKey.select] = "Buttons/-";
                    wiiMapping[InputKey.start] = "Buttons/+";
                    wiiMapping[InputKey.l2] = "Shake/X";
                    wiiMapping[InputKey.l2] = "Shake/Y";
                    wiiMapping[InputKey.l2] = "Shake/Z";
                }
            }

            // cc : Classic Controller Settings
            else if (Program.SystemConfig["controller_mode"] == "cc" || Program.SystemConfig["controller_mode"] == "ccp" || romName.Contains(".cc.") || romName.Contains(".ccp."))
            {
                bool revertall = Program.Features.IsSupported("wii_cc_buttons") && Program.SystemConfig.isOptSet("wii_cc_buttons") && Program.SystemConfig["wii_cc_buttons"] == "xbox";

                extraOptions["Extension"] = "Classic";

                if (revertall)
                {
                    wiiMapping[InputKey.y] = "Classic/Buttons/X";
                    wiiMapping[InputKey.x] = "Classic/Buttons/Y";
                    wiiMapping[InputKey.a] = "Classic/Buttons/B";
                    wiiMapping[InputKey.b] = "Classic/Buttons/A";
                }
                else
                {
                    wiiMapping[InputKey.x] = "Classic/Buttons/X";
                    wiiMapping[InputKey.y] = "Classic/Buttons/Y";
                    wiiMapping[InputKey.b] = "Classic/Buttons/B";
                    wiiMapping[InputKey.a] = "Classic/Buttons/A";
                }
                wiiMapping[InputKey.select] = "Classic/Buttons/-";
                wiiMapping[InputKey.start] = "Classic/Buttons/+";

                wiiMapping[InputKey.pageup] = "Classic/Buttons/ZL";
                wiiMapping[InputKey.pagedown] = "Classic/Buttons/ZR";
                
                wiiMapping[InputKey.l2] = "Classic/Triggers/L-Analog";
                wiiMapping[InputKey.r2] = "Classic/Triggers/R-Analog";
                wiiMapping.Add(InputKey.l2, "Classic/Triggers/L");
                wiiMapping.Add(InputKey.r2, "Classic/Triggers/R");

                wiiMapping[InputKey.up] = "Classic/D-Pad/Up";
                wiiMapping[InputKey.down] = "Classic/D-Pad/Down";
                wiiMapping[InputKey.left] = "Classic/D-Pad/Left";
                wiiMapping[InputKey.right] = "Classic/D-Pad/Right";
                wiiMapping[InputKey.joystick1up] = "Classic/Left Stick/Up";
                wiiMapping[InputKey.joystick1left] = "Classic/Left Stick/Left";
                wiiMapping[InputKey.joystick2up] = "Classic/Right Stick/Up";
                wiiMapping[InputKey.joystick2left] = "Classic/Right Stick/Left";
                wiiMapping[InputKey.l3] = "Classic/Left Stick/Modifier";
                wiiMapping[InputKey.r3] = "Classic/Right Stick/Modifier";
            }

            GenerateControllerConfig_any(path, "WiimoteNew.ini", "Wiimote", wiiMapping, wiiReverseAxes, false, extraOptions);
        }

        private static void GenerateControllerConfig_gamecube(string path, InputKeyMapping anyMapping, bool triforce = false)
        {
            bool vs4axis = triforce && Program.SystemConfig.isOptSet("triforce_mapping") && Program.SystemConfig["triforce_mapping"] == "vs4";

            GenerateControllerConfig_any(path, "GCPadNew.ini", "GCPad", anyMapping, vs4axis ? vs4ReverseAxes : gamecubeReverseAxes, triforce);

            bool gbaConf = false;
            for (int i = 1; i < 5; i++)
            {
                int gbaPadIndex = i - 1;
                string gbaPad = "gamecubepad" + i.ToString();

                if (Program.SystemConfig[gbaPad] == "13")
                {
                    gbaConf = true;
                    continue;
                }
            }

            if (gbaConf)
                GenerateControllerConfig_any(path, "GBA.ini", "GBA", gbaMapping, vs4axis ? vs4ReverseAxes : gamecubeReverseAxes, triforce);
        }

        static readonly Dictionary<XINPUTMAPPING, string> xInputMapping = new Dictionary<XINPUTMAPPING, string>()
        {
            { XINPUTMAPPING.X,                  "`Button Y`" },
            { XINPUTMAPPING.B,                  "`Button A`" },
            { XINPUTMAPPING.Y,                  "`Button X`" },
            { XINPUTMAPPING.A,                  "`Button B`" },
            { XINPUTMAPPING.BACK,               "Back" },
            { XINPUTMAPPING.START,              "Start" },
            { XINPUTMAPPING.LEFTSHOULDER,       "`Shoulder L`" },
            { XINPUTMAPPING.RIGHTSHOULDER,      "`Shoulder R`" },
            { XINPUTMAPPING.DPAD_UP,            "`Pad N`" },
            { XINPUTMAPPING.DPAD_DOWN,          "`Pad S`" },
            { XINPUTMAPPING.DPAD_LEFT,          "`Pad W`" },
            { XINPUTMAPPING.DPAD_RIGHT,         "`Pad E`" },
            { XINPUTMAPPING.LEFTANALOG_UP,      "`Left Y+`" },
            { XINPUTMAPPING.LEFTANALOG_DOWN,    "`Left Y-`" },
            { XINPUTMAPPING.LEFTANALOG_LEFT,    "`Left X-`" },
            { XINPUTMAPPING.LEFTANALOG_RIGHT,   "`Left X+`"},
            { XINPUTMAPPING.RIGHTANALOG_UP,     "`Right Y+`" },
            { XINPUTMAPPING.RIGHTANALOG_DOWN,   "`Right Y-`" },
            { XINPUTMAPPING.RIGHTANALOG_LEFT,   "`Right X-`" },
            { XINPUTMAPPING.RIGHTANALOG_RIGHT,  "`Right X+`"},
            { XINPUTMAPPING.LEFTSTICK,          "`Thumb L`" },
            { XINPUTMAPPING.RIGHTSTICK,         "`Thumb R`" },
            { XINPUTMAPPING.LEFTTRIGGER,        "`Trigger L`" },
            { XINPUTMAPPING.RIGHTTRIGGER,       "`Trigger R`" }
        };

        private static void GenerateControllerConfig_realwiimotes(string path, string filename, string anyDefKey)
        {
            string iniFile = Path.Combine(path, "User", "Config", filename);

            using (IniFile ini = new IniFile(iniFile, IniOptions.UseSpaces))
            {
                for (int i = 1; i < 5; i++)
                {
                    ini.ClearSection(anyDefKey + i.ToString());
                    ini.WriteValue(anyDefKey + i.ToString(), "Source", "2");
                }

                // Balance board
                if (Program.SystemConfig.isOptSet("wii_balanceboard") && Program.SystemConfig.getOptBoolean("wii_balanceboard"))
                {
                    ini.WriteValue("BalanceBoard", "Source", "2");
                }
                else
                    ini.WriteValue("BalanceBoard", "Source", "0");

                ini.Save();
            }
        }

        private static void GenerateControllerConfig_realEmulatedwiimotes(string path, string filename, string anyDefKey)
        {
            string iniFile = Path.Combine(path, "User", "Config", filename);

            using (IniFile ini = new IniFile(iniFile, IniOptions.UseSpaces))
            {
                for (int i = 1; i < 5; i++)
                {
                    string btDevice = (i - 1).ToString();
                    ini.ClearSection(anyDefKey + i.ToString());
                    ini.WriteValue(anyDefKey + i.ToString(), "Source", "1");
                    ini.WriteValue(anyDefKey + i.ToString(), "Device", "Bluetooth/" + btDevice + "/Wii Remote");

                    foreach (KeyValuePair<string, string> x in realEmulatedWiimote)
                        ini.WriteValue(anyDefKey + i.ToString(), x.Key, x.Value);

                    if (Program.SystemConfig["emulatedwiimotes"] == "3")
                        ini.WriteValue(anyDefKey + i.ToString(), "Extension", "Nunchuk");
                    else if (Program.SystemConfig["emulatedwiimotes"] == "4")
                        ini.WriteValue(anyDefKey + i.ToString(), "Extension", "Classic");
                }

                // Balance board
                if (Program.SystemConfig.isOptSet("wii_balanceboard") && Program.SystemConfig.getOptBoolean("wii_balanceboard"))
                {
                    ini.WriteValue("BalanceBoard", "Source", "2");
                }
                else
                    ini.WriteValue("BalanceBoard", "Source", "0");

                ini.Save();
            }

            // Set hotkeys
            string hotkeyini = Path.Combine(path, "User", "Config", "Hotkeys.ini");
            if (File.Exists(hotkeyini))
                SetWiimoteHotkeys(hotkeyini);
        }

        static Dictionary<string, string> defaultGunMapping = new Dictionary<string, string>
        {
            { "Device", "DInput/0/Keyboard Mouse" },
            { "Source", "1" },
            { "Buttons/X", "Q" },
            { "Buttons/B", "`Click 0`" },
            { "Buttons/Y", "S" },
            { "Buttons/A", "`Click 1`" },
            { "Buttons/-", "BACK" },
            { "Buttons/+", "RETURN" },
            { "Main Stick/Up", "UP" },
            { "Main Stick/Down", "DOWN" },
            { "Main Stick/Left", "LEFT" },
            { "Main Stick/Right", "RIGHT" },
            { "Tilt/Modifier/Range", "50." },
            { "Nunchuk/Stick/Modifier/Range", "50." },
            { "Nunchuk/Tilt/Modifier/Range", "50." },
            { "uDraw/Stylus/Modifier/Range", "50." },
            { "Drawsome/Stylus/Modifier/Range", "50." },
            { "Buttons/1", "`Click 2`" },
            { "Buttons/2", "`2`" },
            { "D-Pad/Up", "UP" },
            { "D-Pad/Down", "DOWN" },
            { "D-Pad/Left", "LEFT" },
            { "D-Pad/Right", "RIGHT" },
            { "IR/Up", "`Cursor Y-`" },
            { "IR/Down", "`Cursor Y+`" },
            { "IR/Left", "`Cursor X-`" },
            { "IR/Right", "`Cursor X+`" },
            { "Shake/X", "`Click 2`" },
            { "Shake/Y", "`Click 2`" },
            { "Shake/Z", "`Click 2`" },
            { "Extension", "Nunchuk" },
            { "Nunchuk/Buttons/C", "LCONTROL" },
            { "Nunchuk/Buttons/Z", "LSHIFT" },
            { "Nunchuk/Stick/Up", "W" },
            { "Nunchuk/Stick/Down", "S" },
            { "Nunchuk/Stick/Left", "A" },
            { "Nunchuk/Stick/Right", "D" },
            { "Nunchuk/Stick/Calibration", "100.00 141.42 100.00 141.42 100.00 141.42 100.00 141.42" },
            { "Nunchuk/Shake/X", "`Click 2`" },
            { "Nunchuk/Shake/Y", "`Click 2`" },
            { "Nunchuk/Shake/Z", "`Click 2`" },
        };

        static Dictionary<string, string> gunMapping = new Dictionary<string, string>
        {
            { "action", "Buttons/A" },
            { "trigger", "Buttons/B" },
            { "sub3", "Buttons/Home" },
            { "select", "Buttons/-" },
            { "sub1", "Buttons/1" },
            { "sub2", "Buttons/2" },
            { "start", "Buttons/+" },
            { "up", "D-Pad/Up" },
            { "down", "D-Pad/Down" },
            { "left", "D-Pad/Left" },
            { "right", "D-Pad/Right" },
            { "c", "Nunchuk/Buttons/C" },
            { "z", "Nunchuk/Buttons/Z" }
        };

        static Dictionary<string, string> newGunMapping = new Dictionary<string, string>
        {
            { "a", "Buttons/A" },
            { "b", "Buttons/B" },
            { "home", "Buttons/Home" },
            { "-", "Buttons/-" },
            { "1", "Buttons/1" },
            { "2", "Buttons/2" },
            { "+", "Buttons/+" },
            { "up", "D-Pad/Up" },
            { "down", "D-Pad/Down" },
            { "left", "D-Pad/Left" },
            { "right", "D-Pad/Right" },
            { "tiltforward", "Tilt/Forward" },
            { "tiltbackward", "Tilt/Backward" },
            { "tiltleft", "Tilt/Left" },
            { "tiltright", "Tilt/Right" },
            { "shake", "Shake/X" },
            { "c", "Nunchuk/Buttons/C" },
            { "z", "Nunchuk/Buttons/Z" }
        };

        static Dictionary<string, string> gunSpecials = new Dictionary<string, string>
        {
            { "vertical_offset", "IR/Vertical Offset" },
            { "yaw", "IR/Total Yaw" },
            { "pitch", "IR/Total Pitch" },
            { "ir_down", "IR/Down" },
            { "ir_up", "IR/Up" },
            { "ir_left", "IR/Left" },
            { "ir_right", "IR/Right" }
        };

        private static void GenerateControllerConfig_wiilightgun(string path, string filename, string anyDefKey, bool sindenSoft)
        {
            string iniFile = Path.Combine(path, "User", "Config", filename);

            SimpleLogger.Instance.Info("[INFO] Configuring Lightgun buttons in : " + iniFile);

            string rom = Program.SystemConfig["rom"];
            var mappingToUse = defaultGunMapping;
            
            var guns = RawLightgun.GetRawLightguns();

            if (guns.Any(g => g.Type == RawLighGunType.SindenLightgun))
            {
                Guns.StartSindenSoftware();
                sindenSoft = true;
            }

            // Search gun game in gamesDB.xml file
            var game = Program.GunGames.FindGunGame("wii", rom);
            if (game == null || game.Gun == null)
            {
                SimpleLogger.Instance.Info("[GUNS] No gun configuration found for this game.");
            }

            Dictionary<string, string> metadata = new Dictionary<string, string>();
            if (game != null && game.Gun != null)
            {
                metadata["vertical_offset"] = game.Gun.VerticalOffset.ToString();
                metadata["yaw"] = game.Gun.Yaw.ToString();
                metadata["pitch"] = game.Gun.Pitch.ToString();
                metadata["action"] = game.Gun.Action;
                metadata["start"] = game.Gun.Start;
                metadata["select"] = game.Gun.Select;
                metadata["trigger"] = game.Gun.Trigger;
                metadata["ir_down"] = game.Gun.IrDown;
                metadata["ir_right"] = game.Gun.IrRight;
                metadata["ir_up"] = game.Gun.IrUp;
                metadata["ir_left"] = game.Gun.IrLeft;
                metadata["sub1"] = game.Gun.Sub1;
            }

            if (metadata == null)
                return;

            // Modify mapping based on gamesDB.xml file
            foreach (var x in metadata)
            {
                if (x.Value != null && newGunMapping.ContainsKey(x.Value))
                {
                    string toReplace = newGunMapping[x.Value];
                    string newValue = defaultGunMapping[gunMapping[x.Key]];
                    mappingToUse[toReplace] = newValue;
                    if (toReplace == "Shake/X")
                    {
                        mappingToUse["Shake/Y"] = newValue;
                        mappingToUse["Shake/Z"] = newValue;
                    }
                }
            }

            // Remove original button mapping
            foreach (var x in metadata)
            {
                if (x.Value != null && gunMapping.ContainsKey(x.Key))
                    mappingToUse[gunMapping[x.Key]] = "";
            }

            using (IniFile ini = new IniFile(iniFile, IniOptions.UseSpaces))
            {
                for (int i = 1; i < 5; i++)
                {
                    ini.ClearSection(anyDefKey + i.ToString());
                }
                string wiimote = "Wiimote1";

                foreach (var x in mappingToUse)
                {
                    ini.WriteValue(wiimote, x.Key, x.Value);
                }

                // Write specials
                foreach (var x in metadata)
                {
                    if (x.Value != null && x.Value != "" && gunSpecials.ContainsKey(x.Key))
                        ini.WriteValue(wiimote, gunSpecials[x.Key], x.Value);
                }

                ini.Save();
            }

            // Reset hotkeys
            string hotkeyini = Path.Combine(path, "User", "Config", "Hotkeys.ini");
            if (File.Exists(hotkeyini))
                ResetHotkeysToDefault(hotkeyini);
        }

        private static string GetSDLMappingName(Controller pad, InputKey key)
        {
            var input = pad.GetSdlMapping(key);
            if (input == null)
                return null;

            if (input.Type == "button")
            {
                if (input.Id == 0) // invert A&B
                    return "`Button 1`";

                if (input.Id == 1) // invert A&B
                    return "`Button 0`";

                return "`Button " + input.Id.ToString() + "`";
            }

            if (input.Type == "axis")
            {
                Func<Input, bool, string> axisValue = (inp, revertAxis) =>
                {
                    string axis = "`Axis ";

                    if (inp.Id == 0 || inp.Id == 1 || inp.Id == 2 || inp.Id == 3)
                        axis += inp.Id;

                    if ((!revertAxis && inp.Value > 0) || (revertAxis && inp.Value < 0))
                        axis += "+";
                    else
                        axis += "-";

                    if (inp.Id == 4 || inp.Id == 5)
                        axis = "`Full Axis " + inp.Id + "+";

                    return axis + "`";
                };

                return axisValue(input, false);
                /*
                string reverseAxis;
                if (anyReverseAxes.TryGetValue(value, out reverseAxis))
                    ini.WriteValue(gcpad, reverseAxis, axisValue(input, true));*/
            }

            return null;
        }

        private static void GenerateControllerConfig_any(string path, string filename, string anyDefKey, InputKeyMapping anyMapping, Dictionary<string, string> anyReverseAxes, bool triforce = false, Dictionary<string, string> extraOptions = null)
        {
            //string path = Program.AppConfig.GetFullPath("dolphin");
            string iniFile = Path.Combine(path, "User", "Config", filename);

            SimpleLogger.Instance.Info("[INFO] Writing controller configuration in : " + iniFile);

            bool forceSDL = false;
            if (Program.SystemConfig.isOptSet("input_forceSDL") && Program.SystemConfig.getOptBoolean("input_forceSDL"))
                forceSDL = true;

            int nsamepad = 0;
            bool gc = anyDefKey == "GCPad";
            bool gba = anyDefKey == "GBA";

            Dictionary<string, int> double_pads = new Dictionary<string, int>();

            using (IniFile ini = new IniFile(iniFile, IniOptions.UseSpaces))
            {
                foreach (var pad in Program.Controllers.OrderBy(i => i.PlayerIndex).Take(4))
                {
                    bool xinputAsSdl = false;
                    bool isNintendo = pad.VendorID == USB_VENDOR.NINTENDO;
                    string gcpad = anyDefKey + pad.PlayerIndex;
                    if (gcpad != null)
                        ini.ClearSection(gcpad);

                    if (pad.Config == null)
                        continue;

                    // SIDevice0 = 7 -> Keyb GCKeyNew.ini
                    // SIDevice1 = 6 -> controlleur standard GCPadNew.ini

                    string guid = pad.GetSdlGuid(SdlVersion.SDL2_0_X).ToLowerInvariant();
                    var prod = pad.ProductID;
                    string gamecubepad = "gamecubepad" + (pad.PlayerIndex - 1);
                    
                    if (gcAdapters.ContainsKey(guid) && Program.SystemConfig[gamecubepad] != "12" && Program.SystemConfig[gamecubepad] != "13")
                    {
                        ConfigureGCAdapter(gcpad, guid, pad, ini);
                        continue;
                    }

                    string tech = "XInput";
                    string deviceName = "Gamepad";
                    int xIndex = 0;

                    if (pad.Config.Type == "keyboard")
                    {
                        tech = "DInput";
                        deviceName = "Keyboard Mouse";
                    }
                    else if (!pad.IsXInputDevice || forceSDL)
                    {
                        var s = pad.SdlController;
                        if (s == null)
                            continue;

                        tech = "SDL";

                        if (pad.IsXInputDevice)
                        {
                            xinputAsSdl = true;
                            tech = "XInput";
                        }

                        deviceName = pad.Name != null ? pad.Name : "";

                        string newNamePath = Path.Combine(Program.AppConfig.GetFullPath("tools"), "controllerinfo.yml");
                        if (File.Exists(newNamePath))
                        {
                            string newName = SdlJoystickGuid.GetNameFromFile(newNamePath, pad.Guid, "dolphin");

                            if (newName != null)
                                deviceName = newName;
                        }
                    }

                    if (double_pads.ContainsKey(tech + "/" + deviceName))
                        nsamepad = double_pads[tech + "/" + deviceName];
                    else
                        nsamepad = 0;

                    if (pad.PlayerIndex == 1)
                        _p1sdlindex = nsamepad;

                    double_pads[tech + "/" + deviceName] = nsamepad + 1;

                    if (pad.IsXInputDevice)
                        xIndex = pad.XInput != null ? pad.XInput.DeviceIndex : pad.DeviceIndex;

                    if (tech == "XInput" && !xinputAsSdl)
                        ini.WriteValue(gcpad, "Device", tech + "/" + xIndex + "/" + deviceName);
                    else if (xinputAsSdl)
                        ini.WriteValue(gcpad, "Device", "SDL" + "/" + nsamepad.ToString() + "/" + deviceName);
                    else
                        ini.WriteValue(gcpad, "Device", tech + "/" + nsamepad.ToString() + "/" + deviceName);

                    if (extraOptions != null)
                        foreach (var xtra in extraOptions)
                            ini.WriteValue(gcpad, xtra.Key, xtra.Value);

                    bool positional = Program.Features.IsSupported("gamecube_buttons") && Program.SystemConfig.isOptSet("gamecube_buttons") && Program.SystemConfig["gamecube_buttons"] == "position";
                    bool xboxLayout = Program.Features.IsSupported("gamecube_buttons") && Program.SystemConfig.isOptSet("gamecube_buttons") && Program.SystemConfig["gamecube_buttons"] == "xbox";
                    bool revertXY = Program.Features.IsSupported("gamecube_buttons") && Program.SystemConfig.isOptSet("gamecube_buttons") && Program.SystemConfig["gamecube_buttons"] == "reverse_ab";
                    bool rumble = !Program.SystemConfig.isOptSet("input_rumble") || Program.SystemConfig.getOptBoolean("input_rumble");

                    if (isNintendo && pad.PlayerIndex == 1)
                    {
                        string tempMapA = anyMapping[InputKey.a];
                        string tempMapB = anyMapping[InputKey.b];
                        string tempMapX = anyMapping[InputKey.x];
                        string tempMapY = anyMapping[InputKey.y];

                        if (tempMapB != null)
                            anyMapping[InputKey.a] = tempMapB;
                        if (tempMapA != null)
                            anyMapping[InputKey.b] = tempMapA;
                        if (tempMapY != null)
                            anyMapping[InputKey.x] = tempMapY;
                        if (tempMapX != null)
                            anyMapping[InputKey.y] = tempMapX;
                    }

                    // Microphone
                    if (gc && Program.SystemConfig.isOptSet("dolphin_gcpad_microphone") && Program.SystemConfig.getOptBoolean("dolphin_gcpad_microphone") && pad.PlayerIndex == 2)
                    {
                        anyMapping[InputKey.pageup] = "Microphone/Button";
                    }

                    foreach (var x in anyMapping)
                    {
                        string value = x.Value;

                        if (xboxLayout && reversedButtons.ContainsKey(x.Key))
                        {
                            value = reversedButtons[x.Key];
                            if (isNintendo)
                            {
                                if (value == "Buttons/B")
                                    value = "Buttons/A";
                                else if (value == "Buttons/A")
                                    value = "Buttons/B";
                                else if (value == "Buttons/X")
                                    value = "Buttons/Y";
                                else if (value == "Buttons/Y")
                                    value = "Buttons/X";
                            }
                        }

                        if (revertXY && reversedButtonsXY.ContainsKey(x.Key))
                        {
                            value = reversedButtonsXY[x.Key];
                            if (isNintendo)
                            {
                                if (value == "Buttons/B")
                                    value = "Buttons/A";
                                else if (value == "Buttons/A")
                                    value = "Buttons/B";
                                else if (value == "Buttons/X")
                                    value = "Buttons/Y";
                                else if (value == "Buttons/Y")
                                    value = "Buttons/X";
                            }
                        }

                        if (positional && reversedButtonsRotate.ContainsKey(x.Key))
                        {
                            value = reversedButtonsRotate[x.Key];
                            if (isNintendo)
                            {
                                if (value == "Buttons/B")
                                    value = "Buttons/Y";
                                else if (value == "Buttons/A")
                                    value = "Buttons/X";
                                else if (value == "Buttons/X")
                                    value = "Buttons/A";
                                else if (value == "Buttons/Y")
                                    value = "Buttons/B";
                            }
                        }

                        if (triforce && Program.SystemConfig.isOptSet("triforce_mapping") && Program.SystemConfig["triforce_mapping"] == "vs4")
                        {
                            if (vs4mapping.ContainsKey(x.Key))
                                value = vs4mapping[x.Key];
                        }

                        if (pad.Config.Type == "keyboard")
                        {
                            if (x.Key == InputKey.a && (xboxLayout))
                                value = "Buttons/B";
                            else if (x.Key == InputKey.a)
                                value = "Buttons/A";
                            else if (x.Key == InputKey.b && (xboxLayout))
                                value = "Buttons/A";
                            else if (x.Key == InputKey.b)
                                value = "Buttons/B";
                            else if (x.Key == InputKey.x && xboxLayout || revertXY)
                                value = "Buttons/Y";
                            else if (x.Key == InputKey.x)
                                value = "Buttons/X";
                            else if (x.Key == InputKey.y && xboxLayout || revertXY)
                                value = "Buttons/X";
                            else if (x.Key == InputKey.y)
                                value = "Buttons/Y";
                            else if (x.Key == InputKey.up)
                                value = "Main Stick/Up";
                            else if (x.Key == InputKey.down)
                                value = "Main Stick/Down";
                            else if (x.Key == InputKey.left)
                                value = "Main Stick/Left";
                            else if (x.Key == InputKey.right)
                                value = "Main Stick/Right";

                            if (x.Key == InputKey.joystick1left || x.Key == InputKey.joystick1up)
                                continue;

                            if (!gc && !gba)
                            {
                                ini.WriteValue(gcpad, "IR/Up", "`Cursor Y-`");
                                ini.WriteValue(gcpad, "IR/Down", "`Cursor Y+`");
                                ini.WriteValue(gcpad, "IR/Left", "`Cursor X-`");
                                ini.WriteValue(gcpad, "IR/Right", "`Cursor X+`");
                            }

                            var input = pad.Config[x.Key];
                            if (input == null)
                                continue;

                            var name = ToDolphinKey(input.Id);
                            ini.WriteValue(gcpad, value, name);
                        }
                        else if (tech == "XInput")
                        {
                            var mapping = pad.GetXInputMapping(x.Key);
                            if (mapping != XINPUTMAPPING.UNKNOWN && xInputMapping.ContainsKey(mapping))
                                ini.WriteValue(gcpad, value, xInputMapping[mapping]);

                            if (anyReverseAxes.TryGetValue(value, out string reverseAxis))
                            {
                                mapping = pad.GetXInputMapping(x.Key, true);
                                if (mapping != XINPUTMAPPING.UNKNOWN && xInputMapping.ContainsKey(mapping))
                                    ini.WriteValue(gcpad, reverseAxis, xInputMapping[mapping]);
                            }

                            // Z button is used to access test menu, map with R3
                            if (triforce)
                                ini.WriteValue(gcpad, "Buttons/Z", "`Thumb R`");

                        }
                        else if (forceSDL)
                        {
                            var input = pad.Config[x.Key];

                            if (input == null)
                                continue;

                            if (input.Type == "button")
                            {
                                if (input.Id == 0) // invert A&B
                                    ini.WriteValue(gcpad, value, "`Button 1`");
                                else if (input.Id == 1) // invert A&B
                                    ini.WriteValue(gcpad, value, "`Button 0`");
                                else
                                    ini.WriteValue(gcpad, value, "`Button " + input.Id.ToString() + "`");
                            }

                            else if (input.Type == "axis")
                            {
                                Func<Input, bool, string> axisValue = (inp, revertAxis) =>
                                {
                                    string axis = "`Axis ";

                                    if (inp.Id == 0 || inp.Id == 1 || inp.Id == 2 || inp.Id == 3)
                                        axis += inp.Id;

                                    if ((!revertAxis && inp.Value > 0) || (revertAxis && inp.Value < 0))
                                        axis += "+";
                                    else
                                        axis += "-";

                                    if (inp.Id == 4 || inp.Id == 5)
                                        axis = "`Full Axis " + inp.Id + "+";

                                    return axis + "`";
                                };

                                ini.WriteValue(gcpad, value, axisValue(input, false));

                                if (anyReverseAxes.TryGetValue(value, out string reverseAxis))
                                    ini.WriteValue(gcpad, reverseAxis, axisValue(input, true));
                            }

                            else if (input.Type == "hat")
                            {
                                Int64 pid = input.Value;
                                switch (pid)
                                {
                                    case 1:
                                        ini.WriteValue(gcpad, value, "`Hat " + input.Id.ToString() + " N`");
                                        break;
                                    case 2:
                                        ini.WriteValue(gcpad, value, "`Hat " + input.Id.ToString() + " E`");
                                        break;
                                    case 4:
                                        ini.WriteValue(gcpad, value, "`Hat " + input.Id.ToString() + " S`");
                                        break;
                                    case 8:
                                        ini.WriteValue(gcpad, value, "`Hat " + input.Id.ToString() + " W`");
                                        break;
                                }
                            }
                            // Z button is used to access test menu, do not map it with R1
                            if (triforce)
                                ini.WriteValue(gcpad, "Buttons/Z", "@(`Button 8`+`Button 9`)");
                        }

                        else // SDL
                        {
                            var input = pad.GetSdlMapping(x.Key);

                            if (input == null)
                                continue;

                            if (input.Type == "button")
                            {
                                if (input.Id == 0) // invert A&B
                                    ini.WriteValue(gcpad, value, "`Button 1`");
                                else if (input.Id == 1) // invert A&B
                                    ini.WriteValue(gcpad, value, "`Button 0`");
                                else
                                    ini.WriteValue(gcpad, value, "`Button " + input.Id.ToString() + "`");
                            }
                            else if (input.Type == "axis")
                            {
                                Func<Input, bool, string> axisValue = (inp, revertAxis) =>
                                {
                                    string axis = "`Axis ";

                                    if (inp.Id == 0 || inp.Id == 1 || inp.Id == 2 || inp.Id == 3)
                                        axis += inp.Id;

                                    if ((!revertAxis && inp.Value > 0) || (revertAxis && inp.Value < 0))
                                        axis += "+";
                                    else
                                        axis += "-";

                                    if (inp.Id == 4 || inp.Id == 5)
                                        axis = "`Full Axis " + inp.Id + "+";

                                    return axis + "`";
                                };

                                ini.WriteValue(gcpad, value, axisValue(input, false));

                                if (anyReverseAxes.TryGetValue(value, out string reverseAxis))
                                    ini.WriteValue(gcpad, reverseAxis, axisValue(input, true));
                            }

                            // Z button is used to access test menu, do not map it with R1
                            if (triforce)
                                ini.WriteValue(gcpad, "Buttons/Z", "@(`Button 7`+`Button 8`)");
                        }
                    }

                    if (gc)
                    {
                        ini.WriteValue(gcpad, "Main Stick/Modifier/Range", "50.0");
                        ini.WriteValue(gcpad, "C-Stick/Modifier/Range", "50.0");

                        // DEADZONE
                        if (Program.SystemConfig.isOptSet("dolphin_gcpad_deadzone") && !string.IsNullOrEmpty(Program.SystemConfig["dolphin_gcpad_deadzone"]))
                        {
                            string deadzone = Program.SystemConfig["dolphin_gcpad_deadzone"].ToIntegerString() + ".0";
                            ini.WriteValue(gcpad, "Main Stick/Dead Zone", deadzone);
                            ini.WriteValue(gcpad, "C-Stick/Dead Zone", deadzone);
                        }
                        else
                        {
                            ini.WriteValue(gcpad, "Main Stick/Dead Zone", "15.0");
                            ini.WriteValue(gcpad, "C-Stick/Dead Zone", "15.0");
                        }

                        // SENSITIVITY
                        if (Program.SystemConfig.isOptSet("dolphin_gcpad_sensitivity") && !string.IsNullOrEmpty(Program.SystemConfig["dolphin_gcpad_sensitivity"]))
                        {
                            string sensitivity = Program.SystemConfig["dolphin_gcpad_sensitivity"].ToIntegerString() + ".0";
                            ini.WriteValue(gcpad, "Main Stick/Up/Range", sensitivity);
                            ini.WriteValue(gcpad, "Main Stick/Down/Range", sensitivity);
                            ini.WriteValue(gcpad, "Main Stick/Left/Range", sensitivity);
                            ini.WriteValue(gcpad, "Main Stick/Right/Range", sensitivity);
                            ini.WriteValue(gcpad, "C-Stick/Up/Range", sensitivity);
                            ini.WriteValue(gcpad, "C-Stick/Down/Range", sensitivity);
                            ini.WriteValue(gcpad, "C-Stick/Left/Range", sensitivity);
                            ini.WriteValue(gcpad, "C-Stick/Right/Range", sensitivity);
                        }
                        else
                        {
                            ini.WriteValue(gcpad, "Main Stick/Up/Range", "100.0");
                            ini.WriteValue(gcpad, "Main Stick/Down/Range", "100.0");
                            ini.WriteValue(gcpad, "Main Stick/Left/Range", "100.0");
                            ini.WriteValue(gcpad, "Main Stick/Right/Range", "100.0");
                            ini.WriteValue(gcpad, "C-Stick/Up/Range", "100.0");
                            ini.WriteValue(gcpad, "C-Stick/Down/Range", "100.0");
                            ini.WriteValue(gcpad, "C-Stick/Left/Range", "100.0");
                            ini.WriteValue(gcpad, "C-Stick/Right/Range", "100.0");
                        }

                        if (tech == "XInput")
                        {
                            ini.WriteValue(gcpad, "Main Stick/Calibration", "100.00 101.96 108.24 109.27 115.00 109.59 106.10 101.96 100.00 101.96 105.22 107.49 117.34 112.43 108.24 101.96 100.00 101.96 108.24 116.11 116.57 116.72 108.24 101.96 100.00 101.96 108.24 109.75 115.91 109.18 107.47 101.96");
                            ini.WriteValue(gcpad, "C-Stick/Calibration", "100.00 101.96 108.24 112.26 122.26 118.12 108.24 101.96 100.00 101.96 108.24 114.92 117.37 115.98 108.24 101.96 100.00 101.96 105.40 112.07 114.52 113.89 104.20 99.64 99.97 101.73 106.63 108.27 103.63 104.40 107.15 101.96");
                        }

                        if (prod == USB_PRODUCT.NINTENDO_SWITCH_PRO)
                        {
                            ini.WriteValue(gcpad, "Main Stick/Calibration", "98.50 101.73 102.04 106.46 104.62 102.21 102.00 100.53 97.00 96.50 99.95 100.08 102.40 99.37 99.60 100.17 99.60 100.14 98.87 100.48 102.45 101.12 100.92 97.92 99.00 99.92 100.83 100.45 102.27 98.45 97.16 97.36");
                            ini.WriteValue(gcpad, "C-Stick/Calibration", "98.19 101.79 101.37 102.32 103.05 101.19 99.56 99.11 98.45 100.60 98.65 100.67 99.85 97.31 97.24 96.36 95.94 97.94 98.17 100.24 99.22 98.10 99.69 98.77 97.14 100.45 99.08 100.13 102.61 101.37 100.55 97.03");
                        }

                        if (prod == USB_PRODUCT.SONY_DS3 ||
                            prod == USB_PRODUCT.SONY_DS4 ||
                            prod == USB_PRODUCT.SONY_DS4_DONGLE ||
                            prod == USB_PRODUCT.SONY_DS4_SLIM ||
                            prod == USB_PRODUCT.SONY_DS5)
                        {
                            ini.WriteValue(gcpad, "Main Stick/Calibration", "100.00 101.96 104.75 107.35 109.13 110.30 105.04 101.96 100.00 101.96 105.65 105.14 105.94 103.89 104.87 101.04 100.00 101.96 107.16 107.49 105.93 103.65 102.31 101.96 100.00 101.96 103.68 108.28 108.05 105.96 103.66 101.48");
                            ini.WriteValue(gcpad, "C-Stick/Calibration", "100.00 101.96 104.31 104.51 105.93 104.41 103.44 101.96 100.00 101.96 104.07 105.45 109.33 107.39 104.91 101.96 100.00 101.96 106.79 107.84 105.66 104.16 102.91 100.38 98.14 101.63 105.29 107.30 106.77 104.73 104.87 100.92");
                        }

                        else
                        {
                            ini.WriteValue(gcpad, "Main Stick/Calibration", "100.00 101.96 104.75 107.35 109.13 110.30 105.04 101.96 100.00 101.96 105.65 105.14 105.94 103.89 104.87 101.04 100.00 101.96 107.16 107.49 105.93 103.65 102.31 101.96 100.00 101.96 103.68 108.28 108.05 105.96 103.66 101.48");
                            ini.WriteValue(gcpad, "C-Stick/Calibration", "100.00 101.96 104.31 104.51 105.93 104.41 103.44 101.96 100.00 101.96 104.07 105.45 109.33 107.39 104.91 101.96 100.00 101.96 106.79 107.84 105.66 104.16 102.91 100.38 98.14 101.63 105.29 107.30 106.77 104.73 104.87 100.92");
                        }

                        // RUMBLE
                        if (rumble)
                        {
                            if (tech == "XInput")
                                ini.WriteValue(gcpad, "Rumble/Motor", "`Motor L`|`Motor R`");
                            else
                                ini.WriteValue(gcpad, "Rumble/Motor", "Motor");
                        }
                    }

                    else if (!gba)
                    {
                        // DEAD ZONE
                        if (Program.SystemConfig.isOptSet("dolphin_wii_deadzone") && !string.IsNullOrEmpty(Program.SystemConfig["dolphin_wii_deadzone"]))
                        {
                            string deadzone = Program.SystemConfig["dolphin_wii_deadzone"].ToIntegerString() + ".0";
                            ini.WriteValue(gcpad, "Classic/Right Stick/Dead Zone", deadzone);
                            ini.WriteValue(gcpad, "Classic/Left Stick/Dead Zone", deadzone);
                            ini.WriteValue(gcpad, "IR/Dead Zone", deadzone);
                            ini.WriteValue(gcpad, "Tilt/Dead Zone", deadzone);
                            ini.WriteValue(gcpad, "Swing/Dead Zone", deadzone);
                            ini.WriteValue(gcpad, "IMUGyroscope/Dead Zone", deadzone);
                            ini.WriteValue(gcpad, "Nunchuk/Tilt/Dead Zone", deadzone);
                            ini.WriteValue(gcpad, "Nunchuk/Swing/Dead Zone", deadzone);
                            ini.WriteValue(gcpad, "Nunchuk/Stick/Dead Zone", deadzone);
                        }
                        else
                        {
                            ini.WriteValue(gcpad, "Classic/Right Stick/Dead Zone", "15.0");
                            ini.WriteValue(gcpad, "Classic/Left Stick/Dead Zone", "15.0");
                            ini.WriteValue(gcpad, "IR/Dead Zone", "15.0");
                            ini.WriteValue(gcpad, "Tilt/Dead Zone", "15.0");
                            ini.WriteValue(gcpad, "Swing/Dead Zone", "15.0");
                            ini.WriteValue(gcpad, "IMUGyroscope/Dead Zone", "15.0");
                            ini.WriteValue(gcpad, "Nunchuk/Tilt/Dead Zone", "15.0");
                            ini.WriteValue(gcpad, "Nunchuk/Swing/Dead Zone", "15.0");
                            ini.WriteValue(gcpad, "Nunchuk/Stick/Dead Zone", "15.0");
                        }

                        // SENSITIVITY
                        if (Program.SystemConfig.isOptSet("dolphin_wii_sensitivity") && !string.IsNullOrEmpty(Program.SystemConfig["dolphin_wii_sensitivity"]))
                        {
                            string sensitivity = Program.SystemConfig["dolphin_wii_sensitivity"].ToIntegerString() + ".0";
                            ini.WriteValue(gcpad, "IR/Up/Range", sensitivity);
                            ini.WriteValue(gcpad, "IR/Down/Range", sensitivity);
                            ini.WriteValue(gcpad, "IR/Left/Range", sensitivity);
                            ini.WriteValue(gcpad, "IR/Right/Range", sensitivity);
                            ini.WriteValue(gcpad, "Tilt/Forward/Range", sensitivity);
                            ini.WriteValue(gcpad, "Tilt/Left/Range", sensitivity);
                            ini.WriteValue(gcpad, "Tilt/Backward/Range", sensitivity);
                            ini.WriteValue(gcpad, "Tilt/Right/Range", sensitivity);
                            ini.WriteValue(gcpad, "Swing/Up/Range", sensitivity);
                            ini.WriteValue(gcpad, "Swing/Down/Range", sensitivity);
                            ini.WriteValue(gcpad, "Swing/Left/Range", sensitivity);
                            ini.WriteValue(gcpad, "Swing/Right/Range", sensitivity);
                            ini.WriteValue(gcpad, "Nunchuk/Stick/Up/Range", sensitivity);
                            ini.WriteValue(gcpad, "Nunchuk/Stick/Down/Range", sensitivity);
                            ini.WriteValue(gcpad, "Nunchuk/Stick/Left/Range", sensitivity);
                            ini.WriteValue(gcpad, "Nunchuk/Stick/Right/Range", sensitivity);
                            ini.WriteValue(gcpad, "Classic/Left Stick/Up/Range", sensitivity);
                            ini.WriteValue(gcpad, "Classic/Left Stick/Down/Range", sensitivity);
                            ini.WriteValue(gcpad, "Classic/Left Stick/Left/Range", sensitivity);
                            ini.WriteValue(gcpad, "Classic/Left Stick/Right/Range", sensitivity);
                            ini.WriteValue(gcpad, "Classic/Right Stick/Up/Range", sensitivity);
                            ini.WriteValue(gcpad, "Classic/Right Stick/Down/Range", sensitivity);
                            ini.WriteValue(gcpad, "Classic/Right Stick/Left/Range", sensitivity);
                            ini.WriteValue(gcpad, "Classic/Right Stick/Right/Range", sensitivity);
                        }
                        else
                        {
                            ini.WriteValue(gcpad, "IR/Up/Range", "100.0");
                            ini.WriteValue(gcpad, "IR/Down/Range", "100.0");
                            ini.WriteValue(gcpad, "IR/Left/Range", "100.0");
                            ini.WriteValue(gcpad, "IR/Right/Range", "100.0");
                            ini.WriteValue(gcpad, "Tilt/Forward/Range", "100.0");
                            ini.WriteValue(gcpad, "Tilt/Left/Range", "100.0");
                            ini.WriteValue(gcpad, "Tilt/Backward/Range", "100.0");
                            ini.WriteValue(gcpad, "Tilt/Right/Range", "100.0");
                            ini.WriteValue(gcpad, "Swing/Up/Range", "100.0");
                            ini.WriteValue(gcpad, "Swing/Down/Range", "100.0");
                            ini.WriteValue(gcpad, "Swing/Left/Range", "100.0");
                            ini.WriteValue(gcpad, "Swing/Right/Range", "100.0");
                            ini.WriteValue(gcpad, "Nunchuk/Stick/Up/Range", "100.0");
                            ini.WriteValue(gcpad, "Nunchuk/Stick/Down/Range", "100.0");
                            ini.WriteValue(gcpad, "Nunchuk/Stick/Left/Range", "100.0");
                            ini.WriteValue(gcpad, "Nunchuk/Stick/Right/Range", "100.0");
                            ini.WriteValue(gcpad, "Classic/Left Stick/Up/Range", "100.0");
                            ini.WriteValue(gcpad, "Classic/Left Stick/Down/Range", "100.0");
                            ini.WriteValue(gcpad, "Classic/Left Stick/Left/Range", "100.0");
                            ini.WriteValue(gcpad, "Classic/Left Stick/Right/Range", "100.0");
                            ini.WriteValue(gcpad, "Classic/Right Stick/Up/Range", "100.0");
                            ini.WriteValue(gcpad, "Classic/Right Stick/Down/Range", "100.0");
                            ini.WriteValue(gcpad, "Classic/Right Stick/Left/Range", "100.0");
                            ini.WriteValue(gcpad, "Classic/Right Stick/Right/Range", "100.0");
                        }

                        if (Program.SystemConfig["controller_mode"] == "cc" || Program.SystemConfig["controller_mode"] == "ccp")
                        {
                            if (prod == USB_PRODUCT.NINTENDO_SWITCH_PRO)
                            {
                                ini.WriteValue(gcpad, "Classic/Left Stick/Calibration", "98.50 101.73 102.04 106.46 104.62 102.21 102.00 100.53 97.00 96.50 99.95 100.08 102.40 99.37 99.60 100.17 99.60 100.14 98.87 100.48 102.45 101.12 100.92 97.92 99.00 99.92 100.83 100.45 102.27 98.45 97.16 97.36");
                                ini.WriteValue(gcpad, "Classic/Right Stick/Calibration", "98.19 101.79 101.37 102.32 103.05 101.19 99.56 99.11 98.45 100.60 98.65 100.67 99.85 97.31 97.24 96.36 95.94 97.94 98.17 100.24 99.22 98.10 99.69 98.77 97.14 100.45 99.08 100.13 102.61 101.37 100.55 97.03");
                            }
                            else if (prod == USB_PRODUCT.SONY_DS3 ||
                            prod == USB_PRODUCT.SONY_DS4 ||
                            prod == USB_PRODUCT.SONY_DS4_DONGLE ||
                            prod == USB_PRODUCT.SONY_DS4_SLIM ||
                            prod == USB_PRODUCT.SONY_DS5)
                            {
                                ini.WriteValue(gcpad, "Classic/Left Stick/Calibration", "100.00 101.96 104.75 107.35 109.13 110.30 105.04 101.96 100.00 101.96 105.65 105.14 105.94 103.89 104.87 101.04 100.00 101.96 107.16 107.49 105.93 103.65 102.31 101.96 100.00 101.96 103.68 108.28 108.05 105.96 103.66 101.48");
                                ini.WriteValue(gcpad, "Classic/Right Stick/Calibration", "100.00 101.96 104.31 104.51 105.93 104.41 103.44 101.96 100.00 101.96 104.07 105.45 109.33 107.39 104.91 101.96 100.00 101.96 106.79 107.84 105.66 104.16 102.91 100.38 98.14 101.63 105.29 107.30 106.77 104.73 104.87 100.92");
                            }
                            else
                            {
                                ini.WriteValue(gcpad, "Classic/Left Stick/Calibration", "100.00 101.96 104.75 107.35 109.13 110.30 105.04 101.96 100.00 101.96 105.65 105.14 105.94 103.89 104.87 101.04 100.00 101.96 107.16 107.49 105.93 103.65 102.31 101.96 100.00 101.96 103.68 108.28 108.05 105.96 103.66 101.48");
                                ini.WriteValue(gcpad, "Classic/Right Stick/Calibration", "100.00 101.96 104.31 104.51 105.93 104.41 103.44 101.96 100.00 101.96 104.07 105.45 109.33 107.39 104.91 101.96 100.00 101.96 106.79 107.84 105.66 104.16 102.91 100.38 98.14 101.63 105.29 107.30 106.77 104.73 104.87 100.92");
                            }
                        }
                        if (Program.SystemConfig.isOptSet("wii_motionpad") && Program.SystemConfig.getOptBoolean("wii_motionpad"))
                        {
                            ini.WriteValue(gcpad, "IMUAccelerometer/Up", "`Accel Up`");
                            ini.WriteValue(gcpad, "IMUAccelerometer/Down", "`Accel Down`");
                            ini.WriteValue(gcpad, "IMUAccelerometer/Left", "`Accel Left`");
                            ini.WriteValue(gcpad, "IMUAccelerometer/Right", "`Accel Right`");
                            ini.WriteValue(gcpad, "IMUAccelerometer/Forward", "`Accel Forward`");
                            ini.WriteValue(gcpad, "IMUAccelerometer/Backward", "`Accel Backward`");
                            ini.WriteValue(gcpad, "IMUGyroscope/Pitch Up", "`Gyro Pitch Up`");
                            ini.WriteValue(gcpad, "IMUGyroscope/Pitch Down", "`Gyro Pitch Down`");
                            ini.WriteValue(gcpad, "IMUGyroscope/Roll Left", "`Gyro Roll Left`");
                            ini.WriteValue(gcpad, "IMUGyroscope/Roll Right", "`Gyro Roll Right`");
                            ini.WriteValue(gcpad, "IMUGyroscope/Yaw Left", "`Gyro Yaw Left`");
                            ini.WriteValue(gcpad, "IMUGyroscope/Yaw Right", "`Gyro Yaw Right`");
                            ini.Remove(gcpad, "Tilt/Forward");
                            ini.Remove(gcpad, "Tilt/Left");
                            ini.Remove(gcpad, "Tilt/Right");
                            ini.Remove(gcpad, "Tilt/Backward");
                            ini.Remove(gcpad, "Shake/X");
                            ini.Remove(gcpad, "Shake/Y");
                            ini.Remove(gcpad, "Shake/Z");
                            ini.Remove(gcpad, "Swing/Down");
                            ini.Remove(gcpad, "Swing/Right");
                            ini.Remove(gcpad, "Swing/Up");
                            ini.Remove(gcpad, "Swing/Left");
                        }

                        // Hide wiimote cursor
                        if (Program.SystemConfig.getOptBoolean("wii_hidecursor"))
                            ini.WriteValue(gcpad, "IR/Auto-Hide", "True");
                        else
                            ini.WriteValue(gcpad, "IR/Auto-Hide", "False");

                        // Relative input for IR cursor
                        if (Program.SystemConfig.getOptBoolean("wii_relativecursor") || pad.Config.Type == "keyboard")
                            ini.WriteValue(gcpad, "IR/Relative Input", "False");
                        else
                            ini.WriteValue(gcpad, "IR/Relative Input", "True");
                    }

                    SimpleLogger.Instance.Info("[INFO] Assigned controller " + pad.DevicePath + " to player : " + pad.PlayerIndex.ToString());
                }

                ini.Save();
            }

            // Reset hotkeys
            string hotkeyini = Path.Combine(path, "User", "Config", "Hotkeys.ini");
            if (File.Exists(hotkeyini))
                ResetHotkeysToDefault(hotkeyini);
        }

        private static void ResetHotkeysToDefault(string iniFile)
        {
            if (Program.Controllers.Count == 0)
                return;

            bool forceSDL = false;
            if (Program.SystemConfig.isOptSet("input_forceSDL") && Program.SystemConfig.getOptBoolean("input_forceSDL"))
                forceSDL = true;

            using (IniFile ini = new IniFile(iniFile, IniOptions.UseSpaces))
            {
                var c1 = Program.Controllers.FirstOrDefault(c => c.PlayerIndex == 1);

                string tech = "XInput";
                string deviceName = "Gamepad";
                int xIndex = 0;
                bool xinputAsSdl = false;

                if (c1 != null && c1.IsXInputDevice)
                    xIndex = c1.XInput != null ? c1.XInput.DeviceIndex : c1.DeviceIndex;

                if (c1.Config.Type == "keyboard")
                {
                    tech = "DInput";
                    deviceName = "Keyboard Mouse";
                }
                else if (!c1.IsXInputDevice || forceSDL)
                {
                    if (c1.IsXInputDevice && forceSDL)
                    {
                        xinputAsSdl = true;
                    }

                    var s = c1.SdlController;
                    if (s != null)
                    {
                        tech = xinputAsSdl? "XInput" : "SDL";
                        deviceName = s.Name;
                    }

                    string newNamePath = Path.Combine(Program.AppConfig.GetFullPath("tools"), "controllerinfo.yml");
                    string newName = SdlJoystickGuid.GetNameFromFile(newNamePath, c1.Guid, "dolphin");

                    if (newName != null)
                        deviceName = newName;
                }

                var ssss = "@(" + (GetSDLMappingName(c1, InputKey.hotkey) ?? "") + "&" + (GetSDLMappingName(c1, InputKey.y) ?? "") + ")";

                ini.WriteValue("Hotkeys", "Load State/Load State Slot 1", "F1");
                ini.WriteValue("Hotkeys", "Save State/Save State Slot 1", "@(Shift+F1)");

                if (c1.Config.Type != "keyboard")
                {
                    if (xinputAsSdl)
                        ini.WriteValue("Hotkeys", "Device", "SDL" + "/" + _p1sdlindex + "/" + deviceName);
                    else
                        ini.WriteValue("Hotkeys", "Device", tech + "/" + xIndex + "/" + deviceName);
                    ini.WriteValue("Hotkeys", "General/Toggle Pause", c1.IsXInputDevice? "@(Back+`Button B`)" : "@(Back+`Button E`)");
                    ini.WriteValue("Hotkeys", "General/Toggle Fullscreen", c1.IsXInputDevice ? "@(Back+`Button A`)" : "@(Back+`Button S`)");
                    ini.WriteValue("Hotkeys", "General/Exit", "@(Back+Start)");

                    // SaveStates
                    ini.WriteValue("Hotkeys", "General/Take Screenshot", c1.IsXInputDevice ? "@(Back+`Button X`)" : "@(Back+`Button W`)"); // Use Same value as SaveState....
                    ini.WriteValue("Hotkeys", "Save State/Save to Selected Slot", c1.IsXInputDevice ? "@(Back+`Button X`)" : "@(Back+`Button W`)");
                    ini.WriteValue("Hotkeys", "Load State/Load from Selected Slot", c1.IsXInputDevice ? "@(Back+`Button Y`)" : "@(Back+`Button N`)");
                    ini.WriteValue("Hotkeys", "Other State Hotkeys/Increase Selected State Slot", "@(Back+`Pad N`)");
                    ini.WriteValue("Hotkeys", "Other State Hotkeys/Decrease Selected State Slot", "@(Back+`Pad S`)");

                    ini.WriteValue("Hotkeys", "Emulation Speed/Decrease Emulation Speed", "@(Back+`Shoulder L`)");
                    ini.WriteValue("Hotkeys", "Emulation Speed/Increase Emulation Speed", "@(Back+`Shoulder R`)");
                }
                else        // Keyboard
                {
                    ini.WriteValue("Hotkeys", "Device", "DInput/0/Keyboard Mouse");
                    ini.WriteValue("Hotkeys", "General/Toggle Pause", "`F10`");
                    ini.WriteValue("Hotkeys", "General/Toggle Fullscreen", "@(Alt+RETURN)");
                    ini.WriteValue("Hotkeys", "General/Exit", "ESCAPE");
                    ini.WriteValue("Hotkeys", "General/Take Screenshot", "`F9`");
                    ini.WriteValue("Hotkeys", "General/Eject Disc", "Alt+E");
                    ini.WriteValue("Hotkeys", "General/Change Disc", "Alt+S");
                }
            }
        }

        private static void SetWiimoteHotkeys(string iniFile)
        {
            using (IniFile ini = new IniFile(iniFile, IniOptions.UseSpaces))
            {
                ini.WriteValue("Hotkeys", "Device", "Bluetooth/0/Wii Remote");
                ini.WriteValue("Hotkeys", "General/Toggle Pause", "B&`-`");
                ini.WriteValue("Hotkeys", "General/Toggle Fullscreen", "A&`-`");
                ini.WriteValue("Hotkeys", "General/Exit", "HOME&`-`");

                // SaveStates
                ini.WriteValue("Hotkeys", "General/Take Screenshot", "`-`&`1`"); // Use Same value as SaveState....
                ini.WriteValue("Hotkeys", "Save State/Save to Selected Slot", "`-`&`1`");
                ini.WriteValue("Hotkeys", "Load State/Load from Selected Slot", "`-`&`2`");
                ini.WriteValue("Hotkeys", "Other State Hotkeys/Increase Selected State Slot", "Up&`-`");
                ini.WriteValue("Hotkeys", "Other State Hotkeys/Decrease Selected State Slot", "Down&`-`");
            }
        }

        private static void ConfigureGCAdapter(string gcpad, string guid, Controller pad, IniFile ini)
        {
            ini.WriteValue(gcpad, "Main Stick/Modifier/Range", "50.");
            ini.WriteValue(gcpad, "C-Stick/Modifier/Range", "50.");

            string deviceName = pad.DirectInput.Name;

            if (deviceName == null)
                return;
            
            string tech = "SDL";

            int padIndex = Program.Controllers.Where(g => g.GetSdlGuid(SdlVersion.SDL2_0_X).ToLowerInvariant() == guid).OrderBy(o => o.DeviceIndex).ToList().IndexOf(pad);

            ini.WriteValue(gcpad, "Device", tech + "/" + padIndex + "/" + pad.DirectInput.Name);

            Dictionary<string, string> buttons = gcAdapters[guid];

            foreach (var button in buttons)
                ini.WriteValue(gcpad, button.Key, button.Value); 
        }

        private static void RemoveControllerConfig_gamecube(string path, string filename)
        {
            string iniFile = Path.Combine(path, "User", "Config", filename);

            using (var ini = new IniFile(iniFile, IniOptions.UseSpaces))
            {
                for (int i = 0; i < 4; i++)
                    ini.WriteValue("Core", "SIDevice" + i, "0");
            }
        }

        private static string ToDolphinKey(long id)
        {
            if (id >= 97 && id <= 122)
            {
                List<int> azertyLayouts = new List<int>() { 1036, 2060, 3084, 5132, 4108 };
                if (azertyLayouts.Contains(CultureInfo.CurrentCulture.KeyboardLayoutId))
                {
                    if (id == 'a')
                        id = 'q';
                    else if (id == 'q')
                        id = 'a';
                    else if (id == 'w')
                        id = 'z';
                    else if (id == 'z')
                        id = 'w';
                }
                return ((char)id).ToString().ToUpper();
            }

            switch (id)
            {
                case 32: return "SPACE";
                case 13:
                case 0x4000009e: return "RETURN"; // Return2

                case 0x400000e1: return "LSHIFT"; // Shift = 
                case 0x400000e0: return "LCONTROL"; // Ctrl = 
                case 0x400000e2: return "LMENU"; // Alt = 

                case 0x4000004b: return "PRIOR"; // PageUp = ,
                case 0x4000004e: return "NEXT"; // PageDown = ,
                case 0x4000004d: return "END"; // End = ,
                case 0x4000004a: return "HOME"; // Home = ,
                case 0x40000050: return "LEFT"; // Left = ,
                case 0x40000052: return "UP"; // Up = ,
                case 0x4000004f: return "RIGHT"; // Right = ,
                case 0x40000051: return "DOWN"; // Down = 0x40000051,

                case 0x40000049: return "INSERT"; // Insert = 0x40000049,
                case 0x0000007f: return "DELETE"; // Delete = 0x0000007f,

                case 0x40000059: return "NUMPAD1";  //KP_1 = 0x40000059,
                case 0X4000005A: return "NUMPAD2";  //KP_2 = 0X4000005A,
                case 0x4000005b: return "NUMPAD3";  // KP_3 = ,
                case 0x4000005c: return "NUMPAD4"; // KP_4 = ,
                case 0x4000005d: return "NUMPAD5"; // KP_5 = ,
                case 0x4000005e: return "NUMPAD6"; // KP_6 = ,
                case 0x4000005f: return "NUMPAD7"; // KP_7 = ,
                case 0x40000060: return "NUMPAD8"; // KP_8 = ,
                case 0x40000061: return "NUMPAD9"; // KP_9 = ,
                case 0x40000062: return "NUMPAD0";  // KP_0 = 0x40000062,
                case 0x40000055: return "MULTIPLY"; // KP_Multiply
                case 0x40000057: return "ADD"; // KP_Plus
                case 0x40000056: return "SUBSTRACT"; // KP_Minus

                case 0x4000003a: return "F1"; // F1
                case 0x4000003b: return "F2"; // F2
                case 0x4000003c: return "F3"; // F3
                case 0x4000003d: return "F4"; // F4
                case 0x4000003e: return "F5"; // F5
                case 0x4000003f: return "F6"; // F6
                case 0x40000040: return "F7"; // F7
                case 0x40000041: return "F8"; // F8
                case 0x40000042: return "F9"; // F9
                case 0x40000043: return "F10"; // F10
                case 0x40000044: return "F11"; // F11
                case 0x40000045: return "F12"; // F12
                case 0x400000e6: return "RMENU"; // RightAlt
                case 0x400000e4: return "RCONTROL"; // RightCtrl
                case 0x400000e5: return "RSHIFT"; // RightShift
                case 0x40000058: return "NUMPADENTER"; // Kp_ENTER
                    /*        
                    KP_Period = 0x40000063,
                    KP_Divide = 0x40000054,

                    NumlockClear = 0x40000053,
                    ScrollLock = 0x40000047,                
                     * //Select = 0x40000077,
                    //PrintScreen = 0x40000046,
                    //LeftGui = 0x400000e3,
                    //RightGui = 0x400000e7,
                    //Application = 0x40000065,            
                    //Gui = 0x400000e3,
                    //Pause = 0x40000048,
                    //Capslock = 0x40000039,

                     */
            }

            return null;
        }

        static readonly Dictionary<string, string> realEmulatedWiimote = new Dictionary<string, string>()
        {
            { "Tilt/Modifier/Range", "50." },
            { "Nunchuk/Stick/Modifier/Range", "50." },
            { "Nunchuk/Tilt/Modifier/Range", "50." },
            { "Classic/Left Stick/Modifier/Range", "50." },
            { "Classic/Right Stick/Modifier/Range", "50." },
            { "Guitar/Stick/Modifier/Range", "50." },
            { "Drums/Stick/Modifier/Range", "50." },
            { "Turntable/Stick/Modifier/Range", "50." },
            { "uDraw/Stylus/Modifier/Range", "50." },
            { "Drawsome/Stylus/Modifier/Rangee", "50." },
            { "Buttons/A", "`A`" },
            { "Buttons/B", "`B`" },
            { "Buttons/1", "`1`" },
            { "Buttons/2", "`2`" },
            { "Buttons/-", "`-`" },
            { "Buttons/+", "`+`" },
            { "Buttons/Home", "`HOME`" },
            { "D-Pad/Up", "`Up`" },
            { "D-Pad/Down", "`Down`" },
            { "D-Pad/Left", "`Left`" },
            { "D-Pad/Right", "`Right`" },
            { "IMUAccelerometer/Up", "`Accel Up`" },
            { "IMUAccelerometer/Down", "`Accel Down`" },
            { "IMUAccelerometer/Left", "`Accel Left`" },
            { "IMUAccelerometer/Right", "`Accel Right`" },
            { "IMUAccelerometer/Forward", "`Accel Forward`" },
            { "IMUAccelerometer/Backward", "`Accel Backward`" },
            { "IMUGyroscope/Dead Zone", "3." },
            { "IMUGyroscope/Pitch Up", "`Gyro Pitch Up`" },
            { "IMUGyroscope/Pitch Down", "`Gyro Pitch Down`" },
            { "IMUGyroscope/Roll Left", "`Gyro Roll Left`" },
            { "IMUGyroscope/Roll Right", "`Gyro Roll Right`" },
            { "IMUGyroscope/Yaw Left", "`Gyro Yaw Left`" },
            { "IMUGyroscope/Yaw Right", "`Gyro Yaw Right`" },
            { "Extension/Attach MotionPlus", "`Attached MotionPlus`" },
            { "Nunchuk/Buttons/C", "`Nunchuk C`" },
            { "Nunchuk/Buttons/Z", "`Nunchuk Z`" },
            { "Nunchuk/Stick/Up", "`Nunchuk Y+`" },
            { "Nunchuk/Stick/Down", "`Nunchuk Y-`" },
            { "Nunchuk/Stick/Left", "`Nunchuk X-`" },
            { "Nunchuk/Stick/Right", "`Nunchuk X+`" },
            { "Nunchuk/Stick/Calibration", "100.00 100.00 100.00 100.00 100.00 100.00 100.00 100.00" },
            { "Nunchuk/IMUAccelerometer/Up", "`Nunchuk Accel Up`" },
            { "Nunchuk/IMUAccelerometer/Down", "`Nunchuk Accel Down`" },
            { "Nunchuk/IMUAccelerometer/Left", "`Nunchuk Accel Left`" },
            { "Nunchuk/IMUAccelerometer/Right", "`Nunchuk Accel Right`" },
            { "Nunchuk/IMUAccelerometer/Forward", "`Nunchuk Accel Forward`" },
            { "Nunchuk/IMUAccelerometer/Backward", "`Nunchuk Accel Backward`" },
            { "Classic/Buttons/A", "`Classic A`" },
            { "Classic/Buttons/B", "`Classic B`" },
            { "Classic/Buttons/X", "`Classic X`" },
            { "Classic/Buttons/Y", "`Classic Y`" },
            { "Classic/Buttons/ZL", "`Classic ZL`" },
            { "Classic/Buttons/ZR", "`Classic ZR`" },
            { "Classic/Buttons/-", "`Classic -`" },
            { "Classic/Buttons/+", "`Classic +`" },
            { "Classic/Buttons/Home", "`Classic HOME`" },
            { "Classic/Left Stick/Up", "`Classic Left Y+`" },
            { "Classic/Left Stick/Down", "`Classic Left Y-`" },
            { "Classic/Left Stick/Left", "`Classic Left X-`" },
            { "Classic/Left Stick/Right", "`Classic Left X+`" },
            { "Classic/Left Stick/Calibration", "100.00 100.00 100.00 100.00 100.00 100.00 100.00 100.00" },
            { "Classic/Right Stick/Up", "`Classic Right Y+`" },
            { "Classic/Right Stick/Down", "`Classic Right Y-`" },
            { "Classic/Right Stick/Left", "`Classic Right X-`" },
            { "Classic/Right Stick/Right", "`Classic Right X+`" },
            { "Classic/Right Stick/Calibration", "100.00 100.00 100.00 100.00 100.00 100.00 100.00 100.00" },
            { "Classic/Triggers/L", "`Classic L`" },
            { "Classic/Triggers/R", "`Classic R`" },
            { "Classic/Triggers/L-Analog", "`Classic L-Analog`" },
            { "Classic/Triggers/R-Analog", "`Classic R-Analog`" },
            { "Classic/D-Pad/Up", "`Classic Up`" },
            { "Classic/D-Pad/Down", "`Classic Down`" },
            { "Classic/D-Pad/Left", "`Classic Left`" },
            { "Classic/D-Pad/Right", "`Classic Right`" },
            { "Rumble/Motor", "`Motor`" },
            { "Options/Battery", "`Battery`" },
        };

        static readonly Dictionary<string, Dictionary<string, string>> gcAdapters = new Dictionary<string, Dictionary<string, string>>()
        {
            {
                "030000009b2800006500000000000000",
                new Dictionary<string, string>()
                {
                    { "Buttons/A", "`Button 0`" },
                    { "Buttons/B", "`Button 1`" },
                    { "Buttons/X", "`Button 7`" },
                    { "Buttons/Y", "`Button 8`" },
                    { "Buttons/Z", "`Button 2`" },
                    { "Buttons/Start", "`Button 3`" },
                    { "D-Pad/Up", "`Button 10`" },
                    { "D-Pad/Down", "`Button 11`" },
                    { "D-Pad/Left", "`Button 12`" },
                    { "D-Pad/Right", "`Button 13`" },
                    { "Main Stick/Up", "`Axis 1-`" },
                    { "Main Stick/Down", "`Axis 1+`" },
                    { "Main Stick/Left", "`Axis 0-`" },
                    { "Main Stick/Right", "`Axis 0+`" },
                    { "Main Stick/Calibration", "99.27 96.28 95.41 98.50 105.10 101.56 97.09 96.92 99.47 97.29 97.14 98.38 102.95 95.99 93.28 94.23 93.71 91.04 90.65 93.92 100.78 94.03 92.17 93.97 98.00 93.31 93.19 96.03 102.50 96.33 93.79 95.22" },
                    { "C-Stick/Up", "`Axis 4-`" },
                    { "C-Stick/Down", "`Axis 4+`" },
                    { "C-Stick/Left", "`Axis 3-`" },
                    { "C-Stick/Right", "`Axis 3+`" },
                    { "C-Stick/Calibration", "90.54 87.42 87.39 89.59 95.36 91.17 88.92 90.16 93.68 89.71 89.36 90.43 89.88 84.17 82.25 83.56 87.00 83.71 82.29 84.69 89.54 86.49 84.83 85.69 91.00 88.88 88.76 92.16 97.58 90.57 87.72 88.07" },
                    { "Triggers/L", "`Full Axis 5+`" },
                    { "Triggers/R", "`Full Axis 2+`" },
                    { "Triggers/L-Analog", "`Full Axis 5+`" },
                    { "Triggers/R-Analog", "`Full Axis 2+`" },
                    { "Triggers/Threshold", "85." },
                    { "Triggers/Dead Zone", "6." },
                    { "Rumble/Motor", "Constant" },
                }
            },

            {
                "03000000790000004318000000016800",
                new Dictionary<string, string>()
                {
                    { "Buttons/A", "`Button 1`" },
                    { "Buttons/B", "`Button 2`" },
                    { "Buttons/X", "`Button 0`" },
                    { "Buttons/Y", "`Button 3`" },
                    { "Buttons/Z", "`Button 7`" },
                    { "Buttons/Start", "`Button 9`" },
                    { "D-Pad/Up", "`Button 12`" },
                    { "D-Pad/Down", "`Button 14`" },
                    { "D-Pad/Left", "`Button 15`" },
                    { "D-Pad/Right", "`Button 13`" },
                    { "Main Stick/Up", "`Axis 1-`" },
                    { "Main Stick/Down", "`Axis 1+`" },
                    { "Main Stick/Left", "`Axis 0-`" },
                    { "Main Stick/Right", "`Axis 0+`" },
                    { "Main Stick/Calibration", "75.42 74.28 73.52 76.21 82.13 79.47 76.23 76.20 79.88 79.52 78.29 79.31 82.97 76.86 74.57 75.04 78.75 76.38 75.33 77.47 83.20 77.96 75.92 77.09 79.96 77.32 76.31 78.33 80.81 75.37 73.05 73.61" },
                    { "C-Stick/Up", "`Axis 2-`" },
                    { "C-Stick/Down", "`Axis 2+`" },
                    { "C-Stick/Left", "`Axis 5-`" },
                    { "C-Stick/Right", "`Axis 5+`" },
                    { "C-Stick/Calibration", "63.12 61.50 61.87 63.75 68.25 65.24 63.84 64.95 68.10 69.10 68.34 69.84 74.34 68.89 66.25 66.80 69.53 67.22 66.31 68.01 72.16 68.16 67.01 68.33 70.91 67.61 66.56 68.48 70.69 65.43 63.17 61.72" },
                    { "Triggers/L", "`Full Axis 3+`" },
                    { "Triggers/R", "`Full Axis 4+`" },
                    { "Triggers/L-Analog", "`Full Axis 3+`" },
                    { "Triggers/R-Analog", "`Full Axis 4+`" },
                    { "Triggers/Threshold", "85." },
                    { "Triggers/Dead Zone", "7." },
                    { "Rumble/Motor", "Motor" },
                }
            },
        };
    }
}
