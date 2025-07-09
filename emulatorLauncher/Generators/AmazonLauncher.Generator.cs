using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using EmulatorLauncher.Common;
using EmulatorLauncher.Common.Launchers;

namespace EmulatorLauncher
{
    class AmazonLauncherGenerator : Generator
    {
        private string _uriToLaunch;
        private string _gameId;

        public override ProcessStartInfo Generate(string system, string emulator, string core, string rom, string playersControllers, ScreenResolution resolution)
        {
            _uriToLaunch = rom;
            
            // Estraiamo il GameId dall'URI per il monitoraggio successivo
            try { _gameId = new Uri(_uriToLaunch).Segments.Last(); }
            catch { _gameId = string.Empty; }

            return new ProcessStartInfo("cmd.exe", "/c \"exit\"") { CreateNoWindow = true };
        }

        public override int RunAndWait(ProcessStartInfo path)
{
    if (string.IsNullOrEmpty(_uriToLaunch)) return 1;

    // --- WARM START ---
    if (Process.GetProcessesByName("Amazon Games UI").Any())
    {
        SimpleLogger.Instance.Info("[AmazonLauncher] Client già attivo. Invio diretto del comando URI.");
        try
        {
            Process.Start(new ProcessStartInfo(_uriToLaunch) { UseShellExecute = true });
            // Anche qui aggiungiamo l'attesa per coerenza
            SimpleLogger.Instance.Info("[AmazonLauncher] Comando inviato. Mantenimento del processo attivo per 20 secondi...");
            Thread.Sleep(20000);
            return 0;
        }
        catch (Exception ex)
        {
            SimpleLogger.Instance.Error($"[AmazonLauncher] Fallito invio URI a client attivo: {ex.Message}");
            return 1;
        }
    }

    // --- COLD START ---
    SimpleLogger.Instance.Info("[AmazonLauncher] Client non attivo. Avvio della sequenza di installazione corretta.");

    // FASE 1: Avvio pulito con WorkingDirectory corretta
    string clientExePath = AmazonLibrary.GetAmazonClientExePath();
    if (string.IsNullOrEmpty(clientExePath)) return 1;
    try
    {
        Process.Start(new ProcessStartInfo(clientExePath) { WorkingDirectory = Path.GetDirectoryName(clientExePath), UseShellExecute = false });
    }
    catch (Exception ex)
    {
        SimpleLogger.Instance.Error($"[AmazonLauncher] Fallito avvio pulito del client: {ex.Message}");
        return 1;
    }

    // FASE 2: Attesa di stabilità
    var watch = Stopwatch.StartNew();
    bool clientReady = false;
    while (watch.Elapsed.TotalSeconds < 60)
    {
        if (Process.GetProcessesByName("Amazon Games Services").Length > 0 &&
            Process.GetProcessesByName("Amazon Games UI").Length == 4)
        {
            clientReady = true;
            SimpleLogger.Instance.Info("[AmazonLauncher] Client stabile rilevato.");
            Thread.Sleep(2000);
            break;
        }
        Thread.Sleep(1000);
    }
    if (!clientReady)
    {
        SimpleLogger.Instance.Error("[AmazonLauncher] Timeout: il client non si è stabilizzato.");
        return 1;
    }

    // FASE 3: Invio del comando URI
    try
    {
        Process.Start(new ProcessStartInfo(_uriToLaunch) { UseShellExecute = true });
    }
    catch (Exception ex)
    {
        SimpleLogger.Instance.Error($"[AmazonLauncher] Fallito invio URI finale: {ex.Message}");
        return 1;
    }

    // FASE 4: LA TUA IDEA - MANTENERE VIVO IL PROCESSO
    SimpleLogger.Instance.Info("[AmazonLauncher] Comando inviato. Mantenimento del processo attivo per 20 secondi...");
    Thread.Sleep(20000); // <-- L'ATTESA

    SimpleLogger.Instance.Info("[AmazonLauncher] Attesa terminata. Uscita.");
    return 0;
}

        private int MonitorInstallation()
        {
            SimpleLogger.Instance.Info($"[AmazonLauncher] Avvio monitoraggio installazione per GameId: {_gameId}");
            var watch = Stopwatch.StartNew();

            // Timeout di 1 ora per l'installazione
            while (watch.Elapsed.TotalHours < 1)
            {
                try
                {
                    var installedGames = AmazonLibrary.GetInstalledGames();
                    if (installedGames.Any(g => g.Id == _gameId))
                    {
                        SimpleLogger.Instance.Info($"[AmazonLauncher] Installazione completata per GameId: {_gameId}");
                        return 0; // Successo!
                    }
                }
                catch (Exception ex)
                {
                    SimpleLogger.Instance.Error($"[AmazonLauncher] Errore durante il monitoraggio: {ex.Message}");
                }
                
                // Attendi 15 secondi prima di controllare di nuovo
                Thread.Sleep(15000);
            }
            
            SimpleLogger.Instance.Warning("[AmazonLauncher] Timeout monitoraggio installazione. Il gioco potrebbe non essersi installato.");
            return 1; // Timeout
        }
    }
}