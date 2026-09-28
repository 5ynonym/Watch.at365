using System.ComponentModel;
using System.Runtime.InteropServices;
using at365.Native365;

namespace at365.Gesture365;

public class InputSimulator
{
    public static readonly InputSimulator Instance = new();
    internal static readonly nint InputMarker = 0x365A7;
    private static readonly int InputSize = Marshal.SizeOf<NativeMethods.INPUT>();
    public KeyboardSimulator Keyboard { get; } = new();
    public MouseSimulator Mouse { get; } = new();
    public static void LeftButtonClick() => Instance.Mouse.LeftButtonClick();
    public static void RightButtonClick() => Instance.Mouse.RightButtonClick();
    public static void MiddleButtonClick() => Instance.Mouse.MiddleButtonClick();

    internal static void Send(NativeMethods.INPUT[] inputs)
    {
        if (NativeMethods.SendInput((uint)inputs.Length, inputs, InputSize) != inputs.Length)
            throw new Win32Exception(Marshal.GetLastWin32Error());
    }
}

public class MouseSimulator
{
    public void LeftButtonClick() => Click(NativeMethods.MOUSEEVENTF_LEFTDOWN, NativeMethods.MOUSEEVENTF_LEFTUP);
    public void RightButtonClick() => Click(NativeMethods.MOUSEEVENTF_RIGHTDOWN, NativeMethods.MOUSEEVENTF_RIGHTUP);
    public void MiddleButtonClick() => Click(NativeMethods.MOUSEEVENTF_MIDDLEDOWN, NativeMethods.MOUSEEVENTF_MIDDLEUP);

    private static void Click(uint down, uint up) => InputSimulator.Send([CreateInput(down), CreateInput(up)]);
    private static NativeMethods.INPUT CreateInput(uint flags) => new()
    {
        type = NativeMethods.INPUT_MOUSE,
        u = new NativeMethods.INPUTUNION
        {
            mi = new NativeMethods.MOUSEINPUT { dwFlags = flags, dwExtraInfo = InputSimulator.InputMarker }
        }
    };
}

public class KeyboardSimulator
{
    public void ModifiedKeyStroke(uint modifierKeyCode, uint keyCode) =>
        ModifiedKeyStroke(modifierKeyCode == 0 ? [] : new[] { modifierKeyCode }, keyCode);

    public void ModifiedKeyStroke(IEnumerable<uint> modifierKeyCodes, uint keyCode) =>
        InputSimulator.Send(BuildInputs(modifierKeyCodes, keyCode));

    internal static NativeMethods.INPUT[] BuildInputs(IEnumerable<uint> modifierKeyCodes, uint keyCode)
    {
        var modifiers = modifierKeyCodes.Where(key => key != 0).Distinct().ToArray();
        var inputs = new List<NativeMethods.INPUT>(2 * modifiers.Length + 2);
        foreach (var modifier in modifiers) inputs.Add(CreateInput(modifier, false));
        inputs.Add(CreateInput(keyCode, false));
        inputs.Add(CreateInput(keyCode, true));
        for (var i = modifiers.Length - 1; i >= 0; i--) inputs.Add(CreateInput(modifiers[i], true));
        return inputs.ToArray();
    }

    private static NativeMethods.INPUT CreateInput(uint key, bool released) => new()
    {
        type = NativeMethods.INPUT_KEYBOARD,
        u = new NativeMethods.INPUTUNION
        {
            ki = new NativeMethods.KEYBDINPUT
            {
                wVk = (ushort)key,
                dwFlags = (released ? NativeMethods.KEYEVENTF_KEYUP : 0)
                    | (IsExtendedKey(key) ? NativeMethods.KEYEVENTF_EXTENDEDKEY : 0),
                dwExtraInfo = InputSimulator.InputMarker
            }
        }
    };

    private static bool IsExtendedKey(uint key) => key is >= 0x21 and <= 0x28
        or 0x2D or 0x2E or 0x5B or 0x5C or 0x5D or 0x6F or 0x90 or 0xA3 or 0xA5;
}
