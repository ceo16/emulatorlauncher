using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.IO;
using System.Diagnostics;
using System.Threading;
using System.Windows.Forms; // Necessario per MessageBox
using EmulatorLauncher.Common;
using EmulatorLauncher.Common.EmulationStation;
using EmulatorLauncher.Common.FileFormats;
using EmulatorLauncher.PadToKeyboard;
using System.Xml.Linq;
using EmulatorLauncher.Common.Launchers; 
using EmulatorLauncher.Common; // Per User32 e SW enum

namespace EmulatorLauncher
{
    // ------------------------------------------------------------------------------------
    // START: DEFINIZIONE DELLE CLASSI PER I LAUNCHER (Corrette)
    // ------------------------------------------------------------------------------------
    public abstract class GameLauncher
    {
        public string LauncherExe { get; protected set; }
        public Uri GameUri { get; protected set; } 

        public GameLauncher(Uri uri)
        {
            this.GameUri = uri;
        }

        public GameLauncher() { } 

        public abstract int RunAndWait(ProcessStartInfo path);

        public virtual PadToKey SetupCustomPadToKeyMapping(PadToKey mapping)
        {
            if (!string.IsNullOrEmpty(LauncherExe))
                return PadToKey.AddOrUpdateKeyMapping(mapping, LauncherExe, InputKey.hotkey | InputKey.start, "(%{KILL})");

            return mapping;
        }
        
        protected void KillExistingLauncherExes()
        {
            if (string.IsNullOrEmpty(LauncherExe)) return;
            
            foreach (var px in Process.GetProcessesByName(Path.GetFileNameWithoutExtension(LauncherExe)))
            {
                try { px.Kill(); }
                catch { }
            }
        }

        protected Process GetLauncherExeProcess()
        {
            if (string.IsNullOrEmpty(LauncherExe)) return null;

            Process launcherprocess = null;
            int waittime = 30;

            if (Program.SystemConfig.isOptSet("steam_wait") && !string.IsNullOrEmpty(Program.SystemConfig["steam_wait"]))
                waittime = Program.SystemConfig["steam_wait"].ToInteger();

            SimpleLogger.Instance.Info($"[INFO] Attesa di {waittime} secondi per il processo {LauncherExe}");

            for (int i = 0; i < waittime; i++)
            {
                launcherprocess = Process.GetProcessesByName(Path.GetFileNameWithoutExtension(LauncherExe)).FirstOrDefault();
                if (launcherprocess != null)
                    break;
                Thread.Sleep(1000);
            }
            return launcherprocess;
        }
    }

    class SteamGameLauncher : GameLauncher
    {
        public SteamGameLauncher(Uri uri) : base(uri) { LauncherExe = "steam"; }
        
        public override int RunAndWait(ProcessStartInfo path)
        {
            KillExistingLauncherExes(); 

            if (GameUri == null)
            {
                SimpleLogger.Instance.Error("[SteamGameLauncher] URI del gioco non fornito.");
                return 1;
            }

            if (!SteamLibrary.IsInstalled)
            {
                SimpleLogger.Instance.Error("[SteamGameLauncher] Steam non è installato. Impossibile avviare il gioco.");
                MessageBox.Show("Steam non è installato. Impossibile avviare il gioco.", "Errore Steam", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return 1; 
            }

            ProcessExtensions.StartUri(GameUri.AbsoluteUri);

            string gameExeName = SteamLibrary.GetSteamGameExecutableName(GameUri, Path.Combine(Program.AppConfig.GetFullPath("bios"), "steam.json"));
            
            if (string.IsNullOrEmpty(gameExeName))
            {
                SimpleLogger.Instance.Error("[SteamGameLauncher] Impossibile determinare l'eseguibile del gioco o avviare Steam per l'URI: " + GameUri);
                return 1; 
            }

            Process gameProcess = null;
            int waitTime = 120; 

            SimpleLogger.Instance.Info($"[SteamGameLauncher] Attesa di {waitTime} secondi per il processo del gioco: {gameExeName}");

            for (int i = 0; i < waitTime; i++)
            {
                gameProcess = Process.GetProcessesByName(Path.GetFileNameWithoutExtension(gameExeName)).FirstOrDefault();
                if (gameProcess != null)
                {
                    SimpleLogger.Instance.Info($"[SteamGameLauncher] Processo del gioco '{gameExeName}' rilevato.");
                    break;
                }
                Thread.Sleep(1000); 
            }

            if (gameProcess == null)
            {
                SimpleLogger.Instance.Error($"[SteamGameLauncher] Il processo del gioco '{gameExeName}' non è stato avviato entro il tempo limite.");
                return 1; 
            }
            
            Process steamClientProcess = Process.GetProcessesByName("steam").FirstOrDefault();
            if (steamClientProcess != null && steamClientProcess.MainWindowHandle != IntPtr.Zero && User32.IsWindowVisible(steamClientProcess.MainWindowHandle))
            {
                SimpleLogger.Instance.Info("[SteamGameLauncher] Nascondo la finestra di Steam.");
                User32.ShowWindow(steamClientProcess.MainWindowHandle, SW.HIDE); 
            }

            gameProcess.WaitForExit();

            SimpleLogger.Instance.Info("[SteamGameLauncher] Processo del gioco terminato. Ripristino della finestra di Steam.");
            
            if (steamClientProcess != null && !steamClientProcess.HasExited && steamClientProcess.MainWindowHandle != IntPtr.Zero)
            {
                User32.ShowWindow(steamClientProcess.MainWindowHandle, SW.RESTORE); 
                User32.SetForegroundWindow(steamClientProcess.MainWindowHandle); 
            }
            return 0; 
        }
    }

