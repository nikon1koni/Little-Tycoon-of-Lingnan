// BuildingDataConfig.cs - ���ڳ�ʼ������������
using UnityEngine;
using System.Collections.Generic;

public class BuildingDataConfig : MonoBehaviour
{
    [Header("����������Դ")]
    public List<BuildingData> allBuildingData = new List<BuildingData>();

    [Header("����������")]
    public BuildingData smallHouse;      // С����
    public BuildingData mediumHouse;     // �з���
    public BuildingData largeHouse;      // ����

    public BuildingData smallShop;       // С�̵�
    public BuildingData mediumShop;      // ���̵�
    public BuildingData largeShop;       // ���̵�

    public BuildingData smallPostHouse;  // С��վ���ٶȣ�
    public BuildingData mediumPostHouse; // ����վ
    public BuildingData largePostHouse;  // ����վ

    public BuildingData smallTemple;     // С������ˣ�
    public BuildingData mediumTemple;    // ����
    public BuildingData largeTemple;     // ����

    void Start()
    {
        SetupBuildingChains();
    }

    void SetupBuildingChains()
    {
        // ������
        smallHouse.nextLevelBuilding = mediumHouse;
        smallHouse.isFinalLevel = false;

        mediumHouse.nextLevelBuilding = largeHouse;
        mediumHouse.isFinalLevel = false;

        largeHouse.nextLevelBuilding = null;
        largeHouse.isFinalLevel = true;

        // �̵���
        smallShop.nextLevelBuilding = mediumShop;
        smallShop.isFinalLevel = false;

        mediumShop.nextLevelBuilding = largeShop;
        mediumShop.isFinalLevel = false;

        largeShop.nextLevelBuilding = null;
        largeShop.isFinalLevel = true;

        // ��վ�����ٶ�buff��
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

        // ����������buff��
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

        Debug.Log("�����������������");
    }
}