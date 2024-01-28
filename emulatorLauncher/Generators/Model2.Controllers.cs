﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using EmulatorLauncher.Common;
using EmulatorLauncher.Common.FileFormats;
using EmulatorLauncher.Common.EmulationStation;
using EmulatorLauncher.Common.Joysticks;
using System.Windows.Controls;
using System.Security.Cryptography;

namespace EmulatorLauncher
{
    partial class Model2Generator : Generator
    {
        private void ConfigureControllers(byte[] bytes, IniFile ini, string parentRom, int hexLength)
        {
            if (Program.SystemConfig.isOptSet("disableautocontrollers") && Program.SystemConfig["disableautocontrollers"] == "1")
                return;

            if (Program.SystemConfig.isOptSet("m2_joystick_autoconfig") && Program.SystemConfig["m2_joystick_autoconfig"] == "template")
                return;

            if (Program.Controllers.Count > 1)
            {
                var c1 = Program.Controllers.FirstOrDefault(c => c.PlayerIndex == 1);

                var c2 = Program.Controllers.FirstOrDefault(c => c.PlayerIndex == 2);
                if (c1.IsKeyboard)
                    WriteKbMapping(bytes, parentRom, hexLength, c1.Config);
                else
                    WriteJoystickMapping(bytes, parentRom, hexLength, c1, c2);
            }
            else if (Program.Controllers.Count == 1)
            {
                var c1 = Program.Controllers.FirstOrDefault(c => c.PlayerIndex == 1);

                if (c1.IsKeyboard)
                    WriteKbMapping(bytes, parentRom, hexLength, c1.Config);
                else
                    WriteJoystickMapping(bytes, parentRom, hexLength, c1);
            }
            else if (Program.Controllers.Count == 0)
                return;
        }

