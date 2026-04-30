using Godot;
using JmcModLib.Config;
using JmcModLib.Config.UI;

namespace TeamHandView.Core;

public static class TeamHandViewSettings
{
    private const string PreviewGroup = "preview";
    private const string DefaultLockControllerInput = "controller_joystick_press";

    [UIToggle]
    [Config(
        "Enable hand preview",
        group: PreviewGroup,
        Description = "Show remote players' current hand while hovering their multiplayer player state.",
        Key = "enabled",
        Order = 10)]
    public static bool Enabled = true;

    [UIFloatSlider(0.25f, 0.65f, decimalPlaces: 2)]
    [Config(
        "Card scale",
        group: PreviewGroup,
        Description = "Controls how large cards appear in the hover preview.",
        Key = "card_scale",
        Order = 20)]
    public static float CardScale = 0.42f;

    [UIIntSlider(3, 10)]
    [Config(
        "Max cards per row",
        group: PreviewGroup,
        Description = "Controls how many cards are shown before the preview wraps to a new row.",
        Key = "max_columns",
        Order = 30)]
    public static int MaxColumns = 10;

    [UIHotkey(
        "Toggle preview lock",
        PreviewGroup,
        Key = "toggle_preview_lock",
        Description = "Lock or unlock the currently hovered remote hand preview.",
        DefaultKeyboard = Key.H,
        DefaultModifiers = JmcKeyModifiers.Ctrl,
        // JML 的 UIHotkey 保存 Godot InputMap 名称，这里对应左摇杆按下。
        DefaultController = DefaultLockControllerInput,
        AllowController = true,
        ConsumeInput = true,
        Order = 40)]
    public static void TogglePreviewLock()
    {
        RemoteHandPreviewController.TogglePreviewLock();
    }
}
