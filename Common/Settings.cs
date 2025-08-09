using System.Text.Json;

namespace at365.Common365
{
    public static class Settings
    {
        public static void Save<T>(string fileName, T settings) where T : class
        {
            var filePath = GetFilePath(fileName);
            var options = new JsonSerializerOptions { WriteIndented = true };
            File.WriteAllText(filePath, JsonSerializer.Serialize(settings, options));
        }

        public static T Load<T>(string fileName, T fallback) where T : class
        {
            var filePath = GetFilePath(fileName);
            if (!File.Exists(filePath)) return fallback;

            return JsonSerializer.Deserialize<T>(File.ReadAllText(filePath))
                ?? fallback;
        }

        private static string GetFilePath(string fileName)
        {
            return Path.Combine(AppDomain.CurrentDomain.BaseDirectory, fileName);
        }
    }
}
