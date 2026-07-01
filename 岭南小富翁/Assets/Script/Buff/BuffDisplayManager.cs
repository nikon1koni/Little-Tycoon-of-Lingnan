﻿﻿﻿﻿﻿﻿﻿using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class BuffDisplayManager : MonoBehaviour
{
    public static BuffDisplayManager Instance { get; private set; }
    
    [Header("Buff ")]
    public GameObject buffIconPrefab;
    public Transform buffContainer;
    public GameObject buffTooltipPrefab;
    public Vector2 tooltipOffset = new Vector2(50, 50);
    
    [Header("Buff 图标")]
    public Sprite moveSpeedIcon;
    public Sprite diceBoostIcon;
    public Sprite incomeMultiplierIcon;
    public Sprite defenseBoostIcon;
    public Sprite luckBoostIcon;
    public Sprite allIncomeBoostIcon;
    public Sprite bankruptIcon;
    public Sprite incomeReductionIcon;
    public Sprite taxReductionIcon;
    public Sprite immuneIcon;
    public Sprite nextRollMultiplierIcon;
    
    [Header("Buff数据配置")]
    public BuffData bankruptBuffData;        // 破产Debuff数据（用于获取图标）
    
    private Dictionary<Player, List<BuffIcon>> playerBuffIcons = new Dictionary<Player, List<BuffIcon>>();
    private Player currentPlayer;
    
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
    
    private void Start()
    {
        UpdateBuffDisplay();
    }
    
    private void Update()
    {
        // 
        CheckForUpdate();
    }
    
    public void SetCurrentPlayer(Player player)
    {
        if (currentPlayer != player)
        {
            currentPlayer = player;
            UpdateBuffDisplay();
        }
    }
    
    public void UpdateBuffDisplay()
    {
        if (currentPlayer == null || BuffSystem.Instance == null)
            return;
        
        ClearBuffIcons();
        
        List<BuffSystem.Buff> buffs = BuffSystem.Instance.GetPlayerBuffs(currentPlayer);
        foreach (BuffSystem.Buff buff in buffs)
        {
            CreateBuffIcon(buff);
        }
    }
    
    private void CreateBuffIcon(BuffSystem.Buff buff)
    {
        Debug.Log($"BuffDisplayManager: CreateBuffIcon called, buff={buff != null}, buffTooltipPrefab={buffTooltipPrefab != null}");
        
        if (buffIconPrefab == null || buffContainer == null) 
        {
            Debug.Log($"BuffDisplayManager: CreateBuffIcon failed - prefab={buffIconPrefab != null}, container={buffContainer != null}");
            return;
        }
        
        GameObject iconObj = Instantiate(buffIconPrefab, buffContainer);
        BuffIcon buffIcon = iconObj.GetComponent<BuffIcon>();
        
        if (buffIcon != null)
        {
            buffIcon.tooltipPrefab = buffTooltipPrefab;
            buffIcon.tooltipOffset = tooltipOffset;
            Debug.Log($"BuffDisplayManager: tooltipOffset set to {tooltipOffset}");
            Sprite icon = GetBuffIcon(buff.effectType);
            buffIcon.Initialize(buff, icon);
            
            if (!playerBuffIcons.ContainsKey(currentPlayer))
            {
                playerBuffIcons[currentPlayer] = new List<BuffIcon>();
            }
            playerBuffIcons[currentPlayer].Add(buffIcon);
        }
    }
    
    private void ClearBuffIcons()
    {
        if (buffContainer != null)
        {
            foreach (Transform child in buffContainer)
            {
                Destroy(child.gameObject);
            }
        }
        
        if (currentPlayer != null && playerBuffIcons.ContainsKey(currentPlayer))
        {
            playerBuffIcons[currentPlayer].Clear();
        }
    }
    
    private void CheckForUpdate()
    {
        if (GameManager.Instance != null && GameManager.Instance.currentPlayer != currentPlayer)
        {
            SetCurrentPlayer(GameManager.Instance.currentPlayer);
        }
    }
    
    private Sprite GetBuffIcon(BuildingData.BuffEffect effectType)
    {
        // 优先使用 BuffData 中的图标配置
        switch (effectType)
        {
            case BuildingData.BuffEffect.Bankrupt:
                if (bankruptBuffData != null && bankruptBuffData.buffIcon != null)
                {
                    return bankruptBuffData.buffIcon;
                }
                return bankruptIcon;
            case BuildingData.BuffEffect.MoveSpeedBoost:
                return moveSpeedIcon;
            case BuildingData.BuffEffect.DiceBoost:
                return diceBoostIcon;
            case BuildingData.BuffEffect.IncomeMultiplier:
                return incomeMultiplierIcon;
            case BuildingData.BuffEffect.DefenseBoost:
                return defenseBoostIcon;
            case BuildingData.BuffEffect.LuckBoost:
                return luckBoostIcon;
            case BuildingData.BuffEffect.AllIncomeBoost:
                return allIncomeBoostIcon;
            case BuildingData.BuffEffect.IncomeReduction:
                return incomeReductionIcon;
            case BuildingData.BuffEffect.TaxReduction:
                return taxReductionIcon;
            case BuildingData.BuffEffect.Immune:
                return immuneIcon;
            case BuildingData.BuffEffect.NextRollMultiplier:
                return nextRollMultiplierIcon;
            default:
                return null;
        }
    }
}
