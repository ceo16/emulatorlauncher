using System;
using System.Diagnostics;
using System.IO;
using System.Threading;
using EmulatorLauncher.Common;

namespace EmulatorLauncher
{
    // Definiamo questo come un file parziale, in modo che si unisca correttamente
    // con la classe principale ExeLauncherGenerator.
    partial class ExeLauncherGenerator
    {
        // Questa è la classe annidata che gestisce la logica di avvio per i giochi EA.
        class EAGameLauncher : GameLauncher
        {
            // Costruttore della classe.
            public EAGameLauncher(Uri uri)
            {
                // Imposta "EADesktop" come nome del processo da monitorare.
                // Il lancio del gioco avverrà comunque tramite l'URI.
                LauncherExe = "EADesktop";
            }

            // Metodo che esegue e attende il gioco.
            public override int RunAndWait(ProcessStartInfo path)
            {
                // Cerca dinamicamente il percorso di installazione di EA App dal registro di Windows.
                string eaInstallDir = (string)Microsoft.Win32.Registry.GetValue(@"HKEY_LOCAL_MACHINE\SOFTWARE\Electronic Arts\EA Desktop", "InstallDir", null);
                
                if (!string.IsNullOrEmpty(eaInstallDir))
                {
                    string eaExePath = Path.Combine(eaInstallDir, "EADesktop.exe");
                    if (!File.Exists(eaExePath))
                    {
                        SimpleLogger.Instance.Warning("[WARNING] Esecuzione di EA, ma l'eseguibile non è stato trovato in: " + eaExePath);
                    }
                }
                else
                {
                    SimpleLogger.Instance.Warning("[WARNING] Impossibile trovare la cartella di installazione di EA App nel registro.");
                }

                // Lancia l'URI del gioco.
                SimpleLogger.Instance.Info("[INFO] EAGameLauncher: Esecuzione del comando URI: " + path.FileName);
                try
                {
                    Process.Start(new ProcessStartInfo(path.FileName) { UseShellExecute = true });
                }
                catch (Exception ex)
                {
                    SimpleLogger.Instance.Error("[ERROR] Impossibile lanciare l'URI di EA: " + ex.Message);
                    return 1; // Ritorna un codice di errore se il lancio fallisce.
                }

                // Monitora il processo principale di EA.
                var gameProcess = GetLauncherExeProcess();
                if (gameProcess != null)
                {
                    SimpleLogger.Instance.Info("[INFO] Processo EA (" + LauncherExe + ") trovato. In attesa della chiusura del gioco.");
                    gameProcess.WaitForExit();
                }

                return 0; // Ritorna 0 se tutto è andato a buon fine.
            }

        } // Chiusura della classe EAGameLauncher

    } // Chiusura della classe parziale ExeLauncherGenerator

} // Chiusura del namespace EmulatorLauncher