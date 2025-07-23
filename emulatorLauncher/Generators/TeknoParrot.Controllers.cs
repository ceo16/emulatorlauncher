﻿using System.Linq;
using System.IO;
using TeknoParrotUi.Common;
using EmulatorLauncher.Common.EmulationStation;
using EmulatorLauncher.Common.Joysticks;
using System.Collections.Generic;
using EmulatorLauncher.Common.FileFormats;
using System;
using EmulatorLauncher.Common;

namespace EmulatorLauncher
{
    partial class TeknoParrotGenerator : Generator
    {
        private static void ConfigureControllers(GameProfile userProfile, string rom)
        {
            if (Program.SystemConfig.isOptSet("disableautocontrollers") && Program.SystemConfig["disableautocontrollers"] == "1")
            {
                SimpleLogger.Instance.Info("[INFO] Auto controller configuration disabled.");
                return;
            }

            SimpleLogger.Instance.Info("[INFO] Check if Gun configuration is required.");

            if (ConfigureTPGuns(userProfile, rom))
                return;

            SimpleLogger.Instance.Info("[INFO] Gun configuration not required, configuring controller.");

            int padNr = Program.Controllers.Where(c => !c.IsKeyboard).Count();

            if (padNr < 1)  // If no controller, return
                return;

            // Cleanup
            foreach (var joyButton in userProfile.JoystickButtons)
            {
                joyButton.BindName = null;
                joyButton.BindNameDi = null;
                joyButton.BindNameRi = null;
                joyButton.BindNameXi = null;
                joyButton.DirectInputButton = null;
                joyButton.XInputButton = null;
                joyButton.RawInputButton = null;
            }

            int playerNumber = 1;
            List<Controller> TPControllers = new List<Controller>();

            // List controllers
            Controller c1 = Program.Controllers.Where(c => c.PlayerIndex == 1).FirstOrDefault();
            SimpleLogger.Instance.Info("[INFO] Controller for player 1 identified as " + c1.DevicePath.ToString());
            TPControllers.Add(c1);
            
            Controller c2 = null;
            Controller c3 = null;
            Controller c4 = null;
            Controller c5 = null;
            Controller c6 = null;

            if (padNr > 5)
            {
                c6 = Program.Controllers.Where(c => c.PlayerIndex == 6).FirstOrDefault();
                TPControllers.Add(c6);
            }
            if (padNr > 4)
            {
                c5 = Program.Controllers.Where(c => c.PlayerIndex == 5).FirstOrDefault();
                TPControllers.Add(c5);
            }
            if (padNr > 3)
            {
                c4 = Program.Controllers.Where(c => c.PlayerIndex == 4).FirstOrDefault();
                TPControllers.Add(c4);
            }
            if (padNr > 2)
            {
                c3 = Program.Controllers.Where(c => c.PlayerIndex == 3).FirstOrDefault();
                TPControllers.Add(c3);
            }
            if (padNr > 1)
            {
                c2 = Program.Controllers.Where(c => c.PlayerIndex == 2).FirstOrDefault();
                TPControllers.Add(c2);
            }

            // Manage input driver
            bool xInput = c1.IsXInputDevice;
            YmlContainer game = null;
            string tpGameName = Path.GetFileNameWithoutExtension(userProfile.FileName).ToLowerInvariant();

            var inputAPI = userProfile.ConfigValues.FirstOrDefault(c => c.FieldName == "Input API");
            if (inputAPI != null)
            {
                if (xInput && inputAPI.FieldOptions != null && inputAPI.FieldOptions.Any(f => f == "XInput"))
                    inputAPI.FieldValue = "XInput";
                else if (inputAPI.FieldOptions != null && inputAPI.FieldOptions.Any(f => f == "DirectInput"))
                    inputAPI.FieldValue = "DirectInput";
            }

            if (Program.SystemConfig.isOptSet("tp_inputdriver") && !string.IsNullOrEmpty(Program.SystemConfig["tp_inputdriver"]))
            {
                switch (Program.SystemConfig["tp_inputdriver"])
                {
                    case "XInput":
                        if (inputAPI.FieldOptions.Any(f => f == "XInput"))
                            inputAPI.FieldValue = "XInput";
                        break;
                    case "DirectInput":
                        if (inputAPI.FieldOptions.Any(f => f == "DirectInput"))
                            inputAPI.FieldValue = "DirectInput";
                        break;
                    case "RawInput":
                        if (inputAPI.FieldOptions.Any(f => f == "RawInput"))
                            inputAPI.FieldValue = "RawInput";
                        break;
                }
            }
            
            if (inputAPI.FieldValue == "XInput" && c1.IsXInputDevice)
                SimpleLogger.Instance.Info("[INFO] Controller for player 1 has XInput index " + c1.XInput.DeviceIndex.ToString());

            // Look for number of players based on string search in userprofile file (Px, Player x)
            playerNumber = GetNumberOfPlayers(userProfile);

            SimpleLogger.Instance.Info("[INFO] Game identified for " + playerNumber + " player");

            // Search mapping in yml file
            Dictionary<string, Dictionary<string, string>> gameMapping = new Dictionary<string, Dictionary<string, string>>();
            string tpMappingyml = Controller.GetSystemYmlMappingFile("teknoparrot", "", "teknoparrot", mappingPaths);
            var buttonMap = new Dictionary<string, string>();
            string gameName = null;

            if (tpMappingyml != null && File.Exists(tpMappingyml))
            {
                YmlFile ymlFile = YmlFile.Load(tpMappingyml);
                string padGameName = tpGameName + "_pad";
                game = ymlFile.Elements.Where(g => g.Name == padGameName).FirstOrDefault() as YmlContainer;
                
                if (game == null)
                    game = ymlFile.Elements.Where(g => g.Name == tpGameName).FirstOrDefault() as YmlContainer;
                
                if (game != null)
                {
                    gameName = game.Name;

                    foreach (var buttonEntry in game.Elements.Where(t => t.GetType().Name == "YmlElement"))
                    {
                        YmlElement button = buttonEntry as YmlElement;
                        if (button.Name == "Players")
                        {
                            playerNumber = button.Value.ToInteger();
                            continue;
                        }

                        else if (button.Name.StartsWith("mouse"))
                        {
                            continue;
                        }

                        else if (button != null)
                        {
                            buttonMap.Add(button.Name, button.Value);
                        }
                    }

                    gameMapping.Add(gameName, buttonMap);
                    SimpleLogger.Instance.Info("[INFO] Performing mapping based on teknoparrot.yml file");
                }
            }

            foreach (var c in TPControllers.OrderBy(i => i.PlayerIndex).Take(playerNumber))
            {
                if (c.Config == null || c.Config.Type == "key")
                    continue;

                if (game != null)
                {
                    if (buttonMap.Count > 0)
                    {
                        foreach (var button in buttonMap)
                        {
                            InputKey input;
                            if (button.Value == null)
                                continue;

                            string key = button.Key;
                            string value = button.Value;
                            if (value.StartsWith("kb_") || value.StartsWith("mouse"))
                                continue;

                            bool enumExists = Enum.TryParse(key, out InputMapping inputEnum);
                            JoystickButtons xmlPlace = null;
                            if (enumExists)
                                xmlPlace = userProfile.JoystickButtons.FirstOrDefault(j => j.InputMapping == inputEnum);
                            if (xmlPlace == null)
                                continue;

                            switch (c.PlayerIndex)
                            {
                                case 6:
                                    {
                                        if (xmlPlace.ButtonName == "Coin 6" || xmlPlace.ButtonName == "Coin Chute 6" || xmlPlace.ButtonName == "Coin6")
                                            break;
                                        if (!P6include.Any(v => xmlPlace.ButtonName.ToLowerInvariant().Contains(v.ToLowerInvariant())))
                                            continue;
                                        break;
                                    }
                                case 5:
                                    {
                                        if (xmlPlace.ButtonName == "Coin 5" || xmlPlace.ButtonName == "Coin Chute 5" || xmlPlace.ButtonName == "Coin5")
                                            break;
                                        if (!P5include.Any(v => xmlPlace.ButtonName.ToLowerInvariant().Contains(v.ToLowerInvariant())))
                                            continue;
                                        break;
                                    }
                                case 4:
                                    {
                                        if (xmlPlace.ButtonName == "Coin 4" || xmlPlace.ButtonName == "Coin Chute 4" || xmlPlace.ButtonName == "Coin4")
                                            break;
                                        if (!P4include.Any(v => xmlPlace.ButtonName.ToLowerInvariant().Contains(v.ToLowerInvariant())))
                                            continue;
                                        break;
                                    }
                                case 3:
                                    {
                                        if (xmlPlace.ButtonName == "Coin 3" || xmlPlace.ButtonName == "Coin Chute 3" || xmlPlace.ButtonName == "Coin3")
                                            break;
                                        if (!P3include.Any(v => xmlPlace.ButtonName.ToLowerInvariant().Contains(v.ToLowerInvariant())))
                                            continue;
                                        break;
                                    }
                                case 2:
                                    {
                                        if (xmlPlace.ButtonName == "Coin 2" || xmlPlace.ButtonName == "Coin Chute 2" || xmlPlace.ButtonName == "Coin2" || xmlPlace.ButtonName == "Service 2")
                                            break;
                                        if (!P2include.Any(v => xmlPlace.ButtonName.ToLowerInvariant().Contains(v.ToLowerInvariant())))
                                            continue;
                                        break;
                                    }
                                case 1:
                                    {
                                        if (P1exclude.Any(v => xmlPlace.ButtonName.ToLowerInvariant().Contains(v.ToLowerInvariant())))
                                            continue;
                                        break;
                                    }
                            }
                            
                            if (ymlButtonMapping.ContainsKey(value))
                            {
                                if (inputAPI.FieldValue == "DirectInput")
                                {
                                    ymlButtonMapping["north"] = InputKey.y;
                                    ymlButtonMapping["west"] = InputKey.x;
                                }
                                input = ymlButtonMapping[value];
                                if (enumExists)
                                {
                                    switch (inputAPI.FieldValue)
                                    {
                                        case "XInput":
                                            ImportXInputButton(userProfile, c, input, "no", inputEnum);
                                            break;
                                        case "DirectInput":
                                            ImportDirectInputButton(userProfile, c, input, inputEnum);
                                            break;
                                    }
                                }
                            }
                        }
                    }
                }

                else
                {
                    if (xInput)
                    {
                        //            foreach (var btn in userProfile.JoystickButtons)
                        //                btn.XInputButton = null;

                        if (userProfile.EmulationProfile == "NamcoMachStorm")
                        {
                            ImportXInputButton(userProfile, c, InputKey.select, "no", InputMapping.Service1);

                            ImportXInputButton(userProfile, c, InputKey.leftanalogup, "no", InputMapping.Analog6);
                            ImportXInputButton(userProfile, c, InputKey.leftanalogleft, "no", InputMapping.Analog4);
                            ImportXInputButton(userProfile, c, InputKey.r2, "no", InputMapping.Analog2);

                            ImportXInputButton(userProfile, c, InputKey.up, "no", InputMapping.JvsTwoP1ButtonUp, InputMapping.P1ButtonUp, InputMapping.P1RelativeUp);
                            ImportXInputButton(userProfile, c, InputKey.down, "no", InputMapping.JvsTwoP1ButtonDown, InputMapping.P1ButtonDown, InputMapping.P1RelativeDown);

                            ImportXInputButton(userProfile, c, InputKey.a, "no", InputMapping.JvsTwoP1Button1, InputMapping.P1Button1);
                            ImportXInputButton(userProfile, c, InputKey.b, "no", InputMapping.ExtensionOne12);
                            ImportXInputButton(userProfile, c, InputKey.x, "no", InputMapping.ExtensionOne11);
                        }
                        else
                        {
                            if (c.Config[InputKey.leftanalogleft] != null)
                            {
                                if (userProfile.HasAnyXInputButton(InputMapping.Analog0))
                                {
                                    ImportXInputButton(userProfile, c, InputKey.left, "no", InputMapping.JvsTwoP1ButtonLeft, InputMapping.P1ButtonLeft, InputMapping.P1RelativeLeft);
                                    ImportXInputButton(userProfile, c, InputKey.right, "no", InputMapping.JvsTwoP1ButtonRight, InputMapping.P1ButtonRight, InputMapping.P1RelativeRight);
                                    ImportXInputButton(userProfile, c, InputKey.up, "no", InputMapping.JvsTwoP1ButtonUp, InputMapping.P1ButtonUp, InputMapping.P1RelativeUp);
                                    ImportXInputButton(userProfile, c, InputKey.down, "no", InputMapping.JvsTwoP1ButtonDown, InputMapping.P1ButtonDown, InputMapping.P1RelativeDown);

                                    // Wheel
                                    ImportXInputButton(userProfile, c, InputKey.leftanalogleft, "no", InputMapping.Analog0);
                                    ImportXInputButton(userProfile, c, InputKey.leftanalogup, "no", InputMapping.Analog1);

                                    // Gas
                                    ImportXInputButton(userProfile, c, InputKey.r2, "no", InputMapping.Analog2);

                                    // Brake
                                    ImportXInputButton(userProfile, c, InputKey.l2, "no", InputMapping.Analog4);
                                }
                                else
                                {
                                    ImportXInputButton(userProfile, c, InputKey.leftanalogleft, "no", InputMapping.JvsTwoP1ButtonLeft, InputMapping.P1ButtonLeft, InputMapping.P1RelativeLeft);
                                    ImportXInputButton(userProfile, c, InputKey.leftanalogright, "no", InputMapping.JvsTwoP1ButtonRight, InputMapping.P1ButtonRight, InputMapping.P1RelativeRight);
                                    ImportXInputButton(userProfile, c, InputKey.leftanalogup, "no", InputMapping.JvsTwoP1ButtonUp, InputMapping.P1ButtonUp, InputMapping.P1RelativeUp);
                                    ImportXInputButton(userProfile, c, InputKey.leftanalogdown, "no", InputMapping.JvsTwoP1ButtonDown, InputMapping.P1ButtonDown, InputMapping.P1RelativeDown);
                                }
                            }
                            else
                            {
                                ImportXInputButton(userProfile, c, InputKey.left, "no", InputMapping.JvsTwoP1ButtonLeft, InputMapping.P1ButtonLeft, InputMapping.P1RelativeLeft);
                                ImportXInputButton(userProfile, c, InputKey.right, "no", InputMapping.JvsTwoP1ButtonRight, InputMapping.P1ButtonRight, InputMapping.P1RelativeRight);
                                ImportXInputButton(userProfile, c, InputKey.up, "no", InputMapping.JvsTwoP1ButtonUp, InputMapping.P1ButtonUp, InputMapping.P1RelativeUp);
                                ImportXInputButton(userProfile, c, InputKey.down, "no", InputMapping.JvsTwoP1ButtonDown, InputMapping.P1ButtonDown, InputMapping.P1RelativeDown);
                            }

                            if (userProfile.HasAnyXInputButton(InputMapping.JvsTwoP1ButtonStart, InputMapping.P1ButtonStart, InputMapping.JvsTwoCoin1, InputMapping.Coin1))
                            {
                                ImportXInputButton(userProfile, c, InputKey.start, "no", InputMapping.JvsTwoP1ButtonStart, InputMapping.P1ButtonStart);
                                ImportXInputButton(userProfile, c, InputKey.select, "no", InputMapping.JvsTwoCoin1, InputMapping.Coin1);
                            }
                            else
                                ImportXInputButton(userProfile, c, InputKey.start, "no", InputMapping.Service1, InputMapping.JvsTwoService1);

                            ImportXInputButton(userProfile, c, InputKey.a, "no", InputMapping.JvsTwoP1Button1, InputMapping.P1Button1);
                            ImportXInputButton(userProfile, c, InputKey.b, "no", InputMapping.JvsTwoP1Button2, InputMapping.P1Button2);
                            ImportXInputButton(userProfile, c, InputKey.x, "no", InputMapping.JvsTwoP1Button3, InputMapping.P1Button3);
                            ImportXInputButton(userProfile, c, InputKey.y, "no", InputMapping.JvsTwoP1Button4, InputMapping.P1Button4);

                            ImportXInputButton(userProfile, c, InputKey.pageup, "no", InputMapping.JvsTwoP1Button5, InputMapping.P1Button5);
                            ImportXInputButton(userProfile, c, InputKey.pagedown, "no", InputMapping.JvsTwoP1Button6, InputMapping.P1Button6);


                            // Assignation of ExtensionOne buttons
                            if (userProfile.HasAnyXInputButton(InputMapping.ExtensionOne2) && !userProfile.HasAnyXInputButton(InputMapping.P1Button2))
                                ImportXInputButton(userProfile, c, InputKey.b, "no", InputMapping.ExtensionOne2);
                            else if (userProfile.HasAnyXInputButton(InputMapping.ExtensionOne2) && !userProfile.HasAnyXInputButton(InputMapping.P1Button4))
                                ImportXInputButton(userProfile, c, InputKey.y, "no", InputMapping.ExtensionOne2);
                            else if (userProfile.HasAnyXInputButton(InputMapping.ExtensionOne2) && !userProfile.HasAnyXInputButton(InputMapping.P1Button3))
                                ImportXInputButton(userProfile, c, InputKey.x, "no", InputMapping.ExtensionOne2);
                            else if (userProfile.HasAnyXInputButton(InputMapping.ExtensionOne2) && !userProfile.HasAnyXInputButton(InputMapping.P1Button6))
                                ImportXInputButton(userProfile, c, InputKey.pagedown, "no", InputMapping.ExtensionOne2);
                            else if (userProfile.HasAnyXInputButton(InputMapping.ExtensionOne2) && !userProfile.HasAnyXInputButton(InputMapping.P1Button5))
                                ImportXInputButton(userProfile, c, InputKey.pageup, "no", InputMapping.ExtensionOne2);
                        }
                    }
                    else   // DirectInput
                    {
                        if (c.Config[InputKey.leftanalogleft] != null)
                        {
                            ImportDirectInputButton(userProfile, c, InputKey.leftanalogup, InputMapping.JvsTwoP1ButtonUp, InputMapping.P1ButtonUp, InputMapping.P1RelativeUp);
                            ImportDirectInputButton(userProfile, c, InputKey.leftanalogleft, InputMapping.JvsTwoP1ButtonLeft, InputMapping.P1ButtonLeft, InputMapping.P1RelativeLeft);
                            ImportDirectInputButton(userProfile, c, InputKey.leftanalogdown, InputMapping.JvsTwoP1ButtonDown, InputMapping.P1ButtonDown, InputMapping.P1RelativeDown);
                            ImportDirectInputButton(userProfile, c, InputKey.leftanalogright, InputMapping.JvsTwoP1ButtonRight, InputMapping.P1ButtonRight, InputMapping.P1RelativeRight);
                        }
                        else
                        {
                            ImportDirectInputButton(userProfile, c, InputKey.up, InputMapping.JvsTwoP1ButtonUp, InputMapping.P1ButtonUp, InputMapping.P1RelativeUp);
                            ImportDirectInputButton(userProfile, c, InputKey.left, InputMapping.JvsTwoP1ButtonLeft, InputMapping.P1ButtonLeft, InputMapping.P1RelativeLeft);
                            ImportDirectInputButton(userProfile, c, InputKey.down, InputMapping.JvsTwoP1ButtonDown, InputMapping.P1ButtonDown, InputMapping.P1RelativeDown);
                            ImportDirectInputButton(userProfile, c, InputKey.right, InputMapping.JvsTwoP1ButtonRight, InputMapping.P1ButtonRight, InputMapping.P1RelativeRight);
                        }

                        ImportDirectInputButton(userProfile, c, InputKey.select, InputMapping.JvsTwoCoin1, InputMapping.Coin1);
                        ImportDirectInputButton(userProfile, c, InputKey.start, InputMapping.JvsTwoP1ButtonStart, InputMapping.P1ButtonStart);
                        ImportDirectInputButton(userProfile, c, InputKey.a, InputMapping.JvsTwoP1Button1, InputMapping.P1Button1);
                        ImportDirectInputButton(userProfile, c, InputKey.b, InputMapping.JvsTwoP1Button2, InputMapping.P1Button2);
                        ImportDirectInputButton(userProfile, c, InputKey.x, InputMapping.JvsTwoP1Button3, InputMapping.P1Button3);
                        ImportDirectInputButton(userProfile, c, InputKey.y, InputMapping.JvsTwoP1Button4, InputMapping.P1Button4);
                        ImportDirectInputButton(userProfile, c, InputKey.l3, InputMapping.JvsTwoP1Button5, InputMapping.P1Button5);
                        ImportDirectInputButton(userProfile, c, InputKey.r3, InputMapping.JvsTwoP1Button6, InputMapping.P1Button6);

                    }
                    break;
                }
            }
        }

