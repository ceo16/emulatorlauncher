using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;
using System.IO;
using System.Diagnostics;
using System.Runtime.InteropServices;
using Microsoft.Win32;
using System.Security.Principal;
using EmulatorLauncher.Common;
using EmulatorLauncher.Common.FileFormats;
using EmulatorLauncher.Common.EmulationStation;
using EmulatorLauncher.Common.Joysticks;
using EmulatorLauncher.Common.Compression;
using EmulatorLauncher.PadToKeyboard;
using EmulatorLauncher.Libretro;
using EmulatorLauncher.Common.Compression.Wrappers;


namespace EmulatorLauncher
{
    static class Program
    {
        // Link tra emulatore/sistema e il generatore da usare
        static Dictionary<string, Func<Generator>> generators = new Dictionary<string, Func<Generator>>(StringComparer.InvariantCultureIgnoreCase)
        {
            // --- QUESTA LISTA È LA TUA ORIGINALE ---
            { "3dsen", () => new Nes3dGenerator() },
            { "altirra", () => new AltirraGenerator() },
            { "amigaforever", () => new AmigaForeverGenerator() },
            { "angle", () => new LibRetroGenerator() },
            { "apple2", () => new AppleWinGenerator() },
            { "apple2gs", () => new GsPlusGenerator() },
            { "applewin", () => new AppleWinGenerator() },
            { "arcadeflashweb", () => new ArcadeFlashWebGenerator() },
            { "ares", () => new AresGenerator() },
            { "azahar", () => new AzaharGenerator() },
            { "bam", () => new FpinballGenerator() },
            { "bigpemu", () => new BigPEmuGenerator() },
            { "bizhawk", () => new BizhawkGenerator() },
            { "capriceforever", () => new CapriceForeverGenerator() },
            { "cdogs", () => new CDogsGenerator() },
            { "cemu", () => new CemuGenerator() },
            { "cgenius", () => new CGeniusGenerator() },
            { "chihiro", () => new CxbxGenerator() },
            { "citra", () => new CitraGenerator() },
            { "citra-canary", () => new CitraGenerator() },
            { "citron", () => new CitronGenerator() },
            { "corsixth", () => new CorsixTHGenerator() },
            { "cxbx", () => new CxbxGenerator() },
            { "daphne", () => new DaphneGenerator() },
            { "demul", () => new DemulGenerator() },
            { "demul-old", () => new DemulGenerator() },
            { "devilutionx", () => new DevilutionXGenerator() },
            { "dhewm3", () => new Dhewm3Generator() },
            { "dolphin", () => new DolphinGenerator() },
            { "dosbox", () => new DosBoxGenerator() },
            { "duckstation", () => new DuckstationGenerator() },
            { "easyrpg", () => new EasyRpgGenerator() },
            { "eden", () => new EdenGenerator() },
            { "eduke32", () => new EDukeGenerator() },
            { "eka2l1", () => new Eka2l1Generator() },
            { "fbneo", () => new FbneoGenerator() },
            { "flycast", () => new FlycastGenerator() },
            { "fpinball", () => new FpinballGenerator() },
            { "gemrb", () => new GemRBGenerator() },
            { "gopher64", () => new Gopher64Generator() },
            { "gsplus", () => new GsPlusGenerator() },
            { "gzdoom", () => new GZDoomGenerator() },
            { "hatari", () => new HatariGenerator() },
            { "hbmame", () => new Mame64Generator() },
            { "hypseus", () => new HypseusGenerator() },
            { "ikemen", () => new ExeLauncherGenerator() },
            { "jgenesis", () => new JgenesisGenerator() },
            { "jynx", () => new JynxGenerator() },
            { "kega-fusion", () => new KegaFusionGenerator() },
            { "kronos", () => new KronosGenerator() },
            { "libretro", () => new LibRetroGenerator() },
            { "lime3ds", () => new Lime3dsGenerator() },
            { "love", () => new LoveGenerator() },
            { "m2emulator", () => new Model2Generator() },
            { "magicengine", () => new MagicEngineGenerator() },
            { "mame64", () => new Mame64Generator() },
            { "mandarine", () => new MandarineGenerator() },
            { "mednafen", () => new MednafenGenerator() },
            { "melonds", () => new MelonDSGenerator() },
            { "mesen", () => new MesenGenerator() },
            { "mgba", () => new MGBAGenerator() },
            { "model2", () => new Model2Generator() },
            { "model3", () => new Model3Generator() },
            { "mugen", () => new ExeLauncherGenerator() },
            { "mupen64", () => new Mupen64Generator() },
            { "nosgba", () => new NosGbaGenerator() },
            { "no$gba", () => new NosGbaGenerator() },
            { "n-gage", () => new Eka2l1Generator() },
            { "openbor", () => new OpenBorGenerator() },
            { "opengoal", () => new OpenGoalGenerator() },
            { "openjazz", () => new OpenJazzGenerator() },
            { "openmsx", () => new OpenMSXGenerator() },
            { "oricutron", () => new OricutronGenerator() },
            { "pcsx2", () => new Pcsx2Generator() },
            { "pcsx2qt", () => new Pcsx2Generator() },
            { "pcsx2-16", () => new Pcsx2Generator() },
            { "pdark", () => new PDarkGenerator() },
            { "phoenix", () => new PhoenixGenerator() },
            { "pico8", () => new Pico8Generator() },
            { "pinballfx", () => new PinballFXGenerator() },
            { "play", () => new PlayGenerator() },
            { "ppsspp", () => new PpssppGenerator() },
            { "project64", () => new Project64Generator() },
            { "ps2", () => new Pcsx2Generator() },
            { "ps3", () => new Rpcs3Generator() },
            { "psvita", () => new Vita3kGenerator() },
            { "psxmame", () => new PSXMameGenerator() },
            { "raine", () => new RaineGenerator() },
            { "raze", () => new RazeGenerator() },
            { "redream", () => new RedreamGenerator() },
            { "lumaca", () => new LumacaLauncherGenerator() }, // Mantenuta la tua versione
            { "rpcs3", () => new Rpcs3Generator() },
            { "ruffle", () => new RuffleGenerator() },
            { "ryujinx", () => new RyujinxGenerator() },
            { "scummvm", () => new ScummVmGenerator() },
            { "shadps4", () => new ShadPS4Generator() },
            { "simcoupe", () => new SimCoupeGenerator() },
            { "simple64", () => new Simple64Generator() },
            { "singe2", () => new Singe2Generator() },
            { "snes9x", () => new Snes9xGenerator() },
            { "soh", () => new SohGenerator() },
            { "solarus", () => new SolarusGenerator() },
            { "solarus2", () => new SolarusGenerator() },
            { "sonic3air", () => new PortsLauncherGenerator() },
            { "sonicmania", () => new PortsLauncherGenerator() },
            { "sonicretro", () => new PortsLauncherGenerator() },
            { "sonicretrocd", () => new PortsLauncherGenerator() },
            { "ssf", () => new SSFGenerator() },
            { "starship", () => new StarshipGenerator() },
            { "stella", () => new StellaGenerator() },
            { "sudachi", () => new SudachiGenerator() },
            { "supermodel", () => new Model3Generator() },
            { "suyu", () => new SuyuGenerator() },
            { "teknoparrot", () => new TeknoParrotGenerator() },
            { "theforceengine", () => new ForceEngineGenerator() },
            { "triforce", () => new DolphinGenerator() },
            { "tsugaru", () => new TsugaruGenerator() },
            { "vita3k", () => new Vita3kGenerator() },
            { "vpinball", () => new VPinballGenerator() },
            { "wiiu", () => new CemuGenerator() },
            { "winarcadia", () => new WinArcadiaGenerator() },
            { "windows", () => new ExeLauncherGenerator() },
            { "winuae", () => new UaeGenerator() },
            { "xemu", () => new XEmuGenerator() },
            { "xenia", () => new XeniaGenerator() },
            { "xenia-canary", () => new XeniaGenerator() },
            { "xenia-manager", () => new XeniaGenerator() },
            { "xm6pro", () => new Xm6proGenerator() },
            { "xroar", () => new XroarGenerator() },
            { "yabasanshiro", () => new YabasanshiroGenerator() },
            { "ymir", () => new YmirGenerator() },
            { "yuzu", () => new YuzuGenerator() },
            { "yuzu-early-access", () => new YuzuGenerator() },
            { "zaccariapinball", () => new ZaccariaPinballGenerator() },
            { "zesarux", () => new ZEsarUXGenerator() },
            { "zinc", () => new ZincGenerator() },
            
            // Assegniamo gli store a ExeLauncherGenerator
            { "epicgamestore", () => new ExeLauncherGenerator() },
            { "amazon", () => new ExeLauncherGenerator() },
            { "steam", () => new ExeLauncherGenerator() },
            { "xboxstore", () => new ExeLauncherGenerator() },
            { "eagames", () => new ExeLauncherGenerator() },
            { "eagamesstore", () => new ExeLauncherGenerator() },
            { "gog", () => new ExeLauncherGenerator() },
        };

