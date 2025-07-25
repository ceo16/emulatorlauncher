﻿using EmulatorLauncher.Common.FileFormats;
using System.Linq;
using System;
using EmulatorLauncher.Common.EmulationStation;
using System.Collections.Generic;
using System.IO;
using EmulatorLauncher.Common.Joysticks;
using EmulatorLauncher.Common;

namespace EmulatorLauncher
{
    partial class BigPEmuGenerator : Generator
    {
        private bool _teamTapRight = false;
        private bool _useSdl;
        private void ConfigureControllers(DynamicJson json)
        {
            if (Program.SystemConfig.isOptSet("disableautocontrollers") && Program.SystemConfig["disableautocontrollers"] == "1")
            {
                SimpleLogger.Instance.Info("[INFO] Auto controller configuration disabled.");
                return;
            }

            SimpleLogger.Instance.Info("[INFO] Creating controller configuration for BigPEmu");

            _useSdl = !SystemConfig.isOptSet("bigpemu_sdl") || SystemConfig["bigpemu_sdl"] == "1";

            // Set input plugin to dInput
            json["InputPlugin"] = _useSdl? "BigPEmu_Various_SDL2" : "BigPEmu_Input_DirectInput";

            var input = json.GetOrCreateContainer("Input");

            // clear existing device sections of file
            CleanInputConfig(input);

            // Multitap
            int maxPad = 2;
            
            if (SystemConfig.isOptSet("bigpemu_multitap") && !string.IsNullOrEmpty(SystemConfig["bigpemu_multitap"]))
            {
                switch (SystemConfig["bigpemu_multitap"])
                {
                    case "none":
                        maxPad = 2;
                        break;
                    case "port1":
                        maxPad = 5;
                        break;
                    case "port2":
                        maxPad = 5;
                        _teamTapRight = true;
                        break;
                    case "both":
                        maxPad = 8;
                        break;
                }
            }

            // Set devicecount
            if (Controllers.Count < maxPad)
                input["DeviceCount"] = Controllers.Count.ToString();
            else
                input["DeviceCount"] = maxPad.ToString();

            // Analog deadzone
            BindFeatureSlider(input, "AnalDeadMice", "bigpemu_deadzone", "0.15", 2);

            //Inject controllers                
            foreach (var controller in this.Controllers.OrderBy(i => i.PlayerIndex).Take(maxPad))
                ConfigureInput(input, controller);

            input["InputVer"] = "2";

            if (_useSdl)
                input["InputPluginVer"] = "666";
            else
                input["InputPluginVer"] = "0";

        }
        private void ConfigureInput(DynamicJson input, Controller controller)
        {
            if (controller == null || controller.Config == null)
                return;

            if (controller.IsKeyboard)
                ConfigureKeyboard(input, controller.Config);
            else
                ConfigureJoystick(input, controller, controller.PlayerIndex);
        }

        private void ConfigureKeyboard(DynamicJson input, InputConfig keyboard)
        {
            if (keyboard == null)
                return;

            Dictionary<int, string> triggerList = new Dictionary<int, string>();
            var device = input.GetOrCreateContainer("Device0");
            device["DeviceType"] = "0";         // Check other device types for future use
            device.Remove("Bindings");

            string button_c = GetKeyboardCode(keyboard, InputKey.y);
            string button_b = GetKeyboardCode(keyboard, InputKey.a);
            string button_a = GetKeyboardCode(keyboard, InputKey.b);
            string pause = GetKeyboardCode(keyboard, InputKey.select);
            string option = GetKeyboardCode(keyboard, InputKey.start);
            string up = GetKeyboardCode(keyboard, InputKey.up);
            string down = GetKeyboardCode(keyboard, InputKey.down);
            string left = GetKeyboardCode(keyboard, InputKey.left);
            string right = GetKeyboardCode(keyboard, InputKey.right);

            triggerList.Add(1, button_c != null ? button_c : (_useSdl? "4" : "30"));
            triggerList.Add(2, button_b != null ? button_b : (_useSdl? "22" : "31"));
            triggerList.Add(3, button_a != null ? button_a : (_useSdl ? "7" : "32"));
            triggerList.Add(4, pause != null ? pause : (_useSdl ? "20" : "16"));
            triggerList.Add(5, option != null ? option : (_useSdl ? "26" : "17"));
            triggerList.Add(6, up != null ? up : (_useSdl ? "82" : "200"));
            triggerList.Add(7, down != null ? down : (_useSdl ? "81" : "208"));
            triggerList.Add(8, left != null ? left : (_useSdl ? "80" : "203"));
            triggerList.Add(9, right != null ? right : (_useSdl ? "79" : "205"));
            triggerList.Add(10, _useSdl ? "39" : "11");
            triggerList.Add(11, _useSdl ? "30" : "2");
            triggerList.Add(12, _useSdl ? "31" : "3");
            triggerList.Add(13, _useSdl ? "32" : "4");
            triggerList.Add(14, _useSdl ? "33" : "5");
            triggerList.Add(15, _useSdl ? "34" : "6");
            triggerList.Add(16, _useSdl ? "35" : "7");
            triggerList.Add(17, _useSdl ? "36" : "8");
            triggerList.Add(18, _useSdl ? "37" : "9");
            triggerList.Add(19, _useSdl ? "38" : "10");
            triggerList.Add(20, _useSdl ? "18" : "24");
            triggerList.Add(21, _useSdl ? "19" : "25");
            triggerList.Add(22, null);
            triggerList.Add(23, null);
            triggerList.Add(24, null);
            triggerList.Add(25, null);
            triggerList.Add(26, null);
            triggerList.Add(27, null);
            triggerList.Add(28, null);
            triggerList.Add(29, null);
            triggerList.Add(30, null);
            triggerList.Add(31, null);
            triggerList.Add(32, null);
            triggerList.Add(33, null);
            triggerList.Add(34, null);
            triggerList.Add(35, null);
            triggerList.Add(36, null);
            triggerList.Add(37, null);
            triggerList.Add(38, _useSdl ? "41" : "1");
            triggerList.Add(39, _useSdl ? "59" : "60");
            triggerList.Add(40, _useSdl ? "58" : "59");
            triggerList.Add(41, _useSdl ? "60" : "61");
            triggerList.Add(42, _useSdl ? "61" : "62");
            triggerList.Add(43, _useSdl ? "62" : "63");
            triggerList.Add(44, null);
            triggerList.Add(45, _useSdl ? "23" : "20");
            triggerList.Add(46, null);
            triggerList.Add(47, null);
            triggerList.Add(48, null);
            triggerList.Add(49, null);
            triggerList.Add(50, null);

            var bindings = new List<DynamicJson>();

            foreach (var x in triggerList)
            {
                var triggerSection = new DynamicJson();
                var triggers = new List<DynamicJson>();
                var buttonBinding = new DynamicJson();
                if (x.Value != null)
                {
                    buttonBinding["B_KB"] = "true";
                    buttonBinding["B_ID"] = x.Value;
                    buttonBinding["B_AH"] = "0.0";
                }
                
                triggers.Add(buttonBinding);
                triggerSection.SetObject("Triggers", triggers);
                bindings.Add(triggerSection);
            }
            device.SetObject("Bindings", bindings);
        }