        private static void ImportXInputButton(GameProfile userProfile, Controller ctl, InputKey key, string special = "no", params InputMapping[] mapping)
        {
            InputConfig c = ctl.Config;

            var start = userProfile.JoystickButtons.FirstOrDefault(j => !j.HideWithXInput && mapping.Contains(j.InputMapping));

            bool reverseAxis = false;

            if (c[key] == null && (key == InputKey.leftanalogdown || key == InputKey.joystick1down))
            {
                reverseAxis = true;
                key = InputKey.leftanalogup;
            }
            if (c[key] == null && (key == InputKey.leftanalogright || key == InputKey.joystick1right))
            {
                reverseAxis = true;
                key = InputKey.leftanalogleft;
            }
            if (c[key] == null && (key == InputKey.rightanalogright || key == InputKey.joystick2right))
            {
                reverseAxis = true;
                key = InputKey.rightanalogleft;
            }
            if (c[key] == null && (key == InputKey.rightanalogdown || key == InputKey.joystick2down))
            {
                reverseAxis = true;
                key = InputKey.rightanalogup;
            }


            if (start != null && c[key] != null)
            {
                start.XInputButton = new XInputButton();
                int deviceNumber = ctl.XInput != null ? ctl.XInput.DeviceIndex : ctl.DeviceIndex;
                string indexoverride = "tp_padindex" + ctl.PlayerIndex;
                if (Program.SystemConfig.isOptSet(indexoverride) && !string.IsNullOrEmpty(Program.SystemConfig[indexoverride]))
                    deviceNumber = Program.SystemConfig[indexoverride].ToInteger();

                if (c[key].Type == "axis")
                {
                    start.XInputButton.IsLeftThumbX = false;
                    start.XInputButton.IsRightThumbX = false;
                    start.XInputButton.IsLeftThumbY = false;
                    start.XInputButton.IsRightThumbY = false;
                    start.XInputButton.IsAxisMinus = false;
                    start.XInputButton.IsLeftTrigger = false;
                    start.XInputButton.IsRightTrigger = false;
                    start.XInputButton.XInputIndex = deviceNumber;
                    start.XInputButton.ButtonIndex = 0;
                    start.XInputButton.IsButton = false;
                    start.XInputButton.ButtonCode = 0;

                    string bindName = GetXInputName(key, ctl, reverseAxis);
                    start.BindNameXi = "Input Device " + deviceNumber + " " + bindName.Replace("zzz", deviceNumber.ToString());
                    start.BindName = "Input Device " + deviceNumber + " " + bindName.Replace("zzz", deviceNumber.ToString());

                    switch (ctl.GetXInputMapping(key, reverseAxis))
                    {
                        case XINPUTMAPPING.LEFTANALOG_LEFT:
                            start.XInputButton.IsLeftThumbX = true;
                            start.XInputButton.IsAxisMinus = true;
                            break;
                        case XINPUTMAPPING.LEFTANALOG_RIGHT:
                            start.XInputButton.IsLeftThumbX = true;
                            start.XInputButton.IsAxisMinus = false;
                            break;
                        case XINPUTMAPPING.LEFTANALOG_UP:
                            start.XInputButton.IsLeftThumbY = true;
                            start.XInputButton.IsAxisMinus = false;
                            break;
                        case XINPUTMAPPING.LEFTANALOG_DOWN:
                            start.XInputButton.IsLeftThumbY = true;
                            start.XInputButton.IsAxisMinus = true;
                            break;
                        case XINPUTMAPPING.RIGHTANALOG_LEFT:
                            start.XInputButton.IsRightThumbX = true;
                            start.XInputButton.IsAxisMinus = true;
                            break;
                        case XINPUTMAPPING.RIGHTANALOG_RIGHT:
                            start.XInputButton.IsRightThumbX = true;
                            start.XInputButton.IsAxisMinus = false;
                            break;
                        case XINPUTMAPPING.RIGHTANALOG_UP:
                            start.XInputButton.IsRightThumbY = true;
                            start.XInputButton.IsAxisMinus = false;
                            break;
                        case XINPUTMAPPING.RIGHTANALOG_DOWN:
                            start.XInputButton.IsRightThumbY = true;
                            start.XInputButton.IsAxisMinus = true;
                            break;
                        case XINPUTMAPPING.LEFTTRIGGER:
                            start.XInputButton.IsLeftTrigger = true;
                            break;
                        case XINPUTMAPPING.RIGHTTRIGGER:
                            start.XInputButton.IsRightTrigger = true;
                            break;

                    }
                }
                else if (c[key].Type == "button" || c[key].Type == "hat")
                {
                    var button = ctl.GetXInputButtonFlags(key);
                    if (button != XInputButtonFlags.NONE)
                    {
                        start.XInputButton.IsLeftThumbX = false;
                        start.XInputButton.IsRightThumbX = false;
                        start.XInputButton.IsLeftThumbY = false;
                        start.XInputButton.IsRightThumbY = false;
                        start.XInputButton.IsAxisMinus = false;
                        start.XInputButton.IsLeftTrigger = false;
                        start.XInputButton.IsRightTrigger = false;
                        start.XInputButton.XInputIndex = deviceNumber;
                        start.XInputButton.ButtonIndex = 0;
                        start.XInputButton.IsButton = true;
                        start.XInputButton.ButtonCode = (short)button;

                        string bindName = GetXInputName(key, ctl, reverseAxis);
                        start.BindNameXi = "Input Device " + deviceNumber.ToString() + " " + bindName;
                        start.BindName = "Input Device " + deviceNumber.ToString() + " " + bindName;
                    }
                }
            }
        }

