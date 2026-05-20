using System.Collections.Generic;
using UnityEngine;

public class Player : MonoBehaviour
{
    [Header("??????")]
    public string playerName = "???1";
    public int playerID = 1;
    public Color playerColor = Color.red;

    [Header("??????")]
    public int cash = 1500;  // ??????
    public List<BoardTile> ownedProperties = new List<BoardTile>();  // ??快???

    [Header("?????")]
    public bool isInJail = false;
    public int jailTurnsRemaining = 0;
    public bool isBankrupt = false;

    [Header("竹?????")]
    [HideInInspector] public BoardTile currentTile;  // ??????????
    [HideInInspector] public int currentTileIndex = 0;  // ???????

    // ???????
    [Header("Buff??")]
    public bool hasDiceBoost = false;
    public int diceBoostValue = 0;
    public float incomeMultiplier = 1.0f;
    public float luckBoost = 0f;
    public float moveSpeedMultiplier = 1.0f;

    [Header("?????完Buff")]
    public List<BoardTile> activeBuffs = new List<BoardTile>();

    // ????
    private PlayerMovement playerMovement;

    void Start()
    {
        playerMovement = GetComponent<PlayerMovement>();
        if (playerMovement == null)
        {
            Debug.LogWarning($"??? {playerName} ??? PlayerMovement ???");
        }

        // ??????????
        SetPlayerColor();
    }

    void SetPlayerColor()
    {
        // ????????????
        MeshRenderer renderer = GetComponentInChildren<MeshRenderer>();
        if (renderer != null)
        {
            renderer.material.color = playerColor;
        }
    }

    // ???
    public bool PayCash(int amount)
    {
        bool canAfford = cash >= amount;
        cash -= amount;
        Debug.Log($"{playerName} ??? {amount} ?????????: {cash}");

        NotifyCashChanged();
        return canAfford;
    }

    // ???
    public void ReceiveCash(int amount)
    {
        cash += amount;
        Debug.Log($"{playerName} ??? {amount} ??????: {cash} ?");

        NotifyCashChanged();
    }

    private void NotifyCashChanged()
    {
        if (UIManager.Instance != null)
        {
            UIManager.Instance.UpdateCashDisplay(cash);
        }

        if (GameManager.Instance != null && GameManager.Instance.currentPlayer == this)
        {
            GameManager.Instance.UpdateUI();
        }
    }

    // ??????
    public bool BuyProperty(BoardTile property)
    {
        if (property == null) return false;

        if (property.tileType != BoardTile.TileType.Property &&
            property.tileType != BoardTile.TileType.Railroad &&
            property.tileType != BoardTile.TileType.Utility)
        {
            Debug.LogWarning($"??????? {property.tileName}????????????????????");
            return false;
        }

        if (property.ownerPlayer != null)
        {
            Debug.LogWarning($"{property.tileName} ???????????");
            return false;
        }

        if (PayCash(property.propertyPrice))
        {
            property.ownerPlayer = this;
            ownedProperties.Add(property);
            Debug.Log($"{playerName} ????????? {property.tileName}");

            if (SFXManager.Instance != null)
                SFXManager.Instance.PlaySFX(SFXClip.EventPropertyBought);

            return true;
        }

        return false;
    }

    // ??????
    public bool PayRent(int rentAmount, GameObject owner)
    {
        if (PayCash(rentAmount))
        {
            // ???????????
            Player ownerPlayer = owner.GetComponent<Player>();
            if (ownerPlayer != null)
            {
                ownerPlayer.ReceiveCash(rentAmount);
            }
            return true;
        }
        return false;
    }

    // ????????????
    public void MoveToTile(BoardTile tile, bool teleport = false)
    {
        if (tile == null) return;

        if (playerMovement != null && !teleport)
        {
            // ?????????
            int steps = GetStepsToTile(tile);
            if (steps > 0)
            {
                playerMovement.MoveSteps(steps);
            }
        }
        else
        {
            // ?????????完??
            transform.position = tile.transform.position + Vector3.up * 0.5f;
            currentTile = tile;
            currentTileIndex = BoardManager.Instance?.allTiles.IndexOf(tile) ?? 0;

            // ???????????
            tile.OnLanded(this);
        }
    }

    // ??????????????
    private int GetStepsToTile(BoardTile targetTile)
    {
        if (BoardManager.Instance == null || currentTile == null || targetTile == null)
            return 0;

        List<BoardTile> allTiles = BoardManager.Instance.allTiles;
        int currentIndex = allTiles.IndexOf(currentTile);
        int targetIndex = allTiles.IndexOf(targetTile);

        if (currentIndex == -1 || targetIndex == -1)
            return 0;

        if (targetIndex <= currentIndex)
        {
            // ???????
            return (allTiles.Count - currentIndex) + targetIndex;
        }
        else
        {
            return targetIndex - currentIndex;
        }
    }

    // ??????技????????
    public int GetDiceValueWithBoost(int baseValue)
    {
        if (hasDiceBoost)
        {
            int boostedValue = baseValue + diceBoostValue;
            Debug.Log($"{playerName} ?????????: {baseValue} + {diceBoostValue} = {boostedValue}");
            return Mathf.Clamp(boostedValue, 1, 12); // ???12??
        }
        return baseValue;
    }

    // ??????????????
    public int GetIncomeWithMultiplier(int baseIncome)
    {
        float multiplier = incomeMultiplier;
        int finalIncome = Mathf.RoundToInt(baseIncome * multiplier);
        if (multiplier > 1.0f)
        {
            Debug.Log($"{playerName} ?????????: {baseIncome} * {multiplier} = {finalIncome}");
        }
        return finalIncome;
    }

    // ????buff
    public void AddBuff(BoardTile buffSource)
    {
        if (!activeBuffs.Contains(buffSource))
        {
            activeBuffs.Add(buffSource);
            Debug.Log($"{playerName} ???buff: {buffSource.tileName}");
        }
    }

    // ???buff
    public void RemoveBuff(BoardTile buffSource)
    {
        if (activeBuffs.Contains(buffSource))
        {
            activeBuffs.Remove(buffSource);
            Debug.Log($"{playerName} ??buff: {buffSource.tileName}");
        }
    }

    // ?????????
    public bool CheckBankruptcy()
    {
        if (cash < 0)
        {
            isBankrupt = true;
            Debug.Log($"{playerName} ??????");
            return true;
        }
        return false;
    }
}
