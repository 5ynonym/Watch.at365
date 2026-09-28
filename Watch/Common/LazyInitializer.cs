namespace at365.Common365
{
    public static class LazyInitializer<T> where T : new()
    {
        static LazyInitializer() { }

        private static T? _instance;
        private static readonly object Sync = new();
        public static T Instance => GetInstance();
        public static T GetInstance(Action<T>? initializer = null)
        {
            lock (Sync)
            {
                if (_instance == null)
                {
                    var instance = new T();
                    initializer?.Invoke(instance);
                    _instance = instance;
                }
                return _instance;
            }
        }
    }
}