        private static void ImportDirectInputButton(GameProfile userProfile, Controller ctrl, InputKey key, params InputMapping[] mapping)
        {
            var info = ctrl.DirectInput;
            if (info == null)
                return;

            var start = userProfile.JoystickButtons.FirstOrDefault(j => !j.HideWithDirectInput && mapping.Contains(j.InputMapping));
            if (start == null)
                return;

            key = key.GetRevertedAxis(out bool reverseAxis);

            if (key == InputKey.r2 || key == InputKey.l2)
                reverseAxis = true;

            var input = ctrl.GetDirectInputMapping(key);
            if (input != null)
            {
                start.DirectInputButton = new JoystickButton
                {
                    JoystickGuid = info.InstanceGuid,
                    IsAxis = false,
                    IsAxisMinus = false,
                    IsFullAxis = false,
                    IsReverseAxis = false,
                    PovDirection = 0
                };
                start.DirectInputButton.IsReverseAxis = false;
                start.DirectInputButton.Button = 0;

                if (input.Type == "button")
                    start.DirectInputButton.Button = (int)input.Id + 48;
                else if (input.Type == "hat")
                {
                    start.DirectInputButton.Button = 32;
                    if (input.Value == 1) // Top
                        start.DirectInputButton.PovDirection = 0;
                    else if (input.Value == 4) // Down
                        start.DirectInputButton.PovDirection = 18000;
                    else if (input.Value == 8) // Left
                        start.DirectInputButton.PovDirection = 27000;
                    else if (input.Value == 2) // Right
                        start.DirectInputButton.PovDirection = 9000;
                }
                else if (input.Type == "axis")
                {
                    start.DirectInputButton.Button = (int)input.Id * 4;
                    start.DirectInputButton.IsAxis = true;
                    start.DirectInputButton.IsAxisMinus = reverseAxis ? input.Value > 0 : input.Value < 0;
                }

                string bindName = GetDInputName(key, ctrl, reverseAxis);
                start.BindNameDi = "Gamepad " + bindName;
                start.BindName = "Gamepad " + bindName;
            }
        }

