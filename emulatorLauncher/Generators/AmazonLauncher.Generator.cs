using System;
using System.Linq;
using System.Diagnostics;
using EmulatorLauncher.Common.Launchers;
using EmulatorLauncher.Common;
using System.Threading;

namespace EmulatorLauncher
{
    partial class ExeLauncherGenerator : Generator
    {
        class AmazonGameLauncher : GameLauncher
        {
            // Variabile privata per memorizzare SOLO il nome del processo del gioco
            private readonly string _gameExecutableName;

            // Il costruttore ora distingue tra Launcher e Gioco
            public AmazonGameLauncher(Uri uri)
            {
                // 1. Imposta il nome del LAUNCHER in modo permanente.
                // Questo è usato dall'hotkey e dalla logica post-gioco.
                this.LauncherExe = "Amazon Games UI";
                
                // 2. Memorizza il nome del GIOCO in una variabile separata.
                _gameExecutableName = AmazonLibrary.GetAmazonGameExecutableName(uri);
            }
            
            // Il metodo RunAndWait ora usa le variabili corrette
            public override int RunAndWait(ProcessStartInfo path)
            {
                bool uiExists = Process.GetProcessesByName("Amazon Games UI").Any();

                SimpleLogger.Instance.Info("[INFO] Nome eseguibile del gioco da monitorare: " + _gameExecutableName);

                // Uccide eventuali processi del GIOCO già in esecuzione
                foreach (var p in Process.GetProcessesByName(_gameExecutableName))
                {
                    try { p.Kill(); } catch { }
                }

                Process.Start(path);

                // Attende l'avvio del processo del GIOCO
                Process amazonGame = null;
                var watch = Stopwatch.StartNew();
                while(watch.Elapsed.TotalSeconds < 30)
                {
                    amazonGame = Process.GetProcessesByName(_gameExecutableName).FirstOrDefault();
                    if (amazonGame != null)
                        break;
                    
                    Thread.Sleep(500);
                }
                
                // Se il gioco è partito, attende la sua chiusura
                if (amazonGame != null)
                {
                    amazonGame.WaitForExit();

                    // Logica post-gioco per chiudere/nascondere il LAUNCHER
                    if (!uiExists || (Program.SystemConfig.isOptSet("killsteam") && Program.SystemConfig.getOptBoolean("killsteam")))
                    {
                        foreach (var ui in Process.GetProcessesByName("Amazon Games UI"))
                        {
                            try { ui.Kill(); }
                            catch { }
                        }
                    }           
                }
                else
                {
                    SimpleLogger.Instance.Warning($"[WARNING] Il processo del gioco '{_gameExecutableName}' non è stato avviato entro 30 secondi.");
                }

                return 0;
            }
        }
    }
}