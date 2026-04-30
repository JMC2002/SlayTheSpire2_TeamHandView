using Godot;
using JmcModLib.Utils;
using MegaCrit.Sts2.Core.Context;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.UI;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes;
using MegaCrit.Sts2.Core.Nodes.Cards;
using MegaCrit.Sts2.Core.Nodes.Multiplayer;

namespace TeamHandView.Core;

internal static class RemoteHandPreviewController
{
    private const float CardSpacing = 18f;
    private const float EdgePadding = 24f;
    private const float TargetGap = 18f;
    private const int PreviewZIndex = 2_000;

    private static readonly Dictionary<ulong, Control> PreviewRoots = [];
    private static ulong? HoveredNetId;
    private static ulong? LockedNetId;

    public static void UpdateVisibility(NMultiplayerPlayerState playerState, bool isHighlighted)
    {
        try
        {
            if (isHighlighted)
            {
                HoveredNetId = playerState.Player?.NetId;
                ShowOrRefresh(playerState);
            }
            else
            {
                ClearHovered(playerState);
                HideIfUnlocked(playerState);
            }
        }
        catch (Exception ex)
        {
            ModLogger.Warn($"{VersionInfo.Tag} Failed to update remote hand preview: {ex}");
            Hide(playerState);
        }
    }

    public static void RefreshIfVisible(NMultiplayerPlayerState playerState)
    {
        try
        {
            if (PreviewRoots.ContainsKey(playerState.Player.NetId))
                ShowOrRefresh(playerState);
        }
        catch (Exception ex)
        {
            ModLogger.Warn($"{VersionInfo.Tag} Failed to refresh remote hand preview: {ex}");
            Hide(playerState);
        }
    }

    public static void TogglePreviewLock()
    {
        try
        {
            if (!TeamHandViewSettings.Enabled)
            {
                UnlockCurrentPreview(hideLockedPreview: true);
                return;
            }

            if (TryGetVisibleHoveredNetId(out ulong hoveredNetId))
            {
                if (LockedNetId == hoveredNetId)
                {
                    LockedNetId = null;
                    ModLogger.Info($"{VersionInfo.Tag} Remote hand preview unlocked.");
                    return;
                }

                if (LockedNetId is { } previouslyLockedNetId && previouslyLockedNetId != hoveredNetId)
                    Hide(previouslyLockedNetId);

                LockedNetId = hoveredNetId;
                ModLogger.Info($"{VersionInfo.Tag} Remote hand preview locked.");
                return;
            }

            UnlockCurrentPreview(hideLockedPreview: true);
        }
        catch (Exception ex)
        {
            ModLogger.Warn($"{VersionInfo.Tag} Failed to toggle remote hand preview lock: {ex}");
            UnlockCurrentPreview(hideLockedPreview: true);
        }
    }

    public static void Hide(NMultiplayerPlayerState playerState)
    {
        if (playerState.Player is null)
            return;

        Hide(playerState.Player.NetId);
    }

    private static void Hide(ulong netId)
    {
        if (HoveredNetId == netId)
            HoveredNetId = null;

        if (LockedNetId == netId)
            LockedNetId = null;

        if (!PreviewRoots.Remove(netId, out Control? root))
            return;

        root.FreeChildren();
        root.QueueFreeSafely();
    }

    private static void ShowOrRefresh(NMultiplayerPlayerState playerState)
    {
        if (!CanPreview(playerState, out IReadOnlyList<CardModel> cards))
        {
            Hide(playerState);
            return;
        }

        Control? parent = NRun.Instance?.GlobalUi?.CardPreviewContainer;
        if (parent is null || !GodotObject.IsInstanceValid(parent))
            return;

        ulong netId = playerState.Player.NetId;
        Control root = GetOrCreateRoot(parent, netId);
        root.FreeChildren();

        float scale = Mathf.Clamp(TeamHandViewSettings.CardScale, 0.25f, 0.65f);
        int columns = Mathf.Clamp(TeamHandViewSettings.MaxColumns, 1, Math.Max(1, cards.Count));
        Vector2 cardSize = NCard.defaultSize * scale;
        Vector2 previewSize = GetPreviewSize(cards.Count, columns, cardSize);

        root.MouseFilter = Control.MouseFilterEnum.Ignore;
        root.Size = previewSize;
        root.ZIndex = PreviewZIndex;

        AddCards(root, cards, columns, scale, cardSize);
        PositionPreview(playerState, root, previewSize);
    }

