using UnityEngine;

[CreateAssetMenu(fileName = "NewBuffData", menuName = "Game/Buff Data")]
public class BuffData : ScriptableObject
{
    [Header("基本信息")]
    public string buffName = "New Buff";
    public Sprite buffIcon;
    public bool isDebuff = false;
    
    [Header("Buff效果")]
    public BuildingData.BuffEffect effectType = BuildingData.BuffEffect.IncomeMultiplier;
    public float value = 0.1f;
    
    [Header("持续时间")]
    public bool isPermanent = true;
    public float durationSeconds = 0f;
    public int durationRounds = 0;
    
    [Header("描述")]
    [TextArea(2, 4)]
    public string description = "";
    
    [Header("通知消息")]
    public string notificationMessage = "";  // 获得Buff时显示的消息
    
    [Header("来源信息")]
    public string sourceName = "Unknown";
    
    public bool UseRoundTimer()
    {
        return !isPermanent && durationRounds > 0;
    }
    
    public bool UseTimeTimer()
    {
        return !isPermanent && durationSeconds > 0 && durationRounds <= 0;
    }
    
    public float GetDuration()
    {
        if (isPermanent) return 0f;
        return durationSeconds;
    }
    
    public int GetDurationRounds()
    {
        if (isPermanent) return 0;
        return durationRounds;
    }
    
    public string GetEffectName()
    {
        return BuildingData.GetBuffEffectName(effectType);
    }
}