    class EpicGameLauncher : GameLauncher
    {
        public EpicGameLauncher(Uri uri) : base(uri) { LauncherExe = "EpicGamesLauncher"; }
        
        public override int RunAndWait(ProcessStartInfo path)
        {
            KillExistingLauncherExes();

            if (GameUri == null)
            {
                SimpleLogger.Instance.Error("[EpicGameLauncher] URI del gioco non fornito.");
                return 1;
            }

            if (!EpicLibrary.IsInstalled)
            {
                SimpleLogger.Instance.Error("[EpicGameLauncher] Epic Games Launcher non è installato. Impossibile avviare il gioco.");
                MessageBox.Show("Epic Games Launcher non è installato. Impossibile avviare il gioco.", "Errore Epic Games", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return 1; 
            }

            ProcessExtensions.StartUri(GameUri.AbsoluteUri);

            string gameExeName = EpicLibrary.GetEpicGameExecutableName(GameUri);

            if (string.IsNullOrEmpty(gameExeName))
            {
                SimpleLogger.Instance.Error("[EpicGameLauncher] Impossibile determinare l'eseguibile del gioco o avviare Epic Games Launcher per l'URI: " + GameUri);
                return 1;
            }

            Process gameProcess = null;
            int waitTime = 120; 

            SimpleLogger.Instance.Info($"[EpicGameLauncher] Attesa di {waitTime} secondi per il processo del gioco: {gameExeName}");

            for (int i = 0; i < waitTime; i++)
            {
                gameProcess = Process.GetProcessesByName(Path.GetFileNameWithoutExtension(gameExeName)).FirstOrDefault();
                if (gameProcess != null)
                {
                    SimpleLogger.Instance.Info($"[EpicGameLauncher] Processo del gioco '{gameExeName}' rilevato.");
                    break;
                }
                Thread.Sleep(1000);
            }

            if (gameProcess == null)
            {
                SimpleLogger.Instance.Error($"[EpicGameLauncher] Il processo del gioco '{gameExeName}' non è stato avviato entro il tempo limite.");
                return 1;
            }

            Process epicClientProcess = Process.GetProcessesByName("EpicGamesLauncher").FirstOrDefault();
            if (epicClientProcess != null && epicClientProcess.MainWindowHandle != IntPtr.Zero && User32.IsWindowVisible(epicClientProcess.MainWindowHandle))
            {
                SimpleLogger.Instance.Info("[EpicGameLauncher] Nascondo la finestra di Epic Games Launcher.");
                User32.ShowWindow(epicClientProcess.MainWindowHandle, SW.HIDE); 
            }

            gameProcess.WaitForExit();

            SimpleLogger.Instance.Info("[EpicGameLauncher] Processo del gioco terminato. Ripristino della finestra di Epic Games Launcher.");

            if (epicClientProcess != null && !epicClientProcess.HasExited && epicClientProcess.MainWindowHandle != IntPtr.Zero)
            {
                User32.ShowWindow(epicClientProcess.MainWindowHandle, SW.RESTORE); 
                User32.SetForegroundWindow(epicClientProcess.MainWindowHandle);
            }
            return 0;
        }
    }

class AmazonGameLauncher : GameLauncher
{
    private readonly string _gameExecutableName; 

    public AmazonGameLauncher(Uri uri) : base(uri) 
    { 
        this.LauncherExe = "Amazon Games UI"; 
        _gameExecutableName = AmazonLibrary.GetAmazonGameExecutableName(uri);
    }
    
