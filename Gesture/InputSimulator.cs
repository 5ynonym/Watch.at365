namespace at365.Gesture365
{
    public class InputSimulator : WindowsInput.InputSimulator
    {
        public static readonly InputSimulator Instance = new();

        public static void LeftButtonClick() => Instance.Mouse.LeftButtonClick();
        public static void RightButtonClick() => Instance.Mouse.RightButtonClick();
        public static void MiddleButtonClick() => Instance.Mouse.MiddleButtonClick();
    }
}
