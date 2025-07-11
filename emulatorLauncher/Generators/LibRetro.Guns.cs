﻿using System.Collections.Generic;
using System.Linq;
using System.Text;
using EmulatorLauncher.Common.FileFormats;
using EmulatorLauncher.Common.Lightguns;
using EmulatorLauncher.Common;
using EmulatorLauncher.Common.EmulationStation;

namespace EmulatorLauncher.Libretro
{
    partial class LibRetroGenerator : Generator
    {
        private bool _sindenSoft = false;

        public bool HasMultipleGuns()
        {
            if (!SystemConfig.getOptBoolean("use_guns"))
                return false;

            var guns = RawLightgun.GetRawLightguns();
            if (guns.Length < 2)
                return false;

            int gunCount = RawLightgun.GetUsableLightGunCount();
            if (gunCount < 2)
                return false;

            // Set multigun to true in some cases
            return true;
        }

        /// <summary>
        /// Injects guns settings
        /// </summary>
        /// <param name="retroarchConfig"></param>
        /// <param name="deviceType"></param>
        /// <param name="playerIndex"></param>
        private void SetupLightGuns(ConfigFile retroarchConfig, string deviceType, string core, int playerIndex = 1)
        {
            if (!SystemConfig.getOptBoolean("use_guns"))
                return;

            bool multigun = false;

            // Used in some specific cases to invert trigger and reload buttons (for example with wiizapper)
            bool guninvert = SystemConfig.isOptSet("gun_invert") && SystemConfig.getOptBoolean("gun_invert");

            // Force to use only one gun even when multiple gun devices / mouses are connected
            bool useOneGun = SystemConfig.isOptSet("one_gun") && SystemConfig.getOptBoolean("one_gun");

            int gunCount = RawLightgun.GetUsableLightGunCount();
            var guns = RawLightgun.GetRawLightguns();
            SimpleLogger.Instance.Info("[LightGun] Found " + gunCount + " usable guns.");

            if (guns.Any(g => g.Type == RawLighGunType.SindenLightgun))
            {
                Guns.StartSindenSoftware();
                _sindenSoft = true;
            }

            // Set multigun to true in some cases
            // Case 1 = multiple guns are connected, playerindex is 1 and user did not force 'one gun only'
            if (gunCount > 1 && guns.Length > 1 && playerIndex == 1 && !useOneGun)
            {
                SimpleLogger.Instance.Info("[LightGun] Multigun enabled.");
                multigun = true;
            }

            SimpleLogger.Instance.Info("[LightGun] Perform generic lightgun configuration.");

            // Single player - assign buttons of joystick linked with playerIndex to gun buttons
            if (!multigun)
            {
                retroarchConfig["input_driver"] = "dinput";
                // Get gamepad buttons to assign them so that controller buttons can be used along with gun
                string a_padbutton = retroarchConfig["input_player" + playerIndex + "_a_btn"];
                string b_padbutton = retroarchConfig["input_player" + playerIndex + "_b_btn"];
                string c_padbutton = retroarchConfig["input_player" + playerIndex + "_y_btn"];
                string start_padbutton = retroarchConfig["input_player" + playerIndex + "_start_btn"];
                string select_padbutton = retroarchConfig["input_player" + playerIndex + "_select_btn"];
                string up_padbutton = retroarchConfig["input_player" + playerIndex + "_up_btn"];
                string down_padbutton = retroarchConfig["input_player" + playerIndex + "_down_btn"];
                string left_padbutton = retroarchConfig["input_player" + playerIndex + "_left_btn"];
                string right_padbutton = retroarchConfig["input_player" + playerIndex + "_right_btn"];

                // Set mouse buttons for one player (default mapping)
                retroarchConfig["input_libretro_device_p" + playerIndex] = deviceType;
                retroarchConfig["input_player" + playerIndex + "_mouse_index"] = "0";
                retroarchConfig["input_player" + playerIndex + "_gun_trigger_mbtn"] = guninvert ? "2" : "1";
                retroarchConfig["input_player" + playerIndex + "_gun_offscreen_shot_mbtn"] = guninvert ? "1" : "2";
                retroarchConfig["input_player" + playerIndex + "_gun_start_mbtn"] = "3";

                // Assign gamepad buttons to gun buttons
                if (select_padbutton != "" && select_padbutton != null)
                    retroarchConfig["input_player" + playerIndex + "_gun_select_btn"] = select_padbutton;
                if (start_padbutton != "" && start_padbutton != null)
                    retroarchConfig["input_player" + playerIndex + "_gun_start_btn"] = start_padbutton;
                if (a_padbutton != "" && a_padbutton != null)
                    retroarchConfig["input_player" + playerIndex + "_gun_aux_a_btn"] = a_padbutton;
                if (b_padbutton != "" && b_padbutton != null)
                    retroarchConfig["input_player" + playerIndex + "_gun_aux_b_btn"] = b_padbutton;
                if (c_padbutton != "" && c_padbutton != null)
                    retroarchConfig["input_player" + playerIndex + "_gun_aux_c_btn"] = c_padbutton;
                if (up_padbutton != "" && up_padbutton != null)
                    retroarchConfig["input_player" + playerIndex + "_gun_dpad_up_btn"] = up_padbutton;
                if (down_padbutton != "" && down_padbutton != null)
                    retroarchConfig["input_player" + playerIndex + "_gun_dpad_down_btn"] = down_padbutton;
                if (left_padbutton != "" && left_padbutton != null)
                    retroarchConfig["input_player" + playerIndex + "_gun_dpad_left_btn"] = left_padbutton;
                if (right_padbutton != "" && right_padbutton != null)
                    retroarchConfig["input_player" + playerIndex + "_gun_dpad_right_btn"] = right_padbutton;

                retroarchConfig["input_player" + playerIndex + "_analog_dpad_mode"] = "0";

                string joypadIndex = retroarchConfig["input_player" + playerIndex + "_joypad_index"];
                retroarchConfig["input_player" + playerIndex + "_joypad_index"] = joypadIndex;
            }

            // Multigun case
            else
            {
                // DirectInput does not differenciate mouse indexes. We have to use "Raw" with multiple guns
                retroarchConfig["input_driver"] = "raw";

                // Set mouse buttons for multigun
                for (int i = 1; i <= guns.Length; i++)
                {
                    // Get gamepad buttons to assign them so that controller buttons can be used along with gun
                    string a_padbutton = retroarchConfig["input_player" + i + "_a_btn"];
                    string b_padbutton = retroarchConfig["input_player" + i + "_b_btn"];
                    string c_padbutton = retroarchConfig["input_player" + i + "_y_btn"];
                    string start_padbutton = retroarchConfig["input_player" + i + "_start_btn"];
                    string select_padbutton = retroarchConfig["input_player" + i + "_select_btn"];
                    string up_padbutton = retroarchConfig["input_player" + i + "_up_btn"];
                    string down_padbutton = retroarchConfig["input_player" + i + "_down_btn"];
                    string left_padbutton = retroarchConfig["input_player" + i + "_left_btn"];
                    string right_padbutton = retroarchConfig["input_player" + i + "_right_btn"];

                    int deviceIndex = guns[i - 1].Index; // i-1;

                    SimpleLogger.Instance.Info("[LightGun] Assigned player " + i + " to -> " + (guns[i - 1].Name != null ? guns[i-1].Name.ToString() : "") + " index: " + guns[i-1].Index.ToString());

                    retroarchConfig["input_libretro_device_p" + i] = deviceType;

                    string gunPlayerIndex = "p" + i + "_gunIndex";
                    if (SystemConfig.isOptSet(gunPlayerIndex) && !string.IsNullOrEmpty(SystemConfig[gunPlayerIndex]))
                        retroarchConfig["input_player" + i + "_mouse_index"] = SystemConfig[gunPlayerIndex];
                    else
                        retroarchConfig["input_player" + i + "_mouse_index"] = deviceIndex.ToString();

                    retroarchConfig["input_player" + i + "_gun_trigger_mbtn"] = guninvert ? "2" : "1";
                    retroarchConfig["input_player" + i + "_gun_offscreen_shot_mbtn"] = guninvert ? "1" : "2";
                    retroarchConfig["input_player" + i + "_gun_start_mbtn"] = "3";

                    // Assign gamepad buttons to gun buttons
                    if (select_padbutton != "" && select_padbutton != null)
                        retroarchConfig["input_player" + i + "_gun_select_btn"] = select_padbutton;
                    if (start_padbutton != "" && start_padbutton != null)
                        retroarchConfig["input_player" + i + "_gun_start_btn"] = start_padbutton;
                    if (a_padbutton != "" && a_padbutton != null)
                        retroarchConfig["input_player" + i + "_gun_aux_a_btn"] = a_padbutton;
                    if (b_padbutton != "" && b_padbutton != null)
                        retroarchConfig["input_player" + i + "_gun_aux_b_btn"] = b_padbutton;
                    if (c_padbutton != "" && c_padbutton != null)
                        retroarchConfig["input_player" + i + "_gun_aux_c_btn"] = c_padbutton;
                    if (up_padbutton != "" && up_padbutton != null)
                        retroarchConfig["input_player" + i + "_gun_dpad_up_btn"] = up_padbutton;
                    if (down_padbutton != "" && down_padbutton != null)
                        retroarchConfig["input_player" + i + "_gun_dpad_down_btn"] = down_padbutton;
                    if (left_padbutton != "" && left_padbutton != null)
                        retroarchConfig["input_player" + i + "_gun_dpad_left_btn"] = left_padbutton;
                    if (right_padbutton != "" && right_padbutton != null)
                        retroarchConfig["input_player" + i + "_gun_dpad_right_btn"] = right_padbutton;

                    retroarchConfig["input_player" + i + "_analog_dpad_mode"] = "0";

                    string joypadIndex = retroarchConfig["input_player" + i + "_joypad_index"];
                    retroarchConfig["input_player" + i + "_joypad_index"] = joypadIndex;
                }
            }

            // Clean up unused mapping after last gun used
            if (useOneGun)
            {
                // If playerindex is 2, nullify player 1 gun buttons
                if (playerIndex == 2)
                {
                    foreach (string cfg in gunButtons)
                        retroarchConfig["input_player1" + cfg] = "nul";
                }

                // Nullify all buttons after playerindex
                if (guns.Length <= 16)
                {
                    for (int i = playerIndex + 1; i == 16; i++)
                    {
                        foreach (string cfg in gunButtons)
                            retroarchConfig["input_player" + i + cfg] = "nul";
                    }
                }
            }
            else
            {
                // Nullify all buttons after guns.length
                if (guns.Length <= 16)
                {
                    for (int i = guns.Length + 1; i == 16; i++)
                    {
                        foreach (string cfg in gunButtons)
                            retroarchConfig["input_player" + i + cfg] = "nul";
                    }
                }
            }

            // Set additional buttons gun mapping default ...
            if (!LibretroGunCoreInfo.Instance.ContainsKey(core))
                ConfigureLightgunKeyboardActions(retroarchConfig, playerIndex, guns);

            // ... or configure core specific mappings            
            else
                ConfigureGunsCore(retroarchConfig, playerIndex, core, deviceType, guninvert, multigun);
        }

