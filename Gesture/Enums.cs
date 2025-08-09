namespace at365.Gesture365
{
    /// <summary>
    /// ジェスチャーボタン
    /// </summary>
    [Flags]
    public enum GestureButton
    {
        None = 0x00,
        Right = 0x02,
        Middle = 0x04,
        All = Right | Middle,
    }

    /// <summary>
    /// マウスボタントリガー
    /// </summary>
    public enum MouseTrigger
    {
        LeftButtonDown,
        LeftButtonUp,
        RightButtonDown,
        RightButtonUp,
        MiddleButtonDown,
        MiddleButtonUp,
        WheelUp,
        WheelDown,
    }

    /// <summary>
    /// マウス移動トリガー
    /// </summary>
    public enum MoveTrigger
    {
        MoveUp,
        MoveDown,
        MoveLeft,
        MoveRight,
    }
}