        private void ConfigureJoystick(DynamicJson input, Controller ctrl, int playerindex)
        {
            if (ctrl == null || ctrl.DirectInput == null)
                return;

            InputConfig joy = ctrl.Config;
            if (joy == null)
                return;

            // Define if the controller has a hat
            bool useHat = true;
            if (SystemConfig.getOptBoolean("bigpemu_analogstick"))
                useHat = false;

            // Get controller mapping in Gamecontrollerdb
            string gamecontrollerDB = Path.Combine(AppConfig.GetFullPath("tools"), "gamecontrollerdb.txt");
            string shortGuid = (ctrl.Guid.ToString()).Substring(0, 24) + "00000000";
            SdlToDirectInput sdlController = null;

            if (!_useSdl)
            {
                SimpleLogger.Instance.Info("[INFO] Player " + ctrl.PlayerIndex + ". Fetching gamecontrollerdb.txt file with guid : " + shortGuid);

                try { sdlController = GameControllerDBParser.ParseByGuid(gamecontrollerDB, shortGuid); }
                catch { }

                if (sdlController == null || sdlController.ButtonMappings.Count == 0)
                {
                    SimpleLogger.Instance.Info("[INFO] Player " + ctrl.PlayerIndex + ". No mapping found for the controller, autoconfiguration not possible.");
                    return;
                }
            }

            // Compute the Guid used by BigPEmu
            string instanceGuid = ctrl.DirectInput.InstanceGuid.ToString().Replace("-", "");
            Guid tempGuid = new Guid(instanceGuid);
            byte[] bytes = tempGuid.ToByteArray();
            string guid = BitConverter.ToString(bytes).Replace("-", "").ToUpperInvariant();

            if (_useSdl && ctrl.SdlController != null)
            {
                Dictionary<string, int> double_pads = new Dictionary<string, int>();

                foreach (var c in this.Controllers.OrderBy(j => j.DeviceIndex))
                {
                    if (c.SdlController != null)
                    {
                        string sdlGuid = c.Guid.ToString();
                        if (double_pads.ContainsKey(sdlGuid))
                            double_pads[sdlGuid] += 1;
                        else
                            double_pads[sdlGuid] = 0;
                    }
                }

                int nsamePad = 0;
                if (double_pads.ContainsKey(ctrl.Guid.ToString()) && double_pads[ctrl.Guid.ToString()] > 0)
                {
                    List<Controller> samePadList = this.Controllers.Where(c => c.Guid.ToString() == ctrl.Guid.ToString()).OrderBy(c => c.DeviceIndex).ToList();
                    nsamePad = samePadList.IndexOf(ctrl);
                }

                guid = ctrl.SdlController.Guid.ToString().ToUpperInvariant();

                string newGuidPath = Path.Combine(AppConfig.GetFullPath("tools"), "controllerinfo.yml");
                string newGuid = SdlJoystickGuid.GetGuidFromFile(newGuidPath, ctrl.SdlController, ctrl.Guid, "bigpemu");
                if (newGuid != null)
                    guid = newGuid.ToUpperInvariant();

                if (nsamePad > 0)
                {
                    if (guid.Length > 21)
                    {
                        char[] chars = guid.ToCharArray();
                        chars[21] = nsamePad.ToString()[0];
                        guid = new string(chars).ToUpperInvariant();
                    }
                }
            }

            // Define Device section
            string deviceNb = "Device" + (playerindex - 1);
            if (_teamTapRight)
            {
                switch (playerindex)
                {
                    case 1:
                        deviceNb = "Device0";
                        break;
                    case 2:
                        deviceNb = "Device5";
                        break;
                    case 3:
                        deviceNb = "Device6";
                        break;
                    case 4:
                        deviceNb = "Device7";
                        break;
                    case 5:
                        deviceNb = "Device1";
                        break;
                    case 6:
                        deviceNb = "Device2";
                        break;
                    case 7:
                        deviceNb = "Device3";
                        break;
                    case 8:
                        deviceNb = "Device4";
                        break;
                }
            }

            var device = input.GetOrCreateContainer(deviceNb);
            var bindings = new List<DynamicJson>();

            // Clean it up
            device.Remove("Bindings");

            // List all buttons in dictionary
            Dictionary<int, string> triggerList = new Dictionary<int, string>
            {
                { 1, "C" },
                { 2, "B" },
                { 3, "A" },
                { 4, "pause" },
                { 5, "option" },
                { 6, "up" },
                { 7, "down" },
                { 8, "left" },
                { 9, "right" },
                { 10, "0" },
                { 11, "1" },
                { 12, "2" },
                { 13, "3" },
                { 14, "4" },
                { 15, "5" },
                { 16, "6" },
                { 17, "7" },
                { 18, "8" },
                { 19, "9" },
                { 20, "asterisk" },
                { 21, "pound" },
                { 22, "a0left" },
                { 23, "a0right" },
                { 24, "a0up" },
                { 25, "a0down" },
                { 26, "a1left" },
                { 27, "a1right" },
                { 28, "a1up" },
                { 29, "a1down" },
                { 30, "extraup" },
                { 31, "extradown" },
                { 32, "extraleft" },
                { 33, "extraright" },
                { 34, "extra_a" },
                { 35, "extra_b" },
                { 36, "extra_c" },
                { 37, "extra_d" },
                { 38, "menu" },
                { 39, "ff" },
                { 40, "rewind" },
                { 41, "savestate" },
                { 42, "loadstate" },
                { 43, "screenshot" },
                { 44, "overlay" },
                { 45, "chat" },
                { 46, null },
                { 47, null },
                { 48, null },
                { 49, null },
                { 50, null }
            };

            bool hotkey = true;
            if (!_useSdl)
            {
                if (!sdlController.ButtonMappings.ContainsKey("lefttrigger"))
                    hotkey = false;
            }
            else if (ctrl.Config[InputKey.l2] == null)
                hotkey = false;


            foreach (var x in triggerList)
            {
                var triggerSection = new DynamicJson();
                var triggers = new List<DynamicJson>();
                
                // Add Keyboard bindings
                var kbBinding = new DynamicJson();
                if (x.Value != null && padAndKB.Contains(x.Value))
                {
                    string kbKey = GetkbKey(x.Value);

                    if (kbKey != null)
                    {
                        kbBinding["B_KB"] = "true";
                        kbBinding["B_ID"] = kbKey;
                        kbBinding["B_AH"] = "0.0";
                    }

                    triggers.Add(kbBinding);
                }

                // Add joy bindings
                var joyBinding = new DynamicJson();
                if (x.Value != null)
                {
                    switch (x.Value)
                    {
                        case "C":
                            string button_c = _useSdl? GetSDLInputMapping(ctrl, InputKey.y, useHat) : GetDinputMapping(sdlController, "x", useHat);
                            if (button_c != null)
                            {
                                string button_c_id = button_c.Split('_')[0];
                                string button_c_ah = button_c.Split('_')[1];
                                joyBinding["B_KB"] = "false";
                                joyBinding["B_ID"] = button_c_id;
                                joyBinding["B_AH"] = button_c_ah?? "0.0";
                                joyBinding["B_DevID"] = guid;
                            }
                            break;

                        case "B":
                            string button_b = _useSdl ? GetSDLInputMapping(ctrl, InputKey.a, useHat) : GetDinputMapping(sdlController, "a", useHat);
                            if (button_b != null)
                            {
                                string button_b_id = button_b.Split('_')[0];
                                string button_b_ah = button_b.Split('_')[1];
                                joyBinding["B_KB"] = "false";
                                joyBinding["B_ID"] = button_b_id;
                                joyBinding["B_AH"] = button_b_ah ?? "0.0";
                                joyBinding["B_DevID"] = guid;
                            }
                            break;
                        
                        case "A":
                            string button_a = _useSdl ? GetSDLInputMapping(ctrl, InputKey.b, useHat) : GetDinputMapping(sdlController, "b", useHat);
                            if (button_a != null)
                            {
                                string button_a_id = button_a.Split('_')[0];
                                string button_a_ah = button_a.Split('_')[1];
                                joyBinding["B_KB"] = "false";
                                joyBinding["B_ID"] = button_a_id;
                                joyBinding["B_AH"] = button_a_ah ?? "0.0";
                                joyBinding["B_DevID"] = guid;
                            }
                            break;

                        case "pause":
                            string pause = _useSdl ? GetSDLInputMapping(ctrl, InputKey.select, useHat) : GetDinputMapping(sdlController, "back", useHat);
                            if (pause != null)
                            {
                                string pause_id = pause.Split('_')[0];
                                string pause_ah = pause.Split('_')[1];
                                joyBinding["B_KB"] = "false";
                                joyBinding["B_ID"] = pause_id;
                                joyBinding["B_AH"] = pause_ah ?? "0.0";
                                joyBinding["B_DevID"] = guid;
                            }
                            break;

                        case "option":
                            string option = _useSdl ? GetSDLInputMapping(ctrl, InputKey.start, useHat) : GetDinputMapping(sdlController, "start", useHat);
                            if (option != null)
                            {
                                string option_id = option.Split('_')[0];
                                string option_ah = option.Split('_')[1];
                                joyBinding["B_KB"] = "false";
                                joyBinding["B_ID"] = option_id;
                                joyBinding["B_AH"] = option_ah ?? "0.0";
                                joyBinding["B_DevID"] = guid;
                            }
                            break;

                        case "up":
                            string up = _useSdl ? GetSDLInputMapping(ctrl, InputKey.up, useHat) : GetDinputMapping(sdlController, "dpup", useHat);
                            if (up != null)
                            {
                                string up_id = up.Split('_')[0];
                                string up_ah = up.Split('_')[1];
                                joyBinding["B_KB"] = "false";
                                joyBinding["B_ID"] = up_id;
                                joyBinding["B_AH"] = up_ah ?? "0.0";
                                joyBinding["B_DevID"] = guid;
                            }
                            break;

                        case "down":
                            string down = _useSdl ? GetSDLInputMapping(ctrl, InputKey.down, useHat) : GetDinputMapping(sdlController, "dpdown", useHat);
                            if (down != null)
                            {
                                string down_id = down.Split('_')[0];
                                string down_ah = down.Split('_')[1];
                                joyBinding["B_KB"] = "false";
                                joyBinding["B_ID"] = down_id;
                                joyBinding["B_AH"] = down_ah ?? "0.0";
                                joyBinding["B_DevID"] = guid;
                            }
                            break;

                        case "left":
                            string left = _useSdl ? GetSDLInputMapping(ctrl, InputKey.left, useHat) : GetDinputMapping(sdlController, "dpleft", useHat);
                            if (left != null)
                            {
                                string left_id = left.Split('_')[0];
                                string left_ah = left.Split('_')[1];
                                joyBinding["B_KB"] = "false";
                                joyBinding["B_ID"] = left_id;
                                joyBinding["B_AH"] = left_ah ?? "0.0";
                                joyBinding["B_DevID"] = guid;
                            }
                            break;

                        case "right":
                            string right = _useSdl ? GetSDLInputMapping(ctrl, InputKey.right, useHat) : GetDinputMapping(sdlController, "dpright", useHat);
                            if (right != null)
                            {
                                string right_id = right.Split('_')[0];
                                string right_ah = right.Split('_')[1];
                                joyBinding["B_KB"] = "false";
                                joyBinding["B_ID"] = right_id;
                                joyBinding["B_AH"] = right_ah ?? "0.0";
                                joyBinding["B_DevID"] = guid;
                            }
                            break;

                        case "0":
                            string zero = _useSdl ? GetSDLInputMapping(ctrl, InputKey.rightanalogdown, useHat) : GetDinputMapping(sdlController, "righty", useHat, 1);
                            if (zero != null)
                            {
                                string zero_id = zero.Split('_')[0];
                                string zero_ah = zero.Split('_')[1];
                                joyBinding["B_KB"] = "false";
                                joyBinding["B_ID"] = zero_id;
                                joyBinding["B_AH"] = zero_ah ?? "0.0";
                                joyBinding["B_DevID"] = guid;
                            }
                            break;

                        case "1":
                            string one = _useSdl ? GetSDLInputMapping(ctrl, InputKey.pageup, useHat) : GetDinputMapping(sdlController, "leftshoulder", useHat);
                            if (one != null)
                            {
                                string one_id = one.Split('_')[0];
                                string one_ah = one.Split('_')[1];
                                joyBinding["B_KB"] = "false";
                                joyBinding["B_ID"] = one_id;
                                joyBinding["B_AH"] = one_ah ?? "0.0";
                                joyBinding["B_DevID"] = guid;
                            }
                            break;

                        case "2":
                            if (!useHat)
                                break;
                            string two = _useSdl ? GetSDLInputMapping(ctrl, InputKey.leftanalogup, useHat) : GetDinputMapping(sdlController, "lefty", useHat, -1);
                            if (two != null)
                            {
                                string two_id = two.Split('_')[0];
                                string two_ah = two.Split('_')[1];
                                joyBinding["B_KB"] = "false";
                                joyBinding["B_ID"] = two_id;
                                joyBinding["B_AH"] = two_ah ?? "0.0";
                                joyBinding["B_DevID"] = guid;
                            }
                            break;

                        case "3":
                            string three = _useSdl ? GetSDLInputMapping(ctrl, InputKey.pagedown, useHat) : GetDinputMapping(sdlController, "rightshoulder", useHat);
                            if (three != null)
                            {
                                string three_id = three.Split('_')[0];
                                string three_ah = three.Split('_')[1];
                                joyBinding["B_KB"] = "false";
                                joyBinding["B_ID"] = three_id;
                                joyBinding["B_AH"] = three_ah ?? "0.0";
                                joyBinding["B_DevID"] = guid;
                            }
                            break;

                        case "4":
                            if (!useHat)
                                break;
                            string four = _useSdl ? GetSDLInputMapping(ctrl, InputKey.leftanalogleft, useHat) : GetDinputMapping(sdlController, "leftx", useHat, -1);
                            if (four != null)
                            {
                                string four_id = four.Split('_')[0];
                                string four_ah = four.Split('_')[1];
                                joyBinding["B_KB"] = "false";
                                joyBinding["B_ID"] = four_id;
                                joyBinding["B_AH"] = four_ah ?? "0.0";
                                joyBinding["B_DevID"] = guid;
                            }
                            break;

                        case "5":
                            string five = _useSdl ? GetSDLInputMapping(ctrl, InputKey.rightanalogup, useHat) : GetDinputMapping(sdlController, "righty", useHat, -1);
                            if (five != null)
                            {
                                string five_id = five.Split('_')[0];
                                string five_ah = five.Split('_')[1];
                                joyBinding["B_KB"] = "false";
                                joyBinding["B_ID"] = five_id;
                                joyBinding["B_AH"] = five_ah ?? "0.0";
                                joyBinding["B_DevID"] = guid;
                            }
                            break;

                        case "6":
                            if (!useHat)
                                break;
                            string six = _useSdl ? GetSDLInputMapping(ctrl, InputKey.leftanalogright, useHat) : GetDinputMapping(sdlController, "leftx", useHat, 1);
                            if (six != null)
                            {
                                string six_id = six.Split('_')[0];
                                string six_ah = six.Split('_')[1];
                                joyBinding["B_KB"] = "false";
                                joyBinding["B_ID"] = six_id;
                                joyBinding["B_AH"] = six_ah ?? "0.0";
                                joyBinding["B_DevID"] = guid;
                            }
                            break;

                        case "7":
                            string seven = _useSdl ? GetSDLInputMapping(ctrl, InputKey.rightanalogleft, useHat) : GetDinputMapping(sdlController, "rightx", useHat, -1);
                            if (seven != null)
                            {
                                string seven_id = seven.Split('_')[0];
                                string seven_ah = seven.Split('_')[1];
                                joyBinding["B_KB"] = "false";
                                joyBinding["B_ID"] = seven_id;
                                joyBinding["B_AH"] = seven_ah ?? "0.0";
                                joyBinding["B_DevID"] = guid;
                            }
                            break;

                        case "8":
                            if (!useHat)
                                break;
                            string eight = _useSdl ? GetSDLInputMapping(ctrl, InputKey.leftanalogdown, useHat) : GetDinputMapping(sdlController, "lefty", useHat, 1);
                            if (eight != null)
                            {
                                string eight_id = eight.Split('_')[0];
                                string eight_ah = eight.Split('_')[1];
                                joyBinding["B_KB"] = "false";
                                joyBinding["B_ID"] = eight_id;
                                joyBinding["B_AH"] = eight_ah ?? "0.0";
                                joyBinding["B_DevID"] = guid;
                            }
                            break;

                        case "9":
                            string nine = _useSdl ? GetSDLInputMapping(ctrl, InputKey.rightanalogright, useHat) : GetDinputMapping(sdlController, "rightx", useHat, 1);
                            if (nine != null)
                            {
                                string nine_id = nine.Split('_')[0];
                                string nine_ah = nine.Split('_')[1];
                                joyBinding["B_KB"] = "false";
                                joyBinding["B_ID"] = nine_id;
                                joyBinding["B_AH"] = nine_ah ?? "0.0";
                                joyBinding["B_DevID"] = guid;
                            }
                            break;

                        case "asterisk":
                            string asterisk = _useSdl ? GetSDLInputMapping(ctrl, InputKey.l3, useHat) : GetDinputMapping(sdlController, "leftstick", useHat);
                            if (asterisk != null)
                            {
                                string asterisk_id = asterisk.Split('_')[0];
                                string asterisk_ah = asterisk.Split('_')[1];
                                joyBinding["B_KB"] = "false";
                                joyBinding["B_ID"] = asterisk_id;
                                joyBinding["B_AH"] = asterisk_ah ?? "0.0";
                                joyBinding["B_DevID"] = guid;
                            }
                            break;

                        case "pound":
                            string pound = _useSdl ? GetSDLInputMapping(ctrl, InputKey.r3, useHat) : GetDinputMapping(sdlController, "rightstick", useHat);
                            if (pound != null)
                            {
                                string pound_id = pound.Split('_')[0];
                                string pound_ah = pound.Split('_')[1];
                                joyBinding["B_KB"] = "false";
                                joyBinding["B_ID"] = pound_id;
                                joyBinding["B_AH"] = pound_ah ?? "0.0";
                                joyBinding["B_DevID"] = guid;
                            }
                            break;

                        case "menu":
                            if (!hotkey)
                                break;
                            string menu_button = _useSdl ? GetSDLInputMapping(ctrl, InputKey.a, useHat) : GetDinputMapping(sdlController, "a", useHat);
                            string menu_hotkey = _useSdl ? GetSDLInputMapping(ctrl, InputKey.l2, useHat) : GetDinputMapping(sdlController, "lefttrigger", useHat, 1);
                            
                            if (menu_hotkey != null && menu_button != null)
                            {
                                string menu_button_id = menu_button.Split('_')[0];
                                string menu_button_ah = menu_button.Split('_')[1];
                                joyBinding["B_KB"] = "false";
                                joyBinding["B_ID"] = menu_button_id;
                                joyBinding["B_AH"] = menu_button_ah ?? "0.0";
                                joyBinding["B_DevID"] = guid;
                                
                                string menu_hotkey_id = menu_hotkey.Split('_')[0];
                                string menu_hotkey_ah = menu_hotkey.Split('_')[1];
                                joyBinding["M_KB"] = "false";
                                joyBinding["M_ID"] = menu_hotkey_id;
                                joyBinding["M_AH"] = menu_hotkey_ah ?? "0.0";
                                joyBinding["M_DevID"] = guid;
                            }
                            break;

                        case "ff":
                            if (!hotkey)
                                break;
                            string ff_button = _useSdl ? GetSDLInputMapping(ctrl, InputKey.right, useHat) : GetDinputMapping(sdlController, "dpright", useHat);
                            string ff_hotkey = _useSdl ? GetSDLInputMapping(ctrl, InputKey.l2, useHat) : GetDinputMapping(sdlController, "lefttrigger", useHat, 1);

                            if (ff_hotkey != null && ff_button != null)
                            {
                                string ff_button_id = ff_button.Split('_')[0];
                                string ff_button_ah = ff_button.Split('_')[1];
                                joyBinding["B_KB"] = "false";
                                joyBinding["B_ID"] = ff_button_id;
                                joyBinding["B_AH"] = ff_button_ah ?? "0.0";
                                joyBinding["B_DevID"] = guid;

                                string ff_hotkey_id = ff_hotkey.Split('_')[0];
                                string ff_hotkey_ah = ff_hotkey.Split('_')[1];
                                joyBinding["M_KB"] = "false";
                                joyBinding["M_ID"] = ff_hotkey_id;
                                joyBinding["M_AH"] = ff_hotkey_ah ?? "0.0";
                                joyBinding["M_DevID"] = guid;
                            }
                            break;

                        case "rewind":
                            if (!hotkey)
                                break;
                            string rewind_button = _useSdl ? GetSDLInputMapping(ctrl, InputKey.left, useHat) : GetDinputMapping(sdlController, "dpleft", useHat);
                            string rewind_hotkey = _useSdl ? GetSDLInputMapping(ctrl, InputKey.l2, useHat) : GetDinputMapping(sdlController, "lefttrigger", useHat, 1);

                            if (rewind_hotkey != null && rewind_button != null)
                            {
                                string rewind_button_id = rewind_button.Split('_')[0];
                                string rewind_button_ah = rewind_button.Split('_')[1];
                                joyBinding["B_KB"] = "false";
                                joyBinding["B_ID"] = rewind_button_id;
                                joyBinding["B_AH"] = rewind_button_ah ?? "0.0";
                                joyBinding["B_DevID"] = guid;

                                string rewind_hotkey_id = rewind_hotkey.Split('_')[0];
                                string rewind_hotkey_ah = rewind_hotkey.Split('_')[1];
                                joyBinding["M_KB"] = "false";
                                joyBinding["M_ID"] = rewind_hotkey_id;
                                joyBinding["M_AH"] = rewind_hotkey_ah ?? "0.0";
                                joyBinding["M_DevID"] = guid;
                            }
                            break;

                        case "savestate":
                            if (!hotkey)
                                break;
                            string savestate_button = _useSdl ? GetSDLInputMapping(ctrl, InputKey.y, useHat) : GetDinputMapping(sdlController, "x", useHat);
                            string savestate_hotkey = _useSdl ? GetSDLInputMapping(ctrl, InputKey.l2, useHat) : GetDinputMapping(sdlController, "lefttrigger", useHat, 1);

                            if (savestate_hotkey != null && savestate_button != null)
                            {
                                string savestate_button_id = savestate_button.Split('_')[0];
                                string savestate_button_ah = savestate_button.Split('_')[1];
                                joyBinding["B_KB"] = "false";
                                joyBinding["B_ID"] = savestate_button_id;
                                joyBinding["B_AH"] = savestate_button_ah ?? "0.0";
                                joyBinding["B_DevID"] = guid;

                                string savestate_hotkey_id = savestate_hotkey.Split('_')[0];
                                string savestate_hotkey_ah = savestate_hotkey.Split('_')[1];
                                joyBinding["M_KB"] = "false";
                                joyBinding["M_ID"] = savestate_hotkey_id;
                                joyBinding["M_AH"] = savestate_hotkey_ah ?? "0.0";
                                joyBinding["M_DevID"] = guid;
                            }
                            break;

                        case "loadstate":
                            if (!hotkey)
                                break;
                            string loadstate_button = _useSdl ? GetSDLInputMapping(ctrl, InputKey.x, useHat) : GetDinputMapping(sdlController, "y", useHat);
                            string loadstate_hotkey = _useSdl ? GetSDLInputMapping(ctrl, InputKey.l2, useHat) : GetDinputMapping(sdlController, "lefttrigger", useHat, 1);

                            if (loadstate_hotkey != null && loadstate_button != null)
                            {
                                string loadstate_button_id = loadstate_button.Split('_')[0];
                                string loadstate_button_ah = loadstate_button.Split('_')[1];
                                joyBinding["B_KB"] = "false";
                                joyBinding["B_ID"] = loadstate_button_id;
                                joyBinding["B_AH"] = loadstate_button_ah ?? "0.0";
                                joyBinding["B_DevID"] = guid;

                                string loadstate_hotkey_id = loadstate_hotkey.Split('_')[0];
                                string loadstate_hotkey_ah = loadstate_hotkey.Split('_')[1];
                                joyBinding["M_KB"] = "false";
                                joyBinding["M_ID"] = loadstate_hotkey_id;
                                joyBinding["M_AH"] = loadstate_hotkey_ah ?? "0.0";
                                joyBinding["M_DevID"] = guid;
                            }
                            break;

                        case "screenshot":
                            if (!hotkey)
                                break;
                            string screenshot_button = _useSdl ? GetSDLInputMapping(ctrl, InputKey.r3, useHat) : GetDinputMapping(sdlController, "rightstick", useHat);
                            string screenshot_hotkey = _useSdl ? GetSDLInputMapping(ctrl, InputKey.l2, useHat) : GetDinputMapping(sdlController, "lefttrigger", useHat, 1);

                            if (screenshot_hotkey != null && screenshot_button != null)
                            {
                                string screenshot_button_id = screenshot_button.Split('_')[0];
                                string screenshot_button_ah = screenshot_button.Split('_')[1];
                                joyBinding["B_KB"] = "false";
                                joyBinding["B_ID"] = screenshot_button_id;
                                joyBinding["B_AH"] = screenshot_button_ah ?? "0.0";
                                joyBinding["B_DevID"] = guid;

                                string screenshot_hotkey_id = screenshot_hotkey.Split('_')[0];
                                string screenshot_hotkey_ah = screenshot_hotkey.Split('_')[1];
                                joyBinding["M_KB"] = "false";
                                joyBinding["M_ID"] = screenshot_hotkey_id;
                                joyBinding["M_AH"] = screenshot_hotkey_ah ?? "0.0";
                                joyBinding["M_DevID"] = guid;
                            }
                            break;
                    }
                }

                triggers.Add(joyBinding);
                triggerSection.SetObject("Triggers", triggers);
                bindings.Add(triggerSection);
            }
            
            device.SetObject("Bindings", bindings);

            SimpleLogger.Instance.Info("[INFO] Assigned controller " + ctrl.DevicePath + " to player : " + ctrl.PlayerIndex.ToString());
        }