        private void WriteJoystickMapping(byte[] bytes, string parentRom, int hexLength, Controller c1, Controller c2 = null)
        {
            if (c1 == null || c1.Config == null)
                return;

            //initialize controller index, m2emulator uses directinput controller index (+1)
            //only index of player 1 is initialized as there might be only 1 controller at that point
            int j2index;
            int j1index = c1.DirectInput != null ? c1.DirectInput.DeviceIndex + 1 : c1.DeviceIndex + 1;

            //If a secod controller is connected, get controller index of player 2, if there is no 2nd controller, just increment the index
            if (c2 != null && c2.Config != null && !c2.IsKeyboard)
                j2index = c2.DirectInput != null ? c2.DirectInput.DeviceIndex + 1 : c2.DeviceIndex + 1;
            else
                j2index = 0;

            // Define driver to use for input
            string tech1 = "xinput";
            string vendor1 = "";
            string vendor2 = "";
            string tech2 = "xinput";
            bool dinput1 = false;
            bool dinput2 = false;

            if (_dinput || !c1.IsXInputDevice)
            {
                tech1 = "dinput";
                dinput1 = true;
                if (c1.VendorID == USB_VENDOR.SONY)
                    vendor1 = "dualshock";
                else if (c1.VendorID == USB_VENDOR.MICROSOFT)
                    vendor1 = "microsoft";
                else if (c1.VendorID == USB_VENDOR.NINTENDO)
                    vendor1 = "nintendo";
            }

            if ((c2 != null && c2.Config != null && _dinput) || (c2 != null && c2.Config != null && !c2.IsXInputDevice))
            {
                tech2 = "dinput";
                dinput2 = true;
                if (c2.VendorID == USB_VENDOR.SONY)
                    vendor2 = "dualshock";
                else if (c2.VendorID == USB_VENDOR.MICROSOFT)
                    vendor2 = "microsoft";
                else if (c2.VendorID == USB_VENDOR.NINTENDO)
                    vendor2 = "nintendo";
            }

            string gamecontrollerDB = Path.Combine(AppConfig.GetFullPath("tools"), "gamecontrollerdb.txt");
            SdlToDirectInput ctrl1 = null;
            SdlToDirectInput ctrl2 = null;

            if (tech1 == "dinput" || tech2 == "dinput")
            {
                string guid1 = (c1.Guid.ToString()).Substring(0, 27) + "00000";
                string guid2 = (c2 != null && c2.Config != null) ? (c2.Guid.ToString()).Substring(0, 27) + "00000" : null;

                if (!File.Exists(gamecontrollerDB))
                {
                    SimpleLogger.Instance.Info("[INFO] gamecontrollerdb.txt file not found in tools folder. Controller mapping will not be available.");
                    gamecontrollerDB = null;
                }
                else
                {
                    SimpleLogger.Instance.Info("[INFO] gamecontrollerdb.txt file found in tools folder. Controller mapping will be available.");
                }
                ctrl1 = gamecontrollerDB == null ? null : GameControllerDBParser.ParseByGuid(gamecontrollerDB, guid1);

                if (tech2 == "dinput" && guid2 != null)
                    ctrl2 = gamecontrollerDB == null ? null : GameControllerDBParser.ParseByGuid(gamecontrollerDB, guid2);
            }

            // Write end of binary file for service buttons, test buttons and keyboard buttons for stats display
            WriteServiceBytes(bytes, j1index, c1, tech1, vendor1, serviceByte[parentRom], ctrl1);
            WriteStatsBytes(bytes, serviceByte[parentRom] + 8);

            // Per game category mapping
            #region  shooters
            if (shooters.Contains(parentRom))
            {
                // Player index bytes
                bytes[1] = bytes[5] = bytes[9] = bytes[13] = bytes[17] = bytes[21] = bytes[25] = bytes[29] = Convert.ToByte(j1index);
                bytes[41] = bytes[45] = bytes[49] = bytes[53] = bytes[57] = bytes[61] = bytes[65] = bytes[69] = Convert.ToByte(j2index);

                bytes[0] = dinput1 ? GetInputCode(InputKey.up, c1, tech1, vendor1, ctrl1) : (byte)0x02;
                bytes[4] = dinput1 ? GetInputCode(InputKey.down, c1, tech1, vendor1, ctrl1) : (byte)0x03;
                bytes[8] = dinput1 ? GetInputCode(InputKey.left, c1, tech1, vendor1, ctrl1) : (byte)0x00;
                bytes[12] = dinput1 ? GetInputCode(InputKey.right, c1, tech1, vendor1, ctrl1) : (byte)0x01;
                bytes[16] = dinput1 ? GetInputCode(InputKey.a, c1, tech1, vendor1, ctrl1) : (byte)0x30;
                bytes[20] = dinput1 ? GetInputCode(InputKey.b, c1, tech1, vendor1, ctrl1) : (byte)0x40;
                bytes[24] = dinput1 ? GetInputCode(InputKey.y, c1, tech1, vendor1, ctrl1) : (byte)0x10;
                bytes[28] = dinput1 ? GetInputCode(InputKey.x, c1, tech1, vendor1, ctrl1) : (byte)0x20;

                if (SystemConfig.getOptBoolean("m2_gun_usecontroller"))
                {
                    bytes[32] = dinput1 ? GetInputCode(InputKey.start, c1, tech1, vendor1, ctrl1) : (byte)0xB0;
                    bytes[33] = Convert.ToByte(j1index);
                    bytes[36] = dinput1 ? GetInputCode(InputKey.select, c1, tech1, vendor1, ctrl1) : (byte)0xC0;
                    bytes[37] = Convert.ToByte(j1index);
                }
                else
                {
                    bytes[32] = (byte)0x02;
                    bytes[33] = (byte)0x00;
                    bytes[36] = (byte)0x04;
                    bytes[37] = (byte)0x00;
                }

                if (c2 != null && !c2.IsKeyboard)
                {
                    bytes[40] = dinput2 ? GetInputCode(InputKey.up, c2, tech2, vendor2, ctrl2) : (byte)0x02;
                    bytes[44] = dinput2 ? GetInputCode(InputKey.down, c2, tech2, vendor2, ctrl2) : (byte)0x03;
                    bytes[48] = dinput2 ? GetInputCode(InputKey.left, c2, tech2, vendor2, ctrl2) : (byte)0x00;
                    bytes[52] = dinput2 ? GetInputCode(InputKey.right, c2, tech2, vendor2, ctrl2) : (byte)0x01;
                    bytes[56] = dinput2 ? GetInputCode(InputKey.a, c2, tech2, vendor2, ctrl2) : (byte)0x30;
                    bytes[60] = dinput2 ? GetInputCode(InputKey.b, c2, tech2, vendor2, ctrl2) : (byte)0x40;
                    bytes[64] = dinput2 ? GetInputCode(InputKey.y, c2, tech2, vendor2, ctrl2) : (byte)0x10;
                    bytes[68] = dinput2 ? GetInputCode(InputKey.x, c2, tech2, vendor2, ctrl2) : (byte)0x20;
                }
                else
                {
                    bytes[40] = (byte)0x00;
                    bytes[44] = (byte)0x00;
                    bytes[48] = (byte)0x00;
                    bytes[52] = (byte)0x00;
                    bytes[56] = (byte)0x00;
                    bytes[60] = (byte)0x00;
                    bytes[64] = (byte)0x00;
                    bytes[68] = (byte)0x00;
                }

                if (SystemConfig.getOptBoolean("m2_gun_usecontroller") && c2 != null && !c2.IsKeyboard)
                {
                    bytes[72] = dinput2 ? GetInputCode(InputKey.start, c2, tech2, vendor2, ctrl2) : (byte)0xB0;
                    bytes[73] = Convert.ToByte(j2index);
                    bytes[76] = dinput2 ? GetInputCode(InputKey.select, c2, tech2, vendor2, ctrl2) : (byte)0xC0;
                    bytes[77] = Convert.ToByte(j2index);
                }
                else
                {
                    bytes[72] = (byte)0x03;
                    bytes[73] = (byte)0x00;
                    bytes[76] = (byte)0x05;
                    bytes[77] = (byte)0x00;
                }

                bytes[80] = (byte)0x3B;
                bytes[81] = (byte)0x00;
                bytes[84] = (byte)0x3C;
                bytes[85] = (byte)0x00;
            }
            #endregion
            #region driving
            else if (drivingshiftupdown.Contains(parentRom))
            {
                bytes[1] = bytes[5] = bytes[9] = bytes[13] = bytes[17] = bytes[21] = bytes[25] = bytes[29] = bytes[33] = bytes[37] = bytes[41] = bytes[45] = bytes[49] = Convert.ToByte(j1index);

                bytes[0] = dinput1 ? GetInputCode(InputKey.up, c1, tech1, vendor1, ctrl1) : (byte)0x02;
                bytes[4] = dinput1 ? GetInputCode(InputKey.down, c1, tech1, vendor1, ctrl1) : (byte)0x03;
                bytes[8] = dinput1 ? GetInputCode(InputKey.left, c1, tech1, vendor1, ctrl1) : (byte)0x00;
                bytes[12] = dinput1 ? GetInputCode(InputKey.right, c1, tech1, vendor1, ctrl1) : (byte)0x01;

                bytes[16] = dinput1 ? GetInputCode(InputKey.leftanalogleft, c1, tech1, vendor1, ctrl1, true, false) : (byte)0x02;
                bytes[19] = 0xFF;

                bytes[20] = dinput1 ? GetInputCode(InputKey.r2, c1, tech1, vendor1, ctrl1,  false, true) : (byte)0x07;
                if (parentRom == "indy500" || parentRom == "stcc" || parentRom == "motoraid" || parentRom.StartsWith("manxtt"))
                {
                    if (dinput1 && vendor1 != "dualshock")
                        bytes[21] = Convert.ToByte(j1index + 16);
                }
                else if (!dinput1 || vendor1 == "dualshock")
                    bytes[21] = Convert.ToByte(j1index + 16);
                bytes[23] = 0xFF;

                bytes[24] = dinput1 ? GetInputCode(InputKey.l2, c1, tech1, vendor1, ctrl1, false, true) : (byte)0x06;
                if (parentRom != "indy500" && parentRom != "stcc" && parentRom != "motoraid" && !parentRom.StartsWith("manxtt"))
                {
                    bytes[25] = Convert.ToByte(j1index + 16);
                }
                bytes[27] = 0xFF;

                if (parentRom == "motoraid")
                {
                    bytes[28] = dinput1 ? GetInputCode(InputKey.a, c1, tech1, vendor1, ctrl1) : (byte)0x30;
                    bytes[32] = dinput1 ? GetInputCode(InputKey.y, c1, tech1, vendor1, ctrl1) : (byte)0x10;
                    bytes[36] = dinput1 ? GetInputCode(InputKey.start, c1, tech1, vendor1, ctrl1) : (byte)0xB0;
                    bytes[40] = dinput1 ? GetInputCode(InputKey.select, c1, tech1, vendor1, ctrl1) : (byte)0xC0;
                    bytes[44] = (byte)0x00;
                    bytes[45] = (byte)0x00;

                    bytes[68] = (byte)0x01;
                    bytes[69] = (byte)0x01;
                    bytes[70] = (byte)0x01;
                }
                else if (parentRom != "manxtt" && parentRom != "manxttc")
                {
                    bytes[28] = dinput1 ? GetInputCode(InputKey.a, c1, tech1, vendor1, ctrl1) : (byte)0x30;
                    bytes[32] = dinput1 ? GetInputCode(InputKey.b, c1, tech1, vendor1, ctrl1) : (byte)0x40;
                    bytes[36] = dinput1 ? GetInputCode(InputKey.y, c1, tech1, vendor1, ctrl1) : (byte)0x10;
                    bytes[40] = dinput1 ? GetInputCode(InputKey.x, c1, tech1, vendor1, ctrl1) : (byte)0x20;
                    bytes[44] = dinput1 ? GetInputCode(InputKey.start, c1, tech1, vendor1, ctrl1) : (byte)0xB0;
                    bytes[48] = dinput1 ? GetInputCode(InputKey.select, c1, tech1, vendor1, ctrl1) : (byte)0xC0;

                    bytes[72] = (byte)0x01;
                    bytes[73] = (byte)0x01;
                    bytes[74] = (byte)0x01;
                }
                else
                {
                    bytes[28] = dinput1 ? GetInputCode(InputKey.y, c1, tech1, vendor1, ctrl1) : (byte)0x10;
                    bytes[32] = dinput1 ? GetInputCode(InputKey.b, c1, tech1, vendor1, ctrl1) : (byte)0x40;
                    bytes[36] = dinput1 ? GetInputCode(InputKey.start, c1, tech1, vendor1, ctrl1) : (byte)0xB0;
                    bytes[40] = dinput1 ? GetInputCode(InputKey.select, c1, tech1, vendor1, ctrl1) : (byte)0xC0;
                    bytes[44] = (byte)0x00;
                    bytes[45] = (byte)0x00;

                    bytes[68] = (byte)0x01;
                    bytes[69] = (byte)0x01;
                    bytes[70] = (byte)0x01;
                }

            }
            #endregion
            #region driving gear stick
            else if (drivingshiftlever.Contains(parentRom))
            {
                bytes[1] = bytes[5] = bytes[9] = bytes[13] = bytes[17] = bytes[21] = bytes[25] = bytes[29] = bytes[33] = bytes[37] = bytes[41] = bytes[45] = bytes[49] = bytes[53] = bytes[57] = Convert.ToByte(j1index);
                
                bytes[0] = dinput1 ? GetInputCode(InputKey.up, c1, tech1, vendor1, ctrl1) : (byte)0x02;
                bytes[4] = dinput1 ? GetInputCode(InputKey.down, c1, tech1, vendor1, ctrl1) : (byte)0x03;
                bytes[8] = dinput1 ? GetInputCode(InputKey.left, c1, tech1, vendor1, ctrl1) : (byte)0x00;
                bytes[12] = dinput1 ? GetInputCode(InputKey.right, c1, tech1, vendor1, ctrl1) : (byte)0x01;

                bytes[16] = dinput1 ? GetInputCode(InputKey.leftanalogleft, c1, tech1, vendor1, ctrl1, true, false) : (byte)0x02;  // Steering
                bytes[19] = 0xFF;

                bytes[20] = dinput1 ? GetInputCode(InputKey.r2, c1, tech1, vendor1, ctrl1, false, true) : (byte)0x07;  // Accelerate (R2)
                bytes[23] = 0xFF;
                bytes[24] = dinput1 ? GetInputCode(InputKey.l2, c1, tech1, vendor1, ctrl1, false, true) : (byte)0x06;  // Brake (L2)
                bytes[27] = 0xFF;
                bytes[28] = dinput1 ? GetInputCode(InputKey.joystick2up, c1, tech1, vendor1, ctrl1) : (byte)0x0A;
                bytes[32] = dinput1 ? GetInputCode(InputKey.joystick2down, c1, tech1, vendor1, ctrl1) : (byte)0x0B;
                bytes[36] = dinput1 ? GetInputCode(InputKey.joystick2left, c1, tech1, vendor1, ctrl1) : (byte)0x08;
                bytes[40] = dinput1 ? GetInputCode(InputKey.joystick2right, c1, tech1, vendor1, ctrl1) : (byte)0x09;

                bytes[44] = dinput1 ? GetInputCode(InputKey.pagedown, c1, tech1, vendor1, ctrl1) : (byte)0x60;
                bytes[48] = dinput1 ? GetInputCode(InputKey.x, c1, tech1, vendor1, ctrl1) : (byte)0x20;

                if (parentRom == "daytona")
                {
                    bytes[61] = bytes[65] = bytes[69] = Convert.ToByte(j1index);
                    bytes[52] = dinput1 ? GetInputCode(InputKey.a, c1, tech1, vendor1, ctrl1) : (byte)0x30;
                    bytes[56] = dinput1 ? GetInputCode(InputKey.y, c1, tech1, vendor1, ctrl1) : (byte)0x10;
                    bytes[60] = dinput1 ? GetInputCode(InputKey.b, c1, tech1, vendor1, ctrl1) : (byte)0x40;
                    bytes[64] = dinput1 ? GetInputCode(InputKey.start, c1, tech1, vendor1, ctrl1) : (byte)0xB0;
                    bytes[68] = dinput1 ? GetInputCode(InputKey.select, c1, tech1, vendor1, ctrl1) : (byte)0xC0;

                    bytes[96] = (byte)0x01;
                    bytes[97] = (byte)0x01;
                    bytes[98] = (byte)0x01;
                }

                else if (parentRom.StartsWith("srally"))
                {
                    bytes[52] = dinput1 ? GetInputCode(InputKey.start, c1, tech1, vendor1, ctrl1) : (byte)0xB0;
                    bytes[56] = dinput1 ? GetInputCode(InputKey.select, c1, tech1, vendor1, ctrl1) : (byte)0xC0;

                    bytes[84] = (byte)0x01;
                    bytes[85] = (byte)0x01;
                    bytes[86] = (byte)0x01;
                }

            }
            #endregion
            #region fighters
            else if (fighters.Contains(parentRom))
            {
                bytes[1] = bytes[5] = bytes[9] = bytes[13] = bytes[17] = bytes[21] = bytes[25] = bytes[29] = bytes[33] = bytes[37] = Convert.ToByte(j1index);
                bytes[41] = bytes[45] = bytes[49] = bytes[53] = bytes[57] = bytes[61] = bytes[65] = bytes[69] = bytes[73] = bytes[77] = Convert.ToByte(j2index);

                // Player 1
                bytes[0] = dinput1 ? GetInputCode(InputKey.up, c1, tech1, vendor1, ctrl1) : (byte)0x02;
                bytes[4] = dinput1 ? GetInputCode(InputKey.down, c1, tech1, vendor1, ctrl1) : (byte)0x03;
                bytes[8] = dinput1 ? GetInputCode(InputKey.left, c1, tech1, vendor1, ctrl1) : (byte)0x00;
                bytes[12] = dinput1 ? GetInputCode(InputKey.right, c1, tech1, vendor1, ctrl1) : (byte)0x01;

                if (parentRom == "doa")
                {
                    bytes[16] = dinput1 ? GetInputCode(InputKey.b, c1, tech1, vendor1, ctrl1) : (byte)0x40;
                    bytes[20] = dinput1 ? GetInputCode(InputKey.y, c1, tech1, vendor1, ctrl1) : (byte)0x10;
                }
                else
                {
                    bytes[16] = dinput1 ? GetInputCode(InputKey.y, c1, tech1, vendor1, ctrl1) : (byte)0x10;
                    bytes[20] = dinput1 ? GetInputCode(InputKey.x, c1, tech1, vendor1, ctrl1) : (byte)0x20;
                }

                if (parentRom == "fvipers")
                    bytes[24] = dinput1 ? GetInputCode(InputKey.b, c1, tech1, vendor1, ctrl1) : (byte)0x40;
                else
                    bytes[24] = dinput1 ? GetInputCode(InputKey.a, c1, tech1, vendor1, ctrl1) : (byte)0x30;

                if (parentRom == "doa")
                    bytes[28] = dinput1 ? GetInputCode(InputKey.x, c1, tech1, vendor1, ctrl1) : (byte)0x20;
                else if (parentRom == "fvipers")
                    bytes[28] = dinput1 ? GetInputCode(InputKey.a, c1, tech1, vendor1, ctrl1) : (byte)0x30;
                else
                    bytes[28] = dinput1 ? GetInputCode(InputKey.b, c1, tech1, vendor1, ctrl1) : (byte)0x40;

                bytes[32] = dinput1 ? GetInputCode(InputKey.start, c1, tech1, vendor1, ctrl1) : (byte)0xB0;
                bytes[36] = dinput1 ? GetInputCode(InputKey.select, c1, tech1, vendor1, ctrl1) : (byte)0xC0;

                // Player 2
                if (c2 != null)
                {
                    bytes[40] = dinput2 ? GetInputCode(InputKey.up, c2, tech2, vendor2, ctrl2) : (byte)0x02;
                    bytes[44] = dinput2 ? GetInputCode(InputKey.down, c2, tech2, vendor2, ctrl2) : (byte)0x03;
                    bytes[48] = dinput2 ? GetInputCode(InputKey.left, c2, tech2, vendor2, ctrl2) : (byte)0x00;
                    bytes[52] = dinput2 ? GetInputCode(InputKey.right, c2, tech2, vendor2, ctrl2) : (byte)0x01;

                    if (parentRom == "doa")
                    {
                        bytes[56] = dinput2 ? GetInputCode(InputKey.b, c2, tech2, vendor2, ctrl2) : (byte)0x40;
                        bytes[60] = dinput2 ? GetInputCode(InputKey.y, c2, tech2, vendor2, ctrl2) : (byte)0x10;
                    }
                    else
                    {
                        bytes[56] = dinput2 ? GetInputCode(InputKey.y, c2, tech2, vendor2, ctrl2) : (byte)0x10;
                        bytes[60] = dinput2 ? GetInputCode(InputKey.x, c2, tech2, vendor2, ctrl2) : (byte)0x20;
                    }

                    if (parentRom == "fvipers")
                        bytes[64] = dinput2 ? GetInputCode(InputKey.b, c2, tech2, vendor2, ctrl2) : (byte)0x40;
                    else
                        bytes[64] = dinput2 ? GetInputCode(InputKey.a, c2, tech2, vendor2, ctrl2) : (byte)0x30;

                    if (parentRom == "doa")
                        bytes[68] = dinput2 ? GetInputCode(InputKey.x, c2, tech2, vendor2, ctrl2) : (byte)0x20;
                    else if (parentRom == "fvipers")
                        bytes[68] = dinput2 ? GetInputCode(InputKey.a, c2, tech2, vendor2, ctrl2) : (byte)0x30;
                    else
                        bytes[68] = dinput2 ? GetInputCode(InputKey.b, c2, tech2, vendor2, ctrl2) : (byte)0x40;

                    bytes[72] = dinput2 ? GetInputCode(InputKey.start, c2, tech2, vendor2, ctrl2) : (byte)0xB0;
                    bytes[76] = dinput2 ? GetInputCode(InputKey.select, c2, tech2, vendor2, ctrl2) : (byte)0xC0;
                }
                else
                    bytes[40] = bytes[44] = bytes[48] = bytes[52] = bytes[56] = bytes[60] = bytes[64] = bytes[68] = bytes[72] = bytes[76] = 0x00;
            }
            #endregion
            #region standard
            else if (standard.Contains(parentRom))
            {
                bytes[1] = bytes[5] = bytes[9] = bytes[13] = bytes[17] = bytes[21] = bytes[25] = bytes[29] = bytes[33] = bytes[37] = Convert.ToByte(j1index);
                bytes[41] = bytes[45] = bytes[49] = bytes[53] = bytes[57] = bytes[61] = bytes[65] = bytes[69] = bytes[73] = bytes[77] = Convert.ToByte(j2index);

                // Player 1
                bytes[0] = dinput1 ? GetInputCode(InputKey.joystick1up, c1, tech1, vendor1, ctrl1) : (byte)0x06;
                bytes[4] = dinput1 ? GetInputCode(InputKey.joystick1down, c1, tech1, vendor1, ctrl1) : (byte)0x07;
                bytes[8] = dinput1 ? GetInputCode(InputKey.joystick1left, c1, tech1, vendor1, ctrl1) : (byte)0x04;
                bytes[12] = dinput1 ? GetInputCode(InputKey.joystick1right, c1, tech1, vendor1, ctrl1) : (byte)0x05;

                if (parentRom == "vstriker")
                {
                    bytes[16] = dinput1 ? GetInputCode(InputKey.a, c1, tech1, vendor1, ctrl1) : (byte)0x30;
                    bytes[20] = dinput1 ? GetInputCode(InputKey.b, c1, tech1, vendor1, ctrl1) : (byte)0x40;
                    bytes[24] = dinput1 ? GetInputCode(InputKey.y, c1, tech1, vendor1, ctrl1) : (byte)0x10;
                    bytes[28] = dinput1 ? GetInputCode(InputKey.x, c1, tech1, vendor1, ctrl1) : (byte)0x20;
                }
                else if (parentRom == "dynamcop")
                {
                    bytes[16] = dinput1 ? GetInputCode(InputKey.y, c1, tech1, vendor1, ctrl1) : (byte)0x10;
                    bytes[20] = dinput1 ? GetInputCode(InputKey.x, c1, tech1, vendor1, ctrl1) : (byte)0x20;
                    bytes[24] = dinput1 ? GetInputCode(InputKey.a, c1, tech1, vendor1, ctrl1) : (byte)0x30;
                    bytes[28] = dinput1 ? GetInputCode(InputKey.b, c1, tech1, vendor1, ctrl1) : (byte)0x40;
                }
                else if (parentRom == "pltkids")
                {
                    bytes[16] = dinput1 ? GetInputCode(InputKey.y, c1, tech1, vendor1, ctrl1) : (byte)0x10;
                    bytes[20] = dinput1 ? GetInputCode(InputKey.a, c1, tech1, vendor1, ctrl1) : (byte)0x30;
                    bytes[24] = dinput1 ? GetInputCode(InputKey.x, c1, tech1, vendor1, ctrl1) : (byte)0x20;
                    bytes[28] = dinput1 ? GetInputCode(InputKey.b, c1, tech1, vendor1, ctrl1) : (byte)0x40;
                }
                else if (parentRom == "zerogun")
                {
                    bytes[16] = dinput1 ? GetInputCode(InputKey.y, c1, tech1, vendor1, ctrl1) : (byte)0x10;
                    bytes[20] = dinput1 ? GetInputCode(InputKey.a, c1, tech1, vendor1, ctrl1) : (byte)0x30;
                    bytes[24] = dinput1 ? GetInputCode(InputKey.b, c1, tech1, vendor1, ctrl1) : (byte)0x40;
                    bytes[28] = dinput1 ? GetInputCode(InputKey.x, c1, tech1, vendor1, ctrl1) : (byte)0x20;
                }

                bytes[32] = dinput1 ? GetInputCode(InputKey.start, c1, tech1, vendor1, ctrl1) : (byte)0xB0;
                bytes[36] = dinput1 ? GetInputCode(InputKey.select, c1, tech1, vendor1, ctrl1) : (byte)0xC0;

                // Player 2
                if (c2 != null)
                {
                    bytes[40] = dinput2 ? GetInputCode(InputKey.leftanalogup, c2, tech2, vendor2, ctrl2) : (byte)0x06;
                    bytes[44] = dinput2 ? GetInputCode(InputKey.leftanalogdown, c2, tech2, vendor2, ctrl2) : (byte)0x07;
                    bytes[48] = dinput2 ? GetInputCode(InputKey.leftanalogleft, c2, tech2, vendor2, ctrl2) : (byte)0x04;
                    bytes[52] = dinput2 ? GetInputCode(InputKey.leftanalogright, c2, tech2, vendor2, ctrl2) : (byte)0x05;

                    if (parentRom == "vstriker")
                    {
                        bytes[56] = dinput2 ? GetInputCode(InputKey.a, c2, tech2, vendor2, ctrl2) : (byte)0x30;
                        bytes[60] = dinput2 ? GetInputCode(InputKey.b, c2, tech2, vendor2, ctrl2) : (byte)0x40;
                        bytes[64] = dinput2 ? GetInputCode(InputKey.y, c2, tech2, vendor2, ctrl2) : (byte)0x10;
                        bytes[68] = dinput2 ? GetInputCode(InputKey.x, c2, tech2, vendor2, ctrl2) : (byte)0x20;
                    }
                    else if (parentRom == "dynamcop")
                    {
                        bytes[56] = dinput2 ? GetInputCode(InputKey.y, c2, tech2, vendor2, ctrl2) : (byte)0x10;
                        bytes[60] = dinput2 ? GetInputCode(InputKey.x, c2, tech2, vendor2, ctrl2) : (byte)0x20;
                        bytes[64] = dinput2 ? GetInputCode(InputKey.a, c2, tech2, vendor2, ctrl2) : (byte)0x30;
                        bytes[68] = dinput2 ? GetInputCode(InputKey.b, c2, tech2, vendor2, ctrl2) : (byte)0x40;
                    }
                    else if (parentRom == "pltkids")
                    {
                        bytes[56] = dinput2 ? GetInputCode(InputKey.y, c2, tech2, vendor2, ctrl2) : (byte)0x10;
                        bytes[60] = dinput2 ? GetInputCode(InputKey.a, c2, tech2, vendor2, ctrl2) : (byte)0x30;
                        bytes[64] = dinput2 ? GetInputCode(InputKey.x, c2, tech2, vendor2, ctrl2) : (byte)0x20;
                        bytes[68] = dinput2 ? GetInputCode(InputKey.b, c2, tech2, vendor2, ctrl2) : (byte)0x40;
                    }
                    else if (parentRom == "zerogun")
                    {
                        bytes[56] = dinput2 ? GetInputCode(InputKey.y, c2, tech2, vendor2, ctrl2) : (byte)0x10;
                        bytes[60] = dinput2 ? GetInputCode(InputKey.a, c2, tech2, vendor2, ctrl2) : (byte)0x30;
                        bytes[64] = dinput2 ? GetInputCode(InputKey.b, c2, tech2, vendor2, ctrl2) : (byte)0x40;
                        bytes[68] = dinput2 ? GetInputCode(InputKey.x, c2, tech2, vendor2, ctrl2) : (byte)0x20;
                    }

                    bytes[72] = dinput2 ? GetInputCode(InputKey.start, c2, tech2, vendor2, ctrl2) : (byte)0xB0;
                    bytes[76] = dinput2 ? GetInputCode(InputKey.select, c2, tech2, vendor2, ctrl2) : (byte)0xC0;
                }
                else
                    bytes[40] = bytes[44] = bytes[48] = bytes[52] = bytes[56] = bytes[60] = bytes[64] = bytes[68] = bytes[72] = bytes[76] = 0x00;
            }
            #endregion
            #region sports
            else if (sports.Contains(parentRom))
            {
                bytes[1] = bytes[5] = bytes[9] = bytes[13] = bytes[17] = bytes[21] = bytes[25] = bytes[29] = bytes[33] = bytes[37] = Convert.ToByte(j1index);
                if (parentRom != "segawski")
                    bytes[41] = Convert.ToByte(j1index);
                if (parentRom != "segawski" && parentRom != "waverunr")
                    bytes[45] = Convert.ToByte(j1index);

                if (parentRom == "waverunr")
                {
                    bytes[0] = dinput1 ? GetInputCode(InputKey.joystick2left, c1, tech1, vendor1, ctrl1) : (byte)0x08;
                    bytes[4] = dinput1 ? GetInputCode(InputKey.joystick2right, c1, tech1, vendor1, ctrl1) : (byte)0x09;
                    bytes[8] = dinput1 ? GetInputCode(InputKey.joystick1left, c1, tech1, vendor1, ctrl1) : (byte)0x04;
                    bytes[12] = dinput1 ? GetInputCode(InputKey.joystick1right, c1, tech1, vendor1, ctrl1) : (byte)0x05;
                    bytes[16] = dinput1 ? GetInputCode(InputKey.r2, c1, tech1, vendor1, ctrl1, false, false, true) : (byte)0x80;
                    bytes[20] = dinput1 ? GetInputCode(InputKey.joystick1left, c1, tech1, vendor1, ctrl1, true, false) : (byte)0x02;
                    bytes[23] = 0xFF;
                    bytes[24] = dinput1 ? GetInputCode(InputKey.joystick2left, c1, tech1, vendor1, ctrl1, true, false) : (byte)0x04;
                    bytes[27] = 0xFF;
                    bytes[28] = dinput1 ? GetInputCode(InputKey.r2, c1, tech1, vendor1, ctrl1, false, true) : (byte)0x07;
                    bytes[31] = 0xFF;
                    bytes[32] = dinput1 ? GetInputCode(InputKey.x, c1, tech1, vendor1, ctrl1) : (byte)0x20;
                    bytes[36] = dinput1 ? GetInputCode(InputKey.start, c1, tech1, vendor1, ctrl1) : (byte)0xB0;
                    bytes[40] = dinput1 ? GetInputCode(InputKey.select, c1, tech1, vendor1, ctrl1) : (byte)0xC0;

                    bytes[64] = (byte)0x01;
                    bytes[65] = (byte)0x01;
                    bytes[66] = (byte)0x01;
                }

                else if (parentRom == "skisuprg")
                {
                    bytes[0] = dinput1 ? GetInputCode(InputKey.joystick1left, c1, tech1, vendor1, ctrl1) : (byte)0x04;
                    bytes[4] = dinput1 ? GetInputCode(InputKey.joystick1right, c1, tech1, vendor1, ctrl1) : (byte)0x05;
                    bytes[8] = dinput1 ? GetInputCode(InputKey.joystick2left, c1, tech1, vendor1, ctrl1) : (byte)0x08;
                    bytes[12] = dinput1 ? GetInputCode(InputKey.joystick2right, c1, tech1, vendor1, ctrl1) : (byte)0x09;

                    bytes[16] = dinput1 ? GetInputCode(InputKey.joystick2left, c1, tech1, vendor1, ctrl1, true, false) : (byte)0x04;
                    bytes[19] = 0xFF;
                    bytes[20] = dinput1 ? GetInputCode(InputKey.joystick1left, c1, tech1, vendor1, ctrl1, true, false) : (byte)0x02;
                    bytes[21] = Convert.ToByte(j1index + 16);
                    bytes[23] = 0xFF;

                    bytes[24] = dinput1 ? GetInputCode(InputKey.down, c1, tech1, vendor1, ctrl1) : (byte)0x03;
                    bytes[28] = dinput1 ? GetInputCode(InputKey.up, c1, tech1, vendor1, ctrl1) : (byte)0x02;

                    bytes[32] = dinput1 ? GetInputCode(InputKey.y, c1, tech1, vendor1, ctrl1) : (byte)0x10;
                    bytes[36] = dinput1 ? GetInputCode(InputKey.a, c1, tech1, vendor1, ctrl1) : (byte)0x30;
                    bytes[40] = dinput1 ? GetInputCode(InputKey.b, c1, tech1, vendor1, ctrl1) : (byte)0x40;
                    bytes[44] = dinput1 ? GetInputCode(InputKey.select, c1, tech1, vendor1, ctrl1) : (byte)0xC0;

                    bytes[68] = (byte)0x01;
                    bytes[69] = (byte)0x01;
                }

                else if (parentRom == "topskatr" || parentRom == "segawski")
                {
                    bytes[0] = dinput1 ? GetInputCode(InputKey.joystick1up, c1, tech1, vendor1, ctrl1) : (byte)0x06;
                    bytes[4] = dinput1 ? GetInputCode(InputKey.joystick1down, c1, tech1, vendor1, ctrl1) : (byte)0x07;
                    bytes[8] = dinput1 ? GetInputCode(InputKey.joystick1left, c1, tech1, vendor1, ctrl1) : (byte)0x04;
                    bytes[12] = dinput1 ? GetInputCode(InputKey.joystick1right, c1, tech1, vendor1, ctrl1) : (byte)0x05;
                    bytes[16] = dinput1 ? GetInputCode(InputKey.joystick1left, c1, tech1, vendor1, ctrl1, true, false) : (byte)0x02;
                    bytes[17] = Convert.ToByte(j1index + 16);
                    bytes[19] = 0xFF;

                    if (parentRom == "topskatr")
                    {
                        bytes[20] = dinput1 ? GetInputCode(InputKey.joystick2left, c1, tech1, vendor1, ctrl1, true, false) : (byte)0x04;
                        bytes[23] = 0xFF;
                        bytes[24] = dinput1 ? GetInputCode(InputKey.x, c1, tech1, vendor1, ctrl1) : (byte)0x20;
                        bytes[28] = dinput1 ? GetInputCode(InputKey.a, c1, tech1, vendor1, ctrl1) : (byte)0x30;
                        bytes[32] = dinput1 ? GetInputCode(InputKey.y, c1, tech1, vendor1, ctrl1) : (byte)0x10;
                        bytes[36] = dinput1 ? GetInputCode(InputKey.b, c1, tech1, vendor1, ctrl1) : (byte)0x40;
                        bytes[40] = dinput1 ? GetInputCode(InputKey.start, c1, tech1, vendor1, ctrl1) : (byte)0xB0;
                        bytes[44] = dinput1 ? GetInputCode(InputKey.select, c1, tech1, vendor1, ctrl1) : (byte)0xC0;

                        bytes[68] = (byte)0x01;
                        bytes[69] = (byte)0x01;
                    }

                    if (parentRom == "segawski")
                    {
                        bytes[20] = dinput1 ? GetInputCode(InputKey.a, c1, tech1, vendor1, ctrl1) : (byte)0x30;
                        bytes[24] = dinput1 ? GetInputCode(InputKey.l2, c1, tech1, vendor1, ctrl1, false, false, true) : (byte)0x70;
                        bytes[28] = dinput1 ? GetInputCode(InputKey.r2, c1, tech1, vendor1, ctrl1, false, false, true) : (byte)0x80;
                        bytes[32] = dinput1 ? GetInputCode(InputKey.start, c1, tech1, vendor1, ctrl1) : (byte)0xB0;
                        bytes[36] = dinput1 ? GetInputCode(InputKey.select, c1, tech1, vendor1, ctrl1) : (byte)0xC0;

                        bytes[60] = (byte)0x01;
                    }
                }
            }
            #endregion
            #region games with specific schemes
            // Games with completely specific schemes
            // Desert Tank
            else if (parentRom == "desert")
            {
                bytes[1] = bytes[5] = bytes[9] = bytes[13] = bytes[17] = bytes[21] = bytes[25] = bytes[29] = bytes[33] = bytes[37] = bytes[41] = bytes[45] = bytes[49] = bytes[53] = bytes[57] = bytes[61] = bytes[65] = bytes[69] = Convert.ToByte(j1index);
                bytes[0] = dinput1 ? GetInputCode(InputKey.joystick2left, c1, tech1, vendor1, ctrl1) : (byte)0x08;
                bytes[4] = dinput1 ? GetInputCode(InputKey.joystick2right, c1, tech1, vendor1, ctrl1) : (byte)0x09;
                bytes[8] = dinput1 ? GetInputCode(InputKey.joystick2up, c1, tech1, vendor1, ctrl1) : (byte)0x0A;
                bytes[12] = dinput1 ? GetInputCode(InputKey.joystick2down, c1, tech1, vendor1, ctrl1) : (byte)0x0B;
                bytes[16] = dinput1 ? GetInputCode(InputKey.joystick1up, c1, tech1, vendor1, ctrl1) : (byte)0x06;
                bytes[20] = dinput1 ? GetInputCode(InputKey.joystick1down, c1, tech1, vendor1, ctrl1) : (byte)0x07;
                bytes[24] = dinput1 ? GetInputCode(InputKey.joystick2left, c1, tech1, vendor1, ctrl1, true, false) : (byte)0x04;
                bytes[27] = 0xFF;
                bytes[28] = dinput1 ? GetInputCode(InputKey.joystick2up, c1, tech1, vendor1, ctrl1, true, false) : (byte)0x05;
                bytes[31] = 0xFF;
                bytes[32] = dinput1 ? GetInputCode(InputKey.joystick1up, c1, tech1, vendor1, ctrl1, true, false) : (byte)0x03;
                bytes[35] = 0xFF;
                bytes[36] = dinput1 ? GetInputCode(InputKey.l2, c1, tech1, vendor1, ctrl1, false, false, true) : (byte)0x70;
                bytes[40] = dinput1 ? GetInputCode(InputKey.pagedown, c1, tech1, vendor1, ctrl1) : (byte)0x60;
                bytes[44] = dinput1 ? GetInputCode(InputKey.r2, c1, tech1, vendor1, ctrl1, false, false, true) : (byte)0x80;
                bytes[48] = dinput1 ? GetInputCode(InputKey.x, c1, tech1, vendor1, ctrl1) : (byte)0x20;
                bytes[52] = dinput1 ? GetInputCode(InputKey.a, c1, tech1, vendor1, ctrl1) : (byte)0x30;
                bytes[56] = dinput1 ? GetInputCode(InputKey.y, c1, tech1, vendor1, ctrl1) : (byte)0x10;
                bytes[60] = dinput1 ? GetInputCode(InputKey.start, c1, tech1, vendor1, ctrl1) : (byte)0xB0;
                bytes[64] = dinput1 ? GetInputCode(InputKey.select, c1, tech1, vendor1, ctrl1) : (byte)0xC0;
                bytes[68] = 0x07;
                bytes[69] = 0x00;
            }

            else
            {
                bytes[1] = bytes[5] = bytes[9] = bytes[13] = bytes[17] = bytes[21] = bytes[25] = bytes[29] = bytes[33] = bytes[37] = bytes[41] = Convert.ToByte(j1index);
                
                bytes[0] = dinput1 ? GetInputCode(InputKey.joystick1up, c1, tech1, vendor1, ctrl1) : (byte)0x06;
                bytes[4] = dinput1 ? GetInputCode(InputKey.joystick1down, c1, tech1, vendor1, ctrl1) : (byte)0x07;
                bytes[8] = dinput1 ? GetInputCode(InputKey.joystick1left, c1, tech1, vendor1, ctrl1) : (byte)0x04;
                bytes[12] = dinput1 ? GetInputCode(InputKey.joystick1right, c1, tech1, vendor1, ctrl1) : (byte)0x05;

                // Dynamite Baseball '97
                if (parentRom == "dynabb97")
                {
                    if (c2 != null)
                        bytes[45] = bytes[49] = bytes[53] = bytes[57] = bytes[61] = bytes[65] = bytes[69] = bytes[73] = bytes[77] = bytes[81] = bytes[85] = Convert.ToByte(j2index);
                    else
                        bytes[45] = bytes[49] = bytes[53] = bytes[57] = bytes[61] = bytes[65] = bytes[69] = bytes[73] = bytes[77] = bytes[81] = bytes[85] = 0x00;

                    bytes[16] = dinput2 ? GetInputCode(InputKey.r2, c1, tech1, vendor2, ctrl2, false, true) : (byte)0x07;
                    if (!dinput2)
                        bytes[17] = Convert.ToByte(j1index + 16);

                    bytes[19] = 0xFF;
                    bytes[20] = dinput1 ? GetInputCode(InputKey.y, c1, tech1, vendor1, ctrl1) : (byte)0x10;
                    bytes[24] = dinput1 ? GetInputCode(InputKey.a, c1, tech1, vendor1, ctrl1) : (byte)0x30;
                    bytes[28] = dinput1 ? GetInputCode(InputKey.b, c1, tech1, vendor1, ctrl1) : (byte)0x40;
                    bytes[32] = dinput1 ? GetInputCode(InputKey.x, c1, tech1, vendor1, ctrl1) : (byte)0x20;
                    bytes[36] = dinput1 ? GetInputCode(InputKey.start, c1, tech1, vendor1, ctrl1) : (byte)0xB0;
                    bytes[40] = dinput1 ? GetInputCode(InputKey.select, c1, tech1, vendor1, ctrl1) : (byte)0xC0;

                    if (c2 != null)
                    {
                        bytes[44] = dinput2 ? GetInputCode(InputKey.joystick1up, c2, tech2, vendor2, ctrl2) : (byte)0x06;
                        bytes[48] = dinput2 ? GetInputCode(InputKey.joystick1down, c2, tech2, vendor2, ctrl2) : (byte)0x07;
                        bytes[52] = dinput2 ? GetInputCode(InputKey.joystick1left, c2, tech2, vendor2, ctrl2) : (byte)0x04;
                        bytes[56] = dinput2 ? GetInputCode(InputKey.joystick1right, c2, tech2, vendor2, ctrl2) : (byte)0x05;

                        bytes[60] = dinput2 ? GetInputCode(InputKey.r2, c2, tech2, vendor2, ctrl2, false, true) : (byte)0x07;
                        bytes[63] = 0xFF;
                        bytes[64] = dinput2 ? GetInputCode(InputKey.y, c2, tech2, vendor2, ctrl2) : (byte)0x10;
                        bytes[68] = dinput2 ? GetInputCode(InputKey.a, c2, tech2, vendor2, ctrl2) : (byte)0x30;
                        bytes[72] = dinput2 ? GetInputCode(InputKey.b, c2, tech2, vendor2, ctrl2) : (byte)0x40;
                        bytes[76] = dinput2 ? GetInputCode(InputKey.x, c2, tech2, vendor2, ctrl2) : (byte)0x20;
                        bytes[80] = dinput2 ? GetInputCode(InputKey.start, c2, tech2, vendor2, ctrl2) : (byte)0xB0;
                        bytes[84] = dinput2 ? GetInputCode(InputKey.select, c2, tech2, vendor2, ctrl2) : (byte)0xC0;
                    }

                    bytes[108] = (byte)0x01;
                    if (c2 != null && !c2.IsKeyboard)
                        bytes[109] = (byte)0x01;
                    else
                        bytes[109] = (byte)0x00;
                }
                // Sky Target
                else if (parentRom == "skytargt")
                {
                    bytes[16] = dinput1 ? GetInputCode(InputKey.joystick1left, c1, tech1, vendor1, ctrl1, true, false) : (byte)0x02;
                    bytes[19] = 0xFF;
                    bytes[20] = dinput1 ? GetInputCode(InputKey.joystick1up, c1, tech1, vendor1, ctrl1, true, false) : (byte)0x03;
                    bytes[23] = 0xFF;
                    bytes[24] = dinput1 ? GetInputCode(InputKey.r2, c1, tech1, vendor1, ctrl1, false, false, true) : (byte)0x80;
                    bytes[28] = dinput1 ? GetInputCode(InputKey.l2, c1, tech1, vendor1, ctrl1, false, false, true) : (byte)0x70;
                    bytes[32] = dinput1 ? GetInputCode(InputKey.x, c1, tech1, vendor1, ctrl1) : (byte)0x20;
                    bytes[36] = dinput1 ? GetInputCode(InputKey.start, c1, tech1, vendor1, ctrl1) : (byte)0xB0;
                    bytes[40] = dinput1 ? GetInputCode(InputKey.select, c1, tech1, vendor1, ctrl1) : (byte)0xC0;
                    bytes[44] = 0x07;
                    bytes[45] = 0x00;

                    bytes[68] = (byte)0x01;
                }
                else if (parentRom == "von")
                {
                    bytes[45] = bytes[49] = bytes[53] = Convert.ToByte(j1index);
                    bytes[16] = dinput1 ? GetInputCode(InputKey.l2, c1, tech1, vendor1, ctrl1, false, false, true) : (byte)0x70;
                    bytes[20] = dinput1 ? GetInputCode(InputKey.y, c1, tech1, vendor1, ctrl1) : (byte)0x10;
                    bytes[24] = dinput1 ? GetInputCode(InputKey.start, c1, tech1, vendor1, ctrl1) : (byte)0xB0;
                    bytes[28] = dinput1 ? GetInputCode(InputKey.select, c1, tech1, vendor1, ctrl1) : (byte)0xC0;
                    bytes[32] = dinput1 ? GetInputCode(InputKey.joystick2up, c1, tech1, vendor1, ctrl1) : (byte)0x0A;
                    bytes[36] = dinput1 ? GetInputCode(InputKey.joystick2down, c1, tech1, vendor1, ctrl1) : (byte)0x0B;
                    bytes[40] = dinput1 ? GetInputCode(InputKey.joystick2left, c1, tech1, vendor1, ctrl1) : (byte)0x08;
                    bytes[44] = dinput1 ? GetInputCode(InputKey.joystick2right, c1, tech1, vendor1, ctrl1) : (byte)0x09;
                    bytes[48] = dinput1 ? GetInputCode(InputKey.r2, c1, tech1, vendor1, ctrl1, false, false, true) : (byte)0x80;
                    bytes[52] = dinput1 ? GetInputCode(InputKey.b, c1, tech1, vendor1, ctrl1) : (byte)0x40;
                }
            }
            #endregion
        }

