namespace at365.Common365
{
    public abstract class ModuleBase<T> : ModuleBase
        where T : ModuleBase<T>, new()
    {
        public static T Instance => LazyInitializer<T>.GetInstance(instance => instance.Load());
        protected ModuleBase() { }
    }

    public abstract class ModuleBase : IDisposable
    {
        private static readonly List<ModuleBase> _loadedModules = new();

        private bool disposed = false;

        ~ModuleBase()
        {
            Dispose(false);
        }

        public static void DisposeAll()
        {
            _loadedModules.ForEach(module => SafeDispose(module));
            _loadedModules.Clear();
        }

        protected abstract void InitializeCore();
        protected abstract void DisposeCore(bool disposing);

        protected void Load()
        {
            InitializeCore();
            _loadedModules.Add(this);
        }

        void IDisposable.Dispose()
        {
            _loadedModules.Remove(this);
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected static void SafeDispose<T>(in T disposable) where T : class, IDisposable
        {
            try { disposable?.Dispose(); } catch { }
        }

        private void Dispose(bool disposing)
        {
            if (!disposed)
            {
                DisposeCore(disposing);
                disposed = true;
            }
        }
    }
}
