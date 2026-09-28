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

        public static void DisposeAll()
        {
            var modules = _loadedModules.ToArray();
            _loadedModules.Clear();
            foreach (var module in modules.Reverse())
                SafeDispose(module);
        }

        protected abstract void InitializeCore();
        protected abstract void DisposeCore(bool disposing);

        protected void Load()
        {
            try
            {
                InitializeCore();
                _loadedModules.Add(this);
            }
            catch
            {
                SafeDispose(this);
                throw;
            }
        }

        void IDisposable.Dispose()
        {
            _loadedModules.Remove(this);
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected static void SafeDispose<T>(in T disposable) where T : class, IDisposable
        {
            try { disposable?.Dispose(); }
            catch (Exception error) { Diagnostics.Report("Dispose", error); }
        }

        private void Dispose(bool disposing)
        {
            if (!disposed)
            {
                disposed = true;
                DisposeCore(disposing);
            }
        }
    }
}