        private void WriteKbMapping(byte[] bytes, string parentRom, int hexLength, InputConfig keyboard)
        {
            if (keyboard == null)
                return;

            if (shooters.Contains(parentRom))
            {
                // Player index bytes
                bytes[1] = bytes[5] = bytes[9] = bytes[13] = bytes[17] = bytes[21] = bytes[25] = bytes[29] = bytes[33] = bytes[37] = 0x00;
                bytes[41] = bytes[45] = bytes[49] = bytes[53] = bytes[57] = bytes[61] = bytes[65] = bytes[69] = bytes[73] = bytes[77] = bytes[81] = bytes[85] = 0x00;

                bytes[0] = (byte)0xC8;      // up
                bytes[4] = (byte)0xD0;      // down
                bytes[8] = (byte)0xCB;      // left
                bytes[12] = (byte)0xCD;     // right
                bytes[16] = (byte)0x39;     // space
                bytes[20] = (byte)0x2A;     // left maj
                bytes[24] = (byte)0x1D;     // left ctrl
                bytes[28] = (byte)0x38;     // left ALT
                bytes[32] = (byte)0x02;     // 1
                bytes[36] = (byte)0x04;     // 3
                bytes[72] = (byte)0x03;     // 2
                bytes[76] = (byte)0x05;     // 4
                bytes[80] = (byte)0x3B;     // F1
                bytes[84] = (byte)0x3C;     // F2
            }
        }