        public static ConfigFile AppConfig { get; private set; }
        public static string LocalPath { get; private set; }
        public static ConfigFile SystemConfig { get; private set; }
        public static List<Controller> Controllers { get; private set; }
        public static EsFeatures Features { get; private set; }
        public static Game CurrentGame { get; private set; }

        private static EsSystems _esSystems;

        public static EsSystems EsSystems
        {
            get
            {
                if (_esSystems == null)
                {
                    _esSystems = EsSystems.Load(Path.Combine(Program.LocalPath, ".emulationstation", "es_systems.cfg"));

                    if (_esSystems != null)
                    {
                        // Import emulator overrides from additional es_systems_*.cfg files
                        foreach (var file in Directory.GetFiles(Path.Combine(Program.LocalPath, ".emulationstation"), "es_systems_*.cfg"))
                        {
                            try
                            {
                                var esSystemsOverride = EsSystems.Load(file);
                                if (esSystemsOverride != null && esSystemsOverride.Systems != null)
                                {
                                    foreach (var ss in esSystemsOverride.Systems)
                                    {
                                        if (ss.Emulators == null || !ss.Emulators.Any())
                                            continue;

                                        var orgSys = _esSystems.Systems.FirstOrDefault(e => e.Name == ss.Name);
                                        if (orgSys != null)
                                            orgSys.Emulators = ss.Emulators;
                                    }
                                }
                            }
                            catch { }
                        }
                    }
                }

                return _esSystems;
            }
        }

