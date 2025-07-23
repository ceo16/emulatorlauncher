using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;
using System.IO; // Aggiunto per Path.GetFileNameWithoutExtension

namespace EmulatorLauncher.Common
{
    public static class ProcessExtensions
    {
        public static string RunWithOutput(string fileName, string arguments = null)
        {
            var ps = new ProcessStartInfo() { FileName = fileName };
            if (arguments != null)
                ps.Arguments = arguments;

            return RunWithOutput(ps);
        }

        public static string RunWithOutput(this ProcessStartInfo psi)
        {
            psi.WindowStyle = ProcessWindowStyle.Hidden;
            psi.CreateNoWindow = true;
            psi.UseShellExecute = false;
            psi.RedirectStandardOutput = true;
            psi.RedirectStandardError = true;

            var process = Process.Start(psi);
            process.WaitForExit();

            var result = process.StandardOutput.ReadToEnd();
            if (string.IsNullOrEmpty(result))
                result = process.StandardError.ReadToEnd();

            return result;
        }

        // Nuovo metodo: Avvia un processo e attende la sua uscita.
        public static int Start(string fileName, string arguments = null, string workingDirectory = null, bool waitForExit = true)
        {
            ProcessStartInfo psi = new ProcessStartInfo
            {
                FileName = fileName,
                Arguments = arguments ?? "",
                WorkingDirectory = workingDirectory ?? Path.GetDirectoryName(fileName),
                UseShellExecute = true // Importante per avviare eseguibili o URL
            };

            try
            {
                Process process = Process.Start(psi);
                if (waitForExit)
                {
                    process.WaitForExit();
                    return process.ExitCode;
                }
                return 0; // Successo se non si attende
            }
            catch (Exception ex)
            {
                SimpleLogger.Instance.Error($"[ProcessExtensions] Errore durante l'avvio del processo '{fileName}': {ex.Message}", ex);
                return 1; // Errore
            }
        }

        // Nuovo metodo: Avvia un URI (es. "steam://rungameid/...")
        public static int StartUri(string uri)
        {
            if (string.IsNullOrEmpty(uri))
            {
                SimpleLogger.Instance.Error("[ProcessExtensions] URI da avviare è nullo o vuoto.");
                return 1;
            }

            try
            {
                // Avvia l'URI direttamente. Windows si occuperà di aprire l'applicazione appropriata.
                Process.Start(new ProcessStartInfo(uri) { UseShellExecute = true });
                SimpleLogger.Instance.Info($"[ProcessExtensions] Avviato URI: {uri}");
                return 0; // Successo
            }
            catch (Exception ex)
            {
                SimpleLogger.Instance.Error($"[ProcessExtensions] Errore durante l'avvio dell'URI '{uri}': {ex.Message}", ex);
                return 1; // Errore
            }
        }

        public static string GetProcessCommandline(this Process process)
        {
            if (process == null)
                return null;

            try
            {
                using (var cquery = new System.Management.ManagementObjectSearcher("SELECT CommandLine FROM Win32_Process WHERE ProcessId=" + process.Id))
                {
                    var commandLine = cquery.Get()
                        .OfType<System.Management.ManagementObject>()
                        .Select(p => (string)p["CommandLine"])
                        .FirstOrDefault();

                    return commandLine;
                }
            }
            catch
            {

            }

            return null;
        }
    }
}