    public override int RunAndWait(ProcessStartInfo path)
    {
        string uriToLaunch = GameUri.AbsoluteUri;

        if (uriToLaunch.Contains("://install/"))
        {
            // =======================================================
            // ##                  LOGICA DI INSTALLAZIONE            ##
            // =======================================================
            if (Process.GetProcessesByName("Amazon Games UI").Any())
            {
                Process.Start(new ProcessStartInfo(uriToLaunch) { UseShellExecute = true });
                return 0;
            }

            string clientExePath = AmazonLibrary.GetAmazonClientExePath();
            if (string.IsNullOrEmpty(clientExePath)) return 1;
            
            Process.Start(new ProcessStartInfo(clientExePath) { WorkingDirectory = Path.GetDirectoryName(clientExePath), UseShellExecute = false });

            var watch = Stopwatch.StartNew();
            bool clientReady = false;
            while (watch.Elapsed.TotalSeconds < 60)
            {
                if (Process.GetProcessesByName("Amazon Games Services").Length > 0 && Process.GetProcessesByName("Amazon Games UI").Length == 4)
                {
                    clientReady = true;
                    Thread.Sleep(3000);
                    break;
                }
                Thread.Sleep(1000);
            }

            if (!clientReady) return 1;
            
            Process.Start(new ProcessStartInfo(uriToLaunch) { UseShellExecute = true });
            return 0;
        }
        else
        {
            // =======================================================
            // ##                    LOGICA DI GIOCO                  ##
            // =======================================================
            string clientExePath = AmazonLibrary.GetAmazonClientExePath();
            if (string.IsNullOrEmpty(clientExePath)) return 1;

            Process.Start(new ProcessStartInfo(clientExePath)
            {
                Arguments = uriToLaunch,
                WorkingDirectory = Path.GetDirectoryName(clientExePath),
                UseShellExecute = false
            });

            if (string.IsNullOrEmpty(_gameExecutableName)) return 0;

            Process gameProcess = null;
            var gameWatch = Stopwatch.StartNew();
            while (gameWatch.Elapsed.TotalSeconds < 120)
            {
                gameProcess = Process.GetProcessesByName(Path.GetFileNameWithoutExtension(_gameExecutableName)).FirstOrDefault();
                if (gameProcess != null) break;
                Thread.Sleep(10000);
            }

            if (gameProcess != null)
            {
                var amazonClientProcess = Process.GetProcessesByName("Amazon Games UI").FirstOrDefault(p => p.MainWindowHandle != IntPtr.Zero);
                if (amazonClientProcess != null && User32.IsWindowVisible(amazonClientProcess.MainWindowHandle))
                    User32.ShowWindow(amazonClientProcess.MainWindowHandle, SW.HIDE); 

                if (gameProcess.MainWindowHandle != IntPtr.Zero)
                {
                    User32.SetForegroundWindow(gameProcess.MainWindowHandle);
                    User32.ShowWindow(gameProcess.MainWindowHandle, SW.RESTORE);
                }

                gameProcess.WaitForExit();

                // NUOVA LOGICA: Chiudi il client Amazon dopo che il gioco è terminato.
                SimpleLogger.Instance.Info("[AmazonGameLauncher-Play] Gioco chiuso. Chiusura del client Amazon Games.");
                try
                {
                    foreach (var p in Process.GetProcessesByName("Amazon Games UI"))
                        p.Kill();
                    foreach (var p in Process.GetProcessesByName("Amazon Games Services"))
                        p.Kill();
                }
                catch { } // Ignora eventuali errori se i processi sono già chiusi.
            }
            
            return 0;
        }
    }
}

class XboxGameLauncher : GameLauncher
{
    private readonly string _gameAumid;

    // ✅ QUESTO È IL COSTRUTTORE CORRETTO CHE ACCETTA 2 ARGOMENTI
    public XboxGameLauncher(Uri uri, string aumid = null) : base(uri)
    {
        LauncherExe = "explorer.exe"; // Usiamo explorer per lanciare l'app
        _gameAumid = aumid;
    }

    public override int RunAndWait(ProcessStartInfo path)
    {
        // Caso 1: Stiamo installando un gioco tramite URI
        if (!string.IsNullOrEmpty(GameUri?.AbsoluteUri))
        {
            SimpleLogger.Instance.Info($"[XboxGameLauncher] Avvio del Microsoft Store con URI: {GameUri.AbsoluteUri}");
            ProcessExtensions.StartUri(GameUri.AbsoluteUri);
            SimpleLogger.Instance.Info("[XboxGameLauncher] Comando inviato. L'operazione è delegata all'utente nello Store.");
            Thread.Sleep(3000); 
            return 0;
        }

    // Caso 2: Lancio gioco
    try
    {
        string aumidToLaunch = _gameAumid ?? path.FileName;
        if (string.IsNullOrEmpty(aumidToLaunch)) { /*...*/ return 1; }

        SimpleLogger.Instance.Info($"[XboxGameLauncher] Avvio del gioco/launcher con AUMID: shell:appsFolder\\{aumidToLaunch}");
        try
        {
            Process.Start(new ProcessStartInfo("explorer.exe", $"shell:appsFolder\\{aumidToLaunch}") { UseShellExecute = true });
        }
        catch (Exception ex)
{
    SimpleLogger.Instance.Error($"[XboxGameLauncher] Errore critico durante l'avvio del gioco: {ex.Message}", ex);
    MessageBox.Show($"Impossibile avviare il gioco Xbox:\n{aumidToLaunch}\n\nErrore: {ex.Message}", "Errore di avvio", MessageBoxButtons.OK, MessageBoxIcon.Error);
    return 1;
}
        
        // --- FASE 1: TROVA E ATTENDI IL LAUNCHER INTERMEDIO ---
        SimpleLogger.Instance.Info("[XboxGameLauncher] Fase 1: Ricerca del launcher intermedio...");
        var launcherProcess = FindGameProcess(60); // Cerca per 60 secondi

        if (launcherProcess != null)
        {
            SimpleLogger.Instance.Info($"[XboxGameLauncher] Launcher intermedio '{launcherProcess.ProcessName}' trovato. In attesa della sua chiusura...");
            
            // Porta il launcher in primo piano
            User32.SetForegroundWindow(launcherProcess.MainWindowHandle);
            User32.ShowWindow(launcherProcess.MainWindowHandle, SW.RESTORE);
            
            launcherProcess.WaitForExit();
            SimpleLogger.Instance.Info($"[XboxGameLauncher] Launcher intermedio chiuso.");
        }
        else
        {
            SimpleLogger.Instance.Warning("[XboxGameLauncher] Fase 1: Nessun launcher intermedio trovato. Si procederà direttamente alla ricerca del gioco.");
        }

        // --- FASE 2: TROVA E ATTENDI IL GIOCO VERO E PROPRIO ---
        SimpleLogger.Instance.Info("[XboxGameLauncher] Fase 2: Ricerca del processo del gioco principale...");
        Thread.Sleep(2000); // Piccola pausa per dare tempo al gioco di avviarsi

        var mainGameProcess = FindGameProcess(60); // Cerca di nuovo per 60 secondi

        if (mainGameProcess != null)
        {
            SimpleLogger.Instance.Info($"[XboxGameLauncher] Gioco principale '{mainGameProcess.ProcessName}' trovato. In attesa della sua chiusura...");

            // Porta il GIOCO in primo piano
            User32.SetForegroundWindow(mainGameProcess.MainWindowHandle);
            User32.ShowWindow(mainGameProcess.MainWindowHandle, SW.RESTORE);

            mainGameProcess.WaitForExit();
            SimpleLogger.Instance.Info($"[XboxGameLauncher] Gioco principale chiuso.");
        }
        else
        {
            SimpleLogger.Instance.Warning("[XboxGameLauncher] Fase 2: Processo del gioco principale non trovato. Il ritorno a EmulationStation potrebbe essere immediato.");
        }
    }
    finally
    {
        // --- FASE 3: RIPRISTINA EMULATIONSTATION ---
        var esProcess = Process.GetProcessesByName("emulationstation").FirstOrDefault();
        if (esProcess != null && esProcess.MainWindowHandle != IntPtr.Zero)
        {
            SimpleLogger.Instance.Info("[XboxGameLauncher] Ripristino della finestra di EmulationStation.");
            User32.ShowWindow(esProcess.MainWindowHandle, SW.RESTORE);
            User32.SetForegroundWindow(esProcess.MainWindowHandle);
        }
    }

    return 0;
}

// Metodo di supporto per non ripetere il codice
private Process FindGameProcess(int timeoutSeconds)
{
    var watch = Stopwatch.StartNew();
    while (watch.Elapsed.TotalSeconds < timeoutSeconds)
    {
        var gameProcess = Process.GetProcesses().FirstOrDefault(p =>
            p.MainWindowHandle != IntPtr.Zero &&
            User32.IsWindowVisible(p.MainWindowHandle) &&
            p.Id != Process.GetCurrentProcess().Id &&
            p.ProcessName != "explorer" &&
            p.ProcessName != "ApplicationFrameHost" &&
            p.ProcessName != "SystemSettings" &&
            p.ProcessName != "emulationstation" &&
            (DateTime.Now - p.StartTime).TotalSeconds < timeoutSeconds);

        if (gameProcess != null)
            return gameProcess;

        Thread.Sleep(1000);
    }
    return null;
}
}
    