        /// <summary>
        /// Injects keyboard actions for lightgun games
        /// </summary>
        /// <param name="retroarchConfig"></param>
        /// <param name="playerIndex"></param>
        private void ConfigureLightgunKeyboardActions(ConfigFile retroarchConfig, int playerIndex, RawLightgun[] guns)
        {
            if (!SystemConfig.getOptBoolean("use_guns"))
                return;

            SimpleLogger.Instance.Info("[LightGun] Perform standard lightgun buttons configuration for keyboard.");

            switch (playerIndex)
            {
                case 1:
                    if (guns[0].Type == RawLighGunType.MayFlashWiimote && SystemConfig.isOptSet("WiimoteMode") && !string.IsNullOrEmpty(SystemConfig["WiimoteMode"]))
                    {
                        string WiimoteMode = SystemConfig["WiimoteMode"];

                        if (WiimoteMode == "normal")
                        {
                            retroarchConfig["input_player" + playerIndex + "_start"] = "num1";
                            retroarchConfig["input_player" + playerIndex + "_select"] = "num5";
                            retroarchConfig["input_player" + playerIndex + "_gun_start"] = "pageup";
                            retroarchConfig["input_player" + playerIndex + "_gun_select"] = "pagedown";
                            retroarchConfig["input_player" + playerIndex + "_gun_aux_a"] = "left";
                            retroarchConfig["input_player" + playerIndex + "_gun_aux_b"] = "down";
                            retroarchConfig["input_player" + playerIndex + "_gun_aux_c"] = "right";
                            retroarchConfig["input_player" + playerIndex + "_gun_dpad_up"] = "up";
                            retroarchConfig["input_player" + playerIndex + "_gun_dpad_down"] = "down";
                            retroarchConfig["input_player" + playerIndex + "_gun_dpad_left"] = "left";
                            retroarchConfig["input_player" + playerIndex + "_gun_dpad_right"] = "right";
                        }
                        else if (WiimoteMode == "game")
                        {
                            retroarchConfig["input_player" + playerIndex + "_start"] = "num1";
                            retroarchConfig["input_player" + playerIndex + "_select"] = "num5";
                            retroarchConfig["input_player" + playerIndex + "_gun_start"] = "enter";
                            retroarchConfig["input_player" + playerIndex + "_gun_select"] = "escape";
                            retroarchConfig["input_player" + playerIndex + "_gun_aux_a"] = "left";
                            retroarchConfig["input_player" + playerIndex + "_gun_aux_b"] = "down";
                            retroarchConfig["input_player" + playerIndex + "_gun_aux_c"] = "right";
                            retroarchConfig["input_player" + playerIndex + "_gun_dpad_up"] = "up";
                            retroarchConfig["input_player" + playerIndex + "_gun_dpad_down"] = "down";
                            retroarchConfig["input_player" + playerIndex + "_gun_dpad_left"] = "left";
                            retroarchConfig["input_player" + playerIndex + "_gun_dpad_right"] = "right";
                        }
                        else
                        {
                            retroarchConfig["input_player" + playerIndex + "_start"] = "num1";
                            retroarchConfig["input_player" + playerIndex + "_select"] = "num5";
                            retroarchConfig["input_player" + playerIndex + "_gun_start"] = "enter";
                            retroarchConfig["input_player" + playerIndex + "_gun_select"] = "backspace";
                            retroarchConfig["input_player" + playerIndex + "_gun_aux_a"] = "q";
                            retroarchConfig["input_player" + playerIndex + "_gun_dpad_up"] = "up";
                            retroarchConfig["input_player" + playerIndex + "_gun_dpad_down"] = "down";
                            retroarchConfig["input_player" + playerIndex + "_gun_dpad_left"] = "left";
                            retroarchConfig["input_player" + playerIndex + "_gun_dpad_right"] = "right";
                        }
                    }
                    else if (guns[0].Type == RawLighGunType.Mouse && SystemConfig.isOptSet("WiimoteMode") && SystemConfig["WiimoteMode"] == "wiimotegun")
                    {
                        retroarchConfig["input_player" + playerIndex + "_start"] = "num1";
                        retroarchConfig["input_player" + playerIndex + "_select"] = "num5";
                        retroarchConfig["input_player" + playerIndex + "_gun_start"] = "num1";
                        retroarchConfig["input_player" + playerIndex + "_gun_select"] = "num5";
                        retroarchConfig["input_player" + playerIndex + "_gun_aux_a"] = "rctrl";
                        retroarchConfig["input_player" + playerIndex + "_gun_aux_b"] = "enter";
                        retroarchConfig["input_player" + playerIndex + "_gun_dpad_up"] = "up";
                        retroarchConfig["input_player" + playerIndex + "_gun_dpad_down"] = "down";
                        retroarchConfig["input_player" + playerIndex + "_gun_dpad_left"] = "left";
                        retroarchConfig["input_player" + playerIndex + "_gun_dpad_right"] = "right";
                    }
                    else
                    {
                        retroarchConfig["input_player" + playerIndex + "_start"] = "num1";
                        retroarchConfig["input_player" + playerIndex + "_select"] = "num5";
                        retroarchConfig["input_player" + playerIndex + "_gun_start"] = "num1";
                        retroarchConfig["input_player" + playerIndex + "_gun_select"] = "num5";
                        retroarchConfig["input_player" + playerIndex + "_gun_aux_a"] = "q";
                        retroarchConfig["input_player" + playerIndex + "_gun_dpad_up"] = "up";
                        retroarchConfig["input_player" + playerIndex + "_gun_dpad_down"] = "down";
                        retroarchConfig["input_player" + playerIndex + "_gun_dpad_left"] = "left";
                        retroarchConfig["input_player" + playerIndex + "_gun_dpad_right"] = "right";
                    }
                    break;
                case 2:
                    retroarchConfig["input_player" + playerIndex + "_start"] = "num2";
                    retroarchConfig["input_player" + playerIndex + "_select"] = "num6";
                    retroarchConfig["input_player" + playerIndex + "_gun_start"] = "num2";
                    retroarchConfig["input_player" + playerIndex + "_gun_select"] = "num6";
                    retroarchConfig["input_player" + playerIndex + "_gun_aux_a"] = "s";
                    retroarchConfig["input_player" + playerIndex + "_gun_dpad_up"] = "u";
                    retroarchConfig["input_player" + playerIndex + "_gun_dpad_down"] = "v";
                    retroarchConfig["input_player" + playerIndex + "_gun_dpad_left"] = "w";
                    retroarchConfig["input_player" + playerIndex + "_gun_dpad_right"] = "x";
                    break;
                case 3:
                    retroarchConfig["input_player" + playerIndex + "_start"] = "num3";
                    retroarchConfig["input_player" + playerIndex + "_select"] = "num7";
                    retroarchConfig["input_player" + playerIndex + "_gun_start"] = "num3";
                    retroarchConfig["input_player" + playerIndex + "_gun_select"] = "num7";
                    retroarchConfig["input_player" + playerIndex + "_gun_aux_a"] = "t";
                    retroarchConfig["input_player" + playerIndex + "_gun_dpad_up"] = "r";
                    retroarchConfig["input_player" + playerIndex + "_gun_dpad_down"] = "f";
                    retroarchConfig["input_player" + playerIndex + "_gun_dpad_left"] = "d";
                    retroarchConfig["input_player" + playerIndex + "_gun_dpad_right"] = "g";
                    break;
                case 4:
                    retroarchConfig["input_player" + playerIndex + "_start"] = "num4";
                    retroarchConfig["input_player" + playerIndex + "_select"] = "num8";
                    retroarchConfig["input_player" + playerIndex + "_gun_start"] = "num4";
                    retroarchConfig["input_player" + playerIndex + "_gun_select"] = "num8";
                    retroarchConfig["input_player" + playerIndex + "_gun_aux_a"] = "y";
                    retroarchConfig["input_player" + playerIndex + "_gun_dpad_up"] = "i";
                    retroarchConfig["input_player" + playerIndex + "_gun_dpad_down"] = "k";
                    retroarchConfig["input_player" + playerIndex + "_gun_dpad_left"] = "j";
                    retroarchConfig["input_player" + playerIndex + "_gun_dpad_right"] = "l";
                    break;
            }
        }

