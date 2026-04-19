using System.Collections.Generic;
using UnityEngine;

public class Player : MonoBehaviour
{
    [Header("玩家信息")]
    public string playerName = "玩家1";
    public int playerID = 1;
    public Color playerColor = Color.red;

    [Header("财务信息")]
    public int cash = 1500;  // 初始现金
    public List<BoardTile> ownedProperties = new List<BoardTile>();  // 拥有的地产

    [Header("状态信息")]
    public bool isInJail = false;
    public int jailTurnsRemaining = 0;
    public bool isBankrupt = false;

    [Header("位置信息")]
    [HideInInspector] public BoardTile currentTile;  // 当前所在格子
    [HideInInspector] public int currentTileIndex = 0;  // 当前索引

    // 玩家属性
    [Header("Buff状态")]
    public bool hasDiceBoost = false;
    public int diceBoostValue = 0;
    public float incomeMultiplier = 1.0f;
    public float luckBoost = 0f;
    public float moveSpeedMultiplier = 1.0f;

    [Header("当前生效Buff")]
    public List<BoardTile> activeBuffs = new List<BoardTile>();

    // 引用
    private PlayerMovement playerMovement;

    void Start()
    {
        playerMovement = GetComponent<PlayerMovement>();
        if (playerMovement == null)
        {
            Debug.LogWarning($"玩家 {playerName} 缺少 PlayerMovement 组件");
        }

        // 设置玩家颜色
        SetPlayerColor();
    }

    void SetPlayerColor()
    {
        // 设置玩家的颜色
        MeshRenderer renderer = GetComponentInChildren<MeshRenderer>();
        if (renderer != null)
        {
            renderer.material.color = playerColor;
        }
    }

    // 付钱
    public bool PayCash(int amount)
    {
        if (cash >= amount)
        {
            cash -= amount;
            Debug.Log($"{playerName} 支付 {amount} 元，剩余现金: {cash}");

            // === 新增代码开始：通知UI更新独立资金显示面板 ===
            if (UIManager.Instance != null)
            {
                UIManager.Instance.UpdateCashDisplay(cash);
            }
            // === 新增代码结束 ===

            // 同时确保游戏主UI也更新（GameManager.UpdateUI() 会处理其他UI）
            if (GameManager.Instance != null)
            {
                GameManager.Instance.UpdateUI();
            }

            return true;
        }
        else
        {
            Debug.LogWarning($"{playerName} 资金不足！需要 {amount} 元但只有 {cash} 元");
            return false;
        }
    }

    // 收钱
    public void ReceiveCash(int amount)
    {
        cash += amount;
        Debug.Log($"{playerName} 收到 {amount} 元，总计: {cash} 元");

        // === 新增代码开始：通知UI更新独立资金显示面板 ===
        if (UIManager.Instance != null)
        {
            UIManager.Instance.UpdateCashDisplay(cash);
        }
        // === 新增代码结束 ===

        // 同时确保游戏主UI也更新
        if (GameManager.Instance != null)
        {
            GameManager.Instance.UpdateUI();
        }
    }

    // 购买地产
    public bool BuyProperty(BoardTile property)
    {
        if (property == null) return false;

        if (property.tileType != BoardTile.TileType.Property &&
            property.tileType != BoardTile.TileType.Railroad &&
            property.tileType != BoardTile.TileType.Utility)
        {
            Debug.LogWarning($"无法购买 {property.tileName}，因为它不是可购买的地产类型");
            return false;
        }

        if (property.ownerPlayer != null)
        {
            Debug.LogWarning($"{property.tileName} 已经有主人了");
            return false;
        }

        if (PayCash(property.propertyPrice))
        {
            property.ownerPlayer = this;
            ownedProperties.Add(property);
            Debug.Log($"{playerName} 成功购买了 {property.tileName}");
            return true;
        }

        return false;
    }

    // 支付租金
    public bool PayRent(int rentAmount, GameObject owner)
    {
        if (PayCash(rentAmount))
        {
            // 把钱给地主
            Player ownerPlayer = owner.GetComponent<Player>();
            if (ownerPlayer != null)
            {
                ownerPlayer.ReceiveCash(rentAmount);
            }
            return true;
        }
        return false;
    }

    // 移动到指定地块
    public void MoveToTile(BoardTile tile, bool teleport = false)
    {
        if (tile == null) return;

        if (playerMovement != null && !teleport)
        {
            // 走步数移动
            int steps = GetStepsToTile(tile);
            if (steps > 0)
            {
                playerMovement.MoveSteps(steps);
            }
        }
        else
        {
            // 瞬移，不播放效果
            transform.position = tile.transform.position + Vector3.up * 0.5f;
            currentTile = tile;
            currentTileIndex = BoardManager.Instance?.allTiles.IndexOf(tile) ?? 0;

            // 触发地块事件
            tile.OnLanded(this);
        }
    }

    // 计算到目标地块的步数
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
            // 需要绕一圈
            return (allTiles.Count - currentIndex) + targetIndex;
        }
        else
        {
            return targetIndex - currentIndex;
        }
    }

    // 获取带有加成的骰子值
    public int GetDiceValueWithBoost(int baseValue)
    {
        if (hasDiceBoost)
        {
            int boostedValue = baseValue + diceBoostValue;
            Debug.Log($"{playerName} 获得骰子加成: {baseValue} + {diceBoostValue} = {boostedValue}");
            return Mathf.Clamp(boostedValue, 1, 12); // 最大12点
        }
        return baseValue;
    }

    // 获取带收入倍率的收入
    public int GetIncomeWithMultiplier(int baseIncome)
    {
        float multiplier = incomeMultiplier;
        int finalIncome = Mathf.RoundToInt(baseIncome * multiplier);
        if (multiplier > 1.0f)
        {
            Debug.Log($"{playerName} 获得收入倍率: {baseIncome} × {multiplier} = {finalIncome}");
        }
        return finalIncome;
    }

    // 添加buff
    public void AddBuff(BoardTile buffSource)
    {
        if (!activeBuffs.Contains(buffSource))
        {
            activeBuffs.Add(buffSource);
            Debug.Log($"{playerName} 获得buff: {buffSource.tileName}");
        }
    }

    // 移除buff
    public void RemoveBuff(BoardTile buffSource)
    {
        if (activeBuffs.Contains(buffSource))
        {
            activeBuffs.Remove(buffSource);
            Debug.Log($"{playerName} 失去buff: {buffSource.tileName}");
        }
    }

    // 检查是否破产
    public bool CheckBankruptcy()
    {
        if (cash < 0)
        {
            isBankrupt = true;
            Debug.Log($"{playerName} 破产了！");
            return true;
        }
        return false;
    }
}