    class EAGameLauncher : GameLauncher
    {
        public EAGameLauncher(Uri uri) : base(uri) { LauncherExe = "EADesktop"; }
        public override int RunAndWait(ProcessStartInfo path) { return 0; }
    }

    class GogGameLauncher : GameLauncher
    {
        public GogGameLauncher(Uri uri) : base(uri) 
        { 
            this.LauncherExe = "GalaxyClient"; 
        }
        
        public override int RunAndWait(ProcessStartInfo path) 
        { 
            KillExistingLauncherExes(); 

            if (GameUri == null)
            {
                SimpleLogger.Instance.Error("[GogGameLauncher] URI del gioco non fornito.");
                return 1;
            }

            if (!GogLibrary.IsInstalled)
            {
                SimpleLogger.Instance.Error("[GogGameLauncher] GOG Galaxy non è installato. Impossibile avviare il gioco.");
                MessageBox.Show("GOG Galaxy non è installato. Impossibile avviare il gioco.", "Errore GOG Galaxy", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return 1; 
            }

            GogLibrary.StartClient(GameUri.AbsoluteUri); // Questa è la riga cruciale che chiama GogLibrary.StartClient

            string gameExeName = GogLibrary.GetGogGameExecutableName(GameUri);

            if (string.IsNullOrEmpty(gameExeName) || gameExeName == "GameExecutablePlaceholder") 
            {
                SimpleLogger.Instance.Warning("[GogGameLauncher] Impossibile determinare il nome dell'eseguibile del gioco GOG. Monitorerò il processo di GOG Galaxy Client.");
                gameExeName = "GalaxyClient"; 
            }

            Process gameProcess = null;
            int waitTime = 120; 

            SimpleLogger.Instance.Info($"[GogGameLauncher] Attesa di {waitTime} secondi per il processo del gioco/client GOG: {gameExeName}");

            for (int i = 0; i < waitTime; i++)
            {
                gameProcess = Process.GetProcessesByName(Path.GetFileNameWithoutExtension(gameExeName)).FirstOrDefault();
                if (gameProcess != null)
                {
                    SimpleLogger.Instance.Info($"[GogGameLauncher] Processo del gioco/client '{gameExeName}' rilevato.");
                    break;
                }
                Thread.Sleep(1000); 
            }

            if (gameProcess == null)
            {
                SimpleLogger.Instance.Error($"[GogGameLauncher] Il processo del gioco/client '{gameExeName}' non è stato avviato entro il tempo limite.");
                return 1; 
            }

            Process gogClientProcess = Process.GetProcessesByName("GalaxyClient").FirstOrDefault();
            if (gogClientProcess != null && gogClientProcess.MainWindowHandle != IntPtr.Zero && User32.IsWindowVisible(gogClientProcess.MainWindowHandle))
            {
                SimpleLogger.Instance.Info("[GogGameLauncher] Nascondo la finestra di GOG Galaxy Client.");
                User32.ShowWindow(gogClientProcess.MainWindowHandle, SW.HIDE); 
            }

            gameProcess.WaitForExit(); 

            SimpleLogger.Instance.Info("[GogGameLauncher] Processo del gioco terminato. Ripristino della finestra di GOG Galaxy Client.");
            
            if (gogClientProcess != null && !gogClientProcess.HasExited && gogClientProcess.MainWindowHandle != IntPtr.Zero)
            {
                User32.ShowWindow(gogClientProcess.MainWindowHandle, SW.RESTORE); 
                User32.SetForegroundWindow(gogClientProcess.MainWindowHandle);
            }
            return 0; 
        }
    }