        /// <summary>
        /// Import es_savestates.cfg
        /// Used to monitor savestates
        /// </summary>
        private static EsSaveStates _esSaveStates;

        public static EsSaveStates EsSaveStates
        {
            get
            {
                if (_esSaveStates == null)
                    _esSaveStates = EsSaveStates.Load(Path.Combine(Program.LocalPath, ".emulationstation", "es_savestates.cfg"));

                return _esSaveStates;
            }
        }

        public static bool HasEsSaveStates
        {
            get
            {
                return File.Exists(Path.Combine(Program.LocalPath, ".emulationstation", "es_savestates.cfg"));
            }
        }

        /// <summary>
        /// Import gamesdb.xml
        /// Used to get information on game (gun, wheel, ...)
        /// </summary>
        private static GamesDB _gunGames;

        public static GamesDB GunGames
        {
            get
            {
                if (_gunGames == null)
                {
                    string gamesDb = Path.Combine(Program.AppConfig.GetFullPath("resources"), "gamesdb.xml");
                    if (File.Exists(gamesDb))
                        _gunGames = GamesDB.Load(gamesDb);
                    else
                    {
                        string gungamesDb = Path.Combine(Program.AppConfig.GetFullPath("resources"), "gungames.xml");
                        _gunGames = GamesDB.Load(gungamesDb);
                    }
                }

                return _gunGames;
            }
        }

        public static bool EnableHotKeyStart
        {
            get
            {
                return Process.GetProcessesByName("JoyToKey").Length == 0;
            }
        }



        [DllImport("user32.dll")]
        public static extern bool SetProcessDPIAware();

        /// <summary>
        /// Method to show a splash video before a game starts
        /// </summary>
        static void ShowSplashVideo()
        {
            var loadingScreens = AppConfig.GetFullPath("loadingscreens");
            if (string.IsNullOrEmpty(loadingScreens))
                return;

            var system = SystemConfig["system"];
            if (string.IsNullOrEmpty(system))
                return;

            var rom = Path.GetFileNameWithoutExtension(SystemConfig["rom"]??"");

            var paths = new string[] 
            {
                "!screens!\\!system!\\!romname!.mp4",
                "!screens!\\!system!\\!system!.mp4",
                "!screens!\\!system!.mp4",
                "!screens!\\default.mp4"
            };

            var videoPath = paths
                .Select(path => path.Replace("!screens!", loadingScreens).Replace("!system!", system).Replace("!romname!", rom))
                .FirstOrDefault(path => File.Exists(path));

            if (string.IsNullOrEmpty(videoPath))
                return;

           SplashVideo.Start(videoPath, 5000);           
        }