        private static byte GetInputCode(InputKey key, Controller c, string tech, string brand, SdlToDirectInput ctrl ,bool globalAxis = false, bool trigger = false, bool digital = false)
        {

            bool revertAxis = false;
            key = key.GetRevertedAxis(out revertAxis);
            
            string esName = (c.Config[key].Name).ToString();

            if (esName == null || !esToDinput.ContainsKey(esName))
                return 0x00;

            string dinputName = esToDinput[esName];
            if (dinputName == null)
                return 0x00;

            if (!ctrl.ButtonMappings.ContainsKey(dinputName))
                return 0x00;

            string button = ctrl.ButtonMappings[dinputName];

            if (button.StartsWith("b"))
            {
                int buttonID = button.Substring(1).ToInteger();
                switch (buttonID)
                {
                    case 0: return 0x10;
                    case 1: return 0x20;
                    case 2: return 0x30;
                    case 3: return 0x40;
                    case 4: return 0x50;
                    case 5: return 0x60;
                    case 6: return 0x70;
                    case 7: return 0x80;
                    case 8: return 0x90;
                    case 9: return 0xA0;
                    case 10: return 0xB0;
                    case 11: return 0xC0;
                    case 12: return 0xD0;
                    case 13: return 0xE0;
                    case 14: return 0xF0;
                }
            }

            else if (button.StartsWith("h"))
            {
                int hatID = button.Substring(3).ToInteger();
                switch (hatID)
                {
                    case 1: return 0x0E;
                    case 2: return 0x0D;
                    case 4: return 0x0F;
                    case 8: return 0x0C;
                };
            }

            else if (button.StartsWith("a") || button.StartsWith("-a") || button.StartsWith("+a"))
            {
                int axisID = button.Substring(1).ToInteger();

                if (button.StartsWith("-a") || button.StartsWith("+a"))
                    axisID = button.Substring(2).ToInteger();

                else if (button.StartsWith("a"))
                    axisID = button.Substring(1).ToInteger();

                if (globalAxis)
                {
                    switch (axisID)
                    {
                        case 0: return 0x00;
                        case 1: return 0x01;
                        case 2: return 0x03;
                        case 3: return 0x04;
                        case 4: return 0x05;
                        case 5: return 0x02;
                    };
                }

                else if (trigger)
                {
                    switch (axisID)
                    {
                        case 0: return 0x00;
                        case 1: return 0x01;
                        case 2: return 0x03;
                        case 3: return 0x04;
                        case 4: return 0x05;
                        case 5:
                            if (brand == "microsoft") return 0x04;
                            else return 0x02;
                    };
                }

                else if (digital)
                {
                    switch (axisID)
                    {
                        case 0: 
                            return 0x00;
                        case 1: 
                            return 0x01;
                        case 2:
                            if (brand == "microsoft") return 0x07;
                            else return 0x03;
                        case 3:
                            if (brand == "dualshock") return 0x70;
                            else return 0x04;
                        case 4:
                            if (brand == "dualshock") return 0x80;
                            else return 0x05;
                        case 5:
                            if (brand == "microsoft") return 0x06;
                            else return 0x02;
                    };
                }

                else
                {
                    switch (axisID)
                    {
                        case 0: 
                            if (revertAxis) return 0x01;
                            else return 0x00;
                        case 1:
                            if (revertAxis) return 0x03;
                            else return 0x02;
                        case 2:
                            if (revertAxis) return 0x07;
                            else return 0x06;
                        case 3:
                            if (revertAxis) return 0x09;
                            else return 0x08;
                        case 4:
                            if (revertAxis) return 0x0B;
                            else return 0x0A;
                        case 5:
                            if (revertAxis) return 0x05;
                            else return 0x04;
                    };
                }
            }

            return 0x00;
        }