    // ------------------------------------------------------------------------------------
    // END: DEFINIZIONE DELLE CLASSI PER I LAUNCHER
    // ------------------------------------------------------------------------------------

    partial class ExeLauncherGenerator : Generator
    {
		private JoystickListener _gameHotkeys;
		private PadToKey _padToKeyMapping; 
        
        public ExeLauncherGenerator()
        {
            DependsOnDesktopResolution = true;
        }

        private string _systemName;
        private string _exename = null;
        private bool _isGameExePath;
        private bool _steamRun = false;
        private bool _exeFile;
        private bool _nonSteam = false;

        private GameLauncher _gameLauncher; 

        static readonly Dictionary<string, Func<Uri, GameLauncher>> launchers = new Dictionary<string, Func<Uri, GameLauncher>>(StringComparer.OrdinalIgnoreCase)
        {
            { "com.epicgames.launcher", (uri) => new EpicGameLauncher(uri) },
            { "steam", (uri) => new SteamGameLauncher(uri) },
            { "amazon-games", (uri) => new AmazonGameLauncher(uri) },
            { "ms-windows-store", (uri) => new XboxGameLauncher(uri) },
            { "eagames", (uri) => new EAGameLauncher(uri) },
            { "origin2", (uri) => new EAGameLauncher(uri) },
            { "eadesktop", (uri) => new EAGameLauncher(uri) },
            { "goggalaxy", (uri) => new GogGameLauncher(uri) }
        };
        
