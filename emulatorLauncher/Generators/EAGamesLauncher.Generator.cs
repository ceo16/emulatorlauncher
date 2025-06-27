using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using EmulatorLauncher.Common;
// using EmulatorLauncher.Common.Launchers.EAGames; // Rimuovi questa riga se non hai una libreria EAGames in quel namespace

namespace EmulatorLauncher
{
    // Questo è un file parziale della classe ExeLauncherGenerator
    partial class ExeLauncherGenerator : Generator
    {
        // Questa classe nidificata gestisce la logica di avvio per i giochi EA Games
        class EAGameLauncher : GameLauncher
        {
            private static string EaDesktopPath = @"C:\Program Files\Electronic Arts\EA Desktop\EA Desktop\EADesktop.exe"; // Percorso configurabile

            public EAGameLauncher(Uri uri)
            {
                // Per EA Games, useremo il nome del processo di EA Desktop per il monitoraggio.
                LauncherExe = "EADesktop"; 
            }

            public override int RunAndWait(ProcessStartInfo path)
            {
                SimpleLogger.Instance.Info("[INFO] EAGameLauncher: Running command: " + path.FileName);

                // Logica di pre-lancio del client EA Desktop
                if (File.Exists(EaDesktopPath))
                {
                    SimpleLogger.Instance.Info("[INFO] Pre-launching EA Desktop client: " + EaDesktopPath);
                    try
                    {
                        Process.Start(new ProcessStartInfo(EaDesktopPath) { UseShellExecute = true });
                        Thread.Sleep(7000); // Attendi 7 secondi per permettere al client di caricarsi
                    }
                    catch (Exception ex)
                    {
                        SimpleLogger.Instance.Warning($"[WARNING] Failed to pre-launch EA Desktop client: {ex.Message}");
                    }
                }
                else
                {
                    SimpleLogger.Instance.Warning("[WARNING] EA Desktop client not found at: " + EaDesktopPath);
                }

                // Lancia il gioco
                Process.Start(path);

                // Monitora il processo del gioco (se LauncherExe è il nome corretto del processo del gioco lanciato)
                Process gameProcess = GetLauncherExeProcess();

                if (gameProcess != null)
                {
                    SimpleLogger.Instance.Info("[INFO] EA game process (" + LauncherExe + ") found, waiting to exit.");
                    gameProcess.WaitForExit();
                }
                else
                {
                    SimpleLogger.Instance.Info("[INFO] No specific EA game process (" + LauncherExe + ") found to monitor, assuming launch successful.");
                }

                // Opzionale: Uccidi EA Desktop dopo l'uscita del gioco
                if (Program.SystemConfig.getOptBoolean("killeadesktop")) // Richiede una nuova opzione in es_settings.cfg
                {
                    foreach (var ui in Process.GetProcessesByName("EADesktop"))
                    {
                        try { ui.Kill(); SimpleLogger.Instance.Info($"[INFO] Killed EA Desktop process: {ui.ProcessName}"); }
                        catch { }
                    }
                }
                return 0;
            }
        }
    }
}