        [STAThread]
        static void Main(string[] args)
        {
            // Forziamo la cartella di lavoro corretta
            try
            {
                Directory.SetCurrentDirectory(Path.GetDirectoryName(typeof(Program).Assembly.Location));
            }
            catch (Exception ex)
            {
                SimpleLogger.Instance.Error("[CRITICAL] Failed to set current directory: " + ex.Message);
            }

            RegisterShellExtensions();

            if (args.Length == 0) return;
            
            if (args.Length == 2 && args[0] == "-queryxinputinfo")
            {
                var all = XInputDevice.GetDevices(true);
                File.WriteAllText(args[1], string.Join("\r\n", all.Where(d => d.Connected).Select(d => "<xinput index=\"" + d.DeviceIndex + "\" path=\"" + d.Path + "\"/>").ToArray()));
                return;
            }

            AppDomain.CurrentDomain.UnhandledException += OnCurrentDomainUnhandledException;

            SimpleLogger.Instance.Info("--------------------------------------------------------------");
            SimpleLogger.Instance.Info("[Startup] " + Environment.CommandLine);
			
			 // --- INIZIO DEL NUOVO BLOCCO DI CODICE INSERITO (log EmulatorLauncher version) ---
            // Log local version (EmulatorLauncher version)
            try
            {
                // Riga 352 originale: Installer localInstaller = Installer.GetInstaller(null, true);
                // Correzione: Chiama GetInstaller senza argomenti
                Installer localInstaller = Installer.GetInstaller(); 
                if (localInstaller != null)
                {
                    // Riga 355 originale: string localVersion = localInstaller.GetInstalledVersion(true);
                    // Correzione: Chiama GetInstalledVersion senza argomenti
                    string localVersion = localInstaller.GetInstalledVersion();

                    if (localVersion != null)
                        SimpleLogger.Instance.Info("[Startup] EmulatorLauncher version : " + localVersion);
                }
            }
            catch (Exception ex) // Cattura l'eccezione per loggarla meglio
            { 
                SimpleLogger.Instance.Error("[Startup] Error while getting local version: " + ex.Message); 
            }
            // --- FINE DEL NUOVO BLOCCO DI CODICE INSERITO ---

            try { SetProcessDPIAware(); } catch { }

            SimpleLogger.Instance.Info("[Startup] Loading configuration.");
            LocalPath = Path.GetDirectoryName(typeof(Program).Assembly.Location);
            AppConfig = ConfigFile.FromFile(Path.Combine(LocalPath, "emulatorLauncher.cfg"));
            AppConfig.ImportOverrides(ConfigFile.FromArguments(args));

            SimpleLogger.Instance.Info("[Startup] Loading ES settings.");
            SystemConfig = ConfigFile.LoadEmulationStationSettings(Path.Combine(Program.AppConfig.GetFullPath("home"), "es_settings.cfg"));
            
            // Carica tutti gli override in un ordine logico, una sola volta.
            SystemConfig.ImportOverrides(ConfigFile.FromArguments(args));
            SystemConfig.ImportOverrides(SystemConfig.LoadAll("global"));
            SystemConfig.ImportOverrides(SystemConfig.LoadAll(SystemConfig["system"]));
            
            // --- INIZIO BLOCCO DI CORREZIONE PERCORSO ---
            // Ricostruisci il percorso della ROM PRIMA di usarlo per caricare altre configurazioni.
            string romPath = SystemConfig["rom"];
            int romIndex = Array.FindIndex(args, a => a == "-rom");
            if (romIndex != -1 && romIndex < args.Length - 1)
            {
                var pathParts = args.Skip(romIndex + 1).TakeWhile(a => !a.StartsWith("-"));
                string fullRomPath = string.Join(" ", pathParts).Trim('\"');

                if (romPath != fullRomPath)
                {
                    romPath = fullRomPath;
                    SystemConfig["rom"] = romPath; // Aggiorna la configurazione con il percorso corretto
                    SimpleLogger.Instance.Info($"[Parser] Reconstructed ROM path: {romPath}");
                }
            }
            // --- FINE BLOCCO DI CORREZIONE PERCORSO ---

            // Ora che 'romPath' è corretto, carica la configurazione specifica del gioco.
            if (!string.IsNullOrEmpty(romPath))
                SystemConfig.ImportOverrides(SystemConfig.LoadAll(SystemConfig["system"] + "[\"" + Path.GetFileName(romPath) + "\"]"));
            
            // Riapplica gli argomenti della riga di comando per dare loro la massima priorità.
            SystemConfig.ImportOverrides(ConfigFile.FromArguments(args));
            // E assicurati che la nostra correzione del percorso non sia stata sovrascritta per errore.
            if (SystemConfig["rom"] != romPath)
                SystemConfig["rom"] = romPath;

            // Controllo finale sull'esistenza della ROM (ignorando gli URI degli store)
            bool isIdentifierNotFile = romPath.Contains("://") || romPath.Contains(":\\") || SystemConfig["system"] == "xboxstore";
            if (!isIdentifierNotFile && !File.Exists(romPath) && !Directory.Exists(romPath))
            {
                SimpleLogger.Instance.Error("[Error] rom does not exist: " + romPath);
                Environment.ExitCode = (int)ExitCodes.BadCommandLine;
                return;
            }
            
            if (string.IsNullOrEmpty(SystemConfig["emulator"])) SystemConfig["emulator"] = SystemDefaults.GetDefaultEmulator(SystemConfig["system"]);
            if (string.IsNullOrEmpty(SystemConfig["core"])) SystemConfig["core"] = SystemDefaults.GetDefaultCore(SystemConfig["system"]);
            if (string.IsNullOrEmpty(SystemConfig["emulator"])) SystemConfig["emulator"] = SystemConfig["system"];

            // Caricamento CurrentGame
            if (SystemConfig.isOptSet("gameinfo") && File.Exists(SystemConfig.GetFullPath("gameinfo")))
            {
                var gamelist = GameList.Load(SystemConfig.GetFullPath("gameinfo"));
                if (gamelist != null) CurrentGame = gamelist.Games.FirstOrDefault();
            }
            if (CurrentGame == null)
            {
                if (!isIdentifierNotFile && File.Exists(romPath))
                {
                    var gamelistPath = Path.Combine(Path.GetDirectoryName(romPath), "gamelist.xml");
                    if (File.Exists(gamelistPath))
                    {
                        var gamelist = GameList.Load(gamelistPath);
                        if (gamelist?.Games != null) CurrentGame = gamelist.Games.FirstOrDefault(g => g.GetRomFile() == romPath);
                    }
                }
                if (CurrentGame == null) CurrentGame = new Game() { Path = romPath, Name = Path.GetFileNameWithoutExtension(romPath), Tag = "missing" };
            }
			
			// Log Lumaca version && emulatorlauncher version
            string rbVersionPath = Path.Combine(Program.AppConfig.GetFullPath("lumaca"), "system", "version.info");
            string emulatorlauncherExePath = Path.Combine(Program.AppConfig.GetFullPath("lumaca"), "emulationstation", "emulatorlauncher.exe");

            if (File.Exists(rbVersionPath))
            {
                string rbVersion = File.ReadAllText(rbVersionPath).Trim();
                SimpleLogger.Instance.Info("[Startup] Lumaca version : " + rbVersion);
            }
            else
                SimpleLogger.Instance.Info("[Startup] Lumaca version : not found");

            if (File.Exists(emulatorlauncherExePath))
            {
                DateTime lastModifiedDate = File.GetLastWriteTime(emulatorlauncherExePath);
                if (lastModifiedDate != null)
                    SimpleLogger.Instance.Info("[Startup] EmulatorLauncher.exe version : " + lastModifiedDate.ToString());
            }
			
			string esUpdateCmd = Path.Combine(Program.LocalPath, "es-update.cmd");
            string esCheckVersionCmd = Path.Combine(Program.LocalPath, "es-checkversion.cmd");

            if (File.Exists(esUpdateCmd))
            {
                SimpleLogger.Instance.Info("[Startup] Deleting " + esUpdateCmd);
                FileTools.TryDeleteFile(esUpdateCmd);
            }
            if (File.Exists(esCheckVersionCmd))
            {
                SimpleLogger.Instance.Info("[Startup] Deleting " + esCheckVersionCmd);
                FileTools.TryDeleteFile(esCheckVersionCmd);
            }

            // FIX 3: Logica di selezione del generatore chiara e separata
            Generator generator = null;
            string system = SystemConfig["system"];
            string emulator = SystemConfig["emulator"];
            var storeSystems = new List<string> { "amazon", "steam", "epicgamestore", "xboxstore", "eagames", "eagamesstore", "gog" };

            if (storeSystems.Contains(system, StringComparer.InvariantCultureIgnoreCase))
            {
                SimpleLogger.Instance.Info($"[Generator] Store system '{system}' detected. Using ExeLauncherGenerator.");
                generator = new ExeLauncherGenerator();
            }
            else
            {
                SimpleLogger.Instance.Info($"[Generator] Standard system '{system}'. Looking for emulator '{emulator}'.");
                if (generators.TryGetValue(emulator, out Func<Generator> genFunc))
                    generator = genFunc();
                else if (!string.IsNullOrEmpty(emulator) && emulator.StartsWith("lr-"))
                    generator = new LibRetroGenerator();
                else if (generators.TryGetValue(system, out genFunc))
                    generator = genFunc();
            }

            if (generator == null)
            {
                SimpleLogger.Instance.Error($"[Generator] Can't find generator for system '{system}' and emulator '{emulator}'");
                Environment.ExitCode = (int)ExitCodes.UnknownEmulator;
                return;
            }
            
            LoadControllerConfiguration(args);
            ShowSplashVideo();
            
            // FIX 4: Updater disabilitato di default
            bool updatesEnabled = SystemConfig.getOptBoolean("updates.enabled");
            if (updatesEnabled)
            {
                 Installer installer = Installer.GetInstaller();
                 if (installer != null && (!installer.IsInstalled() || installer.HasUpdateAvailable()) && installer.CanInstall())
                 {
                     using (InstallerFrm frm = new InstallerFrm(installer))
                         if (frm.ShowDialog() != DialogResult.OK) return;
                 }
            }
            else
            {
                SimpleLogger.Instance.Info("[Startup] Skipping update check (updates.enabled=false or not set).");
            }
            
            // Logica finale di esecuzione del generatore...
            if (generator != null)
            {
                SimpleLogger.Instance.Info("[Generator] Using " + generator.GetType().Name);
                
                try
                {
                    Features = EsFeatures.Load(Path.Combine(Program.AppConfig.GetFullPath("home"), "es_features.cfg"));
                }
                catch (Exception ex)
                {
                    WriteCustomErrorFile("[Error] es_features.cfg is invalid :\r\n" + ex.Message);
                    Environment.ExitCode = (int)ExitCodes.CustomError;
                    return;
                }

                Features.SetFeaturesContext(SystemConfig["system"], SystemConfig["emulator"], SystemConfig["core"]);

                using (var screenResolution = ScreenResolution.Parse(SystemConfig["videomode"]))
                {
                    ProcessStartInfo path = null;
                    try
                    {
                        path = generator.Generate(SystemConfig["system"], SystemConfig["emulator"], SystemConfig["core"], SystemConfig["rom"], null, screenResolution);
                    }
                    catch (Exception ex)
                    {
                        generator.Cleanup();
                        Program.WriteCustomErrorFile(ex.Message);
                        Environment.ExitCode = (int)ExitCodes.CustomError;
                        SimpleLogger.Instance.Error("[Generator] Exception : " + ex.Message, ex);
                        return;
                    }

                    if (path != null)
                    {
                        path.UseShellExecute = true;
                        if (screenResolution != null && generator.DependsOnDesktopResolution)
                            screenResolution.Apply();
                        
                        PadToKey mapping = null;
                        if (generator.UseEsPadToKey)
                            mapping = PadToKey.Load(Path.Combine(Program.AppConfig.GetFullPath("home"), "es_padtokey.cfg"));

                        mapping = LoadGamePadToKeyMapping(path, mapping);
                        mapping = generator.SetupCustomPadToKeyMapping(mapping);

                        if (path.Arguments != null)
                            SimpleLogger.Instance.Info("[Running] " + path.FileName + " " + path.Arguments);
                        else
                            SimpleLogger.Instance.Info("[Running]  " + path.FileName);

                        using (new HighPerformancePowerScheme())
                        using (var joy = new JoystickListener(Controllers.Where(c => c.Config.DeviceName != "Keyboard").ToArray(), mapping))
                        {
                            int exitCode = generator.RunAndWait(path);
                            if (exitCode != 0 && !joy.ProcessKilled)
                                Environment.ExitCode = (int)ExitCodes.EmulatorExitedUnexpectedly;
                        }
                        generator.RestoreFiles();
                    }
                    else
                    {
                        SimpleLogger.Instance.Error("[Generator] Failed. path is null");
                        Environment.ExitCode = (int)generator.ExitCode;
                    }
                }
                generator.Cleanup();
            }
        }

