using System;
using System.Linq;
using System.Diagnostics;
using EmulatorLauncher.Common.Launchers;
using EmulatorLauncher.Common;

namespace EmulatorLauncher
{
    partial class ExeLauncherGenerator : Generator
    {
        class EpicGameLauncher : GameLauncher
        {
            public EpicGameLauncher(Uri uri)
{
    // Imposta il nome del processo generico da monitorare.
    // Il lancio vero e proprio avverrà comunque tramite l'URI.
    LauncherExe = "EpicGamesLauncher";
    SimpleLogger.Instance.Info("[INFO] EpicGameLauncher: Impostato 'EpicGamesLauncher' come processo da monitorare.");
}

            public override int RunAndWait(ProcessStartInfo path)
            {
                bool epicLauncherExists = Process.GetProcessesByName("EpicGamesLauncher").Any();
                SimpleLogger.Instance.Info("[INFO] Executable name : " + LauncherExe);
                KillExistingLauncherExes();

                Process.Start(path);

                var epicGame = GetLauncherExeProcess();
                if (epicGame != null)
                {
                    epicGame.WaitForExit();

                    if (!epicLauncherExists || (Program.SystemConfig.isOptSet("killsteam") && Program.SystemConfig.getOptBoolean("killsteam")))
                    {
                        foreach (var ui in Process.GetProcessesByName("EpicGamesLauncher"))
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
