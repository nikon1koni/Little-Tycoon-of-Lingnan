using System.Collections.Generic;
using UnityEngine;

public class Player : MonoBehaviour
{
    [Header("")]
    public string playerName = "1";
    public int playerID = 1;
    public Color playerColor = Color.red;

    [Header("")]
    public int cash = 1500;  // 
    public int totalCashEarned = 0;  // 累计获得金币（结算统计用）
    public List<BoardTile> ownedProperties = new List<BoardTile>();  // 

    [Header("")]
    public bool isInJail = false;
    public int jailTurnsRemaining = 0;
    public bool isBankrupt = false;

    [Header("")]
    [HideInInspector] public BoardTile currentTile;  // 
    [HideInInspector] public int currentTileIndex = 0;  // 

    [Header("Buff")]
    public int stepsModifier = 0;
    public float incomeReductionPercent = 0f;
    public int incomeReductionRounds = 0;
    public float taxReductionPercent = 0f;
    public int taxReductionRounds = 0;
    public int roundsImmuneToNegativeEvents = 0;
    public float nextRollMultiplier = 1f;
    
    public int loanAmount = 0;
    public float loanRepayMultiplier = 1f;
    public int loanRepayRounds = 0;

    public int receivableAmount = 0;
    public float receivableMultiplier = 1f;
    public int receivableRounds = 0;

    // 
    private PlayerMovement playerMovement;

    void Start()
    {
        playerMovement = GetComponent<PlayerMovement>();
        if (playerMovement == null)
        {
            Debug.LogWarning($"玩家 {playerName} 缺少 PlayerMovement 组件");
        }

        // 
        SetPlayerColor();
    }

    void SetPlayerColor()
    {
        // 
        MeshRenderer renderer = GetComponentInChildren<MeshRenderer>();
        if (renderer != null)
        {
            renderer.material.color = playerColor;
        }
    }

    public bool PayCash(int amount)
    {
        int previousCash = cash;
        bool canAfford = cash >= amount;
        cash -= amount;
        Debug.Log($"{playerName} 支出现金 {amount}，当前现金: {cash}");

        NotifyCashChanged();
        UpdateBankruptState(previousCash);
        
        return canAfford;
    }