        private static void OnCurrentDomainUnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            var ex = e.ExceptionObject as System.Exception;
            if (ex == null)
                SimpleLogger.Instance.Error("[CurrentDomain] Unhandled exception");
            else
            {
                SimpleLogger.Instance.Error("[CurrentDomain] Unhandled exception : " + ex.Message, ex);

                if (e.IsTerminating)
                {
                    Program.WriteCustomErrorFile(ex.Message);
                    Environment.Exit((int)ExitCodes.CustomError);
                }
            }
        }

        private static PadToKey LoadGamePadToKeyMapping(ProcessStartInfo path, PadToKey mapping)
        {
            string filePath = SystemConfig["rom"] + (Directory.Exists(SystemConfig["rom"]) ? "\\padto.keys" : ".keys");

            EvMapyKeysFile gameMapping = EvMapyKeysFile.TryLoad(filePath);
            if (gameMapping == null && SystemConfig["system"] != null)
            {
                var core = SystemConfig["core"];
                var system = SystemConfig["system"];

                string systemMapping = "";

                if (!string.IsNullOrEmpty(core))
                {
                    systemMapping = Path.Combine(Program.LocalPath, ".emulationstation", "padtokey", system + "." + core + ".keys");

                    if (!File.Exists(systemMapping))
                        systemMapping = Path.Combine(Program.AppConfig.GetFullPath("padtokey"), system + "." + core + ".keys");
                }

                if (!File.Exists(systemMapping))
                    systemMapping = Path.Combine(Program.LocalPath, ".emulationstation", "padtokey", system + ".keys");

                if (!File.Exists(systemMapping))
                    systemMapping = Path.Combine(Program.AppConfig.GetFullPath("padtokey"), system + ".keys");

                if (File.Exists(systemMapping))
                    gameMapping = EvMapyKeysFile.TryLoad(systemMapping);
            }

            if (gameMapping == null || gameMapping.All(c => c == null))
                return mapping;

            PadToKeyApp app = new PadToKeyApp();
            app.Name = Path.GetFileNameWithoutExtension(path.FileName).ToLower();

            int playerIndex = 0;

            foreach (var player in gameMapping)
            {
                if (player == null)
                {
                    playerIndex++;
                    continue;
                }

                var controller = Program.Controllers.FirstOrDefault(c => c.PlayerIndex == playerIndex + 1);

                foreach (var action in player)
                {
                    if (action.type == "mouse")
                    {
                        if (action.Triggers == null || action.Triggers.Length == 0)
                            continue;

                        if (action.Triggers.FirstOrDefault() == "joystick1")
                        {
                            PadToKeyInput mouseInput = new PadToKeyInput();
                            mouseInput.Name = InputKey.joystick1left;
                            mouseInput.Type = PadToKeyType.Mouse;
                            mouseInput.Code = "X";
                            app.Input.Add(mouseInput);

                            mouseInput = new PadToKeyInput();
                            mouseInput.Name = InputKey.joystick1up;
                            mouseInput.Type = PadToKeyType.Mouse;
                            mouseInput.Code = "Y";
                            app.Input.Add(mouseInput);
                        }
                        else if (action.Triggers.FirstOrDefault() == "joystick2")
                        {
                            PadToKeyInput mouseInput = new PadToKeyInput();
                            mouseInput.Name = InputKey.joystick2left;
                            mouseInput.Type = PadToKeyType.Mouse;
                            mouseInput.Code = "X";
                            app.Input.Add(mouseInput);

                            mouseInput = new PadToKeyInput();
                            mouseInput.Name = InputKey.joystick2up;
                            mouseInput.Type = PadToKeyType.Mouse;
                            mouseInput.Code = "Y";
                            app.Input.Add(mouseInput);
                        }
                        
                        continue;
                    }

                    if (action.type != "key")
                        continue;

                    InputKey k;
                    if (!Enum.TryParse<InputKey>(string.Join(", ", action.Triggers.ToArray()).ToLower(), out k))
                        continue;

                    PadToKeyInput input = new PadToKeyInput
                    {
                        Name = k,
                        ControllerIndex = controller == null ? playerIndex : controller.DeviceIndex
                    };

                    bool custom = false;

                    foreach (var target in action.Targets)
                    {
                        if (target == "(%{KILL})" || target == "%{KILL}")
                        {
                            custom = true;
                            input.Key = "(%{KILL})";
                            continue;
                        }

                        if (target == "(%{CLOSE})" || target == "%{CLOSE}")
                        {
                            custom = true;
                            input.Key = "(%{CLOSE})";
                            continue;
                        }

                        if (target == "(%{F4})" || target == "%{F4}")
                        {
                            custom = true;
                            input.Key = "(%{F4})";
                            continue;
                        }

                        LinuxScanCode sc;
                        if (!Enum.TryParse<LinuxScanCode>(target.ToUpper(), out sc))
                            continue;

                        input.SetScanCode((uint)sc);
                    }

                    if (input.ScanCodes.Length > 0 || custom)
                        app.Input.Add(input);
                }

                playerIndex++;
            }

            if (app.Input.Count > 0)
            {
                if (mapping == null)
                    mapping = new PadToKey();

                var existingApp = mapping.Applications.FirstOrDefault(a => a.Name.Equals(app.Name, StringComparison.InvariantCultureIgnoreCase));
                if (existingApp != null)
                {
                    // Merge with existing by replacing inputs
                    foreach (var input in app.Input)
                    {
                        existingApp.Input.RemoveAll(i => i.Name == input.Name);
                        existingApp.Input.Add(input);
                    }
                }
                else
                    mapping.Applications.Add(app);
            }

            return mapping;
        }

