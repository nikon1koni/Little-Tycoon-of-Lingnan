﻿﻿﻿﻿﻿using System.Collections.Generic;
using UnityEngine;

public class ItemManager : MonoBehaviour
{
    public static ItemManager Instance { get; private set; }

    [Header("初始物品")]
    [Tooltip("游戏开始时给每个玩家的物品")]
    public List<ItemData> startingItems = new List<ItemData>();

    [Header("调试当前物品")]
    [Tooltip("在编辑器中显示当前玩家的物品列表")]
    public List<ItemData> debugCurrentItems = new List<ItemData>();

    [Header("卡池")]
    [Tooltip("收获(Harvest)格子随机发卡使用的卡池")]
    public CardPool cardPool;

    [Header("手牌上限")]
    [Tooltip("玩家手牌（物品）数量上限，达到后从卡池抽到的卡将无法获得，改为按稀有度补偿金币")]
    public int maxHandSize = 12;

    private Dictionary<Player, List<ItemData>> playerInventories = new Dictionary<Player, List<ItemData>>();

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    /// <summary>
    /// 给玩家发放初始物品
    /// </summary>
    public void GiveStartingItemsToPlayer(Player player)
    {
        if (player == null) return;

        foreach (ItemData item in startingItems)
        {
            if (item != null)
            {
                GiveItem(player, item);
            }
        }

        Debug.Log($"已给 {player.playerName} 发放 {startingItems.Count} 个初始物品");
    }

    /// <summary>
    /// 给所有玩家发放初始物品
    /// </summary>
    public void GiveStartingItemsToAllPlayers()
    {
        if (GameManager.Instance == null) return;

        // 通过GameManager的玩家列表循环调用GiveStartingItemsToPlayer
        // 这里暂时留空，根据实际需求添加
        Debug.Log("请调用 GiveStartingItemsToPlayer(player) 给每个玩家发放物品");
    }
    
    /// <summary>
    /// 清空指定玩家的物品栏（重置用）
    /// </summary>
    public void ResetPlayerInventory(Player player)
    {
        if (playerInventories.ContainsKey(player))
        {
            playerInventories[player].Clear();
            UpdateItemDisplay();
            UpdateDebugList(player);
            Debug.Log($"已清空 {player.playerName} 的物品");
        }
    }

    public void GiveItem(Player player, ItemData item)
    {
        if (!playerInventories.ContainsKey(player))
        {
            playerInventories[player] = new List<ItemData>();
        }

        playerInventories[player].Add(item);
        Debug.Log($"{player.playerName} 获得物品: {item.itemName}");

        UpdateItemDisplay();
        UpdateDebugList(player);
    }

    /// <summary>
    /// 从卡池发卡的结果：成功获得 / 手牌已满（折算金币） / 无卡池或未抽到
    /// </summary>
    public enum HarvestCardResult { GotCard, HandFull, NoPool }

    /// <summary>
    /// 从卡池随机抽一张卡发给玩家（用于 Harvest 格子）。
    /// 手牌已满时不发卡，按卡稀有度补偿金币，并通过 out 参数返回补偿数额。
    /// </summary>
    public HarvestCardResult TryGiveRandomCardFromPool(Player player, out ItemData drawnCard, out int compensationGold)
    {
        drawnCard = null;
        compensationGold = 0;

        if (player == null) return HarvestCardResult.NoPool;

        if (cardPool == null)
        {
            Debug.LogWarning("ItemManager: 未配置 cardPool，无法发卡");
            return HarvestCardResult.NoPool;
        }

        ItemData card = cardPool.DrawCard();
        if (card == null)
        {
            Debug.LogWarning("ItemManager: 卡池为空或未抽到卡");
            return HarvestCardResult.NoPool;
        }

        // 手牌已满：不发卡，按稀有度补偿金币
        int current = GetPlayerItems(player).Count;
        if (current >= maxHandSize)
        {
            compensationGold = cardPool.GetCompensationGold(card.rarity);
            if (compensationGold > 0)
            {
                player.ReceiveCash(compensationGold);
            }
            Debug.Log($"{player.playerName} 手牌已满（{current}/{maxHandSize}），卡牌 {card.itemName}({card.rarity}) 折算 {compensationGold} 金币");
            return HarvestCardResult.HandFull;
        }

        GiveItem(player, card);

        // 刷新当前玩家手牌显示
        if (ItemHandManager.Instance != null)
        {
            ItemHandManager.Instance.RefreshHand();
        }

        drawnCard = card;
        return HarvestCardResult.GotCard;
    }

    public bool UseItem(Player player, ItemData item)
    {
        if (!HasItem(player, item))
        {
            Debug.LogWarning($"{player.playerName} 没有该物品: {item.itemName}");
            return false;
        }

        if (!CanUseItem(player, item))
        {
            Debug.LogWarning($"{player.playerName} 当前无法使用该物品: {item.itemName}");
            return false;
        }

        ApplyItemEffect(player, item);
        RemoveItem(player, item);
        return true;
    }

    public bool HasItem(Player player, ItemData item)
    {
        if (!playerInventories.ContainsKey(player)) return false;
        return playerInventories[player].Contains(item);
    }

    public List<ItemData> GetPlayerItems(Player player)
    {
        if (!playerInventories.ContainsKey(player))
        {
            return new List<ItemData>();
        }
        return new List<ItemData>(playerInventories[player]);
    }

    public void RemoveItem(Player player, ItemData item)
    {
        if (playerInventories.ContainsKey(player))
        {
            playerInventories[player].Remove(item);
            Debug.Log($"{player.playerName} 失去/使用物品: {item.itemName}");
            UpdateItemDisplay();
            UpdateDebugList(player);
        }
    }