        private string GetDinputMapping(SdlToDirectInput c, string buttonkey, bool useHat, int direction = -1)
        {
            if (c == null)
                return null;

            if (!useHat)
            {
                if (buttonkey == "dpup")
                {
                    buttonkey = "lefty";
                    direction = -1;
                }
                else if (buttonkey == "dpdown")
                {
                    buttonkey = "lefty";
                    direction = 1;
                }
                else if (buttonkey == "dpleft")
                {
                    buttonkey = "leftx";
                    direction = -1;
                }
                else if (buttonkey == "dpright")
                {
                    buttonkey = "leftx";
                    direction = 1;
                }
            }
            
            if (!c.ButtonMappings.ContainsKey(buttonkey))
            {
                SimpleLogger.Instance.Info("[INFO] No mapping found for " + buttonkey + " in gamecontrollerdb file");
                return null;
            }

            string button = c.ButtonMappings[buttonkey];

            if (button.StartsWith("b"))
            {
                int buttonID = (button.Substring(1).ToInteger());
                return buttonID.ToString() + "_0.0";
            }

            else if (button.StartsWith("h"))
            {
                int hatID = button.Substring(3).ToInteger();

                switch (hatID)
                {
                    case 1:
                        return "134_0.0";
                    case 2:
                        return "134_0.25";
                    case 4:
                        return "134_0.5";
                    case 8:
                        return "134_0.75";
                }
            }

            else if (button.StartsWith("a") || button.StartsWith("-a") || button.StartsWith("+a"))
            {
                int axisID = button.Substring(1).ToInteger();

                if (button.StartsWith("-a"))
                {
                    direction = -1;
                    axisID = button.Substring(2).ToInteger();
                }

                else if (button.StartsWith("+a"))
                {
                    direction = 1;
                    axisID = button.Substring(2).ToInteger();
                }

                else if (button.StartsWith("a"))
                {
                    axisID = button.Substring(1).ToInteger();
                }

                if (direction == -1)
                    return (128 + axisID).ToString() + "_-1.0";
                else
                    return (128 + axisID).ToString() + "_1.0";
            }

            return null;
        }