        private static InputConfig[] LoadControllerConfiguration(string[] args)
        {
            SimpleLogger.Instance.Info("[Startup] Loading Controller configuration.");
            var controllers = new Dictionary<int, Controller>();

            for (int i = 0; i < args.Length; i++)
            {
                if (args[i].StartsWith("-p") && args[i].Length > 3)
                {
                    int playerId = new string(args[i].Substring(2).TakeWhile(c => char.IsNumber(c)).ToArray()).ToInteger();

                    Controller player;
                    if (!controllers.TryGetValue(playerId, out player))
                    {
                        player = new Controller() { PlayerIndex = playerId };
                        controllers[playerId] = player;
                    }
                    
                    if (args.Length < i + 1)
                        break;

                    string var = args[i].Substring(3);
                    string val = args[i + 1];
                    if (val.StartsWith("-"))
                        continue;

                    switch (var)
                    {
                        case "index": player.DeviceIndex = val.ToInteger(); break;
                        case "guid": player.Guid = new SdlJoystickGuid(val); break;
                        case "path": player.DevicePath = val; break;
                        case "name": player.Name = val; break;
                        case "nbbuttons": player.NbButtons = val.ToInteger(); break;
                        case "nbhats": player.NbHats = val.ToInteger(); break;
                        case "nbaxes": player.NbAxes = val.ToInteger(); break;
                    }
                }
            }

            Controllers = controllers.Select(c => c.Value).OrderBy(c => c.PlayerIndex).ToList();

            try
            {
                var inputConfig = EsInput.Load(Path.Combine(Program.AppConfig.GetFullPath("home"), "es_input.cfg"));
                if (inputConfig != null)
                {
                    foreach (var pi in Controllers)
                    {
                        if (pi.IsKeyboard)
                        {
                            pi.Config = inputConfig.FirstOrDefault(c => "Keyboard".Equals(c.DeviceName, StringComparison.InvariantCultureIgnoreCase));
                            if (pi.Config != null)
                                continue;
                        }

                        pi.Config = inputConfig.FirstOrDefault(c => pi.CompatibleSdlGuids.Contains(c.DeviceGUID.ToLowerInvariant()) && c.DeviceName == pi.Name);
                        if (pi.Config == null)
                            pi.Config = inputConfig.FirstOrDefault(c => pi.CompatibleSdlGuids.Contains(c.DeviceGUID.ToLowerInvariant()));
                        if (pi.Config == null)
                            pi.Config = inputConfig.FirstOrDefault(c => c.DeviceName == pi.Name);
                    }

                    Controllers.RemoveAll(c => c.Config == null);

                    if (!Controllers.Any() || SystemConfig.getOptBoolean("use_guns") || Misc.HasWiimoteGun())
                    {
                        var keyb = new Controller() { PlayerIndex = Controllers.Count + 1 };
                        keyb.Config = inputConfig.FirstOrDefault(c => c.DeviceName == "Keyboard");
                        if (keyb.Config != null)
                        {
                            keyb.Name = "Keyboard";
                            keyb.Guid = new SdlJoystickGuid("00000000000000000000000000000000");
                            Controllers.Add(keyb);
                        }
                    }
                }

                return inputConfig;
            }
            catch (Exception ex)
            {
                SimpleLogger.Instance.Error("[LoadControllerConfiguration] Failed " + ex.Message, ex);
            }

            return null;
        }
        
