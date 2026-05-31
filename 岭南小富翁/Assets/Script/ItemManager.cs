using System.Collections.Generic;
using UnityEngine;

public class ItemManager : MonoBehaviour
{
    public static ItemManager Instance { get; private set; }

    [System.Serializable]
    public class PlayerInventory
    {
        public Player player;
        public List<ItemData> items = new List<ItemData>();
    }

    private Dictionary<Player, List<ItemData>> playerInventories = new Dictionary<Player, List<ItemData>>();

    private void Awake()
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

    public void GiveItem(Player player, ItemData item)
    {
        if (!playerInventories.ContainsKey(player))
        {
            playerInventories[player] = new List<ItemData>();
        }

        playerInventories[player].Add(item);
        Debug.Log($"{player.playerName} ??????: {item.itemName}");

        if (UIManager.Instance != null)
        {
            UIManager.Instance.ShowToast($"??????: {item.itemName}!", 2f);
        }

        UpdateItemDisplay();
    }

    public bool UseItem(Player player, ItemData item)
    {
        if (!HasItem(player, item))
        {
            Debug.LogWarning($"{player.playerName} ??????????: {item.itemName}");
            return false;
        }

        if (!CanUseItem(player, item))
        {
            Debug.LogWarning($"{player.playerName} ?????????????????: {item.itemName}");
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
            Debug.Log($"{player.playerName} ???/???????: {item.itemName}");
            UpdateItemDisplay();
        }
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
        Debug.Log($"{player.playerName} ??????: {item.itemName}");

        switch (item.effectType)
        {
            case ItemData.ItemEffectType.GainMoney:
                player.ReceiveCash(item.effectValue);
                UIManager.Instance?.ShowToast($"??? {item.effectValue} ???!", 2f);
                break;

            case ItemData.ItemEffectType.LoseMoney:
                player.PayCash(item.effectValue);
                UIManager.Instance?.ShowToast($"?? {item.effectValue} ???!", 2f);
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
                Debug.LogWarning($"?????????????: {item.effectType}");
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

    public void ClearAllItems()
    {
        playerInventories.Clear();
        UpdateItemDisplay();
    }
}