    public void ClearPlayerItems(Player player)
    {
        if (playerInventories.ContainsKey(player))
        {
            playerInventories[player].Clear();
            Debug.Log($"{player.playerName} 物品已清空");
            UpdateItemDisplay();
            UpdateDebugList(player);
        }
    }

    public void ClearAllItems()
    {
        playerInventories.Clear();
        debugCurrentItems.Clear();
        UpdateItemDisplay();
        Debug.Log("已清空所有玩家的物品");
    }

    public bool CanUseItem(Player player, ItemData item)
    {
        if (GameManager.Instance != null)
        {
            if (GameManager.Instance.currentPlayer != player && !item.canUseAnytime)
            {
                return false;
            }
        }
        return true;
    }

    private void ApplyItemEffect(Player player, ItemData item)
    {
        Debug.Log($"{player.playerName} 使用物品: {item.itemName}");

        // 遍历所有效果并逐个应用
        foreach (var effect in item.effects)
        {
            ApplySingleEffect(player, item, effect);
        }

        if (SFXManager.Instance != null)
        {
            SFXManager.Instance.PlaySFX(SFXClip.UIOpen);
        }
    }

    private void ApplySingleEffect(Player player, ItemData item, ItemEffect effect)
    {
        switch (effect.effectType)
        {
            case ItemData.ItemEffectType.GainMoney:
                player.ReceiveCash(effect.effectValue);
                UIManager.Instance?.ShowToast($"获得 {effect.effectValue} 金币!", 2f);
                break;

            case ItemData.ItemEffectType.LoseMoney:
                player.PayCash(effect.effectValue);
                UIManager.Instance?.ShowToast($"失去 {effect.effectValue} 金币!", 2f);
                break;

            case ItemData.ItemEffectType.IncomeBoost:
                if (BuffSystem.Instance != null)
                {
                    var buff = new BuffSystem.Buff(
                        $"item_{item.GetInstanceID()}_{effect.effectType}",
                        item.itemName,
                        BuildingData.BuffEffect.IncomeMultiplier,
                        effect.effectPercent,
                        0f,
                        effect.durationRounds,
                        item
                    );
                    BuffSystem.Instance.AddBuff(player, buff);
                }
                break;

            case ItemData.ItemEffectType.ImmuneToNegative:
                if (BuffSystem.Instance != null)
                {
                    var buff = new BuffSystem.Buff(
                        $"item_{item.GetInstanceID()}_{effect.effectType}",
                        item.itemName,
                        BuildingData.BuffEffect.DefenseBoost,
                        1f,
                        0f,
                        effect.durationRounds,
                        item
                    );
                    BuffSystem.Instance.AddBuff(player, buff);
                }
                break;

            case ItemData.ItemEffectType.MoveToStart:
                if (BoardManager.Instance != null && BoardManager.Instance.allTiles.Count > 0)
                {
                    player.MoveToTile(BoardManager.Instance.allTiles[0], true);
                }
                break;

            case ItemData.ItemEffectType.MoveToRandom:
                if (BoardManager.Instance != null && BoardManager.Instance.allTiles.Count > 0)
                {
                    int randomIndex = Random.Range(0, BoardManager.Instance.allTiles.Count);
                    player.MoveToTile(BoardManager.Instance.allTiles[randomIndex], true);
                }
                break;

            case ItemData.ItemEffectType.AddDice:
                if (BuffSystem.Instance != null)
                {
                    var buff = new BuffSystem.Buff(
                        $"item_{item.GetInstanceID()}_{effect.effectType}",
                        item.itemName,
                        BuildingData.BuffEffect.DiceBoost,
                        effect.effectValue,
                        0f,
                        effect.durationRounds,
                        item
                    );
                    BuffSystem.Instance.AddBuff(player, buff);
                }
                break;

            case ItemData.ItemEffectType.SkipTurn:
                // 跳过回合逻辑
                Debug.Log($"{player.playerName} 跳过下回合");
                break;

            case ItemData.ItemEffectType.StealMoney:
                // 偷取金钱逻辑（需要目标玩家）
                Debug.Log($"偷取金钱效果: {effect.effectValue}");
                break;

            case ItemData.ItemEffectType.DestroyBuilding:
                // 摧毁建筑逻辑
                Debug.Log($"摧毁建筑效果");
                break;

            case ItemData.ItemEffectType.GiveBuff:
                // 给予Buff逻辑
                Debug.Log($"给予Buff效果");
                break;

            case ItemData.ItemEffectType.TeleportToTile:
                // 传送到指定格子
                if (BoardManager.Instance != null && effect.effectValue >= 0 && effect.effectValue < BoardManager.Instance.allTiles.Count)
                {
                    player.MoveToTile(BoardManager.Instance.allTiles[effect.effectValue], true);
                }
                break;

            case ItemData.ItemEffectType.MoveForward:
                if (GameManager.Instance != null)
                {
                    GameManager.Instance.MovePlayerExtraSteps(effect.effectValue);
                    UIManager.Instance?.ShowToast($"向前移动 {effect.effectValue} 步！", 2f);
                }
                break;

            case ItemData.ItemEffectType.Custom:
                // 自定义效果
                Debug.Log($"自定义效果: {effect.effectValue}");
                break;

            default:
                Debug.LogWarning($"未处理的物品效果类型: {effect.effectType}");
                break;
        }
    }

    private void UpdateItemDisplay()
    {
        if (ItemPanelUI.Instance != null)
        {
            ItemPanelUI.Instance.UpdateItemDisplay();
        }
    }

    private void UpdateDebugList(Player player)
    {
        if (GameManager.Instance != null && GameManager.Instance.currentPlayer == player)
        {
            debugCurrentItems = GetPlayerItems(player);
        }
    }
}