        private static void ImportShaderOverrides()
        {
            if (AppConfig.isOptSet("shaders") && SystemConfig.isOptSet("shaderset") && SystemConfig["shaderset"] != "none")
            {
                string path = Path.Combine(AppConfig.GetFullPath("shaders"), "configs", SystemConfig["shaderset"], "rendering-defaults.yml");
                if (File.Exists(path))
                {
                    string renderconfig = SystemShaders.GetShader(File.ReadAllText(path), SystemConfig["system"], SystemConfig["emulator"], SystemConfig["core"]);
                    if (!string.IsNullOrEmpty(renderconfig))
                        SystemConfig["shader"] = renderconfig;
                }
            }
        }

        /// <summary>
        /// To use with Environment.ExitCode = (int)ExitCodes.CustomError;
        /// Deletes the file if message == null
        /// </summary>
        /// <param name="message"></param>
        public static void WriteCustomErrorFile(string message)
        {
            SimpleLogger.Instance.Error("[Error] " + message);

            string fn = Path.Combine(Installer.GetTempPath(), "launch_error.log");

            try
            {
                if (string.IsNullOrEmpty(message))
                {
                    if (File.Exists(fn))
                        File.Delete(fn);
                }
                else
                    File.WriteAllText(fn, message);
            }
            catch { }
        }