        static readonly string[] mappingPaths =
        {            
            // User specific
            "{userpath}\\inputmapping\\teknoparrot.yml",

            // Lumaca Default
            "{systempath}\\resources\\inputmapping\\teknoparrot.yml",
        };

        private readonly static Dictionary<string, InputKey> ymlButtonMapping = new Dictionary<string, InputKey>()
        {
            { "r3", InputKey.righttrigger },
            { "l3", InputKey.lefttrigger },
            { "select", InputKey.select },
            { "start", InputKey.start },
            { "righttrigger", InputKey.r2 },
            { "lefttrigger", InputKey.l2 },
            { "rightshoulder", InputKey.pagedown },
            { "leftshoulder", InputKey.pageup },
            { "south", InputKey.a },
            { "north", InputKey.x },
            { "west", InputKey.y },
            { "east", InputKey.b },
            { "up", InputKey.up },
            { "left", InputKey.left },
            { "down", InputKey.down },
            { "right", InputKey.right },
            { "leftstickleft", InputKey.leftanalogleft },
            { "leftstickright", InputKey.leftanalogright },
            { "leftstickup", InputKey.leftanalogup },
            { "leftstickdown", InputKey.leftanalogdown },
            { "rightstickleft", InputKey.rightanalogleft },
            { "rightstickright", InputKey.rightanalogright },
            { "rightstickup", InputKey.rightanalogup },
            { "rightstickdown", InputKey.rightanalogdown }
        };