        /// <summary>
        /// Dedicated core mappings for lightgun games
        private void ConfigureGunsCore(ConfigFile retroarchConfig, int playerIndex, string core, string deviceType, bool guninvert = false, bool multigun = false)
        {
            // Some systems offer multiple type of guns (justifier, guncon...). Option must be available in es_features.cfg
            if (SystemConfig.isOptSet("gun_type") && !string.IsNullOrEmpty(SystemConfig["gun_type"]) && SystemConfig["gun_type"] != "justifiers")
                deviceType = SystemConfig["gun_type"];

            var guns = RawLightgun.GetRawLightguns();
            if (guns.Length == 0)
                return;

            SimpleLogger.Instance.Info("[LightGun] Perform core specific lightgun configuration for: " + core);

            // If option in ES is forced to use one gun, only one gun will be configured on the playerIndex defined for the core
            if (!multigun)
            {
                retroarchConfig["input_driver"] = "dinput";
                // Set deviceType and DeviceIndex
                retroarchConfig["input_libretro_device_p" + playerIndex] = deviceType;
                retroarchConfig["input_player" + playerIndex + "_mouse_index"] = "0";

                // Set mouse buttons (mouse only has 3 buttons, that can be mapped differently for each core)
                retroarchConfig["input_player" + playerIndex + "_gun_trigger_mbtn"] = guninvert ? "2" : "1";
                retroarchConfig["input_player" + playerIndex + "_gun_offscreen_shot_mbtn"] = GetCoreMouseButton(core, guninvert, "reload");
                retroarchConfig["input_player" + playerIndex + "_gun_aux_a_mbtn"] = GetCoreMouseButton(core, guninvert, "aux_a");
                retroarchConfig["input_player" + playerIndex + "_gun_aux_b_mbtn"] = GetCoreMouseButton(core, guninvert, "aux_b");
                retroarchConfig["input_player" + playerIndex + "_gun_aux_c_mbtn"] = GetCoreMouseButton(core, guninvert, "aux_c");
                retroarchConfig["input_player" + playerIndex + "_gun_start_mbtn"] = GetCoreMouseButton(core, guninvert, "start");
                retroarchConfig["input_player" + playerIndex + "_gun_select_mbtn"] = GetCoreMouseButton(core, guninvert, "select");

                retroarchConfig["input_player" + playerIndex + "_analog_dpad_mode"] = "0";

                string joypadIndex = retroarchConfig["input_player" + playerIndex + "_joypad_index"];
                retroarchConfig["input_player" + playerIndex + "_joypad_index"] = joypadIndex;

                var ctrl = Controllers.Where(c => c.Name != "Keyboard" && c.Config != null && c.Config.Input != null).Select(c => c.Config).FirstOrDefault();

                // For first player (or player 2 if playerindex is 2 and player 1 has a gamepad), we set keyboard keys to auxiliary gun buttons
                if (playerIndex == 1 || ctrl != null)
                {
                    // Start always set to 1 and select to 5
                    retroarchConfig["input_player" + playerIndex + "_start"] = "num1";
                    retroarchConfig["input_player" + playerIndex + "_select"] = "num5";

                    if (guns[0].Type == RawLighGunType.MayFlashWiimote && SystemConfig.isOptSet("WiimoteMode") && !string.IsNullOrEmpty(SystemConfig["WiimoteMode"]))
                    {
                        string WiimoteMode = SystemConfig["WiimoteMode"];
                        if (WiimoteMode == "normal")
                        {
                            retroarchConfig["input_player" + playerIndex + "_gun_start"] = "pageup";
                            retroarchConfig["input_player" + playerIndex + "_gun_select"] = "pagedown";
                        }
                        else if (WiimoteMode == "game")
                        {
                            retroarchConfig["input_player" + playerIndex + "_gun_start"] = "enter";
                            retroarchConfig["input_player" + playerIndex + "_gun_select"] = "escape";
                        }
                    }
                    else if (guns[0].Type == RawLighGunType.MayFlashWiimote)
                    {
                        retroarchConfig["input_player" + playerIndex + "_gun_start"] = "enter";
                        retroarchConfig["input_player" + playerIndex + "_gun_select"] = "backspace";
                    }
                    else if (guns[0].Type == RawLighGunType.Mouse && SystemConfig.isOptSet("WiimoteMode") && SystemConfig["WiimoteMode"] == "wiimotegun")
                    {
                        retroarchConfig["input_player" + playerIndex + "_gun_start"] = "num1";
                        retroarchConfig["input_player" + playerIndex + "_gun_select"] = "num5";
                    }
                    else
                    {
                        retroarchConfig["input_player" + playerIndex + "_gun_start"] = "num1";
                        retroarchConfig["input_player" + playerIndex + "_gun_select"] = "num5";
                    }

                    // Auxiliary buttons can be set to directions if using a wiimote
                    if (SystemConfig.isOptSet("gun_ab") && SystemConfig["gun_ab"] == "directions")
                    {
                        retroarchConfig["input_player" + playerIndex + "_gun_aux_a"] = "left";
                        retroarchConfig["input_player" + playerIndex + "_gun_aux_b"] = "right";
                        retroarchConfig["input_player" + playerIndex + "_gun_aux_c"] = "up";
                    }
                    
                    // By default auxiliary buttons are set to keys defined in es_input for the keyboard
                    else
                    {
                        if (guns[0].Type == RawLighGunType.MayFlashWiimote && SystemConfig.isOptSet("WiimoteMode") && !string.IsNullOrEmpty(SystemConfig["WiimoteMode"]))
                        {
                            string WiimoteMode = SystemConfig["WiimoteMode"];
                            if (WiimoteMode == "normal")
                            {
                                retroarchConfig["input_player" + playerIndex + "_gun_aux_a"] = "left";
                                retroarchConfig["input_player" + playerIndex + "_gun_aux_b"] = "down";
                                retroarchConfig["input_player" + playerIndex + "_gun_aux_c"] = "right";
                            }
                            else if (WiimoteMode == "game")
                            {
                                retroarchConfig["input_player" + playerIndex + "_gun_aux_a"] = "left";
                                retroarchConfig["input_player" + playerIndex + "_gun_aux_b"] = "down";
                                retroarchConfig["input_player" + playerIndex + "_gun_aux_c"] = "right";
                            }
                            else
                            {
                                retroarchConfig["input_player" + playerIndex + "_gun_aux_a"] = "q";
                            }
                        }
                        else if (guns[0].Type == RawLighGunType.Mouse && SystemConfig.isOptSet("WiimoteMode") && SystemConfig["WiimoteMode"] == "wiimotegun")
                        {
                            retroarchConfig["input_player" + playerIndex + "_gun_aux_a"] = "rctrl";
                            retroarchConfig["input_player" + playerIndex + "_gun_aux_b"] = "enter";
                        }
                        else
                        {
                            retroarchConfig["input_player" + playerIndex + "_gun_aux_a"] = "q";
                            retroarchConfig["input_player" + playerIndex + "_gun_aux_b"] = "s";
                            retroarchConfig["input_player" + playerIndex + "_gun_aux_c"] = "d";
                        }
                    }
                }

                // Additionaly we configure keyboard buttons for directions
                retroarchConfig["input_player" + playerIndex + "_gun_dpad_up"] = "up";
                retroarchConfig["input_player" + playerIndex + "_gun_dpad_down"] = "down";
                retroarchConfig["input_player" + playerIndex + "_gun_dpad_left"] = "left";
                retroarchConfig["input_player" + playerIndex + "_gun_dpad_right"] = "right";

                // Case of justifiers (use port 3)
                if (SystemConfig.isOptSet("gun_type") && SystemConfig["gun_type"] == "justifiers" && guns[1] != null && playerIndex == 2)
                {
                    retroarchConfig["input_driver"] = "raw";
                    int deviceIndex2 = guns[1].Index;
                    retroarchConfig["input_libretro_device_p3"] = "772";

                    if (SystemConfig.isOptSet("p2_gunIndex") && !string.IsNullOrEmpty(SystemConfig["p2_gunIndex"]))
                        retroarchConfig["input_player3_mouse_index"] = SystemConfig["p2_gunIndex"];
                    else
                        retroarchConfig["input_player3_mouse_index"] = deviceIndex2.ToString();

                    retroarchConfig["input_player3_gun_trigger_mbtn"] = guninvert ? "2" : "1";
                    retroarchConfig["input_player3_gun_offscreen_shot_mbtn"] = GetCoreMouseButton(core, guninvert, "reload");
                    retroarchConfig["input_player3_gun_aux_a_mbtn"] = GetCoreMouseButton(core, guninvert, "aux_a");
                    retroarchConfig["input_player3_gun_aux_b_mbtn"] = GetCoreMouseButton(core, guninvert, "aux_b");
                    retroarchConfig["input_player3_gun_aux_c_mbtn"] = GetCoreMouseButton(core, guninvert, "aux_c");
                    retroarchConfig["input_player3_gun_start_mbtn"] = GetCoreMouseButton(core, guninvert, "start");
                    retroarchConfig["input_player3_gun_select_mbtn"] = GetCoreMouseButton(core, guninvert, "select");
                    retroarchConfig["input_player3_analog_dpad_mode"] = "0";

                    string joypadIndex2 = retroarchConfig["input_player3_joypad_index"];
                    retroarchConfig["input_player3_joypad_index"] = joypadIndex2;

                    // Delete keyboard assignment
                    retroarchConfig["input_player3_a"] = "nul";
                    retroarchConfig["input_player3_b"] = "nul";
                    retroarchConfig["input_player3_x"] = "nul";
                    retroarchConfig["input_player3_y"] = "nul";
                    retroarchConfig["input_player3_down"] = "nul";
                    retroarchConfig["input_player3_l"] = "nul";
                    retroarchConfig["input_player3_l2"] = "nul";
                    retroarchConfig["input_player3_l3"] = "nul";
                    retroarchConfig["input_player3_left"] = "nul";
                    retroarchConfig["input_player3_r"] = "nul";
                    retroarchConfig["input_player3_r2"] = "nul";
                    retroarchConfig["input_player3_r3"] = "nul";
                    retroarchConfig["input_player3_right"] = "nul";
                    retroarchConfig["input_player3_select"] = "num7";
                    retroarchConfig["input_player3_start"] = "num3";
                    retroarchConfig["input_player3_up"] = "nul";
                }
            }

            // Multigun case
            else
            {
                retroarchConfig["input_driver"] = "raw";
                for (int i = 1; i <= guns.Length; i++)
                {
                    int deviceIndex = guns[i - 1].Index; // i-1;

                    SimpleLogger.Instance.Info("[LightGun core] Assigned player " + i + " to -> " + (guns[i-1].Name != null ? guns[i-1].Name.ToString() : "") + " index: " + guns[i-1].Index.ToString());

                    retroarchConfig["input_libretro_device_p" + i] = deviceType;

                    string gunPlayerIndex = "p" + i + "_gunIndex";
                    if (SystemConfig.isOptSet(gunPlayerIndex) && !string.IsNullOrEmpty(SystemConfig[gunPlayerIndex]))
                        retroarchConfig["input_player" + i + "_mouse_index"] = SystemConfig[gunPlayerIndex];
                    else
                        retroarchConfig["input_player" + i + "_mouse_index"] = deviceIndex.ToString();

                    retroarchConfig["input_player" + i + "_gun_trigger_mbtn"] = guninvert ? "2" : "1";
                    retroarchConfig["input_player" + i + "_gun_offscreen_shot_mbtn"] = GetCoreMouseButton(core, guninvert, "reload");
                    retroarchConfig["input_player" + i + "_gun_aux_a_mbtn"] = GetCoreMouseButton(core, guninvert, "aux_a");
                    retroarchConfig["input_player" + i + "_gun_aux_b_mbtn"] = GetCoreMouseButton(core, guninvert, "aux_b");
                    retroarchConfig["input_player" + i + "_gun_aux_c_mbtn"] = GetCoreMouseButton(core, guninvert, "aux_c");
                    retroarchConfig["input_player" + i + "_gun_start_mbtn"] = GetCoreMouseButton(core, guninvert, "start");
                    retroarchConfig["input_player" + i + "_gun_select_mbtn"] = GetCoreMouseButton(core, guninvert, "select");

                    retroarchConfig["input_player" + i + "_analog_dpad_mode"] = "0";

                    string joypadIndex = retroarchConfig["input_player" + i + "_joypad_index"];
                    retroarchConfig["input_player" + i + "_joypad_index"] = joypadIndex;

                    if (i == 1)
                    {
                        retroarchConfig["input_player" + i + "_start"] = "num1";
                        retroarchConfig["input_player" + i + "_select"] = "num5";

                        if (guns[0].Type == RawLighGunType.MayFlashWiimote && SystemConfig.isOptSet("WiimoteMode") && !string.IsNullOrEmpty(SystemConfig["WiimoteMode"]))
                        {
                            string WiimoteMode = SystemConfig["WiimoteMode"];

                            if (WiimoteMode == "normal")
                            {
                                retroarchConfig["input_player" + i + "_gun_start"] = "pageup";
                                retroarchConfig["input_player" + i + "_gun_select"] = "pagedown";
                            }
                            else if (WiimoteMode == "game")
                            {
                                retroarchConfig["input_player" + i + "_gun_start"] = "enter";
                                retroarchConfig["input_player" + i + "_gun_select"] = "escape";
                            }
                            else
                            {
                                retroarchConfig["input_player" + i + "_gun_start"] = "enter";
                                retroarchConfig["input_player" + i + "_gun_select"] = "backspace";
                            }
                        }
                        else if (guns[0].Type == RawLighGunType.MayFlashWiimote)
                        {
                            retroarchConfig["input_player" + i + "_gun_start"] = "enter";
                            retroarchConfig["input_player" + i + "_gun_select"] = "backspace";
                        }
                        else if (guns[0].Type == RawLighGunType.Mouse && SystemConfig.isOptSet("WiimoteMode") && SystemConfig["WiimoteMode"] == "wiimotegun")
                        {
                            retroarchConfig["input_player" + i + "_gun_start"] = "num1";
                            retroarchConfig["input_player" + i + "_gun_select"] = "num5";
                        }
                        else
                        {
                            retroarchConfig["input_player" + i + "_gun_start"] = "num1";
                            retroarchConfig["input_player" + i + "_gun_select"] = "num5";
                        }

                        retroarchConfig["input_player" + i + "_gun_dpad_up"] = "up";
                        retroarchConfig["input_player" + i + "_gun_dpad_down"] = "down";
                        retroarchConfig["input_player" + i + "_gun_dpad_left"] = "left";
                        retroarchConfig["input_player" + i + "_gun_dpad_right"] = "right";

                        if (SystemConfig.isOptSet("gun_ab") && SystemConfig["gun_ab"] == "directions")
                        {
                            retroarchConfig["input_player1_gun_aux_a"] = "left";
                            retroarchConfig["input_player1_gun_aux_b"] = "right";
                            retroarchConfig["input_player1_gun_aux_c"] = "up";
                        }
                        else
                        {
                            retroarchConfig["input_player1_gun_aux_a"] = "q";
                        }
                    }
                    else if (i == 2)
                    {
                        retroarchConfig["input_player" + i + "_start"] = "num2";
                        retroarchConfig["input_player" + i + "_select"] = "num6";
                        retroarchConfig["input_player" + i + "_gun_start"] = "num2";
                        retroarchConfig["input_player" + i + "_gun_select"] = "num6";
                        retroarchConfig["input_player" + i + "_gun_aux_a"] = "s";
                        retroarchConfig["input_player" + i + "_gun_dpad_up"] = "u";
                        retroarchConfig["input_player" + i + "_gun_dpad_down"] = "v";
                        retroarchConfig["input_player" + i + "_gun_dpad_left"] = "w";
                        retroarchConfig["input_player" + i + "_gun_dpad_right"] = "x";
                    }
                    else if (i == 3)
                    {
                        retroarchConfig["input_player" + i + "_start"] = "num3";
                        retroarchConfig["input_player" + i + "_select"] = "num7";
                        retroarchConfig["input_player" + i + "_gun_start"] = "num3";
                        retroarchConfig["input_player" + i + "_gun_select"] = "num7";
                        retroarchConfig["input_player" + i + "_gun_aux_a"] = "t";
                        retroarchConfig["input_player" + i + "_gun_dpad_up"] = "r";
                        retroarchConfig["input_player" + i + "_gun_dpad_down"] = "f";
                        retroarchConfig["input_player" + i + "_gun_dpad_left"] = "d";
                        retroarchConfig["input_player" + i + "_gun_dpad_right"] = "g";
                    }
                    else if (i == 4)
                    {
                        retroarchConfig["input_player" + i + "_start"] = "num4";
                        retroarchConfig["input_player" + i + "_select"] = "num8";
                        retroarchConfig["input_player" + i + "_gun_start"] = "num4";
                        retroarchConfig["input_player" + i + "_gun_select"] = "num8";
                        retroarchConfig["input_player" + i + "_gun_aux_a"] = "y";
                        retroarchConfig["input_player" + i + "_gun_dpad_up"] = "i";
                        retroarchConfig["input_player" + i + "_gun_dpad_down"] = "k";
                        retroarchConfig["input_player" + i + "_gun_dpad_left"] = "j";
                        retroarchConfig["input_player" + i + "_gun_dpad_right"] = "l";
                    }
                }
            }
        }

