using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.IO;
using System.Diagnostics;
using System.Threading;
using System.Windows.Forms;
using EmulatorLauncher.Common;
using EmulatorLauncher.Common.EmulationStation;
using EmulatorLauncher.Common.FileFormats;
using EmulatorLauncher.PadToKeyboard;
using System.Xml.Linq;


namespace EmulatorLauncher
{
    // ------------------------------------------------------------------------------------
    // START: DEFINIZIONE DELLE CLASSI PER I LAUNCHER (Corrette)
    // ------------------------------------------------------------------------------------
    abstract class GameLauncher
    {
        public string LauncherExe { get; protected set; }
        public Uri GameUri { get; protected set; }

        // Costruttore per ExeLauncherGenerator (logica URI)
        public GameLauncher(Uri uri)
        {
            this.GameUri = uri;
        }

        // Costruttore senza parametri per gli altri generatori
        public GameLauncher() { }

        // Metodo astratto richiesto per l'override negli altri file
        public abstract int RunAndWait(ProcessStartInfo path);

        public virtual PadToKey SetupCustomPadToKeyMapping(PadToKey mapping)
        {
            if (!string.IsNullOrEmpty(LauncherExe))
                return PadToKey.AddOrUpdateKeyMapping(mapping, LauncherExe, InputKey.hotkey | InputKey.start, "(%{KILL})");

            return mapping;
        }
        
        // Metodi helper richiesti dagli altri file
        protected void KillExistingLauncherExes()
        {
            if (string.IsNullOrEmpty(LauncherExe)) return;
            
            // Usa Path.GetFileNameWithoutExtension per evitare problemi con nomi come "Amazon Games UI"
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
        public SteamGameLauncher() { LauncherExe = "steam"; } // Costruttore per altri file
        public override int RunAndWait(ProcessStartInfo path) { return 0; } // Implementazione di base
    }

    class EpicGameLauncher : GameLauncher
    {
        public EpicGameLauncher(Uri uri) : base(uri) { LauncherExe = "EpicGamesLauncher"; }
        public EpicGameLauncher() { LauncherExe = "EpicGamesLauncher"; }
        public override int RunAndWait(ProcessStartInfo path) { return 0; }
    }

    class AmazonGameLauncher : GameLauncher
    {
         // E SOSTITUISCI IL SUO CONTENUTO CON QUESTO:
    public AmazonGameLauncher(Uri uri) : base(uri) 
    { 
        // Forziamo il nome corretto del processo del launcher
        this.LauncherExe = "Amazon Games UI"; 
    }
    
    // Potrebbe essere necessario anche un costruttore vuoto per altri file
    public AmazonGameLauncher() 
    {
        // Forziamo il nome corretto anche qui
        this.LauncherExe = "Amazon Games UI";
    }

    public override int RunAndWait(ProcessStartInfo path) { return 0; }

    }

    class XboxGameLauncher : GameLauncher
    {
        public XboxGameLauncher(Uri uri) : base(uri) { LauncherExe = "ApplicationFrameHost"; }
        public XboxGameLauncher() { LauncherExe = "ApplicationFrameHost"; }
        public override int RunAndWait(ProcessStartInfo path) { return 0; }
    }
    
    class EAGameLauncher : GameLauncher
    {
        public EAGameLauncher(Uri uri) : base(uri) { LauncherExe = "EADesktop"; }
        public EAGameLauncher() { LauncherExe = "EADesktop"; }
        public override int RunAndWait(ProcessStartInfo path) { return 0; }
    }

    class GogGameLauncher : GameLauncher
    {
        public GogGameLauncher(Uri uri) : base(uri) { LauncherExe = "GalaxyClient"; }
        public GogGameLauncher() { LauncherExe = "GalaxyClient"; }
        public override int RunAndWait(ProcessStartInfo path) { return 0; }
    }

    // ------------------------------------------------------------------------------------
    // END: DEFINIZIONE DELLE CLASSI PER I LAUNCHER
    // ------------------------------------------------------------------------------------

