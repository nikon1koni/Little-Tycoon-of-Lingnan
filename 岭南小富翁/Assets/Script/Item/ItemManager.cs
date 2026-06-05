using System.Collections.Generic;
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

        if (UIManager.Instance != null)
        {
            UIManager.Instance.ShowToast($"获得卡牌: {item.itemName}!", 2f);
        }

        UpdateItemDisplay();
        UpdateDebugList(player);
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

        switch (item.effectType)
        {
            case ItemData.ItemEffectType.GainMoney:
                player.ReceiveCash(item.effectValue);
                UIManager.Instance?.ShowToast($"获得 {item.effectValue} 金币!", 2f);
                break;

            case ItemData.ItemEffectType.LoseMoney:
                player.PayCash(item.effectValue);
                UIManager.Instance?.ShowToast($"失去 {item.effectValue} 金币!", 2f);
                break;

            case ItemData.ItemEffectType.IncomeBoost:
                if (BuffSystem.Instance != null)
                {
                    var buff = new BuffSystem.Buff(
                        $"item_{item.GetInstanceID()}",
                        item.itemName,
                        BuildingData.BuffEffect.IncomeMultiplier,
                        item.effectPercent,
                        0f,
                        item.durationRounds,
                        item
                    );
                    BuffSystem.Instance.AddBuff(player, buff);
                }
                break;

            case ItemData.ItemEffectType.ImmuneToNegative:
                if (BuffSystem.Instance != null)
                {
                    var buff = new BuffSystem.Buff(
                        $"item_{item.GetInstanceID()}",
                        item.itemName,
                        BuildingData.BuffEffect.DefenseBoost,
                        1f,
                        0f,
                        item.durationRounds,
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

            default:
                Debug.LogWarning($"未处理的物品效果类型: {item.effectType}");
                break;
        }

        if (SFXManager.Instance != null)
        {
            SFXManager.Instance.PlaySFX(SFXClip.UIOpen);
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