        // List of retroarch.cfg gun input lines (used to clean up)
        static readonly List<string> gunButtons = new List<string>()
        {
            "_mouse_index",
            "_gun_aux_a",
            "_gun_aux_a_axis",
            "_gun_aux_a_btn",
            "_gun_aux_a_mbtn",
            "_gun_aux_b",
            "_gun_aux_b_axis",
            "_gun_aux_b_btn",
            "_gun_aux_b_mbtn",
            "_gun_aux_c",
            "_gun_aux_c_axis",
            "_gun_aux_c_btn",
            "_gun_aux_c_mbtn",
            "_gun_dpad_down",
            "_gun_dpad_down_axis",
            "_gun_dpad_down_btn",
            "_gun_dpad_down_mbtn",
            "_gun_dpad_left",
            "_gun_dpad_left_axis",
            "_gun_dpad_left_btn",
            "_gun_dpad_left_mbtn",
            "_gun_dpad_right",
            "_gun_dpad_right_axis",
            "_gun_dpad_right_btn",
            "_gun_dpad_right_mbtn",
            "_gun_dpad_up",
            "_gun_dpad_up_axis",
            "_gun_dpad_up_btn",
            "_gun_dpad_up_mbtn",
            "_gun_offscreen_shot",
            "_gun_offscreen_shot_axis",
            "_gun_offscreen_shot_btn",
            "_gun_offscreen_shot_mbtn",
            "_gun_select",
            "_gun_select_axis",
            "_gun_select_btn",
            "_gun_select_mbtn",
            "_gun_start",
            "_gun_start_axis",
            "_gun_start_btn",
            "_gun_start_mbtn",
            "_gun_trigger",
            "_gun_trigger_axis",
            "_gun_trigger_btn",
            "_gun_trigger_mbtn"
        };