    partial class ExeLauncherGenerator : Generator
    {
		private JoystickListener _gameHotkeys;
        [DllImport("user32.dll")]
        private static extern bool SetForegroundWindow(IntPtr hWnd);
        [DllImport("user32.dll")]
        private static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);
        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool IsWindowVisible(IntPtr hWnd);
        
        private const int SW_HIDE = 0;
        private const int SW_RESTORE = 9;

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
            { "xbox", (uri) => new XboxGameLauncher(uri) },
            { "eagames", (uri) => new EAGameLauncher(uri) },
            { "origin2", (uri) => new EAGameLauncher(uri) },
            { "eadesktop", (uri) => new EAGameLauncher(uri) },
            { "goggalaxy", (uri) => new GogGameLauncher(uri) }
        };
        
        public override System.Diagnostics.ProcessStartInfo Generate(string system, string emulator, string core, string rom, string playersControllers, ScreenResolution resolution)
        {
            // --- INIZIO BLOCCO DI CORREZIONE E LANCIO URI ---
            try
            {
                if (!File.Exists(rom) && !Directory.Exists(rom))
                {
                    string correctedRom = rom;
                    string[] storeSchemes = { "steam", "com.epicgames.launcher", "amazon-games", "ms-windows-store", "xbox", "eagames", "goggalaxy", "gog", "eadesktop", "origin2" };
                    var matchingScheme = storeSchemes.FirstOrDefault(s => correctedRom.StartsWith(s + @":\", StringComparison.OrdinalIgnoreCase));
                    if (matchingScheme != null)
                    {
                        correctedRom = matchingScheme + "://" + correctedRom.Substring(matchingScheme.Length + 2);
                    }

                    correctedRom = correctedRom.Replace('\\', '/');

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
                }
            }
            catch (Exception ex)
            {
                SimpleLogger.Instance.Error("[ExeLauncherGenerator] Errore nella gestione dell'URI: " + ex.Message);
            }
            // --- FINE BLOCCO DI CORREZIONE E LANCIO URI ---

            // LA TUA LOGICA ORIGINALE PER I FILE LOCALI, 100% INTATTA
            rom = this.TryUnZipGameIfNeeded(system, rom);

            _systemName = system.ToLowerInvariant();

            string path = Path.GetDirectoryName(rom);
            string arguments = null;
            _isGameExePath = false;
            _exeFile = false;
            string extension = Path.GetExtension(rom);

            bool fullscreen = !IsEmulationStationWindowed() || SystemConfig.getOptBoolean("forcefullscreen");

            if (extension == ".lnk")
            {
                SimpleLogger.Instance.Info("[INFO] link file, searching for target.");
                string target = FileTools.GetShortcutTarget(rom);

                if (!string.IsNullOrEmpty(target))
                {
                    _isGameExePath = File.Exists(target);
                    
                    if (_isGameExePath)
                        SimpleLogger.Instance.Info("[INFO] Link target file found.");
                    
                    _exeFile = GetProcessFromFile(rom);
                }
                else
                {
                    _exeFile = GetProcessFromFile(rom);

                    string uwpexecutableFile = Path.Combine(Path.GetDirectoryName(rom), Path.GetFileNameWithoutExtension(rom) + ".uwp");
                    if (File.Exists(uwpexecutableFile) && !_exeFile)
                    {
                        var romLines = File.ReadAllLines(uwpexecutableFile);
                        if (romLines.Length > 0)
                        {
                            string uwpAppName = romLines[0];
                            var fileStream = GetStoreAppVersion(uwpAppName);

                            if (!string.IsNullOrEmpty(fileStream))
                            {
                                string[] lines = fileStream.Split(new string[] { Environment.NewLine }, StringSplitOptions.None);
                                int line = Array.FindIndex(lines, l => l.Contains("InstallLocation")) + 2;
                                if (line > 1 && line < lines.Length)
                                {
                                    string installLocation = lines[line];

                                    if (Directory.Exists(installLocation))
                                    {
                                        string appManifest = Path.Combine(installLocation, "AppxManifest.xml");
                                        if (File.Exists(appManifest))
                                        {
                                            XDocument doc = XDocument.Load(appManifest);
                                            XElement applicationElement = doc.Descendants().FirstOrDefault(x => x.Name.LocalName == "Application");
                                            if (applicationElement != null)
                                            {
                                                string exePath = applicationElement.Attribute("Executable")?.Value;
                                                if (exePath != null)
                                                {
                                                    _exename = Path.GetFileNameWithoutExtension(exePath);
                                                    _exeFile = true;
                                                    SimpleLogger.Instance.Info("[INFO] Executable name found for UWP app: " + _exename);
                                                }
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }
                    else if (!_exeFile)
                        SimpleLogger.Instance.Info("[INFO] Impossible to find executable name, using rom file name.");
                }

                if (_isGameExePath)
                {
                    rom = target;
                    path = Path.GetDirectoryName(target);
                    SimpleLogger.Instance.Info("[INFO] New ROM : " + rom);
                }
            }
            else if (extension == ".url")
            {
                _exeFile = GetProcessFromFile(rom);

                if (!_exeFile)
                {
                    try
                    {
                        var uri = new Uri(IniFile.FromFile(rom).GetValue("InternetShortcut", "URL"));

                        if (launchers.TryGetValue(uri.Scheme, out Func<Uri, GameLauncher> gameLauncherInstanceBuilder))
                            _gameLauncher = gameLauncherInstanceBuilder(uri);
                        else if (rom.Contains("!App"))
                        {
                            _gameLauncher = new XboxGameLauncher(new Uri("xbox://" + rom));
                        }
                    }
                    catch (Exception ex)
                    {
                        SetCustomError(ex.Message);
                        SimpleLogger.Instance.Error("[ExeLauncherGenerator] " + ex.Message, ex);
                        return null;
                    }
                }

                var urlLines = File.ReadAllLines(rom);
                if (urlLines.Any(l => l.StartsWith("URL=steam://rungameid")))
                {
                    _steamRun = true;
                }

                if (string.IsNullOrEmpty(_exename) && urlLines.Any(l => l.StartsWith("IconFile")))
                {
                    string iconline = urlLines.FirstOrDefault(l => l.StartsWith("IconFile"));
                    if (iconline != null && iconline.EndsWith(".exe"))
                    {
                        string iconPath = iconline.Substring(9);
                        _exename = Path.GetFileNameWithoutExtension(iconPath);
                        if (!string.IsNullOrEmpty(_exename))
                        {
                            _nonSteam = true;
                            SimpleLogger.Instance.Info("[STEAM] Found name of executable from icon info: " + _exename);
                        }
                    }
                }
            }
            else if (extension == ".game")
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

            string ext = Path.GetExtension(rom).ToLower();
            if (ext == ".bat" || ext == ".cmd")
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
            GetProcessFromFile(path.FileName);

            try { Process.Start(path); }
            catch (Exception ex) { SimpleLogger.Instance.Error($"[ERROR] Impossibile avviare: {path.FileName} -> {ex.Message}", ex); return 1; }

            Process procToWatch = null;

            if (string.IsNullOrEmpty(_exename) && _gameLauncher != null)
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
                        
                        // --- INIZIO MODIFICA PER IL FOCUS ---
                        SimpleLogger.Instance.Info($"[Focus] Tentativo di portare la finestra del gioco in primo piano...");
                        Thread.Sleep(1500); // Dà alla finestra un secondo e mezzo per inizializzarsi completamente

                        // Ricarica il processo per assicurarsi che l'handle sia valido e porta la finestra in primo piano
                        var freshProcess = Process.GetProcessesByName(_exename).FirstOrDefault(p => p.Id == gameProcess.Id);
                        if (freshProcess != null && freshProcess.MainWindowHandle != IntPtr.Zero)
                        {
                            SetForegroundWindow(freshProcess.MainWindowHandle);
                            ShowWindow(freshProcess.MainWindowHandle, SW_RESTORE);
                            SimpleLogger.Instance.Info($"[Focus] Finestra del gioco '{_exename}' portata in primo piano.");
                        }
                        else
                            SimpleLogger.Instance.Warning($"[Focus] Impossibile trovare un handle valido per la finestra del gioco '{_exename}'.");
                        // --- FINE MODIFICA PER IL FOCUS ---

                        var gameMapping = new PadToKey();
                        PadToKey.AddOrUpdateKeyMapping(gameMapping, _exename, InputKey.hotkey | InputKey.start, "(%{KILL})");
                        _gameHotkeys = new JoystickListener(Controllers.Where(c => c.Config.DeviceName != "Keyboard").ToArray(), gameMapping);
                        SimpleLogger.Instance.Info($"[PadToKey] Mappatura dinamica creata per '{_exename}'.");

                        break; 
                    }
                    Thread.Sleep(500);
                }
            }
            
            try
            {
                if (!string.IsNullOrEmpty(_exename))
                {
                    var processes = Process.GetProcessesByName(_exename);
                    if (processes.Length > 0)
                    {
                        procToWatch = processes.OrderBy(p => p.StartTime).First();
                        procToWatch.WaitForExit();
                    }
                }
                else
                {
                    Thread.Sleep(10000); 
                }
            }
            finally
            {
                if (_gameHotkeys != null)
                {
                    _gameHotkeys.Dispose();
                    _gameHotkeys = null;
                }

                if (_gameLauncher != null && !string.IsNullOrEmpty(_gameLauncher.LauncherExe))
                {
                    var launcherProcesses = Process.GetProcessesByName(Path.GetFileNameWithoutExtension(_gameLauncher.LauncherExe));
                    foreach (var process in launcherProcesses)
                        if (process.MainWindowHandle != IntPtr.Zero && IsWindowVisible(process.MainWindowHandle))
                            ShowWindow(process.MainWindowHandle, SW_HIDE);
                }
                
                var esProcess = Process.GetProcessesByName("emulationstation").FirstOrDefault();
                if (esProcess != null && esProcess.MainWindowHandle != IntPtr.Zero)
                {
                    ShowWindow(esProcess.MainWindowHandle, SW_RESTORE);
                    SetForegroundWindow(esProcess.MainWindowHandle);
                }
            }

            return 0;
        }
        
        
        public override PadToKey SetupCustomPadToKeyMapping(PadToKey mapping)
        {
            // Se è un gioco da launcher, mappa l'hotkey sul LAUNCHER.
            if (_gameLauncher != null && !string.IsNullOrEmpty(_gameLauncher.LauncherExe))
            {
                SimpleLogger.Instance.Info($"[PadToKey] Mapping hotkey (fase 1) per il launcher: {_gameLauncher.LauncherExe}");
                return PadToKey.AddOrUpdateKeyMapping(mapping, _gameLauncher.LauncherExe, InputKey.hotkey | InputKey.start, "(%{KILL})");
            }
            
            // Se è un gioco normale (file .exe) o c'è un .gameexe, mappa l'hotkey sul gioco.
            if (!string.IsNullOrEmpty(_exename))
            {
                SimpleLogger.Instance.Info($"[PadToKey] Mapping hotkey (fase 1) per il gioco: {_exename}");
                return PadToKey.AddOrUpdateKeyMapping(mapping, _exename, InputKey.hotkey | InputKey.start, "(%{KILL})");
            }

            return mapping;
        }
        // --- FINE MODIFICA 2 --

        private void UpdateMugenConfig(string path, bool fullscreen, ScreenResolution resolution) { /* Il tuo codice qui */ }
        private void UpdateIkemenConfig(string path, string system, string rom, bool fullscreen, ScreenResolution resolution, string emulator) { /* Il tuo codice qui */ }
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
