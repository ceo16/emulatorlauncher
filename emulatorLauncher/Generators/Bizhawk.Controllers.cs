﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Diagnostics;
using System.Windows.Forms;
using System.Drawing;
using System.Management;
using emulatorLauncher.Tools;
using System.Reflection;
using System.Collections;
using ValveKeyValue;

namespace emulatorLauncher
{
    partial class BizhawkGenerator : Generator
    {
        private static List<string> systemMonoPlayer = new List<string>() { "apple2", "gb", "gbc", "gba", "lynx", "nds" };
        private static List<string> computersystem = new List<string>() { "apple2" };

        private static Dictionary<string, int> inputPortNb = new Dictionary<string, int>()
        {
            { "A26", 2 },
            { "A78", 2 },
            { "AppleII", 1 },
            { "Ares64", 4 },
            { "BSNES", 8 },
            { "Coleco", 2 },
            { "Faust", 8 },
            { "Gambatte", 1 },
            { "GBHawk", 1 },
            { "Genplus-gx", 8 },
            { "HyperNyma", 5 },
            { "Jaguar", 2 },
            { "Lynx", 1 },
            { "mGBA", 1 },
            { "melonDS", 1 },
            { "Mupen64Plus", 4 },
            { "NeoPop", 1 },
            { "NesHawk", 4 },
            { "PCEHawk", 5 },
            { "QuickNes", 2 },
            { "SameBoy", 1 },
            { "Saturnus", 12 },
            { "SMSHawk", 2 },
            { "Snes9x", 5 },
            { "TurboNyma", 5 }, 
        };

        private void CreateControllerConfiguration(DynamicJson json, string system, string core)
        {
            if (Program.SystemConfig.isOptSet("disableautocontrollers") && Program.SystemConfig["disableautocontrollers"] == "1")
                return;

            int maxPad = inputPortNb[core];

            if (!computersystem.Contains(system))
                ResetControllerConfiguration(json, maxPad, system, core);

            foreach (var controller in this.Controllers.OrderBy(i => i.PlayerIndex).Take(maxPad))
                ConfigureInput(controller, json, system, core);
        }

        private void ConfigureInput(Controller controller, DynamicJson json, string system, string core)
        {
            if (controller == null || controller.Config == null)
                return;

            if (computersystem.Contains(system))
                ConfigureKeyboardSystem(json, system);
            else if (controller.IsKeyboard)
                ConfigureKeyboard(controller, json, system, core);
            else
                ConfigureJoystick(controller, json, system, core);
        }