        private string GetSDLInputMapping(Controller c, InputKey buttonkey, bool useHat)
        {
            Int64 pid;
            buttonkey = buttonkey.GetRevertedAxis(out bool revertAxis);

            
            if (!useHat)
            {
                if (buttonkey == InputKey.up)
                {
                    buttonkey = InputKey.leftanalogup;
                }
                else if (buttonkey == InputKey.down)
                {
                    buttonkey = InputKey.leftanalogup;
                    revertAxis = true;
                }
                else if (buttonkey == InputKey.left)
                {
                    buttonkey = InputKey.leftanalogleft;
                }
                else if (buttonkey == InputKey.right)
                {
                    buttonkey = InputKey.leftanalogleft;
                    revertAxis = true;
                }
            }

            var input = c.Config[buttonkey];

            if (input == null)
                return null;


            if (input.Type == "button")
            {
                return input.Id.ToString() + "_0.0";
            }

            else if (input.Type == "hat")
            {
                pid = input.Value;
                switch (pid)
                {
                    case 1: return "134_1.0";
                    case 2: return "134_2.0";
                    case 4: return "134_4.0";
                    case 8: return "134_8.0";
                }
            }

            else if (input.Type == "axis")
            {
                pid = input.Id;

                if ((!revertAxis && input.Value > 0) || (revertAxis && input.Value < 0)) return ((128 + pid).ToString() + "_1.0");
                else return ((128 + pid).ToString() + "_-1.0");
            }

            return null;
        }

