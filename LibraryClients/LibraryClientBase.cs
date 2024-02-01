namespace RetroBat.LibraryClients
{
    public abstract class LibraryClientBase
    {
        public abstract string Name { get; }
        public abstract string Version { get; }
        public abstract bool IsSupported { get; }
        public abstract void Init();
        public abstract void Shutdown();
        public abstract void LoadGame(string romPath);
        public abstract void Run();
        public abstract void Pause();
        public abstract void Resume();
        public abstract void Stop();
    }
}