        private void ConfigureJoystick(Controller controller, DynamicJson json, string system, string core)
        {
            if (controller == null)
                return;

            var ctrlrCfg = controller.Config;
            if (ctrlrCfg == null)
                return;

            if (controller.DirectInput == null && controller.XInput == null)
                return;

            bool isXInput = controller.IsXInputDevice;
            bool monoplayer = systemMonoPlayer.Contains(system);
            var trollers = json.GetOrCreateContainer("AllTrollers");
            var controllerConfig = trollers.GetOrCreateContainer(systemController[system]);
            InputKeyMapping mapping = mappingToUse[system];

            int playerIndex = controller.PlayerIndex;
            int index = 1;

            if (!isXInput)
            {
                var list = new List<Controller>();
                foreach (var c in this.Controllers.Where(c => !c.IsKeyboard).OrderBy(c => c.DirectInput != null ? c.DirectInput.DeviceIndex : c.DeviceIndex))
                {
                    if (!c.IsXInputDevice)
                        list.Add(c);
                }
                index = list.IndexOf(controller) + 1;
            }
            else
            {
                var list = new List<Controller>();
                foreach (var c in this.Controllers.Where(c => !c.IsKeyboard).OrderBy(c => c.WinmmJoystick != null ? c.WinmmJoystick.Index : c.DeviceIndex))
                {
                    if (c.IsXInputDevice)
                        list.Add(c);
                }
                index = list.IndexOf(controller) + 1;
            }

            foreach (var x in mapping)
            {
                string value = x.Value;
                InputKey key = x.Key;

                if (!monoplayer)
                {
                    if (isXInput)
                    {
                        controllerConfig["P" + playerIndex + " " + value] = "X" + index + " " + GetXInputKeyName(controller, key);
                    }
                    else
                    {
                        controllerConfig["P" + playerIndex + " " + value] = "J" + index + " " + GetInputKeyName(controller, key);
                    }
                }
                else
                {
                    if (isXInput)
                    {
                        controllerConfig[value] = "X" + index + " " + GetXInputKeyName(controller, key);
                    }
                    else
                    {
                        controllerConfig[value] = "J" + index + " " + GetInputKeyName(controller, key);
                    }
                }
            }

            // Configure analog part of .ini
            var analog = json.GetOrCreateContainer("AllTrollersAnalog");
            var analogConfig = analog.GetOrCreateContainer(systemController[system]);

            if (system == "atari2600" || system == "atari7800")
            {
                controllerConfig["Reset"] = "";
                controllerConfig["Power"] = "";
                controllerConfig["Select"] = GetInputKeyName(controller, InputKey.start);
                controllerConfig["Toggle Left Difficulty"] = "";
                controllerConfig["Toggle Right Difficulty"] = "";

                if (system == "atari7800")
                {
                    controllerConfig["BW"] = "";
                    controllerConfig["Pause"] = GetInputKeyName(controller, InputKey.select);
                }
            }

            if (system == "colecovision")
            {
                for (int i = 1; i < 3; i++)
                {
                    controllerConfig["P" + i + " Key 0"] = "";
                    controllerConfig["P" + i + " Key 9"] = "";
                }
            }

            if (system == "jaguar")
            {
                for (int i = 1; i<3; i++)
                {
                    controllerConfig["P" + i + " 7"] = "";
                    controllerConfig["P" + i + " 8"] = "";
                    controllerConfig["P" + i + " 9"] = "";
                    controllerConfig["P" + i + " Asterisk"] = "";
                    controllerConfig["P" + i + " Pound"] = "";
                }
            }

            if (system == "jaguar")
                controllerConfig["Power"] = "";

            if (system == "n64")
            {
                var xAxis = analogConfig.GetOrCreateContainer("P" + playerIndex + " X Axis");
                var yAxis = analogConfig.GetOrCreateContainer("P" + playerIndex + " Y Axis");

                xAxis["Value"] = isXInput ? "X" + index + " LeftThumbX" : "J" + index + " X";
                xAxis.SetObject("Mult", 1.0);
                xAxis.SetObject("Deadzone", 0.1);

                yAxis["Value"] = isXInput ? "X" + index + " LeftThumbY" : "J" + index + " Y";
                yAxis.SetObject("Mult", 1.0);
                yAxis.SetObject("Deadzone", 0.1);
            }

            if (system == "nds")
            {
                var xAxis = analogConfig.GetOrCreateContainer("Touch X");
                var yAxis = analogConfig.GetOrCreateContainer("Touch Y");

                xAxis["Value"] = "WMouse X";
                xAxis.SetObject("Mult", 1.0);
                xAxis.SetObject("Deadzone", 0.0);

                yAxis["Value"] = "WMouse Y";
                yAxis.SetObject("Mult", 1.0);
                yAxis.SetObject("Deadzone", 0.0);

                controllerConfig["Touch"] = "WMouse L";
            }
        }

