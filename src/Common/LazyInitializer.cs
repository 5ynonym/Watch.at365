namespace at365.Common365
{
    public static class LazyInitializer<T> where T : new()
    {
        static LazyInitializer() { }

        private static T? _instance;
        public static T Instance => GetInstance();
        public static T GetInstance(Action<T>? initializer = null)
        {
            if (_instance == null)
            {
                _instance = new();
                initializer?.Invoke(_instance);
            }
            return _instance;
        }
    }
}
