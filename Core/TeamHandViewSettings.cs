using Godot;
using JmcModLib.Config;
using JmcModLib.Config.UI;

namespace TeamHandView.Core;

public static class TeamHandViewSettings
{
    private const string PreviewGroup = "preview";
    private const string DefaultLockControllerInput = "controller_joystick_press";
    public const float MinPositionOffset = -1000f;
    public const float MaxPositionOffset = 1000f;

    [UIToggle]
    [Config(
        "Enable hand preview",
        onChanged: nameof(OnEnabledChanged),
        group: PreviewGroup,
        Description = "Show remote players' current hand while hovering their multiplayer player state.",
        Key = "enabled",
        Order = 10)]
    public static bool Enabled = true;

    [UISlider(0.25, 0.65, 0.01)]
    [Config(
        "Card scale",
        onChanged: nameof(OnPreviewLayoutFloatChanged),
        group: PreviewGroup,
        Description = "Controls how large cards appear in the hover preview.",
        Key = "card_scale",
        Order = 20)]
    public static float CardScale = 0.42f;

    [UIIntSlider(3, 10)]
    [Config(
        "Max cards per row",
        onChanged: nameof(OnPreviewLayoutIntChanged),
        group: PreviewGroup,
        Description = "Controls how many cards are shown before the preview wraps to a new row.",
        Key = "max_columns",
        Order = 30)]
    public static int MaxColumns = 10;

    [UISlider(MinPositionOffset, MaxPositionOffset, 1.0)]
    [Config(
        "Horizontal offset",
        onChanged: nameof(OnPositionOffsetChanged),
        group: PreviewGroup,
        Description = "Moves the preview horizontally from its default position.",
        Key = "offset_x",
        Order = 40)]
    public static float PositionOffsetX = 0f;

    [UISlider(MinPositionOffset, MaxPositionOffset, 1.0)]
    [Config(
        "Vertical offset",
        onChanged: nameof(OnPositionOffsetChanged),
        group: PreviewGroup,
        Description = "Moves the preview vertically from its default position.",
        Key = "offset_y",
        Order = 50)]
    public static float PositionOffsetY = 0f;

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
        Order = 60)]
    public static void TogglePreviewLock()
    {
        RemoteHandPreviewController.TogglePreviewLock();
    }

    private static void OnEnabledChanged(bool enabled)
    {
        RemoteHandPreviewController.ApplyEnabledSetting(enabled, "启用设置变更");
    }

    private static void OnPreviewLayoutFloatChanged(float _)
    {
        RemoteHandPreviewController.RefreshLockedPreview("预览尺寸设置变更");
    }

    private static void OnPreviewLayoutIntChanged(int _)
    {
        RemoteHandPreviewController.RefreshLockedPreview("预览布局设置变更");
    }

    private static void OnPositionOffsetChanged(float _)
    {
        RemoteHandPreviewController.RefreshLockedPreviewPosition("预览位置设置变更");
    }
}