        private static void ConfigureKeyboard(Controller controller, DynamicJson json, string system, string core)
        {
            if (controller == null)
                return;

            InputConfig keyboard = controller.Config;
            if (keyboard == null)
                return;
            
            var trollers = json.GetOrCreateContainer("AllTrollers");
            var controllerConfig = trollers.GetOrCreateContainer(systemController[system]);
            var analog = json.GetOrCreateContainer("AllTrollersAnalog");
            var analogConfig = analog.GetOrCreateContainer(systemController[system]);
            bool monoplayer = systemMonoPlayer.Contains(system);

            var mapping = mappingToUse[system];

            foreach (var x in mapping)
            {
                string value = x.Value;
                var a = keyboard[x.Key];
                if (a != null)
                {
                    if (monoplayer)
                        controllerConfig[value] = SdlToKeyCode(a.Id);
                    else
                        controllerConfig["P1 " + value] = SdlToKeyCode(a.Id);
                }
            }

            if (system == "atari2600" || system == "atari7800")
            {
                controllerConfig["Reset"] = "";
                controllerConfig["Toggle Left Difficulty"] = "";
                controllerConfig["Toggle Right Difficulty"] = "";
                controllerConfig["Power"] = "";
                controllerConfig["Select"] = SdlToKeyCode(keyboard[InputKey.start].Id);

                if (system == "atari7800")
                {
                    controllerConfig["BW"] = "";
                    controllerConfig["Pause"] = SdlToKeyCode(keyboard[InputKey.select].Id);
                }
            }

            if (system == "colecovision")
            {
                controllerConfig["P1 Key 0"] = "Number0";
                controllerConfig["P1 Key 1"] = "Number1";
                controllerConfig["P1 Key 2"] = "Number2";
                controllerConfig["P1 Key 3"] = "Number3";
                controllerConfig["P1 Key 4"] = "Number4";
                controllerConfig["P1 Key 5"] = "Number5";
                controllerConfig["P1 Key 6"] = "Number6";
                controllerConfig["P1 Key 7"] = "Number7";
                controllerConfig["P1 Key 8"] = "Number8";
                controllerConfig["P1 Key 9"] = "Number9";
                controllerConfig["P1 Star"] = "Minus";
                controllerConfig["P1 Pound"] = "Plus";
                controllerConfig["P2 Star"] = "";
                controllerConfig["P2 Pound"] = "";
            }

            if (system == "jaguar")
            {
                controllerConfig["P1 0"] = "Number0";
                controllerConfig["P1 1"] = "Number1";
                controllerConfig["P1 2"] = "Number2";
                controllerConfig["P1 3"] = "Number3";
                controllerConfig["P1 4"] = "Number4";
                controllerConfig["P1 5"] = "Number5";
                controllerConfig["P1 6"] = "Number6";
                controllerConfig["P1 7"] = "Number7";
                controllerConfig["P1 8"] = "Number8";
                controllerConfig["P1 9"] = "Number9";
                controllerConfig["P1 Asterisk"] = "Minus";
                controllerConfig["P1 Pound"] = "Plus";
                controllerConfig["P2 7"] = "";
                controllerConfig["P2 8"] = "";
                controllerConfig["P2 9"] = "";
                controllerConfig["P2 Asterisk"] = "";
                controllerConfig["P2 Pound"] = "";
            }

            if (system == "nds")
            {
                var xAxis = analogConfig.GetOrCreateContainer("Touch X");
                var yAxis = analogConfig.GetOrCreateContainer("Touch Y");

                xAxis["Value"] = "WMouse X";
                xAxis.SetObject("Mult", 1.0);
                xAxis.SetObject("Deadzone", 0.0);

                yAxis["Value"] = "WMouse Y";
                yAxis.SetObject("Mult", 1.0);
                yAxis.SetObject("Deadzone", 0.0);

                controllerConfig["Touch"] = "WMouse L";
            }
        }

        private static void ConfigureKeyboardSystem(DynamicJson json, string system)
        {
            var trollers = json.GetOrCreateContainer("AllTrollers");
            var controllerConfig = trollers.GetOrCreateContainer(systemController[system]);

            Dictionary<string, string> kbmapping = null;
            
            if (system == "apple2")
                kbmapping = apple2Mapping;

            if (kbmapping == null)
                return;

            foreach (var x in kbmapping)
            {
                string value = x.Value;
                string key = x.Key;
                controllerConfig[key] = value;
            }
        }

        private static InputKeyMapping atariMapping = new InputKeyMapping()
        {
            { InputKey.up,              "Up"},
            { InputKey.down,            "Down"},
            { InputKey.left,            "Left" },
            { InputKey.right,           "Right"},
            { InputKey.a,               "Button" }
        };

        private static InputKeyMapping colecoMapping = new InputKeyMapping()
        {
            { InputKey.up,                  "Up"},
            { InputKey.down,                "Down"},
            { InputKey.left,                "Left" },
            { InputKey.right,               "Right"},
            { InputKey.a,                   "L" },
            { InputKey.b,                   "R" },
            { InputKey.x,                   "Key 1" },
            { InputKey.y,                   "Key 2" },
            { InputKey.pagedown,            "Key 3" },
            { InputKey.pageup,              "Key 4" },
            { InputKey.r2,                  "Key 5" },
            { InputKey.l2,                  "Key 6" },
            { InputKey.r3,                  "Key 7" },
            { InputKey.l3,                  "Key 8" },
            { InputKey.select,              "Star" },
            { InputKey.start,               "Pound" }
        };