        private void WriteServiceBytes(byte[] bytes, int index, Controller c, string tech, string brand, int startByte, SdlToDirectInput ctrl)
        {
            bool dinput = tech == "dinput";

            if (!SystemConfig.isOptSet("m2_enable_service") || !SystemConfig.getOptBoolean("m2_enable_service"))
            {
                bytes[startByte] = (byte)0x3B;
                bytes[startByte + 1] = 0x00;
                bytes[startByte + 4] = (byte)0x3C;
                bytes[startByte + 5] = 0x00;
            }
            else
            {
                bytes[startByte] = dinput ? GetInputCode(InputKey.l3, c, tech, brand, ctrl) : (byte)0x90;
                bytes[startByte + 1] = (byte)index;
                bytes[startByte + 4] = dinput ? GetInputCode(InputKey.r3, c, tech, brand, ctrl) : (byte)0xA0;
                bytes[startByte + 5] = (byte)index;
            }
        }

        static readonly Dictionary<string, int> serviceByte = new Dictionary<string, int>()
        { 
            { "bel", 80 },
            { "daytona", 76 },
            { "desert", 72 },
            { "doa", 80 },
            { "dynabb97", 88 },
            { "dynamcop", 80 },
            { "fvipers", 80 },
            { "gunblade", 80 },
            { "hotd", 80 },
            { "indy500", 52 },
            { "lastbrnx", 80 },
            { "manxtt", 48 },
            { "manxttc", 48 },
            { "motoraid", 48 },
            { "overrev", 52 },
            { "pltkids", 80 },
            { "rchase2", 80 },
            { "schamp", 80 },
            { "segawski", 40 },
            { "sgt24h", 52 },
            { "skisuprg", 48 },
            { "skytargt", 48 },
            { "srallyc", 64 },
            { "srallyp", 64 },
            { "stcc", 52 },
            { "topskatr", 48 },
            { "vcop", 80 },
            { "vcop2", 80 },
            { "vf2", 80 },
            { "von", 56 },
            { "vstriker", 80 },
            { "waverunr", 44 },
            { "zerogun", 80 }
        };

