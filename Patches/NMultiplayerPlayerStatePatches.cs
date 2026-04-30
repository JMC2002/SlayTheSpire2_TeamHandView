using HarmonyLib;
using JmcModLib.Reflection;
using JmcModLib.Utils;
using MegaCrit.Sts2.Core.Nodes.Multiplayer;
using TeamHandView.Core;

namespace TeamHandView.Patches;

[HarmonyPatch(typeof(NMultiplayerPlayerState))]
internal static class NMultiplayerPlayerStatePatches
{
    private static readonly MemberAccessor IsHighlightedAccessor =
        MemberAccessor.Get(typeof(NMultiplayerPlayerState), "_isHighlighted");

    private static readonly MemberAccessor IsMouseOverAccessor =
        MemberAccessor.Get(typeof(NMultiplayerPlayerState), "_isMouseOver");

    private static readonly MemberAccessor IsCreatureHoveredAccessor =
        MemberAccessor.Get(typeof(NMultiplayerPlayerState), "_isCreatureHovered");

    [HarmonyPatch("UpdateHighlightedState")]
    [HarmonyPostfix]
    private static void UpdateHandPreviewVisibility(NMultiplayerPlayerState __instance)
    {
        try
        {
            bool isHighlighted = IsHighlightedAccessor.GetValue<NMultiplayerPlayerState, bool>(__instance);
            bool isMouseOver = IsMouseOverAccessor.GetValue<NMultiplayerPlayerState, bool>(__instance);
            bool isCreatureHovered = IsCreatureHoveredAccessor.GetValue<NMultiplayerPlayerState, bool>(__instance);
            // 手柄选中玩家角色时，原版走的是 _isCreatureHovered，而不是头像 Hitbox 的鼠标悬停。
            RemoteHandPreviewController.UpdateVisibility(__instance, isHighlighted && (isMouseOver || isCreatureHovered));
        }
        catch (Exception ex)
        {
            ModLogger.Warn($"{VersionInfo.Tag} 读取联机玩家高亮状态失败：{ex}");
            RemoteHandPreviewController.Hide(__instance);
        }
    }

    [HarmonyPatch("OnFocus")]
    [HarmonyPostfix]
    private static void ShowHandPreviewOnFocus(NMultiplayerPlayerState __instance)
    {
        // NButton 的 Focused 同时覆盖鼠标悬停和手柄焦点，直接挂这里能补齐手柄导航路径。
        RemoteHandPreviewController.UpdateVisibility(__instance, true);
    }

    [HarmonyPatch("OnUnfocus")]
    [HarmonyPostfix]
    private static void HideHandPreviewOnUnfocus(NMultiplayerPlayerState __instance)
    {
        RemoteHandPreviewController.UpdateVisibility(__instance, false);
    }

    [HarmonyPatch("RefreshCombatValues")]
    [HarmonyPostfix]
    private static void RefreshHandPreview(NMultiplayerPlayerState __instance)
    {
        RemoteHandPreviewController.RefreshIfVisible(__instance);
    }

    [HarmonyPatch("OnCombatEnded")]
    [HarmonyPostfix]
    private static void HideHandPreviewAfterCombat(NMultiplayerPlayerState __instance)
    {
        RemoteHandPreviewController.Hide(__instance);
    }

    [HarmonyPatch(nameof(NMultiplayerPlayerState._ExitTree))]
    [HarmonyPrefix]
    private static void HideHandPreviewBeforeExitTree(NMultiplayerPlayerState __instance)
    {
        RemoteHandPreviewController.Hide(__instance);
    }
}