        private void CleanInputConfig(DynamicJson json)
        {
            for (int i = 0; i < 8; i++)
            {
                var device = json.GetOrCreateContainer("Device" + i);
                var bindings = new List<DynamicJson>();

                device.Remove("Bindings");

                for (int j = 1; j <51; j++)
                {
                    var triggers = new List<DynamicJson>();
                    var binding = new DynamicJson();
                    binding.SetObject("Triggers", triggers);
                    bindings.Add(binding);
                }

                var Device = json.GetOrCreateContainer("Device" + i);
                Device.SetObject("Bindings", bindings);
            }
        }

        private string GetKeyboardCode(InputConfig keyboard, InputKey v)
        {
            Input button = keyboard[v];

            string ret = null;

            if (button != null)
                ret = _useSdl? SdlKeyCode(button.Id) : SdlToDinputKeyCode(button.Id);

            if (ret == "")
                ret = null;

            return ret;
        }

        private string GetkbKey(string key)
        {
            switch (key)
            {
                case "C":
                    return _useSdl ? "20" : "30";
                case "B":
                    return _useSdl ? "22" : "31";
                case "A":
                    return _useSdl ? "7" : "32";
                case "pause":
                    return _useSdl ? "4" : "16";
                case "option":
                    return _useSdl ? "29" : "17";
                case "up":
                    return _useSdl ? "82" : "200";
                case "down":
                    return _useSdl ? "81" : "208";
                case "left":
                    return _useSdl ? "80" : "203";
                case "right":
                    return _useSdl ? "79" : "205";
                case "0":
                    return _useSdl ? "39" : "11";
                case "1":
                    return _useSdl ? "30" : "2";
                case "2":
                    return _useSdl ? "31" : "3";
                case "3":
                    return _useSdl ? "32" : "4";
                case "4":
                    return _useSdl ? "33" : "5";
                case "5":
                    return _useSdl ? "34" : "6";
                case "6":
                    return _useSdl ? "35" : "7";
                case "7":
                    return _useSdl ? "36" : "8";
                case "8":
                    return _useSdl ? "37" : "9";
                case "9":
                    return _useSdl ? "38" : "10";
                case "asterisk":
                    return _useSdl ? "18" : "24";
                case "pound":
                    return _useSdl ? "19" : "25";
                case "menu":
                    return _useSdl ? "41" : "1";
                case "ff":
                    return _useSdl ? "59" : "60";
                case "rewind":
                    return _useSdl ? "58" : "59";
                case "savestate":
                    return _useSdl ? "60" : "61";
                case "loadstate":
                    return _useSdl ? "61" : "62";
                case "screenshot":
                    return _useSdl ? "67" : "68";
                case "chat":
                    return _useSdl ? "23" : "20";
            }

            return null;
        }