    public void ReceiveCash(int amount)
    {
        int previousCash = cash;
        cash += amount;
        if (amount > 0)
        {
            totalCashEarned += amount;
        }
        Debug.Log($"{playerName} 获得现金 {amount}，当前现金: {cash}");

        NotifyCashChanged();
        UpdateBankruptState(previousCash);
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
    
    public void UpdateBankruptState(int previousCash)
    {
        if (cash < 0 && previousCash >= 0)
        {
            if (!HasBankruptBuff() && !isBankrupt)
            {
                GameManager.Instance?.ApplyBankruptDebuff(this);
            }
        }
        else if (cash >= 0 && previousCash < 0)
        {
            if (HasBankruptBuff())
            {
                ClearBankruptBuff();
            }
        }
    }

    public bool BuyProperty(BoardTile property)
    {
        if (property == null) return false;

        if (property.tileType != BoardTile.TileType.Property &&
            property.tileType != BoardTile.TileType.Railroad &&
            property.tileType != BoardTile.TileType.Utility)
        {
            Debug.LogWarning($"地块 {property.tileName} 不能购买");
            return false;
        }

        if (property.ownerPlayer != null)
        {
            Debug.LogWarning($"{property.tileName} 已被购买");
            return false;
        }

        if (PayCash(property.propertyPrice))
        {
            property.ownerPlayer = this;
            ownedProperties.Add(property);
            Debug.Log($"{playerName} 成功购买 {property.tileName}");

            if (SFXManager.Instance != null)
                SFXManager.Instance.PlaySFX(SFXClip.EventPropertyBought);

            return true;
        }

        return false;
    }

    public bool PayRent(int rentAmount, GameObject owner)
    {
        if (PayCash(rentAmount))
        {
            Player ownerPlayer = owner.GetComponent<Player>();
            if (ownerPlayer != null)
            {
                ownerPlayer.ReceiveCash(rentAmount);
            }
            return true;
        }
        return false;
    }

    public void MoveToTile(BoardTile tile, bool teleport = false)
    {
        if (tile == null) return;

        if (playerMovement != null && !teleport)
        {
            int steps = GetStepsToTile(tile);
            if (steps > 0)
            {
                playerMovement.MoveSteps(steps);
            }
        }
        else
        {
            transform.position = tile.transform.position + Vector3.up * 0.5f;
            currentTile = tile;
            currentTileIndex = BoardManager.Instance?.allTiles.IndexOf(tile) ?? 0;

            tile.OnLanded(this);
        }
    }

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
            return (allTiles.Count - currentIndex) + targetIndex;
        }
        else
        {
            return targetIndex - currentIndex;
        }
    }

    public int GetDiceValueWithBoost(int baseValue)
    {
        if (BuffSystem.Instance != null && BuffSystem.Instance.HasDiceBoost(this))
        {
            int boost = BuffSystem.Instance.GetDiceBoostValue(this);
            int boostedValue = baseValue + boost;
            Debug.Log($"{playerName} 骰子加成: {baseValue} + {boost} = {boostedValue}");
            return Mathf.Clamp(boostedValue, 1, 12); // 12
        }
        return baseValue;
    }

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
            Debug.Log($"{playerName} 收入加成: {baseIncome} * {multiplier} = {finalIncome}");
        }
        return finalIncome;
    }

    public float GetMoveSpeedMultiplier()
    {
        if (BuffSystem.Instance != null)
        {
            return BuffSystem.Instance.GetMoveSpeedMultiplier(this);
        }
        return 1f;
    }

    public float GetLuckBoost()
    {
        if (BuffSystem.Instance != null)
        {
            return BuffSystem.Instance.GetLuckBoost(this);
        }
        return 0f;
    }

    public float GetDefenseBoost()
    {
        if (BuffSystem.Instance != null)
        {
            return BuffSystem.Instance.GetDefenseBoost(this);
        }
        return 0f;
    }

    public bool CheckBankruptcy()
    {
        if (cash < 0)
        {
            isBankrupt = true;
            Debug.Log($"{playerName} 破产了");
            return true;
        }
        return false;
    }
    
    public bool HasBankruptBuff()
    {
        if (BuffSystem.Instance != null)
        {
            List<BuffSystem.Buff> buffs = BuffSystem.Instance.GetPlayerBuffs(this);
            foreach (var buff in buffs)
            {
                if (buff.effectType == BuildingData.BuffEffect.Bankrupt)
                {
                    return true;
                }
            }
        }
        return false;
    }
    
    public void ClearBankruptBuff()
    {
        if (BuffSystem.Instance != null)
        {
            List<BuffSystem.Buff> buffs = BuffSystem.Instance.GetPlayerBuffs(this);
            foreach (var buff in buffs)
            {
                if (buff.effectType == BuildingData.BuffEffect.Bankrupt)
                {
                    BuffSystem.Instance.RemoveBuff(this, buff);
                    isBankrupt = false;
                    Debug.Log($"{playerName} 恢复破产状态");
                    if (UIManager.Instance != null)
                    {
                        UIManager.Instance.ShowToast($"{playerName} 恢复正常", 2f);
                    }
                    break;
                }
            }
        }
    }

    public void AddStepsModifier(int modifier)
    {
        stepsModifier += modifier;
    }

    public int GetStepsModifier()
    {
        int modifier = stepsModifier;
        stepsModifier = 0;
        return modifier;
    }

    public void AddIncomeReductionDebuff(float percent, int rounds)
    {
        incomeReductionPercent = percent;
        incomeReductionRounds = rounds;
    }

    public float GetIncomeReduction()
    {
        float reduction = incomeReductionPercent;
        if (incomeReductionRounds > 0)
        {
            incomeReductionRounds--;
            if (incomeReductionRounds <= 0)
            {
                incomeReductionPercent = 0f;
            }
        }
        return reduction;
    }

    public void AddTaxReductionBuff(float percent, int rounds)
    {
        taxReductionPercent = percent;
        taxReductionRounds = rounds;
    }

    public float GetTaxReduction()
    {
        float reduction = taxReductionPercent;
        if (taxReductionRounds > 0)
        {
            taxReductionRounds--;
            if (taxReductionRounds <= 0)
            {
                taxReductionPercent = 0f;
            }
        }
        return reduction;
    }

    public void SetImmuneToNegativeEvents(int rounds)
    {
        roundsImmuneToNegativeEvents = rounds;
    }

    public bool IsImmuneToNegativeEvents()
    {
        if (roundsImmuneToNegativeEvents > 0)
        {
            roundsImmuneToNegativeEvents--;
            return true;
        }
        return false;
    }

    public void SetNextRollMultiplier(float multiplier)
    {
        nextRollMultiplier = multiplier;
    }

    public float GetNextRollMultiplier()
    {
        float multiplier = nextRollMultiplier;
        nextRollMultiplier = 1f;
        return multiplier;
    }

    public void AddLoanDebt(int amount, float repayMultiplier, int repayRounds)
    {
        loanAmount = amount;
        loanRepayMultiplier = repayMultiplier;
        loanRepayRounds = repayRounds;
    }

    public void ProcessLoanRepayment()
    {
        if (loanAmount > 0)
        {
            loanRepayRounds--;
            if (loanRepayRounds <= 0)
            {
                int repayAmount = Mathf.RoundToInt(loanAmount * loanRepayMultiplier);
                PayCash(repayAmount);
                Debug.Log($"{playerName} 还款: {repayAmount}");
                loanAmount = 0;
                loanRepayMultiplier = 1f;
                loanRepayRounds = 0;
            }
        }
    }

    public void AddReceivableDebt(int amount, float multiplier, int rounds)
    {
        receivableAmount = amount;
        receivableMultiplier = multiplier;
        receivableRounds = rounds;
    }

    public void ProcessReceivableRepayment()
    {
        if (receivableAmount > 0)
        {
            receivableRounds--;
            if (receivableRounds <= 0)
            {
                int receiveAmount = Mathf.RoundToInt(receivableAmount * receivableMultiplier);
                ReceiveCash(receiveAmount);
                Debug.Log($"{playerName} 收到应收账款: {receiveAmount}"); receivableAmount = 0;
                receivableMultiplier = 1f;
                receivableRounds = 0;
                
                if (UIManager.Instance != null)
                {
                    UIManager.Instance.ShowToast($"收到应收账款 {receiveAmount} 现金", 2f);
                }
            }
        }
    }
}