        // Rule to get mouse button assignment for each core, based on dictionaries
        // 2 type of cases : 
        // 1 - Classic case ==> mouse left = trigger
        // 2 - reverse case ==> mouse right = trigger
        private string GetCoreMouseButton(string core, bool guninvert, string mbtn)
        {
            bool changeReload = SystemConfig.isOptSet("gun_reload_button") && SystemConfig.getOptBoolean("gun_reload_button");

            string ret = "nul";

            LibretroGunCoreInfo conf;
            if (!LibretroGunCoreInfo.Instance.TryGetValue(core, out conf))
                conf = new LibretroGunCoreInfo() { reload = "2", aux_a = "3" };

            switch (mbtn)
            {
                case "reload":
                    ret = changeReload ? "2" : conf.reload;
                    break;
                case "aux_a":
                    ret = changeReload ? conf.aux_a_changereload : conf.aux_a;
                    break;
                case "aux_b":
                    ret = changeReload ? conf.aux_b_changereload : conf.aux_b;
                    break;
                case "aux_c":
                    ret = changeReload ? conf.aux_c_changereload : conf.aux_c;
                    break;
                case "start":
                    ret = changeReload ? conf.start_changereload : conf.start;
                    break;
                case "select":
                    ret = changeReload ? conf.select_changereload : conf.select;
                    break;
            }

            if (ret == "2" && guninvert)
                return "1";
            
            return ret;
        }