        private static InputKeyMapping gbMapping = new InputKeyMapping()
        {
            { InputKey.up,              "Up"},
            { InputKey.down,            "Down"},
            { InputKey.left,            "Left" },
            { InputKey.right,           "Right"},
            { InputKey.start,           "Start" },
            { InputKey.select,          "Select" },
            { InputKey.a,               "B" },
            { InputKey.b,               "A" }
        };

        private static InputKeyMapping gbaMapping = new InputKeyMapping()
        {
            { InputKey.up,              "Up"},
            { InputKey.down,            "Down"},
            { InputKey.left,            "Left" },
            { InputKey.right,           "Right"},
            { InputKey.start,           "Start" },
            { InputKey.select,          "Select" },
            { InputKey.a,               "B" },
            { InputKey.b,               "A" },
            { InputKey.pageup,          "L" },
            { InputKey.pagedown,        "R" }
        };

        private static InputKeyMapping jaguarMapping = new InputKeyMapping()
        {
            { InputKey.up,                  "Up"},
            { InputKey.down,                "Down"},
            { InputKey.left,                "Left" },
            { InputKey.right,               "Right"},
            { InputKey.y,                   "A" },
            { InputKey.a,                   "B" },
            { InputKey.b,                   "C" },
            { InputKey.start,               "Option" },
            { InputKey.select,              "Pause" },
            { InputKey.x,                   "0" },
            { InputKey.pageup,              "1" },
            { InputKey.pagedown,            "2" },
            { InputKey.l2,                  "3" },
            { InputKey.r2,                  "4" },
            { InputKey.l3,                  "5" },
            { InputKey.r3,                  "6" }
        };

        private static InputKeyMapping lynxMapping = new InputKeyMapping()
        {
            { InputKey.up,                  "Up"},
            { InputKey.down,                "Down"},
            { InputKey.left,                "Left" },
            { InputKey.right,               "Right"},
            { InputKey.b,                   "A" },
            { InputKey.a,                   "B" },
            { InputKey.pageup,              "Option 1" },
            { InputKey.pagedown,            "Option 2" },
            { InputKey.start,               "Pause" }
        };

        private static InputKeyMapping mdMapping = new InputKeyMapping()
        {
            { InputKey.up,                  "Up"},
            { InputKey.down,                "Down"},
            { InputKey.left,                "Left" },
            { InputKey.right,               "Right"},
            { InputKey.y,                   "A" },
            { InputKey.a,                   "B" },
            { InputKey.b,                   "C" },
            { InputKey.start,               "Start" },
            { InputKey.pageup,              "X" },
            { InputKey.x,                   "Y" },
            { InputKey.pagedown,            "Z" },
            { InputKey.select,              "Mode" },
        };

        private static InputKeyMapping n64Mapping = new InputKeyMapping()
        {
            { InputKey.leftanalogup,        "A Up" },
            { InputKey.leftanalogdown,      "A Down" },
            { InputKey.leftanalogleft,      "A Left" },
            { InputKey.leftanalogright,     "A Right" },
            { InputKey.up,                  "DPad U"},
            { InputKey.down,                "DPad D"},
            { InputKey.left,                "DPad L" },
            { InputKey.right,               "DPad R"},
            { InputKey.start,               "Start" },
            { InputKey.r2,                  "Z" },
            { InputKey.y,                   "B" },
            { InputKey.a,                   "A" },
            { InputKey.rightanalogup,       "C Up" },
            { InputKey.rightanalogdown,     "C Down" },
            { InputKey.rightanalogleft,     "C Left" },
            { InputKey.rightanalogright,    "C Right" },
            { InputKey.pageup,              "L" },
            { InputKey.pagedown,            "R" }
        };

        private static InputKeyMapping ndsMapping = new InputKeyMapping()
        {
            { InputKey.b,                   "A" },
            { InputKey.a,                   "B" },
            { InputKey.x,                   "X" },
            { InputKey.y,                   "Y" },
            { InputKey.up,                  "Up"},
            { InputKey.down,                "Down"},
            { InputKey.left,                "Left" },
            { InputKey.right,               "Right"},
            { InputKey.pageup,              "L" },
            { InputKey.pagedown,            "R" },
            { InputKey.select,              "Select" },
            { InputKey.start,               "Start" },
        };