        private static string SdlToDinputKeyCode(long sdlCode)
        {
            switch (sdlCode)
            {
                case 0x0D: return "28"; // ENTREE
                case 0x00: return "";
                case 0x08: return "14"; // Backspace
                case 0x09: return "15"; // Tab
                case 0x1B: return "";   // Escape
                case 0x20: return "57"; // Space
                case 0x21: return "53"; // Exclam
                case 0x22: return "";
                case 0x23: return "";   // Diese
                case 0x24: return "27"; // Dollar
                case 0x25: return "";
                case 0x26: return "";   // Ampersand
                case 0x27: return "";   // Antislash
                case 0x28: return "";   // Parent left
                case 0x29: return "12"; // Parent right
                case 0x2A: return "55"; // *
                case 0x2B: return "78"; // +
                case 0x2C: return "50"; // Comma
                case 0x2D: return "74"; // Minus
                case 0x2E: return "";   // dot
                case 0x2F: return "";   // Slash
                case 0x30: return "11"; // 0
                case 0x31: return "2";  // 1
                case 0x32: return "3";
                case 0x33: return "4";
                case 0x34: return "5";
                case 0x35: return "6";
                case 0x36: return "7";
                case 0x37: return "8";
                case 0x38: return "9";
                case 0x39: return "10"; // 9
                case 0x3A: return "52";
                case 0x3B: return "51";
                case 0x3C: return "86";
                case 0x3D: return "13"; // Equals
                case 0x3F: return "";   // >
                case 0x40: return "";
                case 0x5B: return "";   // Left bracket
                case 0x5C: return "";   // Antislash
                case 0x5D: return "";   // Right bracket
                case 0x5E: return "";   // Chapeau
                case 0x5F: return "";   // Underscore
                case 0x60: return "";   // '
                case 0x61: return "16"; // A
                case 0x62: return "48";
                case 0x63: return "46";
                case 0x64: return "32"; // D
                case 0x65: return "18";
                case 0x66: return "33";
                case 0x67: return "34";
                case 0x68: return "35";
                case 0x69: return "23";
                case 0x6A: return "36";
                case 0x6B: return "37"; // K
                case 0x6C: return "38";
                case 0x6D: return "39";
                case 0x6E: return "49";
                case 0x6F: return "24"; // O
                case 0x70: return "25"; // P
                case 0x71: return "30"; // Q
                case 0x72: return "19";
                case 0x73: return "31";
                case 0x74: return "20";
                case 0x75: return "22";
                case 0x76: return "47";
                case 0x77: return "44";
                case 0x78: return "45";
                case 0x79: return "21";
                case 0x7A: return "17"; // Z
                case 0x7F: return "211";        // Delete
                case 0x40000039: return "58";   // Capslock
                case 0x4000003A: return "59";   // F1
                case 0x4000003B: return "60";   // F2
                case 0x4000003C: return "61";
                case 0x4000003D: return "62";
                case 0x4000003E: return "63";
                case 0x4000003F: return "64";
                case 0x40000040: return "65";
                case 0x40000041: return "66";
                case 0x40000042: return "67";   // F9
                case 0x40000043: return "68";   // F10
                case 0x40000044: return "87";   // F11
                case 0x40000045: return "88";   // F12
                case 0x40000046: return "";     // Printscreen
                case 0x40000047: return "";     // Scrolllock
                case 0x40000048: return "";     // Pause
                case 0x40000049: return "210";  // INSERT
                case 0x4000004A: return "199";  // Home
                case 0x4000004B: return "201";  // PageUp
                case 0x4000004D: return "207";  // End
                case 0x4000004E: return "209";  // PageDown
                case 0x4000004F: return "205";  // Right
                case 0x40000050: return "203";  // Left
                case 0x40000051: return "208";  // Down
                case 0x40000052: return "200";  // Up
                case 0x40000053: return "69";   // Numlock
                case 0x40000054: return "181";  // Num divide
                case 0x40000055: return "55";   // Num multiply
                case 0x40000056: return "74";   // Num -
                case 0x40000057: return "78";   // Num+
                case 0x40000058: return "156";  // Num ENTER
                case 0x40000059: return "79";   // Num 1
                case 0x4000005A: return "80";
                case 0x4000005B: return "81";
                case 0x4000005C: return "75";
                case 0x4000005D: return "76";
                case 0x4000005E: return "77";
                case 0x4000005F: return "71";
                case 0x40000060: return "72";
                case 0x40000061: return "73";
                case 0x40000062: return "82";   // Num 0
                case 0x40000063: return "83";   // Num .
                case 0x40000067: return "";     // Num =
                case 0x40000068: return "";     // F13
                case 0x40000069: return "";
                case 0x4000006A: return "";
                case 0x4000006B: return "";
                case 0x4000006C: return "";
                case 0x4000006D: return "";
                case 0x4000006E: return "";
                case 0x4000006F: return "";
                case 0x40000070: return "";
                case 0x40000071: return "";
                case 0x40000072: return "";
                case 0x40000073: return "";     // F24
                case 0x40000074: return "";     // Execute
                case 0x40000075: return "";     // Help
                case 0x40000076: return "";     // Menu
                case 0x40000077: return "";     // Select
                case 0x40000078: return "";     // Stop
                case 0x40000079: return "";     // Again
                case 0x4000007A: return "";     // Undo
                case 0x4000007B: return "";     // Cut
                case 0x4000007C: return "";     // Copy
                case 0x4000007D: return "";     // Paste
                case 0x4000007E: return "";     // Menu
                case 0x4000007F: return "";     // Mute
                case 0x40000080: return "";     // Volume up
                case 0x40000081: return "";     // Volume down
                case 0x40000085: return "";     // Num ,
                case 0x400000E0: return "29";   // Left CTRL
                case 0x400000E1: return "42";   // Left SHIFT
                case 0x400000E2: return "56";   // Left ALT
                case 0x400000E4: return "157";  // Right CTRL
                case 0x400000E5: return "54";   // Right SHIFT
                case 0x400000E6: return "184";  // Right ALT
                case 0x40000101: return "";     // Mode
                case 0x40000102: return "";     // Media next
                case 0x40000103: return "";     // Media previous
                case 0x40000105: return "";     // Media play
            }
            return "";
        }

