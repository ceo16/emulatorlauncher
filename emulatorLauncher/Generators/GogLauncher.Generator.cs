using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using EmulatorLauncher.Common;
// using EmulatorLauncher.Common.Launchers.GOG; // Rimuovi questa riga se non hai una libreria GOG in quel namespace

namespace EmulatorLauncher
{
    // Questo è un file parziale della classe ExeLauncherGenerator
    partial class ExeLauncherGenerator : Generator
    {
        // Questa classe nidificata gestisce la logica di avvio per i giochi GOG
        class GogGameLauncher : GameLauncher
        {
            public GogGameLauncher(Uri uri)
            {
                // Per GOG, spesso l'URI (goggalaxy://) non punta a un eseguibile specifico da monitorare.
                // Ci affidiamo al monitoraggio di GOG Galaxy se l'opzione è abilitata.
                LauncherExe = null; // Nessun exe specifico del gioco da monitorare per default
            }

            public override int RunAndWait(ProcessStartInfo path)
            {
                SimpleLogger.Instance.Info("[INFO] GogGameLauncher: Running command: " + path.FileName);

                // Lancia il processo. UseShellExecute è fondamentale per i protocolli URI.
                Process.Start(path);

                // Opzionale: Uccidi GOG Galaxy dopo l'uscita del gioco
                if (Program.SystemConfig.getOptBoolean("killgoggalaxy")) // Richiede una nuova opzione in es_settings.cfg
                {
                    foreach (var ui in Process.GetProcessesByName("GalaxyClient")) // Nome tipico del processo GOG Galaxy
                    {
                        try { ui.Kill(); SimpleLogger.Instance.Info($"[INFO] Killed GOG Galaxy process: {ui.ProcessName}"); }
                        catch { }
                    }
                }

                // Poiché spesso non monitoriamo un exe specifico del gioco lanciato via URI,
                // assumiamo il successo dell'avvio e non attendiamo un processo specifico del gioco.
                SimpleLogger.Instance.Info("[INFO] Assuming GOG game launched successfully via URI. No specific process to wait for.");

                return 0;
            }
        }
    }
}