        private static string GetXInputName(InputKey key, Controller c, bool revertAxis = false)
        {
            var input = c.Config[key];
            if (input == null)
                return "UNKNOWN";

            switch (input.Type)
            {
                case "button":
                    {
                        switch (input.Id)
                        {
                            case 0: return "A";
                            case 1: return "B";
                            case 2: return "X";
                            case 3: return "Y";
                            case 4: return "LeftShoulder";
                            case 5: return "RightShoulder";
                            case 6: return "Back";
                            case 7: return "Start";
                            case 8: return "LeftThumb";
                            case 9: return "RightThumb";
                            case 10: return "RightThumb";
                            default: return "UNKNOWN";
                        }
                    }

                case "hat":
                    {
                        switch (input.Value)
                        {
                            case 1: return "DPadUp";
                            case 2: return "DPadRight";
                            case 4: return "DPadDown";
                            case 8: return "DPadLeft";
                            default: return "UNKNOWN";
                        }
                    }
                case "axis":
                    {
                        switch (input.Id)
                        {
                            case 0:
                                if ((!revertAxis && input.Value > 0) || (revertAxis && input.Value < 0))
                                    return "LeftThumbInput Device zzz X+";
                                else return "LeftThumbInput Device zzz X-";
                            case 1:
                                if ((!revertAxis && input.Value > 0) || (revertAxis && input.Value < 0))
                                    return "LeftThumbInput Device zzz Y-";
                                else return "LeftThumbInput Device zzz Y+";
                            case 2:
                                if ((!revertAxis && input.Value > 0) || (revertAxis && input.Value < 0))
                                    return "RightThumbInput Device zzz X+";
                                else return "RightThumbInput Device zzz X-";
                            case 3:
                                if ((!revertAxis && input.Value > 0) || (revertAxis && input.Value < 0))
                                    return "RightThumbInput Device zzz Y-";
                                else return "RightThumbInput Device zzz Y+";
                            case 4:
                                return "LeftTrigger";
                            case 5:
                                return "RightTrigger";
                            default: 
                                return "UNKNOWN";
                        }
                    }
            }
            return "UNKNOWN";
        }