        private static string SdlKeyCode(long sdlCode)
        {
            switch (sdlCode)
            {
                case 0x0D: return "40"; // ENTREE
                case 0x00: return "";
                case 0x08: return "42"; // Backspace
                case 0x09: return "43"; // Tab
                case 0x1B: return "41";   // Escape
                case 0x20: return "44"; // Space
                case 0x21: return "";   // Exclam
                case 0x22: return "";
                case 0x23: return "";   // Diese
                case 0x24: return "";   // Dollar
                case 0x25: return "";   // Percent
                case 0x26: return "";   // Ampersand (esperluette)
                case 0x27: return "49";   // Antislash
                case 0x28: return "";   // Parent left
                case 0x29: return "";   // Parent right
                case 0x2A: return ""; // *
                case 0x2B: return ""; // +
                case 0x2C: return "54"; // Comma
                case 0x2D: return "45"; // Minus
                case 0x2E: return "55";   // dot
                case 0x2F: return "56";   // Slash
                case 0x30: return "39"; // 0
                case 0x31: return "30";  // 1
                case 0x32: return "31";
                case 0x33: return "32";
                case 0x34: return "33";
                case 0x35: return "34";
                case 0x36: return "35";
                case 0x37: return "36";
                case 0x38: return "37";
                case 0x39: return "38"; // 9
                case 0x3A: return ""; // :
                case 0x3B: return "51";
                case 0x3C: return "";
                case 0x3D: return "46"; // Equals
                case 0x3F: return "";   // >
                case 0x40: return "";
                case 0x5B: return "47";   // Left bracket
                case 0x5C: return "49";   // Antislash
                case 0x5D: return "48";   // Right bracket
                case 0x5E: return "";   // Chapeau
                case 0x5F: return "";   // Underscore
                case 0x60: return "";   // '
                case 0x61: return "4";  // A - done
                case 0x62: return "5";
                case 0x63: return "6";
                case 0x64: return "7"; // D
                case 0x65: return "8";
                case 0x66: return "9";
                case 0x67: return "10";
                case 0x68: return "11";
                case 0x69: return "12";
                case 0x6A: return "13";
                case 0x6B: return "14"; // K
                case 0x6C: return "15";
                case 0x6D: return "16";
                case 0x6E: return "17";
                case 0x6F: return "18"; // O
                case 0x70: return "19"; // P
                case 0x71: return "20"; // Q
                case 0x72: return "21";
                case 0x73: return "22";
                case 0x74: return "23";
                case 0x75: return "24";
                case 0x76: return "25";
                case 0x77: return "26";
                case 0x78: return "27";
                case 0x79: return "28";
                case 0x7A: return "29"; // Z
                case 0x7F: return "76";        // Delete
                case 0x40000039: return "57";   // Capslock
                case 0x4000003A: return "58";   // F1
                case 0x4000003B: return "59";   // F2
                case 0x4000003C: return "60";
                case 0x4000003D: return "61";
                case 0x4000003E: return "62";
                case 0x4000003F: return "63";
                case 0x40000040: return "64";
                case 0x40000041: return "65";
                case 0x40000042: return "66";   // F9
                case 0x40000043: return "67";   // F10
                case 0x40000044: return "68";   // F11
                case 0x40000045: return "69";   // F12
                case 0x40000046: return "70";     // Printscreen
                case 0x40000047: return "";     // Scrolllock
                case 0x40000048: return "72";     // Pause
                case 0x40000049: return "73";  // INSERT
                case 0x4000004A: return "74";  // Home
                case 0x4000004B: return "75";  // PageUp
                case 0x4000004D: return "77";  // End
                case 0x4000004E: return "78";  // PageDown
                case 0x4000004F: return "79";  // Right
                case 0x40000050: return "80";  // Left
                case 0x40000051: return "81";  // Down
                case 0x40000052: return "82";  // Up
                case 0x40000053: return "83";   // Numlock
                case 0x40000054: return "84";  // Num divide
                case 0x40000055: return "85";   // Num multiply
                case 0x40000056: return "86";   // Num -
                case 0x40000057: return "87";   // Num+
                case 0x40000058: return "88";  // Num ENTER
                case 0x40000059: return "89";   // Num 1
                case 0x4000005A: return "90";
                case 0x4000005B: return "91";
                case 0x4000005C: return "92";
                case 0x4000005D: return "93";
                case 0x4000005E: return "94";
                case 0x4000005F: return "95";
                case 0x40000060: return "96";
                case 0x40000061: return "97";
                case 0x40000062: return "98";   // Num 0
                case 0x40000063: return "99";   // Num .
                case 0x40000067: return "103";     // Num =
                case 0x40000068: return "104";     // F13
                case 0x40000069: return "105";
                case 0x4000006A: return "106";
                case 0x4000006B: return "107";
                case 0x4000006C: return "108";
                case 0x4000006D: return "109";
                case 0x4000006E: return "110";
                case 0x4000006F: return "111";
                case 0x40000070: return "112";
                case 0x40000071: return "113";
                case 0x40000072: return "114";
                case 0x40000073: return "115";     // F24
                case 0x40000074: return "116";     // Execute
                case 0x40000075: return "117";     // Help
                case 0x40000076: return "118";     // Menu
                case 0x40000077: return "119";     // Select
                case 0x40000078: return "120";     // Stop
                case 0x40000079: return "121";     // Again
                case 0x4000007A: return "122";     // Undo
                case 0x4000007B: return "123";     // Cut
                case 0x4000007C: return "124";     // Copy
                case 0x4000007D: return "125";     // Paste
                case 0x4000007E: return "126";     // Find
                case 0x4000007F: return "127";     // Mute
                case 0x40000080: return "128";     // Volume up
                case 0x40000081: return "129";     // Volume down
                case 0x40000085: return "133";     // Num ,
                case 0x400000E0: return "224";   // Left CTRL
                case 0x400000E1: return "225";   // Left SHIFT
                case 0x400000E2: return "226";   // Left ALT
                case 0x400000E4: return "228";  // Right CTRL
                case 0x400000E5: return "229";   // Right SHIFT
                case 0x400000E6: return "230";  // Right ALT
                case 0x40000101: return "257";     // Mode
                case 0x40000102: return "258";     // Media next
                case 0x40000103: return "259";     // Media previous
                case 0x40000105: return "261";     // Media play
            }
            return "";
        }

        private readonly List<string> padAndKB = new List<string>()
        { "C", "B", "A", "pause", "option", "up", "down", "left", "right", "0", "1", "2", "3", "4", "5", "6", "7", "8", "9", 
            "asterisk", "pound", "menu", "ff", "rewind", "savestate", "loadstate", "screenshot" };
    }
}