        public static void RegisterShellExtensions()
        {
            try
            {
                if (!SquashFsArchive.IsSquashFsAvailable)
                    return;

                RegisterConvertToIso(".squashfs");
                RegisterConvertToIso(".wsquashfs");
                RegisterExtractAsFolder(".squashfs");
                RegisterExtractAsFolder(".wsquashfs");
            }
            catch { }
        }

        private static void RegisterConvertToIso(string extension)
        {
            if (string.IsNullOrEmpty(extension))
                return;

            RegistryKey key = Registry.ClassesRoot.CreateSubKey(extension);
            if (key == null)
                return;

            RegistryKey shellKey = key.CreateSubKey("Shell");
            if (shellKey == null)
                return;

            var openWith = typeof(Program).Assembly.Location;
            shellKey.CreateSubKey("Convert to ISO").CreateSubKey("command").SetValue("", "\"" + openWith + "\"" + " -makeiso \"%1\"");
            shellKey.Close();

            key.Close();
        }

        private static void RegisterExtractAsFolder(string extension)
        {
            if (string.IsNullOrEmpty(extension))
                return;

            RegistryKey key = Registry.ClassesRoot.CreateSubKey(extension);
            if (key == null)
                return;

            RegistryKey shellKey = key.CreateSubKey("Shell");
            if (shellKey == null)
                return;

            var openWith = typeof(Program).Assembly.Location;
            shellKey.CreateSubKey("Extract as folder").CreateSubKey("command").SetValue("", "\"" + openWith + "\"" + " -extract \"%1\"");
            shellKey.Close();

            key.Close();
        }

        private static int ObscureCode(byte x, byte y)
        {
            return (x ^ y) + 0x80;
        }
    }
}
