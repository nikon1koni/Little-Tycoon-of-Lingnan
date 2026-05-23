using System.Collections.Generic;
using UnityEngine;

public class Player : MonoBehaviour
{
    [Header("??????")]
    public string playerName = "???1";
    public int playerID = 1;
    public Color playerColor = Color.red;

    [Header("???????")]
    public int cash = 1500;  // ??????
    public List<BoardTile> ownedProperties = new List<BoardTile>();  // ??§Ö???

    [Header("??")]
    public bool isInJail = false;
    public int jailTurnsRemaining = 0;
    public bool isBankrupt = false;

    [Header("¦Ë?????")]
    [HideInInspector] public BoardTile currentTile;  // ??????????
    [HideInInspector] public int currentTileIndex = 0;  // ???????????

    // ???
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
        // ?????????????????
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
        Debug.Log($"{playerName} ??? {amount} ???????: {cash}");

        NotifyCashChanged();
        return canAfford;
    }

    // ???
    public void ReceiveCash(int amount)
    {
        cash += amount;
        Debug.Log($"{playerName} ??? {amount} ???????: {cash} ???");

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
            Debug.LogWarning($"????? {property.tileName} ???????????????");
            return false;
        }

        if (property.ownerPlayer != null)
        {
            Debug.LogWarning($"{property.tileName} ?????????");
            return false;
        }

        if (PayCash(property.propertyPrice))
        {
            property.ownerPlayer = this;
            ownedProperties.Add(property);
            Debug.Log($"{playerName} ???????? {property.tileName}");

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
            // ????????????
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
            // ???????????
            int steps = GetStepsToTile(tile);
            if (steps > 0)
            {
                playerMovement.MoveSteps(steps);
            }
        }
        else
        {
            // ??????????????????
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

    // ??????§Þ????????
    public int GetDiceValueWithBoost(int baseValue)
    {
        if (BuffSystem.Instance != null && BuffSystem.Instance.HasDiceBoost(this))
        {
            int boost = BuffSystem.Instance.GetDiceBoostValue(this);
            int boostedValue = baseValue + boost;
            Debug.Log($"{playerName} ????????: {baseValue} + {boost} = {boostedValue}");
            return Mathf.Clamp(boostedValue, 1, 12); // ????12??
        }
        return baseValue;
    }

    // ??????§Ò????????
    public int GetIncomeWithMultiplier(int baseIncome)
    {
        float multiplier = 1f;
        if (BuffSystem.Instance != null)
        {
            multiplier = BuffSystem.Instance.GetIncomeMultiplier(this);
        }
        int finalIncome = Mathf.RoundToInt(baseIncome * multiplier);
        if (multiplier > 1.0f)
        {
            Debug.Log($"{playerName} ??????: {baseIncome} * {multiplier} = {finalIncome}");
        }
        return finalIncome;
    }

    // ????????????
    public float GetMoveSpeedMultiplier()
    {
        if (BuffSystem.Instance != null)
        {
            return BuffSystem.Instance.GetMoveSpeedMultiplier(this);
        }
        return 1f;
    }

    // ?????????
    public float GetLuckBoost()
    {
        if (BuffSystem.Instance != null)
        {
            return BuffSystem.Instance.GetLuckBoost(this);
        }
        return 0f;
    }

    // ??¡Â??????
    public float GetDefenseBoost()
    {
        if (BuffSystem.Instance != null)
        {
            return BuffSystem.Instance.GetDefenseBoost(this);
        }
        return 0f;
    }

    // ??????
    public bool CheckBankruptcy()
    {
        if (cash < 0)
        {
            isBankrupt = true;
            Debug.Log($"{playerName} ?????");
            return true;
        }
        return false;
    }
}
