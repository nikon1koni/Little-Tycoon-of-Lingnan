// BuildingDataConfig.cs - 用于初始化建筑配置链
using UnityEngine;
using System.Collections.Generic;

public class BuildingDataConfig : MonoBehaviour
{
    [Header("建筑数据资源")]
    public List<BuildingData> allBuildingData = new List<BuildingData>();

    [Header("建筑升级链")]
    public BuildingData smallHouse;      // 小房屋
    public BuildingData mediumHouse;     // 中房屋
    public BuildingData largeHouse;      // 大房屋

    public BuildingData smallShop;       // 小商店
    public BuildingData mediumShop;      // 中商店
    public BuildingData largeShop;       // 大商店

    public BuildingData smallPostHouse;  // 小驿站（速度）
    public BuildingData mediumPostHouse; // 中驿站
    public BuildingData largePostHouse;  // 大驿站

    public BuildingData smallTemple;     // 小庙（幸运）
    public BuildingData mediumTemple;    // 中庙
    public BuildingData largeTemple;     // 大庙

    void Start()
    {
        SetupBuildingChains();
    }

    void SetupBuildingChains()
    {
        // 房屋链
        smallHouse.nextLevelBuilding = mediumHouse;
        smallHouse.isFinalLevel = false;

        mediumHouse.nextLevelBuilding = largeHouse;
        mediumHouse.isFinalLevel = false;

        largeHouse.nextLevelBuilding = null;
        largeHouse.isFinalLevel = true;

        // 商店链
        smallShop.nextLevelBuilding = mediumShop;
        smallShop.isFinalLevel = false;

        mediumShop.nextLevelBuilding = largeShop;
        mediumShop.isFinalLevel = false;

        largeShop.nextLevelBuilding = null;
        largeShop.isFinalLevel = true;

        // 驿站链（速度buff）
        smallPostHouse.nextLevelBuilding = mediumPostHouse;
        smallPostHouse.isFinalLevel = false;
        smallPostHouse.functionType = BuildingData.BuildingFunctionType.Buff;
        smallPostHouse.buffEffect = BuildingData.BuffEffect.MoveSpeedBoost;
        smallPostHouse.buffValues = new float[] { 0.1f, 0.15f, 0.2f };

        mediumPostHouse.nextLevelBuilding = largePostHouse;
        mediumPostHouse.isFinalLevel = false;
        mediumPostHouse.functionType = BuildingData.BuildingFunctionType.Buff;
        mediumPostHouse.buffEffect = BuildingData.BuffEffect.MoveSpeedBoost;
        mediumPostHouse.buffValues = new float[] { 0.15f, 0.2f, 0.25f };

        largePostHouse.nextLevelBuilding = null;
        largePostHouse.isFinalLevel = true;
        largePostHouse.functionType = BuildingData.BuildingFunctionType.Buff;
        largePostHouse.buffEffect = BuildingData.BuffEffect.MoveSpeedBoost;
        largePostHouse.buffValues = new float[] { 0.2f, 0.25f, 0.3f };

        // 庙链（幸运buff）
        smallTemple.nextLevelBuilding = mediumTemple;
        smallTemple.isFinalLevel = false;
        smallTemple.functionType = BuildingData.BuildingFunctionType.Buff;
        smallTemple.buffEffect = BuildingData.BuffEffect.LuckBoost;
        smallTemple.buffValues = new float[] { 0.1f, 0.15f, 0.2f };

        mediumTemple.nextLevelBuilding = largeTemple;
        mediumTemple.isFinalLevel = false;
        mediumTemple.functionType = BuildingData.BuildingFunctionType.Buff;
        mediumTemple.buffEffect = BuildingData.BuffEffect.LuckBoost;
        mediumTemple.buffValues = new float[] { 0.15f, 0.2f, 0.25f };

        largeTemple.nextLevelBuilding = null;
        largeTemple.isFinalLevel = true;
        largeTemple.functionType = BuildingData.BuildingFunctionType.Buff;
        largeTemple.buffEffect = BuildingData.BuffEffect.LuckBoost;
        largeTemple.buffValues = new float[] { 0.2f, 0.25f, 0.3f };

        Debug.Log("建筑升级链配置完成");
    }
}