    private static bool CanPreview(NMultiplayerPlayerState playerState, out IReadOnlyList<CardModel> cards)
    {
        cards = [];

        if (!TeamHandViewSettings.Enabled)
            return false;

        if (playerState.Player is null || LocalContext.IsMe(playerState.Player))
            return false;

        if (!playerState.IsInsideTree())
            return false;

        IReadOnlyList<CardModel>? handCards = playerState.Player.PlayerCombatState?.Hand.Cards;
        if (handCards is not { Count: > 0 })
            return false;

        cards = handCards;
        return true;
    }

    private static Control GetOrCreateRoot(Control parent, ulong netId)
    {
        if (PreviewRoots.TryGetValue(netId, out Control? root) && GodotObject.IsInstanceValid(root))
        {
            if (root.GetParent() != parent)
            {
                root.GetParent()?.RemoveChild(root);
                parent.AddChildSafely(root);
            }

            return root;
        }

        root = new Control
        {
            Name = $"TeamHandViewRemoteHandPreview_{netId}",
            MouseFilter = Control.MouseFilterEnum.Ignore,
            ZIndex = PreviewZIndex
        };
        parent.AddChildSafely(root);
        PreviewRoots[netId] = root;
        return root;
    }

    private static void AddCards(Control root, IReadOnlyList<CardModel> cards, int columns, float scale, Vector2 cardSize)
    {
        for (int i = 0; i < cards.Count; i++)
        {
            NCard? cardNode = NCard.Create(cards[i]);
            if (cardNode is null)
                continue;

            int row = i / columns;
            int column = i % columns;

            cardNode.MouseFilter = Control.MouseFilterEnum.Ignore;
            cardNode.Scale = Vector2.One * scale;
            cardNode.Position = new Vector2(
                column * (cardSize.X + CardSpacing),
                row * (cardSize.Y + CardSpacing));

            root.AddChildSafely(cardNode);
            cardNode.UpdateVisuals(PileType.Hand, CardPreviewMode.Normal);
        }
    }

    private static Vector2 GetPreviewSize(int cardCount, int columns, Vector2 cardSize)
    {
        int rows = (cardCount + columns - 1) / columns;
        int actualColumns = Math.Min(columns, cardCount);

        return new Vector2(
            actualColumns * cardSize.X + Math.Max(0, actualColumns - 1) * CardSpacing,
            rows * cardSize.Y + Math.Max(0, rows - 1) * CardSpacing);
    }

    private static void PositionPreview(NMultiplayerPlayerState playerState, Control root, Vector2 previewSize)
    {
        Vector2 viewportSize = playerState.GetViewport().GetVisibleRect().Size;
        Vector2 targetPosition = playerState.GlobalPosition;
        Vector2 targetSize = playerState.Size;

        Vector2 position = new(targetPosition.X + targetSize.X + TargetGap, targetPosition.Y);
        if (position.X + previewSize.X > viewportSize.X - EdgePadding)
            position.X = targetPosition.X - previewSize.X - TargetGap;

        position.Y = targetPosition.Y - Math.Max(0f, (previewSize.Y - targetSize.Y) * 0.5f);
        position.X = Mathf.Clamp(position.X, EdgePadding, Math.Max(EdgePadding, viewportSize.X - previewSize.X - EdgePadding));
        position.Y = Mathf.Clamp(position.Y, EdgePadding, Math.Max(EdgePadding, viewportSize.Y - previewSize.Y - EdgePadding));

        root.GlobalPosition = position;
    }

    private static void HideIfUnlocked(NMultiplayerPlayerState playerState)
    {
        if (playerState.Player is null)
            return;

        ulong netId = playerState.Player.NetId;
        if (LockedNetId == netId)
            return;

        Hide(netId);
    }

    private static void ClearHovered(NMultiplayerPlayerState playerState)
    {
        if (playerState.Player is null)
            return;

        if (HoveredNetId == playerState.Player.NetId)
            HoveredNetId = null;
    }

    private static bool TryGetVisibleHoveredNetId(out ulong netId)
    {
        if (HoveredNetId is { } hoveredNetId
            && PreviewRoots.TryGetValue(hoveredNetId, out Control? root)
            && GodotObject.IsInstanceValid(root))
        {
            netId = hoveredNetId;
            return true;
        }

        netId = default;
        return false;
    }

    private static void UnlockCurrentPreview(bool hideLockedPreview)
    {
        if (LockedNetId is not { } lockedNetId)
            return;

        LockedNetId = null;
        if (hideLockedPreview)
            Hide(lockedNetId);

        ModLogger.Info($"{VersionInfo.Tag} Remote hand preview unlocked.");
    }
}