        /// <summary>
        /// Set all dictionnaries for mouse buttons (2 dictionaries for each button, one for default value, one for value when reload is forced on mouse rightclick)
        /// </summary>
        class LibretroGunCoreInfo
        {
            #region Factory
            public static Dictionary<string, LibretroGunCoreInfo> Instance
            {
                get
                {
                    if (_instance == null)
                    {
                        _instance = SimpleYml<LibretroGunCoreInfo>
                            .Parse(Encoding.UTF8.GetString(Properties.Resources.libretrocoreguns))
                            .ToDictionary(a => a.system, a => a);
                    }

                    return _instance;
                }
            }

            private static Dictionary<string, LibretroGunCoreInfo> _instance;

            public LibretroGunCoreInfo()
            {
                reload = aux_a = aux_a_changereload = aux_b = aux_b_changereload = aux_c = aux_c_changereload = start = start_changereload = select = select_changereload = "nul";
            }
            #endregion

            [YmlName]
            public string system { get; set; }
            public string reload { get; set; }
            public string aux_a { get; set; }
            public string aux_a_changereload { get; set; }
            public string aux_b { get; set; }
            public string aux_b_changereload { get; set; }
            public string aux_c { get; set; }
            public string aux_c_changereload { get; set; }
            public string start { get; set; }
            public string start_changereload { get; set; }
            public string select { get; set; }
            public string select_changereload { get; set; }
        }
    }
}