using System;
using System.IO;
using System.Linq;
using System.Diagnostics;
using EmulatorLauncher.Common.Launchers;
using EmulatorLauncher.Common;

namespace EmulatorLauncher
{
    partial class ExeLauncherGenerator : Generator
    {
        class SteamGameLauncher : GameLauncher
        {
           public SteamGameLauncher(Uri uri)
{
    // NON cercare più l'eseguibile del gioco, perché la funzione è rotta.
    // Imposta "steam" come nome generico del processo da monitorare.
    // Il lancio vero e proprio avverrà tramite l'URI, non tramite questo eseguibile.
    LauncherExe = "steam";
    SimpleLogger.Instance.Info("[INFO] SteamGameLauncher: Impostato 'steam' come processo da monitorare, il lancio avverrà tramite URI.");
}

            public override int RunAndWait(System.Diagnostics.ProcessStartInfo path)
            {
                // Check if steam is already running
                bool uiExists = Process.GetProcessesByName("steam").Any();
                SimpleLogger.Instance.Info("[INFO] Executable name : " + LauncherExe);
                
                // Kill game if already running
                KillExistingLauncherExes();

                // Start game
                Process.Start(path);

                // Get running game process (30 seconds delay 30x1000)
                var steamGame = GetLauncherExeProcess();

                if (steamGame != null)
                {
                    steamGame.WaitForExit();

                    // Kill steam if it was not running previously or if option is set in Lumaca
                    if (!uiExists || (Program.SystemConfig.isOptSet("killsteam") && Program.SystemConfig.getOptBoolean("killsteam")))
                    {
                        foreach (var ui in Process.GetProcessesByName("steam"))
                        {
                            try { ui.Kill(); }
                            catch { }
                        }
                    }
                }
                return 0;
            }
        }
    }
}
