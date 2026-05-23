using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class BuffDisplayManager : MonoBehaviour
{
    public static BuffDisplayManager Instance { get; private set; }
    
    [Header("Buff ???????")]
    public GameObject buffIconPrefab;
    public Transform buffContainer;
    public GameObject buffTooltipPrefab;
    
    [Header("Buff ??????")]
    public Sprite moveSpeedIcon;
    public Sprite diceBoostIcon;
    public Sprite incomeMultiplierIcon;
    public Sprite defenseBoostIcon;
    public Sprite luckBoostIcon;
    public Sprite allIncomeBoostIcon;
    
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
        // ??????????????????
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
        if (buffIconPrefab == null || buffContainer == null) return;
        
        GameObject iconObj = Instantiate(buffIconPrefab, buffContainer);
        BuffIcon buffIcon = iconObj.GetComponent<BuffIcon>();
        
        if (buffIcon != null)
        {
            buffIcon.tooltipPrefab = buffTooltipPrefab;
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
        switch (effectType)
        {
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
            default:
                return null;
        }
    }
}
