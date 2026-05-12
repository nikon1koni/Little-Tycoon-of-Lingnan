using UnityEngine;

[CreateAssetMenu(fileName = "NewBuildingData", menuName = "Building/Building Data")]
public class BuildingData : ScriptableObject
{
    [Header("��������")]
    public BoardTile.BuildingType buildingType = BoardTile.BuildingType.None;

    [Header("�ȼ�ϵͳ")]
    public bool isFinalLevel = false; // �Ƿ�Ϊ���յȼ�

    [Header("Buff��ֵ����")]
    public float[] buffValues; // Buff��ֵ���飬���ڲ�ͬ�ȼ���Buffֵ

    [Header("����������Ϣ")]
    public string buildingName = "����";
    public int purchasePrice = 100;
    public int upgradePrice = 50;
    public int minTileScale = 1;
    public int maxTileScale = 4;
    public Scale requiredScale = Scale.Small;

    [Header("��������")]
    public BuildingFunctionType functionType = BuildingFunctionType.Income;

    [Header("���빦������")]
    public int baseIncome = 10; // ��������
    public float incomeGrowthRate = 1.2f; // ÿ������������

    [Header("Buff��������")]
    public BuffEffect buffEffect = BuffEffect.IncomeMultiplier;
    public float baseBuffValue = 0.1f;
    public float buffGrowthRate = 1.1f;
    public float buffDuration = 10f; // Buff����ʱ�䣨�룩

    [Header("��Ϲ�������")]
    public int mixedBaseIncome = 5; // ��Ϲ��ܵ�����
    public float mixedIncomeGrowthRate = 1.1f;

    [Header("�Ӿ�")]
    public Sprite buildingIcon;
    public GameObject buildingPrefab;
    public BuildingData nextLevelBuilding;

    [Header("����")]
    [TextArea(3, 5)]
    public string description = "��������";

    // ������ģö��
    public enum Scale
    {
        Small = 1,
        Medium = 2,
        Large = 3,
        ExtraLarge = 4
    }

    // ������������
    public enum BuildingFunctionType
    {
        Income,     // ����
        Buff,       // ����Ч��
        Mixed       // ���
    }

    // BuffЧ��ö��
    public enum BuffEffect
    {
        MoveSpeedBoost,     // �ƶ��ٶ�
        DiceBoost,          // ���Ӽӳ�
        IncomeMultiplier,   // ���뱶��
        DefenseBoost,       // �����ӳ�
        LuckBoost,          // ���˼ӳ�
        AllIncomeBoost      // ȫ����ӳ�
    }

    // ��ȡ��������
    public int GetIncomeAmount(int level)
    {
        switch (functionType)
        {
            case BuildingFunctionType.Income:
                return CalculateIncome(baseIncome, incomeGrowthRate, level);

            case BuildingFunctionType.Mixed:
                return CalculateIncome(mixedBaseIncome, mixedIncomeGrowthRate, level);

            default:
                return 0;
        }
    }

    // ��ȡBuff��ֵ
    public float GetBuffValue(int level)
    {
        if (functionType == BuildingFunctionType.Buff || functionType == BuildingFunctionType.Mixed)
        {
            return baseBuffValue * Mathf.Pow(buffGrowthRate, level - 1);
        }
        return 0f;
    }

    // ��������
    private int CalculateIncome(int baseAmount, float growthRate, int level)
    {
        if (level <= 1) return baseAmount;
        return Mathf.RoundToInt(baseAmount * Mathf.Pow(growthRate, level - 1));
    }

    // ���ؿ��ģ�Ƿ�ƥ��
    public bool CheckTileScale(int tileScale)
    {
        return tileScale >= minTileScale && tileScale <= maxTileScale;
    }

    // ��ȡ��������
    public string GetDescription(int level = 1)
    {
        string desc = $"{buildingName}\n";
        desc += $"�۸�: {purchasePrice}���\n";
        desc += $"��ģҪ��: {minTileScale}-{maxTileScale}\n";

        switch (functionType)
        {
            case BuildingFunctionType.Income:
                desc += $"����: ÿ������ {GetIncomeAmount(1)} �������\n";
                if (level > 1)
                {
                    desc += $"��ǰ�ȼ�({level})����: {GetIncomeAmount(level)} ���";
                }
                break;

            case BuildingFunctionType.Buff:
                desc += $"����: �ṩ {GetBuffEffectName(buffEffect)} �ӳ�\n";
                desc += $"����ֵ: {baseBuffValue * 100}%\n";
                if (buffDuration > 0)
                {
                    desc += $"����ʱ��: {buffDuration}��";
                }
                break;

            case BuildingFunctionType.Mixed:
                desc += $"����: ���(����+Buff)\n";
                desc += $"����: {GetIncomeAmount(1)} ���\n";
                desc += $"Buff: {GetBuffEffectName(buffEffect)} {baseBuffValue * 100}%";
                break;
        }

        if (!string.IsNullOrEmpty(description))
        {
            desc += $"\n\n{description}";
        }

        return desc;
    }

    // ��ȡBuffЧ������
    public static string GetBuffEffectName(BuffEffect effect)
    {
        switch (effect)
        {
            case BuffEffect.MoveSpeedBoost: return "�ƶ��ٶ�";
            case BuffEffect.DiceBoost: return "���Ӽӳ�";
            case BuffEffect.IncomeMultiplier: return "���뱶��";
            case BuffEffect.DefenseBoost: return "�����ӳ�";
            case BuffEffect.LuckBoost: return "���˼ӳ�";
            case BuffEffect.AllIncomeBoost: return "ȫ����ӳ�";
            default: return "δ֪Ч��";
        }
    }
}