        private static string GetDInputName(InputKey key, Controller c, bool revertAxis = false)
        {
            var input = c.GetDirectInputMapping(key);
            if (input == null)
                return "UNKNOWN";

            switch (input.Type)
            {
                case "button":
                    {
                        return "Buttons" + input.Id.ToString();
                    }

                case "hat":
                    {
                        switch (input.Value)
                        {
                            case 1: return "PointOfViewControllers0 Up";
                            case 2: return "PointOfViewControllers0 Right";
                            case 4: return "PointOfViewControllers0 Down";
                            case 8: return "PointOfViewControllers0 Left";
                            default: return "UNKNOWN";
                        }
                    }
                case "axis":
                    {
                        Int64 pid = input.Id;
                        if (c.IsXInputDevice && input.Id == 5)
                            pid = 2;

                        switch (pid)
                        {
                            case 0:
                                if ((!revertAxis && input.Value > 0) || (revertAxis && input.Value < 0))
                                    return "X +";
                                else return "X -";
                            case 1:
                                if ((!revertAxis && input.Value > 0) || (revertAxis && input.Value < 0))
                                    return "Y +";
                                else return "Y -";
                            case 2:
                                if ((!revertAxis && input.Value > 0) || (revertAxis && input.Value < 0))
                                    return "Z +";
                                else return "Z -";
                            case 3:
                                if ((!revertAxis && input.Value > 0) || (revertAxis && input.Value < 0))
                                    return "RotationX +";
                                else return "RotationX -";
                            case 4:
                                if ((!revertAxis && input.Value > 0) || (revertAxis && input.Value < 0))
                                    return "RotationY +";
                                else return "RotationY -";
                            case 5:
                                if ((!revertAxis && input.Value > 0) || (revertAxis && input.Value < 0))
                                    return "RotationZ +";
                                else return "RotationZ -";
                            default:
                                return "UNKNOWN";
                        }
                    }
            }
            return "UNKNOWN";
        }

