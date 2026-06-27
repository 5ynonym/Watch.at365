using System.Windows.Input;

namespace at365.Gesture365
{
    public class MouseGestureManager
    {
        public static readonly MouseGestureManager Instance = new();

        private readonly Dictionary<(GestureButton, string trigger, string processName), (Action action, string? caption)> _actionMap = new();
        private readonly HashSet<string> _moveActionTargets = [];

        public bool HasMoveAction(string process) => _moveActionTargets.Contains(process);

        public void RegisterMouseAction(MouseTrigger mouseTrigger, Action action, string[]? process = null)
        {
            RegisterAction(action, CreateTrigger(mouseTrigger), GestureButton.All, process ?? [], null);
        }

        public void RegisterMoveAction(IEnumerable<MoveTrigger> moveTriggers, Action action, string caption, string[] process)
        {
            RegisterAction(action, CreateTrigger(moveTriggers), GestureButton.Right, process, caption);
            Array.ForEach(process, (each) => _moveActionTargets.Add(each));
        }

        public void RegisterKeyAction(ModifierKeys modifierKeys, Key key, Action action, string[]? process = null)
        {
            RegisterAction(action, CreateTrigger(modifierKeys, key), GestureButton.All, process ?? [], null);
        }

        public (Action acction, string? caption) GetAction(GestureButton gestureButton, string trigger, string process)
        {
            return _actionMap.ContainsKey((gestureButton, trigger, process))
                ? _actionMap.GetValueOrDefault((gestureButton, trigger, process))
                : _actionMap.GetValueOrDefault((gestureButton, trigger, string.Empty));
        }

        public (Action acction, string? caption) GetAction(GestureButton gestureButton, IEnumerable<MoveTrigger> moveTriggers, string process)
        {
            return GetAction(gestureButton, CreateTrigger(moveTriggers), process);
        }

        public static string CreateTrigger(MouseTrigger mouseTrigger)
        {
            return Enum.GetName(mouseTrigger) ?? "None";
        }

        public static string CreateTrigger(IEnumerable<MoveTrigger> moveTriggers)
        {
            return string.Join("+", moveTriggers.Select(Enum.GetName));
        }

        public static string CreateTrigger(ModifierKeys modifierKeys, Key key)
        {
            return KeyHelper.ToString(modifierKeys, key);
        }

        public static Action<MouseTrigger, Action> WhenMouse(params string[] process)
        {
            return (mouseTrigger, action) => Instance.RegisterMouseAction(mouseTrigger, action, process);
        }

        public static Action<string, IEnumerable<MoveTrigger>, Action> WhenMove(params string[] process)
        {
            return (caption, moveTriggers, action) => Instance.RegisterMoveAction(moveTriggers, action, caption, process);
        }

        private void RegisterAction(
            Action action,
            string trigger,
            GestureButton gestureButton,
            string[] process,
            string? caption)
        {
            if (process.Length == 0)
            {
                SetActionMap(string.Empty);
            }
            else
            {
                foreach (var processName in process)
                {
                    SetActionMap(processName);
                }
            }

            void SetActionMap(string processName)
            {
                if ((gestureButton & GestureButton.Right) != 0)
                {
                    _actionMap[(GestureButton.Right, trigger, processName)] = (action, caption);
                }
                if ((gestureButton & GestureButton.Middle) != 0)
                {
                    _actionMap[(GestureButton.Middle, trigger, processName)] = (action, caption);
                }
            }
        }
    }
}