        private static InputKeyMapping nesMapping = new InputKeyMapping()
        {
            { InputKey.up,              "Up"},
            { InputKey.down,            "Down"},
            { InputKey.left,            "Left" },
            { InputKey.right,           "Right"},
            { InputKey.start,           "Start" },
            { InputKey.select,          "Select" },
            { InputKey.x,               "B" },
            { InputKey.a,               "A" }
        };

        private static InputKeyMapping ngpMapping = new InputKeyMapping()
        {
            { InputKey.up,              "Up"},
            { InputKey.down,            "Down"},
            { InputKey.left,            "Left" },
            { InputKey.right,           "Right"},
            { InputKey.b,               "B" },
            { InputKey.a,               "A" },
            { InputKey.start,           "Option"}
        };

        private static InputKeyMapping pceMapping = new InputKeyMapping()
        {
            { InputKey.up,              "Up"},
            { InputKey.down,            "Down"},
            { InputKey.left,            "Left" },
            { InputKey.right,           "Right"},
            { InputKey.a,               "I" },
            { InputKey.b,               "II" },
            { InputKey.y,               "III" },
            { InputKey.x,               "IV" },
            { InputKey.pageup,          "V" },
            { InputKey.pagedown,        "VI" },
            { InputKey.select,          "Select" },
            { InputKey.start,           "Run" },
            { InputKey.l2,              "Mode: Set 2-button" },
            { InputKey.r2,              "Mode: Set 6-button" },

        };

        private static InputKeyMapping saturnMapping = new InputKeyMapping()
        {
            { InputKey.up,                  "Up"},
            { InputKey.down,                "Down"},
            { InputKey.left,                "Left" },
            { InputKey.right,               "Right"},
            { InputKey.start,               "Start" },
            { InputKey.pageup,              "X" },
            { InputKey.x,                   "Y" },
            { InputKey.pagedown,            "Z" },
            { InputKey.y,                   "A" },
            { InputKey.a,                   "B" },
            { InputKey.b,                   "C" },
            { InputKey.l2,                  "L" },
            { InputKey.r2,                  "R" },
        };

        private static InputKeyMapping smsMapping = new InputKeyMapping()
        {
            { InputKey.up,              "Up"},
            { InputKey.down,            "Down"},
            { InputKey.left,            "Left" },
            { InputKey.right,           "Right"},
            { InputKey.a,               "B1" },
            { InputKey.b,               "B2" }
        };

        private static InputKeyMapping snesMapping = new InputKeyMapping()
        {
            { InputKey.up,              "Up"},
            { InputKey.down,            "Down"},
            { InputKey.left,            "Left" },
            { InputKey.right,           "Right"},
            { InputKey.start,           "Start" },
            { InputKey.select,          "Select" },
            { InputKey.a,               "B" },
            { InputKey.b,               "A" },
            { InputKey.x,               "X" },
            { InputKey.y,               "Y" },
            { InputKey.pageup,          "L" },
            { InputKey.pagedown,        "R" }
        };

        private static string GetXInputKeyName(Controller c, InputKey key)
        {
            Int64 pid = -1;

            bool revertAxis = false;
            key = key.GetRevertedAxis(out revertAxis);

            var input = c.Config[key];
            if (input != null)
            {
                if (input.Type == "button")
                {
                    pid = input.Id;
                    switch (pid)
                    {
                        case 0: return "A";
                        case 1: return "B";
                        case 2: return "Y";
                        case 3: return "X";
                        case 4: return "LeftShoulder";
                        case 5: return "RightShoulder";
                        case 6: return "Back";
                        case 7: return "Start";
                        case 8: return "LeftThumb";
                        case 9: return "RightThumb";
                        case 10: return "Guide";
                    }
                }

                if (input.Type == "axis")
                {
                    pid = input.Id;
                    switch (pid)
                    {
                        case 0:
                            if ((!revertAxis && input.Value > 0) || (revertAxis && input.Value < 0)) return "LStickRight";
                            else return "LStickLeft";
                        case 1:
                            if ((!revertAxis && input.Value > 0) || (revertAxis && input.Value < 0)) return "LStickDown";
                            else return "LStickUp";
                        case 2:
                            if ((!revertAxis && input.Value > 0) || (revertAxis && input.Value < 0)) return "RStickRight";
                            else return "RStickLeft";
                        case 3:
                            if ((!revertAxis && input.Value > 0) || (revertAxis && input.Value < 0)) return "RStickDown";
                            else return "RStickUp";
                        case 4: return "LeftTrigger";
                        case 5: return "RightTrigger";
                    }
                }

                if (input.Type == "hat")
                {
                    pid = input.Value;
                    switch (pid)
                    {
                        case 1: return "DpadUp";
                        case 2: return "DpadRight";
                        case 4: return "DpadDown";
                        case 8: return "DpadLeft";
                    }
                }
            }
            return "";
        }

