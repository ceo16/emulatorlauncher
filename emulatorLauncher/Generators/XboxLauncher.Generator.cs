using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using EmulatorLauncher.Common;
// using EmulatorLauncher.Common.Launchers.Xbox; // Rimuovi questa riga se non hai una libreria Xbox in quel namespace

namespace EmulatorLauncher
{
    // Questo è un file parziale della classe ExeLauncherGenerator
    partial class ExeLauncherGenerator : Generator
    {
        // Questa classe nidificata gestisce la logica di avvio per i giochi Xbox PC
        class XboxGameLauncher : GameLauncher
    {
        public XboxGameLauncher(Uri uri) : base(uri)
        {
            // Non impostiamo più un LauncherExe, perché non dobbiamo monitorare nulla per l'installazione.
        }

        public override int RunAndWait(ProcessStartInfo path)
        {
            // path.FileName contiene già l'URI ufficiale ms-windows-store://...
            SimpleLogger.Instance.Info($"[XboxGameLauncher] Avvio del Microsoft Store con URI: {path.FileName}");

            try
            {
                // Lanciamo l'URI e il nostro lavoro finisce qui.
                Process.Start(new ProcessStartInfo(path.FileName) { UseShellExecute = true });
            }
            catch (Exception ex)
            {
                SimpleLogger.Instance.Error($"[XboxGameLauncher] Impossibile avviare il Microsoft Store: {ex.Message}");
                return 1;
            }
            
            // Rimuoviamo TUTTA la vecchia logica di monitoraggio.
            // Restituiamo subito 0 per ridare il controllo a EmulationStation.
            SimpleLogger.Instance.Info("[XboxGameLauncher] Comando inviato. L'operazione è delegata all'utente nello Store.");
            return 0;
        }
    }

      } 
      } 
	  