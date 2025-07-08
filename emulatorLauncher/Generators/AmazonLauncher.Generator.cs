using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using EmulatorLauncher.Common;
using EmulatorLauncher.Common.Launchers;
using System.Threading.Tasks;


namespace EmulatorLauncher
{
    class AmazonLauncherGenerator : Generator
    {
        private string _uriToLaunch;

        public override ProcessStartInfo Generate(string system, string emulator, string core, string rom, string playersControllers, ScreenResolution resolution)
        {
            _uriToLaunch = rom;
            return new ProcessStartInfo("cmd.exe", "/c \"exit\"") { CreateNoWindow = true };
        }

        public override int RunAndWait(ProcessStartInfo path)
{
    if (string.IsNullOrEmpty(_uriToLaunch)) return 1;

    // La modifica è qui: usiamo Task.Factory.StartNew invece di Task.Run
    Task.Factory.StartNew(() =>
    {
        // FASE 1: AVVIO PULITO (se necessario)
        if (!Process.GetProcessesByName("Amazon Games UI").Any())
        {
            SimpleLogger.Instance.Info("[AmazonLauncher] Client non rilevato. Avvio con WorkingDirectory corretta.");
            string clientExePath = AmazonLibrary.GetAmazonClientExePath();
            if (string.IsNullOrEmpty(clientExePath))
            {
                SimpleLogger.Instance.Error("[AmazonLauncher] Fallito: percorso di Amazon Games.exe non trovato.");
                return;
            }

            try
            {
                var psi = new ProcessStartInfo(clientExePath)
                {
                    WorkingDirectory = Path.GetDirectoryName(clientExePath),
                    UseShellExecute = false
                };
                Process.Start(psi);
            }
            catch (Exception ex)
            {
                SimpleLogger.Instance.Error($"[AmazonLauncher] Fallito avvio pulito del client: {ex.Message}");
                return;
            }
        }

        // FASE 2: ATTESA DI STABILITÀ
        SimpleLogger.Instance.Info("[AmazonLauncher] Attesa stabilizzazione del client...");
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
            return;
        }

        // FASE 3: INVIO DEL COMANDO URI
        SimpleLogger.Instance.Info($"[AmazonLauncher] Invio del comando URI finale: {_uriToLaunch}");
        try
        {
            var psiUri = new ProcessStartInfo(_uriToLaunch) { UseShellExecute = true };
            Process.Start(psiUri);
        }
        catch (Exception ex)
        {
            SimpleLogger.Instance.Error($"[AmazonLauncher] Fallito invio URI finale: {ex.Message}");
        }
    });

    SimpleLogger.Instance.Info("[AmazonLauncher] Task di installazione avviato in background.");
    return 0;
}

    }
}