        private static string GetInputKeyName(Controller c, InputKey key)
        {
            Int64 pid = -1;

            bool revertAxis = false;
            key = key.GetRevertedAxis(out revertAxis);

            var input = c.GetDirectInputMapping(key);
            if (input == null)
                return "\"\"";

            long nb = input.Id + 1;


            if (input.Type == "button")
                return ("B" + nb);

            if (input.Type == "hat")
            {
                pid = input.Value;
                switch (pid)
                {
                    case 1: return "POV1U";
                    case 2: return "POV1R";
                    case 4: return "POV1D";
                    case 8: return "POV1L";
                }
            }

            if (input.Type == "axis")
            {
                pid = input.Id;
                switch (pid)
                {
                    case 0:
                        if ((!revertAxis && input.Value > 0) || (revertAxis && input.Value < 0)) return "X+";
                        else return "X-";
                    case 1:
                        if ((!revertAxis && input.Value > 0) || (revertAxis && input.Value < 0)) return "Y+";
                        else return "Y-";
                    case 2:
                        if (c.VendorID == USB_VENDOR.SONY && ((!revertAxis && input.Value > 0) || (revertAxis && input.Value < 0))) return "Z+";
                        else if (c.VendorID == USB_VENDOR.SONY) return "Z-";
                        else if ((!revertAxis && input.Value > 0) || (revertAxis && input.Value < 0)) return "RotationX+";
                        else return "RotationX-";
                    case 3:
                        if (c.VendorID == USB_VENDOR.SONY && ((!revertAxis && input.Value > 0) || (revertAxis && input.Value < 0))) return "RotationX+";
                        else if (c.VendorID == USB_VENDOR.SONY) return "RotationX-";
                        else if ((!revertAxis && input.Value > 0) || (revertAxis && input.Value < 0)) return "RotationY+";
                        else return "RotationY-";
                    case 4:
                        if (c.VendorID == USB_VENDOR.SONY && ((!revertAxis && input.Value > 0) || (revertAxis && input.Value < 0))) return "RotationY+";
                        else if (c.VendorID == USB_VENDOR.SONY) return "RotationY-";
                        else if ((!revertAxis && input.Value > 0) || (revertAxis && input.Value < 0)) return "Z+";
                        else return "Z-";
                    case 5:
                        if ((!revertAxis && input.Value > 0) || (revertAxis && input.Value < 0)) return "RotationZ+";
                        else return "RotationZ-";
                }
            }
            return "";
        }

        private static Dictionary<string, string> systemController = new Dictionary<string, string>()
        {
            { "apple2", "Apple IIe Keyboard" },
            { "atari2600", "Atari 2600 Basic Controller" },
            { "atari7800", "Atari 7800 Basic Controller" },
            { "c64", "Commodore 64 Controller" },
            { "colecovision", "ColecoVision Basic Controller" },
            { "gamegear", "GG Controller" },
            { "gb", "Gameboy Controller" },
            { "gba", "GBA Controller" },
            { "jaguar", "Jaguar Controller" },
            { "lynx", "Lynx Controller" },
            { "mastersystem", "SMS Controller" },
            { "megadrive", "GPGX Genesis Controller" },
            { "n64", "Nintendo 64 Controller" },
            { "nds", "NDS Controller" },
            { "nes", "NES Controller" },
            { "ngp", "NeoGeo Portable Controller" },
            { "pcengine", "PC Engine Controller" },
            { "pcfx", "PC-FX Controller" },
            { "psx", "PSX Front Panel" },
            { "saturn", "Saturn Controller" },
            { "snes", "SNES Controller" },
            { "wswan", "WonderSwan Controller" },
            { "zxspectrum", "ZXSpectrum Controller" },
        };