        private static int GetNumberOfPlayers(GameProfile userProfile)
        {
            // Look for number of players based on string search in userprofile file (Px, Player x)
            if (userProfile.JoystickButtons.Any(j => j.ButtonName.Contains("P6") || j.ButtonName.Contains("Player 6")))
                return 6;
            else if (userProfile.JoystickButtons.Any(j => j.ButtonName.Contains("P5") || j.ButtonName.Contains("Player 5")))
                return 5;
            else if (userProfile.JoystickButtons.Any(j => j.ButtonName.Contains("P4") || j.ButtonName.Contains("Player 4")))
                return 4;
            else if (userProfile.JoystickButtons.Any(j => j.ButtonName.Contains("P3") || j.ButtonName.Contains("Player 3")))
                return 3;
            else if (userProfile.JoystickButtons.Any(j => j.ButtonName.Contains("P2") || j.ButtonName.Contains("Player 2")))
                return 2;
            else
                return 1;
        }

        private static readonly List<string> P1exclude = new List<string> 
        { 
            "P2", "Player 2", "P3", "Player 3", "P4", "Player 4", "P5", "Player 5", "P6", "Player 6", 
            "Coin 2", "Coin Chute 2", "Coin2", "Service 2",
            "Coin 3", "Coin Chute 3", "Coin3",
            "Coin 4", "Coin Chute 4", "Coin4",
            "Coin 5", "Coin Chute 5", "Coin5",
            "Coin 6", "Coin Chute 6", "Coin6"
        };
        private static readonly List<string> P2include = new List<string> { "P2", "Player 2", "Coin 2", "Coin Chute 2", "Coin2" };
        private static readonly List<string> P3include = new List<string> { "P3", "Player 3", "Coin 3", "Coin Chute 3", "Coin3" };
        private static readonly List<string> P4include = new List<string> { "P4", "Player 4", "Coin 4", "Coin Chute 4", "Coin4" };
        private static readonly List<string> P5include = new List<string> { "P5", "Player 5" };
        private static readonly List<string> P6include = new List<string> { "P6", "Player 6" };
    }

    static class Exts
    {
        public static bool HasAnyXInputButton(this GameProfile pthi, params InputMapping[] lists)
        {
            return pthi.JoystickButtons.Any(j => !j.HideWithXInput && lists.Contains(j.InputMapping));
        }
    }
}