        private void WriteStatsBytes(byte[] bytes, int startByte)
        {
            bytes[startByte] = (byte)0x42;
            bytes[startByte + 1] = (byte)0x00;
            bytes[startByte + 4] = (byte)0x41;
            bytes[startByte + 5] = (byte)0x00;
            bytes[startByte + 8] = (byte)0x40;
            bytes[startByte + 9] = (byte)0x00;
        }

        static readonly List<string> shooters = new List<string>() { "bel", "gunblade", "hotd", "rchase2", "vcop", "vcop2" };
        static readonly List<string> fighters = new List<string>() { "doa", "fvipers", "lastbrnx", "schamp", "vf2" };
        static readonly List<string> standard = new List<string>() { "dynamcop", "pltkids", "vstriker", "zerogun" };
        static readonly List<string> drivingshiftupdown = new List<string>() { "indy500", "motoraid", "overrev", "sgt24h", "stcc", "manxtt", "manxttc" };
        static readonly List<string> drivingshiftlever = new List<string>() { "daytona", "srallyc" };
        static readonly List<string> sports = new List<string>() { "segawski", "skisuprg", "topskatr", "waverunr" };

        private static readonly Dictionary<string, string> esToDinput = new Dictionary<string, string>()
        {
            { "a", "a" },
            { "b", "b" },
            { "x", "y" },
            { "y", "x" },
            { "select", "back" },
            { "start", "start" },
            { "joystick1left", "leftx" },
            { "leftanalogleft", "leftx" },
            { "joystick1up", "lefty" },
            { "leftanalogup", "lefty" },
            { "joystick2left", "rightx" },
            { "rightanalogleft", "rightx" },
            { "joystick2up", "righty" },
            { "rightanalogup", "righty" },
            { "up", "dpup" },
            { "down", "dpdown" },
            { "left", "dpleft" },
            { "right", "dpright" },
            { "l2", "lefttrigger" },
            { "l3", "leftstick" },
            { "pagedown", "rightshoulder" },
            { "pageup", "leftshoulder" },
            { "r2", "righttrigger" },
            { "r3", "rightstick" },
            { "leftthumb", "lefttrigger" },
            { "rightthumb", "righttrigger" },
            { "l1", "leftshoulder" },
            { "r1", "rightshoulder" },
            { "lefttrigger", "leftstick" },
            { "righttrigger", "rightstick" },
        };
    }
}