        private static Dictionary<string, InputKeyMapping> mappingToUse = new Dictionary<string, InputKeyMapping>()
        {
            { "atari2600", atariMapping },
            { "atari7800", atariMapping },
            { "colecovision", colecoMapping },
            { "gb", gbMapping },
            { "gba", gbaMapping },
            { "gbc", gbMapping },
            { "jaguar", jaguarMapping },
            { "lynx", lynxMapping },
            { "mastersystem", smsMapping },
            { "megadrive", mdMapping },
            { "n64", n64Mapping },
            { "nds", ndsMapping },
            { "nes", nesMapping },
            { "ngp", ngpMapping },
            { "pcengine", pceMapping },
            { "saturn", saturnMapping },
            { "snes", snesMapping },
        };

        private void ResetControllerConfiguration(DynamicJson json, int totalNB, string system, string core)
        {
            bool monoplayer = systemMonoPlayer.Contains(system);
            InputKeyMapping mapping = mappingToUse[system];

            var trollers = json.GetOrCreateContainer("AllTrollers");
            var controllerConfig = trollers.GetOrCreateContainer(systemController[system]);

            if (monoplayer)
            {
                foreach (var x in mapping)
                {
                    string value = x.Value;
                    InputKey key = x.Key;
                    controllerConfig[value] = "";
                }
            }

            else
            {
                for (int i = 1; i < totalNB; i++)
                {
                    foreach (var x in mapping)
                    {
                        string value = x.Value;
                        InputKey key = x.Key;
                        controllerConfig["P" + i + " " + value] = "";
                    }
                }
            }
        }

        private static string SdlToKeyCode(long sdlCode)
        {
            switch (sdlCode)
            {
                case 0x0D: return "Enter";
                case 0x08: return "Backspace";
                case 0x09: return "Tab";
                case 0x20: return "Space";
                case 0x27: return "Apostrophe";
                case 0x2B: return "Plus";
                case 0x2C: return "Comma";
                case 0x2D: return "Minus";
                case 0x2E: return "Period";
                case 0x2F: return "Slash";
                case 0x30: return "Number0";
                case 0x31: return "Number1";
                case 0x32: return "Number2";
                case 0x33: return "Number3";
                case 0x34: return "Number4";
                case 0x35: return "Number5";
                case 0x36: return "Number6";
                case 0x37: return "Number7";
                case 0x38: return "Number8";
                case 0x39: return "Number9";
                case 0x3A: return "Semicolon";
                case 0x3B: return "Semicolon";
                case 0x3C: return "Comma";
                case 0x3D: return "Equal";
                case 0x3F: return "Period";
                case 0x5B: return "LeftBracket";
                case 0x5C: return "Backslash";
                case 0x5D: return "RightBracket";
                case 0x5F: return "Minus";
                case 0x60: return "Apostrophe";
                case 0x61: return "A";
                case 0x62: return "B";
                case 0x63: return "C";
                case 0x64: return "D";
                case 0x65: return "E";
                case 0x66: return "F";
                case 0x67: return "G";
                case 0x68: return "H";
                case 0x69: return "I";
                case 0x6A: return "J";
                case 0x6B: return "K";
                case 0x6C: return "L";
                case 0x6D: return "M";
                case 0x6E: return "N";
                case 0x6F: return "O";
                case 0x70: return "P";
                case 0x71: return "Q";
                case 0x72: return "R";
                case 0x73: return "S";
                case 0x74: return "T";
                case 0x75: return "U";
                case 0x76: return "V";
                case 0x77: return "W";
                case 0x78: return "X";
                case 0x79: return "Y";
                case 0x7A: return "Z";
                case 0x7F: return "Delete";
                case 0x40000039: return "CapsLock";
                case 0x4000003A: return "F1";
                case 0x4000003B: return "F2";
                case 0x4000003C: return "F3";
                case 0x4000003D: return "F4";
                case 0x4000003E: return "F5";
                case 0x4000003F: return "F6";
                case 0x40000040: return "F7";
                case 0x40000041: return "F8";
                case 0x40000042: return "F9";
                case 0x40000043: return "F10";
                case 0x40000044: return "F11";
                case 0x40000045: return "F12";
                case 0x40000047: return "ScrollLock";
                case 0x40000048: return "Pause";
                case 0x40000049: return "Insert";
                case 0x4000004A: return "Home";
                case 0x4000004B: return "PageUp";
                case 0x4000004D: return "End";
                case 0x4000004E: return "PageDown";
                case 0x4000004F: return "Right";
                case 0x40000050: return "Left";
                case 0x40000051: return "Down";
                case 0x40000052: return "Up";
                case 0x40000053: return "NumLock";
                case 0x40000054: return "KeypadDivide";
                case 0x40000055: return "KeypadMultiply";
                case 0x40000056: return "KeypadSubtract";
                case 0x40000057: return "KeypadAdd";
                case 0x40000058: return "KeypadEnter";
                case 0x40000059: return "Keypad1";
                case 0x4000005A: return "Keypad2";
                case 0x4000005B: return "Keypad3";
                case 0x4000005C: return "Keypad4";
                case 0x4000005D: return "Keypad5";
                case 0x4000005E: return "Keypad6";
                case 0x4000005F: return "Keypad7";
                case 0x40000060: return "Keypad8";
                case 0x40000061: return "Keypad9";
                case 0x40000062: return "Keypad0";
                case 0x40000063: return "KeypadDecimal";
                case 0x40000067: return "KeypadEquals";
                case 0x40000068: return "F13";
                case 0x40000069: return "F14";
                case 0x4000006A: return "F15";
                case 0x4000006B: return "F16";
                case 0x4000006C: return "F17";
                case 0x4000006D: return "F18";
                case 0x4000006E: return "F19";
                case 0x4000006F: return "F20";
                case 0x40000070: return "F21";
                case 0x40000071: return "F22";
                case 0x40000072: return "F23";
                case 0x40000073: return "F24";
                case 0x4000007F: return "Volume Mute";
                case 0x40000080: return "Volume Up";
                case 0x40000081: return "Volume Down";
                case 0x40000085: return "KeypadDecimal";
                case 0x400000E0: return "Ctrl";
                case 0x400000E1: return "Shift";
                case 0x400000E2: return "Alt";
                case 0x400000E4: return "Ctrl";
                case 0x400000E5: return "Shift";
                case 0x400000E6: return "Alt";
            }
            return "";
        }

