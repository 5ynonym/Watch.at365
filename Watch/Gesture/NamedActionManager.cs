namespace at365.Gesture365
{
    using NamedAction = (Action Action, string Caption);

    public class NamedActionManager
    {
        public static readonly NamedActionManager Instance = new();

        private readonly Dictionary<string, NamedAction> _actionMap = new();

        public void RegisterAction(string name, Action action, string? caption = null)
        {
            _actionMap[name] = (action, caption ?? string.Empty);
        }

        public void UnregisterAction(string name)
        {
            _actionMap.Remove(name);
        }

        public (Action? action, string? caption) GetAction(string name)
        {
            return _actionMap.GetValueOrDefault(name);
        }

        public string? GetCaption(string name)
        {
            return _actionMap.GetValueOrDefault(name).Caption;
        }
    }
}