        public override System.Diagnostics.ProcessStartInfo Generate(string system, string emulator, string core, string rom, string playersControllers, ScreenResolution resolution)
        {
            bool fullscreen = !IsEmulationStationWindowed() || SystemConfig.getOptBoolean("forcefullscreen"); 

            try
            {
                string correctedRom = rom;
                string[] storeSchemes = { "steam", "com.epicgames.launcher", "amazon-games", "ms-windows-store", "xboxstore", "eagames", "goggalaxy", "gog", "eadesktop", "origin2" };

                var matchingSchemePrefix = storeSchemes.FirstOrDefault(s => correctedRom.StartsWith(s + @":\", StringComparison.OrdinalIgnoreCase));
                if (matchingSchemePrefix != null)
                {
                    correctedRom = matchingSchemePrefix + "://" + correctedRom.Substring(matchingSchemePrefix.Length + 2);
                }
                
                correctedRom = correctedRom.Replace('\\', '/');
				
				 if (correctedRom.StartsWith("xboxstore:/install/", StringComparison.OrdinalIgnoreCase))
        {
            string productId = correctedRom.Substring("xboxstore:/install/".Length);
            // Traduce il tuo comando nell'URI ufficiale che Windows capisce.
            correctedRom = $"ms-windows-store://pdp/?productid={productId}";
            SimpleLogger.Instance.Info($"[Generator] Rilevato e tradotto URI Xbox Store in: {correctedRom}");
        }

                if (Uri.TryCreate(correctedRom, UriKind.Absolute, out Uri uriResult) && launchers.ContainsKey(uriResult.Scheme))
                {
                    SimpleLogger.Instance.Info("[INFO] Rilevato e corretto URI del gioco: " + correctedRom);

                    _gameLauncher = launchers[uriResult.Scheme](uriResult); 
                    SimpleLogger.Instance.Info($"[INFO] GameLauncher impostato: {_gameLauncher.GetType().Name} (Launcher EXE: {_gameLauncher.LauncherExe})");

                    return new ProcessStartInfo()
                    {
                        FileName = correctedRom,
                        UseShellExecute = true 
                    };
                }
                else if (File.Exists(rom))
                {
                    string fileExtension = Path.GetExtension(rom).ToLower(); 
                    if (fileExtension == ".url" || fileExtension == ".lnk" || fileExtension == ".game")
                    {
                        string urlContent = null;
                        if (fileExtension == ".url")
                        {
                            urlContent = IniFile.FromFile(rom).GetValue("InternetShortcut", "URL");
                        }
                        else if (fileExtension == ".lnk")
                        {
                            urlContent = FileTools.GetShortcutTarget(rom);
                        }
                        else if (fileExtension == ".game")
                        {
                            var lines = File.ReadAllLines(rom);
                            if (lines.Length > 0)
                            {
                                urlContent = lines[0];
                                if (urlContent.StartsWith("URI=", StringComparison.OrdinalIgnoreCase))
                                {
                                    urlContent = urlContent.Substring(4);
                                }
                            }
                        }

                        if (!string.IsNullOrEmpty(urlContent) && Uri.TryCreate(urlContent, UriKind.Absolute, out Uri fileUriResult) && launchers.ContainsKey(fileUriResult.Scheme))
                        {
                            SimpleLogger.Instance.Info("[INFO] Rilevato e corretto URI del gioco da file (.url/.lnk/.game): " + urlContent);
                            _gameLauncher = launchers[fileUriResult.Scheme](fileUriResult);
                            SimpleLogger.Instance.Info($"[INFO] GameLauncher impostato: {_gameLauncher.GetType().Name} (Launcher EXE: {_gameLauncher.LauncherExe})");
                            return new ProcessStartInfo()
                            {
                                FileName = urlContent,
                                UseShellExecute = true
                            };
                        }
                    }
                }
				 // <<< INIZIO BLOCCO DA AGGIUNGERE >>>
         else if (system.Equals("xboxstore", StringComparison.OrdinalIgnoreCase) && rom.Contains("!"))
    {
        SimpleLogger.Instance.Info($"[Generator] Rilevato AUMID per gioco Xbox installato: {rom}");
        _gameLauncher = new XboxGameLauncher(null, rom); // Passiamo l'AUMID al costruttore
        SimpleLogger.Instance.Info($"[INFO] GameLauncher impostato: {_gameLauncher.GetType().Name}");

        // Creiamo un ProcessStartInfo fittizio; la vera logica è in RunAndWait
        return new ProcessStartInfo() { FileName = rom };
    }
            }
            catch (Exception ex)
            {
                SimpleLogger.Instance.Error("[ExeLauncherGenerator] Errore nella gestione dell'URI: " + ex.Message);
            }

            rom = this.TryUnZipGameIfNeeded(system, rom);

            _systemName = system.ToLowerInvariant();

            string path = Path.GetDirectoryName(rom);
            string arguments = null;
            _isGameExePath = false;
            _exeFile = false;
            string extension = Path.GetExtension(rom); 

            if (extension == ".game") 
            {
                string[] lines = File.ReadAllLines(rom);
                if (lines.Length == 0) throw new Exception("No path specified in .game file.");
                string linkTarget = lines[0];
                if (!File.Exists(linkTarget)) throw new Exception("Target file " + linkTarget + " does not exist.");
                
                _isGameExePath = true;
                rom = linkTarget;
                path = Path.GetDirectoryName(linkTarget);
            }
            else if (Directory.Exists(rom))
            {
                path = rom;

                string autorun = new[] { "autorun.cmd", "autorun.bat", "autoexec.cmd", "autoexec.bat" }
                    .Select(f => Path.Combine(rom, f))
                    .FirstOrDefault(f => File.Exists(f));

                if (autorun != null)
                    rom = autorun;
                else
                    rom = Directory.GetFiles(path, "*.exe").FirstOrDefault();

                if (rom != null && Path.GetFileName(rom).ToLower().Contains("autorun"))
                {
                    var wineCmd = File.ReadAllLines(rom);
                    if (wineCmd.Length == 0) throw new Exception("autorun.cmd is empty");

                    var dir = wineCmd.FirstOrDefault(l => l.StartsWith("DIR="))?.Substring(4);
                    var wineCommand = wineCmd.FirstOrDefault(l => l.StartsWith("CMD="))?.Substring(4) ?? wineCmd.FirstOrDefault();
                    
                    if (string.IsNullOrEmpty(wineCommand)) throw new Exception("Invalid autorun.cmd command");

                    var args = wineCommand.SplitCommandLine();
                    if (args.Length > 0)
                    {
                        string exe = string.IsNullOrEmpty(dir) ? Path.Combine(path, args[0]) : Path.Combine(path, dir.Replace("/", "\\"), args[0]);
                        if (File.Exists(exe))
                        {
                            rom = exe;
                            if (!string.IsNullOrEmpty(dir))
                            {
                                string customDir = Path.Combine(path, dir);
                                path = Directory.Exists(customDir) ? customDir : Path.GetDirectoryName(rom);
                            }
                            else
                                path = Path.GetDirectoryName(rom);

                            if (args.Length > 1)
                                arguments = string.Join(" ", args.Skip(1).ToArray());
                        }
                        else
                            throw new Exception("Invalid autorun.cmd executable");
                    }
                }
            }

            SimpleLogger.Instance.Info($"[Debug] Controllo finale prima del lancio.");
            SimpleLogger.Instance.Info($"[Debug] -> Il percorso della ROM è: '{rom}'");
            SimpleLogger.Instance.Info($"[Debug] -> Il controllo File.Exists(rom) restituisce: {File.Exists(rom)}");
            SimpleLogger.Instance.Info($"[Debug] -> Il flag _steamRun è: {_steamRun}");

            if (!File.Exists(rom) && !_steamRun)
            {
                SimpleLogger.Instance.Error("[Debug] CONDIZIONE VERA. Il generatore sta per restituire NULL.");
                return null;
            }
            
            if (Path.GetExtension(rom).ToLower() == ".m3u")
            {
                rom = File.ReadAllText(rom);
                if (rom.StartsWith(".\\") || rom.StartsWith("./"))
                    rom = Path.Combine(path, rom.Substring(2));
                else if (rom.StartsWith("\\") || rom.StartsWith("/"))
                    rom = Path.Combine(path, rom.Substring(1));
            }

            UpdateMugenConfig(path, fullscreen, resolution); 
            UpdateIkemenConfig(path, system, rom, fullscreen, resolution, emulator); 

            var ret = new ProcessStartInfo()
            {
                FileName = rom,
                WorkingDirectory = path
            };

            if (arguments != null)
                ret.Arguments = arguments;

            string currentExtension = Path.GetExtension(rom).ToLower(); 
            if (currentExtension == ".bat" || currentExtension == ".cmd")
            {
                ret.WindowStyle = ProcessWindowStyle.Hidden;
                ret.UseShellExecute = true;
            }
            else if (string.IsNullOrEmpty(_exename) && _gameLauncher == null)
            {
                _exename = Path.GetFileNameWithoutExtension(rom);
                SimpleLogger.Instance.Info("[INFO] Executable name : " + _exename);
            }

            ValidateUncompressedGame();
            ConfigureExeLauncherGuns(system, rom);
            return ret;
        }
        
        public override int RunAndWait(ProcessStartInfo path)
        {
            if (_gameLauncher != null)
            {
                SimpleLogger.Instance.Info($"[ExeLauncherGenerator] Delega RunAndWait a {_gameLauncher.GetType().Name}.");
                return _gameLauncher.RunAndWait(path);
            }

            GetProcessFromFile(path.FileName);

            Process procToWatch = null;

            try { Process.Start(path); }
            catch (Exception ex) { SimpleLogger.Instance.Error($"[ERROR] Impossibile avviare: {path.FileName} -> {ex.Message}", ex); return 1; }

            if (!string.IsNullOrEmpty(_exename) && (_isGameExePath || _exeFile))
            {
                SimpleLogger.Instance.Info("[INFO] Rilevamento automatico del processo del gioco in corso...");
                
                var processBlacklist = new HashSet<string>(StringComparer.OrdinalIgnoreCase) {
                    "steam", "EpicGamesLauncher", "EADesktop", "Amazon Games UI", "GalaxyClient", "Origin",
                    "emulationstation", "EmulatorLauncher", "steamwebhelper",
                    "NVIDIA Overlay", "msedgewebview2", "ApplicationFrameHost", "SystemSettings", "explorer"
                };

                int waitTime = 30;
                var watch = Stopwatch.StartNew();
                while (watch.Elapsed.TotalSeconds < waitTime)
                {
                    Process gameProcess = Process.GetProcesses()
                        .Where(p => p.MainWindowHandle != IntPtr.Zero && p.Id != Process.GetCurrentProcess().Id && !processBlacklist.Contains(p.ProcessName) && (DateTime.Now - p.StartTime).TotalSeconds < waitTime)
                        .OrderByDescending(p => p.StartTime).FirstOrDefault();

                    if (gameProcess != null)
                    {
                        _exename = gameProcess.ProcessName;
                        SimpleLogger.Instance.Info($"[INFO] Rilevato processo del gioco: {_exename}");

                        if (_padToKeyMapping != null)
                        {
                            SimpleLogger.Instance.Info($"[PadToKey] Aggiornamento dinamico e forzatura della mappatura per il processo '{_exename}'.");
                            PadToKey.AddOrUpdateKeyMapping(_padToKeyMapping, _exename, InputKey.hotkey | InputKey.start, "(%{KILL})");
                            _padToKeyMapping.ForceApplyToProcess = _exename;
                        }
                        
                        SimpleLogger.Instance.Info($"[Focus] Tentativo di portare la finestra del gioco in primo piano...");
                        Thread.Sleep(1500);

                        var freshProcess = Process.GetProcessesByName(_exename).FirstOrDefault(p => p.Id == gameProcess.Id);
                        if (freshProcess != null && freshProcess.MainWindowHandle != IntPtr.Zero)
                        {
                            User32.SetForegroundWindow(freshProcess.MainWindowHandle); 
                            User32.ShowWindow(freshProcess.MainWindowHandle, SW.RESTORE); 
                            SimpleLogger.Instance.Info($"[Focus] Finestra del gioco '{_exename}' portata in primo piano.");
                        }
                        else
                            SimpleLogger.Instance.Warning($"[Focus] Impossibile trovare un handle valido per la finestra del gioco '{_exename}'.");

                        break; 
                    }
                    Thread.Sleep(500);
                }
            }
            
            try
            {
                if (!string.IsNullOrEmpty(_exename))
                {
                    SimpleLogger.Instance.Info($"[INFO] In attesa della chiusura del processo: {_exename}");
                    var processes = Process.GetProcessesByName(_exename);
                    if (processes.Length > 0)
                    {
                        procToWatch = processes.OrderBy(p => p.StartTime).First();
                        procToWatch.WaitForExit();
                    }
                    else
                         SimpleLogger.Instance.Warning($"[WARN] Il processo del gioco '{_exename}' si è chiuso prima che potesse essere monitorato.");
                }
                else
                {
                    SimpleLogger.Instance.Info("[INFO] Nessun processo specifico da monitorare, attesa generica.");
                    Thread.Sleep(10000); 
                }
            }
            finally
            {
                var esProcess = Process.GetProcessesByName("emulationstation").FirstOrDefault();
                if (esProcess != null && esProcess.MainWindowHandle != IntPtr.Zero)
                {
                    User32.ShowWindow(esProcess.MainWindowHandle, SW.RESTORE); 
                    User32.SetForegroundWindow(esProcess.MainWindowHandle); 
                }
            }

            return 0;
        }
        
        public override PadToKey SetupCustomPadToKeyMapping(PadToKey mapping)
        {
            _padToKeyMapping = mapping;

            if (_gameLauncher != null)
            {
                return _gameLauncher.SetupCustomPadToKeyMapping(mapping);
            }

            if (_isGameExePath || _exeFile)
                return PadToKey.AddOrUpdateKeyMapping(mapping, _exename, InputKey.hotkey | InputKey.start, "(%{KILL})");

            if (!string.IsNullOrEmpty(_exename) && (_systemName == "mugen" || _systemName == "ikemen"))
                return PadToKey.AddOrUpdateKeyMapping(mapping, _exename, InputKey.hotkey | InputKey.start, "(%{KILL})");

            return mapping; 
        }

       private void UpdateMugenConfig(string path, bool fullscreen, ScreenResolution resolution)
        {
            if (_systemName != "mugen")
                return;

            var cfg = Path.Combine(path, "data", "mugen.cfg");
            if (!File.Exists(cfg))
                return;

            if (resolution == null)
                resolution = ScreenResolution.CurrentResolution;

            using (var ini = IniFile.FromFile(cfg, IniOptions.UseSpaces | IniOptions.AllowDuplicateValues | IniOptions.KeepEmptyValues | IniOptions.KeepEmptyLines))
            {

                if (!string.IsNullOrEmpty(ini.GetValue("Config", "GameWidth")))
                {
                    ini.WriteValue("Config", "GameWidth", resolution.Width.ToString());
                    ini.WriteValue("Config", "GameHeight", resolution.Height.ToString()); 
                }

                if (SystemConfig["resolution"] == "480p")
                {
                    ini.WriteValue("Config", "GameWidth", "640");
                    ini.WriteValue("Config", "GameHeight", "480");
                }
                else if (SystemConfig["resolution"] == "720p")
                {
                    ini.WriteValue("Config", "GameWidth", "960");
                    ini.WriteValue("Config", "GameHeight", "720");
                }
                else if (SystemConfig["resolution"] == "960p")
                {
                    ini.WriteValue("Config", "GameWidth", "1280");
                    ini.WriteValue("Config", "GameHeight", "960");
                }
                else
                {
                    ini.WriteValue("Config", "GameWidth", resolution.Width.ToString());
                    ini.WriteValue("Config", "GameHeight", resolution.Height.ToString());
                }

                BindBoolIniFeatureOn(ini, "Video", "VRetrace", "VRetrace", "1", "0");
                ini.WriteValue("Video", "FullScreen", fullscreen ? "1" : "0"); 

            }
        }

        private void UpdateIkemenConfig(string path, string system, string rom, bool fullscreen, ScreenResolution resolution, string emulator)
        {
            if (_systemName != "ikemen")
                return;

            var json = DynamicJson.Load(Path.Combine(path, "save", "config.json"));

            ReshadeManager.Setup(ReshadeBezelType.opengl, ReshadePlatform.x64, system, rom, path, resolution, emulator);

            if (resolution == null)
                resolution = ScreenResolution.CurrentResolution;

            json["FirstRun"] = "false";           
            json["Fullscreen"] = fullscreen ? "true" : "false"; 

            if (SystemConfig["resolution"] == "240p")
            {
                json["GameWidth"] = "320";
                json["GameHeight"] = "240";
            }
            else if (SystemConfig["resolution"] == "480p")
            {
                json["GameWidth"] = "640";
                json["GameHeight"] = "480";
            }
            else if (SystemConfig["resolution"] == "720p")
            {
                json["GameWidth"] = "1280";
                json["GameHeight"] = "720";
            }
            else if (SystemConfig["resolution"] == "960p")
            {
                json["GameWidth"] = "1280";
                json["GameHeight"] = "960";
            }
            else if (SystemConfig["resolution"] == "1080p")
            {
                json["GameWidth"] = "1920";
                json["GameHeight"] = "1080";
            }
            else
            {
                json["GameWidth"] = resolution.Width.ToString();
                json["GameHeight"] = resolution.Height.ToString();
            }

            BindBoolFeatureOn(json, "VRetrace", "VRetrace", "1", "0");

            json.Save();
        }
        private bool GetProcessFromFile(string rom) 
        {
            string executableFile = Path.Combine(Path.GetDirectoryName(rom), Path.GetFileNameWithoutExtension(rom) + ".gameexe");
            if (!File.Exists(executableFile)) return false;
            
            var lines = File.ReadAllLines(executableFile);
            if (lines.Length > 0 && !string.IsNullOrEmpty(lines[0]))
            {
                _exename = lines[0].Trim();
                SimpleLogger.Instance.Info("[INFO] Executable name specified in .gameexe file: " + _exename);
                return true;
            }
            return false;
        }
        static string GetStoreAppVersion(string appName) 
        { 
            Process process = new Process();
            process.StartInfo.FileName = "powershell.exe";
            process.StartInfo.Arguments = $"-Command (Get-AppxPackage -Name {appName} | Select Installlocation)";
            process.StartInfo.RedirectStandardOutput = true;
            process.StartInfo.UseShellExecute = false;
            process.StartInfo.CreateNoWindow = true;
            process.Start();
            string output = process.StandardOutput.ReadToEnd();
            process.WaitForExit();
            return output;
        }
    }
}