        private static Dictionary<string, string> apple2Mapping = new Dictionary<string, string>()
        {
            { "Delete", "Delete" },
            { "Left", "Left" },
            { "Tab", "Tab" },
            { "Down", "Down" },
            { "Up", "Up" },
            { "Return", "Enter" },
            { "Right", "Right" },
            { "Escape", "Escape" },
            { "Space", "Space" },
            { "'", "Apostrophe" },
            { ",", "Comma" },
            { "-", "Minus" },
            { ".", "Period" },
            { "/", "Slash" },
            { "0", "Number0" },
            { "1", "Number1" },
            { "2", "Number2"},
            { "3", "Number3" },
            { "4", "Number4" },
            { "5", "Number5" },
            { "6", "Number6" },
            { "7", "Number7" },
            { "8", "Number8" },
            { "9", "Number9" },
            { ";", "Semicolon" },
            { "=", "Equals" },
            { "[", "LeftBracket" },
            { "\\", "Backslash" },
            { "]", "RightBracket" },
            { "`", "Backtick" },
            { "A", "A" },
            { "B", "B" },
            { "C", "C" },
            { "D", "D" },
            { "E", "E" },
            { "F", "F" },
            { "G", "G" },
            { "H", "H" },
            { "I", "I" },
            { "J", "J" },
            { "K", "K" },
            { "L", "L" },
            { "M", "M" },
            { "N", "N" },
            { "O", "O" },
            { "P", "P" },
            { "Q", "Q" },
            { "R", "R" },
            { "S", "S" },
            { "T", "T" },
            { "U", "U" },
            { "V", "V" },
            { "W", "W" },
            { "X", "X" },
            { "Y", "Y" },
            { "Z", "Z" },
            { "Control", "Ctrl" },
            { "Shift", "Shift" },
            { "Caps Lock", "CapsLock" },
            { "White Apple", "Home" },
            { "Black Apple", "End" },
            { "Previous Disk", "PageUp" },
            { "Next Disk", "PageDown" }
        };
    }
}