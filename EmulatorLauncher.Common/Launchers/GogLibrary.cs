using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Diagnostics; // Aggiungere per Process
using EmulatorLauncher.Common; // Per RegistryKeyEx e SimpleLogger

namespace EmulatorLauncher.Common.Launchers
{
    public static class GogLibrary
    {
        // --- MODIFICA 1: Cambiato l'URL per visualizzare il gioco ---
        public const string GameLaunchUrl = @"goggalaxy://openGameView/{0}"; // ID del gioco come argomento per il client GOG Galaxy

        // Ottiene il percorso di installazione del client GOG Galaxy.
        public static string GetGogClientPath()
        {
            string clientPath = null;
            string registryKeyWow64 = @"SOFTWARE\WOW6432Node\GOG.com\GalaxyClient\paths"; 
            string registryKeyNormal = @"SOFTWARE\GOG.com\GalaxyClient\paths"; 
            string clientExeFileName = "GalaxyClient.exe"; 

            SimpleLogger.Instance.Info($"[GogLibrary] Tentativo di rilevare il percorso di installazione di GOG Galaxy.");
            
            clientPath = (string)RegistryKeyEx.GetRegistryValue(RegistryKeyEx.LocalMachine, registryKeyWow64, "client");

            if (string.IsNullOrEmpty(clientPath))
            {
                clientPath = (string)RegistryKeyEx.GetRegistryValue(RegistryKeyEx.LocalMachine, registryKeyNormal, "client");
            }

            if (!string.IsNullOrEmpty(clientPath))
            {
                string fullClientExePath = Path.Combine(clientPath, clientExeFileName);
                if (File.Exists(fullClientExePath))
                {
                    SimpleLogger.Instance.Info($"[GogLibrary] Eseguibile GOG Galaxy trovato e percorso valido: {clientPath}");
                    return clientPath; 
                }
            }
            
            SimpleLogger.Instance.Info("[GogLibrary] Percorso di installazione di GOG Galaxy non rilevato tramite registro.");
            return null; 
        }

        // Verifica se il client GOG Galaxy è in esecuzione.
        public static bool IsClientRunning()
        {
            return Process.GetProcessesByName("GalaxyClient").Any();
        }

        // --- MODIFICA 2: Metodo StartClient migliorato ---
        public static int StartClient(string uri = null)
        {
            SimpleLogger.Instance.Info($"[GogLibrary] Tentativo di avviare il client GOG Galaxy. URI fornito: {(string.IsNullOrEmpty(uri) ? "Nessuno" : uri)}");

            // Se l'URI è fornito, proviamo a lanciarlo direttamente.
            // Questo è il modo più robusto per gestire i protocolli personalizzati come goggalaxy://
            if (!string.IsNullOrEmpty(uri))
            {
                try
                {
                    SimpleLogger.Instance.Info($"[GogLibrary] Avvio diretto dell'URI tramite Process.Start: {uri}");
                    Process.Start(new ProcessStartInfo(uri) { UseShellExecute = true });
                    SimpleLogger.Instance.Info("[GogLibrary] Avvio URI completato (o tentato).");
                    return 0; // Successo
                }
                catch (Exception ex)
                {
                    SimpleLogger.Instance.Error($"[GogLibrary] Errore durante l'avvio diretto dell'URI '{uri}': {ex.Message}", ex);
                    // Non tornare qui, potremmo provare ad avviare il client come fallback.
                }
            }

            // Logica di fallback: se l'URI fallisce o non è fornito, avvia GalaxyClient.exe direttamente.
            string clientPath = GetGogClientPath(); 
            if (string.IsNullOrEmpty(clientPath))
            {
                SimpleLogger.Instance.Error("[GogLibrary] Fallback: Percorso del client GOG Galaxy non trovato.");
                return 1; 
            }

            string clientExe = Path.Combine(clientPath, "GalaxyClient.exe");
            if (!File.Exists(clientExe))
            {
                SimpleLogger.Instance.Error("[GogLibrary] Fallback: Eseguibile del client GOG Galaxy non trovato: " + clientExe);
                return 1; 
            }

            try
            {
                Process.Start(clientExe);
                SimpleLogger.Instance.Info("[GogLibrary] Fallback: Avvio eseguibile completato.");
                return 0; 
            }
            catch (Exception ex)
            {
                SimpleLogger.Instance.Error($"[GogLibrary] Fallback: Errore durante l'avvio del client GOG Galaxy (eseguibile): {ex.Message}", ex);
                return 1; 
            }
        }

        public static string GetGogGameExecutableName(Uri uri)
        {
            if (uri == null || uri.Segments.Length < 2)
                return null;

            string gameId = uri.Segments.Last();

            SimpleLogger.Instance.Info($"[GogLibrary] Tentativo di ottenere il nome dell'eseguibile per GOG Game ID: {gameId}. Restituito il nome del client per il monitoraggio.");

            // Per GOG, monitorare il client è spesso l'unica opzione affidabile
            // perché non c'è un modo standard per ottenere l'eseguibile del gioco prima dell'installazione.
            return "GalaxyClient"; 
        }

        public static bool IsInstalled
        {
            get
            {
                return !string.IsNullOrEmpty(GetGogClientPath());
            }
        }
    }
}