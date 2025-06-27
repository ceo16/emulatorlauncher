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
            // Il costruttore riceve l'URI, che può contenere l'AUMID o l'URL dello store
            public XboxGameLauncher(Uri uri)
            {
                // Se l'URI è un AUMID, lo usiamo come nome del processo da monitorare
                // Altrimenti, ci basiamo sul nome del processo generico o su un nome noto.
                if (uri.Scheme == "xbox" && uri.LocalPath.Contains("!App"))
                {
                    // Esempio: "Microsoft.GamingApp" da "Microsoft.GamingApp_8wekyb3d8bbwe!App"
                    LauncherExe = uri.LocalPath.Split('!')[0]; 
                    SimpleLogger.Instance.Info("[INFO] XboxGameLauncher: Detected AUMID, process name: " + LauncherExe);
                }
                else
                {
                    // Fallback per altri scenari, potresti voler aggiungere una logica per rilevare il nome dell'eseguibile se non un AUMID
                    // Puoi usare un nome di processo comune o lasciare a null se non c'è un processo specifico da monitorare.
                    LauncherExe = "GamingServices"; // Nome di un processo comune associato a Xbox PC Game Pass / Xbox App
                }
            }

            public override int RunAndWait(ProcessStartInfo path)
            {
                SimpleLogger.Instance.Info("[INFO] XboxGameLauncher: Running command: " + path.FileName);
                
                // Lancia il processo. UseShellExecute è cruciale per i protocolli URI e AUMID.
                Process.Start(path);

                // La logica di monitoraggio è delegata a GameLauncher o gestita qui
                // LauncherExe è il nome del processo da monitorare
                Process xboxGameProcess = GetLauncherExeProcess();

                if (xboxGameProcess != null)
                {
                    SimpleLogger.Instance.Info("[INFO] Xbox game process (" + LauncherExe + ") found, waiting to exit.");
                    xboxGameProcess.WaitForExit();
                }
                else
                {
                    SimpleLogger.Instance.Info("[INFO] No specific Xbox game process (" + LauncherExe + ") found to monitor, assuming launch successful.");
                }

                // Opzionale: Uccidi l'Xbox App o processi correlati dopo l'uscita del gioco
                // Questo è simile alla logica di killsteam/killamazon in altri launcher
                if (Program.SystemConfig.getOptBoolean("killxboxapp")) // Richiede una nuova opzione in es_settings.cfg
                {
                    foreach (var ui in Process.GetProcessesByName("GamingServices").Concat(Process.GetProcessesByName("XboxGamingOverlay")).Concat(Process.GetProcessesByName("XboxApp")))
                    {
                        try { ui.Kill(); SimpleLogger.Instance.Info($"[INFO] Killed Xbox related process: {ui.ProcessName}"); }
                        catch { }
                    }
                }

                return 0; // Ritorna 0 per successo
            }
        }
    }
}