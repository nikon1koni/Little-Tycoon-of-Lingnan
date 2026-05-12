using UnityEngine;
using UnityEditor;

public class DiceFaceCalibrator : MonoBehaviour
{
    [Header("骰子对象")]
    public GameObject dice3D;

    [Header("当前预览点数")]
    [Range(1, 6)]
    public int previewFace = 1;

    [Header("各点旋转角度")]
    public Vector3 face1Rotation = Vector3.zero;
    public Vector3 face2Rotation = new Vector3(90, 0, 0);
    public Vector3 face3Rotation = new Vector3(0, 0, 90);
    public Vector3 face4Rotation = new Vector3(0, 0, -90);
    public Vector3 face5Rotation = new Vector3(-90, 0, 0);
    public Vector3 face6Rotation = new Vector3(0, 180, 0);

    void OnValidate()
    {
        if (dice3D != null && Application.isEditor && !Application.isPlaying)
        {
            PreviewFace(previewFace);
        }
    }

    public void PreviewFace(int face)
    {
        if (dice3D == null) return;

        Vector3 rotation = Vector3.zero;
        switch (face)
        {
            case 1: rotation = face1Rotation; break;
            case 2: rotation = face2Rotation; break;
            case 3: rotation = face3Rotation; break;
            case 4: rotation = face4Rotation; break;
            case 5: rotation = face5Rotation; break;
            case 6: rotation = face6Rotation; break;
        }

        dice3D.transform.localRotation = Quaternion.Euler(rotation);
    }
}

#if UNITY_EDITOR
[CustomEditor(typeof(DiceFaceCalibrator))]
public class DiceFaceCalibratorEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        DiceFaceCalibrator calibrator = (DiceFaceCalibrator)target;

        GUILayout.Space(10);
        GUILayout.Label("校准工具", EditorStyles.boldLabel);

        // 记录当前旋转的按钮
        for (int i = 1; i <= 6; i++)
        {
            if (GUILayout.Button($"记录当前旋转为 {i} 点"))
            {
                RecordCurrentRotation(calibrator, i);
            }
        }

        GUILayout.Space(10);

        if (GUILayout.Button("复制配置到Dice3DController"))
        {
            CopyToDiceController(calibrator);
        }

        GUILayout.Space(10);
        GUILayout.Label("使用说明：", EditorStyles.wordWrappedLabel);
        GUILayout.Label("1. 在场景中手动旋转骰子到想要的朝向", EditorStyles.wordWrappedLabel);
        GUILayout.Label("2. 点击对应点数的'记录当前旋转'按钮", EditorStyles.wordWrappedLabel);
        GUILayout.Label("3. 用previewFace验证每个面是否正确", EditorStyles.wordWrappedLabel);
        GUILayout.Label("4. 重复直到所有6个面都设置好", EditorStyles.wordWrappedLabel);
        GUILayout.Label("5. 点击'复制配置到Dice3DController'保存", EditorStyles.wordWrappedLabel);
    }

    void RecordCurrentRotation(DiceFaceCalibrator calibrator, int face)
    {
        if (calibrator.dice3D == null)
        {
            Debug.LogError("请先设置dice3D对象！");
            return;
        }

        Vector3 currentRotation = calibrator.dice3D.transform.localRotation.eulerAngles;

        // 记录到对应的字段
        SerializedObject so = new SerializedObject(calibrator);
        so.FindProperty($"face{face}Rotation").vector3Value = currentRotation;
        so.ApplyModifiedProperties();

        Debug.Log($"已记录 {face} 点的旋转: {currentRotation}");
    }

    void CopyToDiceController(DiceFaceCalibrator calibrator)
    {
        Dice3DController controller = calibrator.GetComponent<Dice3DController>();
        if (controller == null)
        {
            Debug.LogError("请在同一个物体上添加Dice3DController组件！");
            EditorUtility.DisplayDialog("错误", "请在同一个物体上添加Dice3DController组件！", "确定");
            return;
        }

        // 直接复制旋转值
        SerializedObject controllerSO = new SerializedObject(controller);
        controllerSO.FindProperty("face1Rotation").vector3Value = calibrator.face1Rotation;
        controllerSO.FindProperty("face2Rotation").vector3Value = calibrator.face2Rotation;
        controllerSO.FindProperty("face3Rotation").vector3Value = calibrator.face3Rotation;
        controllerSO.FindProperty("face4Rotation").vector3Value = calibrator.face4Rotation;
        controllerSO.FindProperty("face5Rotation").vector3Value = calibrator.face5Rotation;
        controllerSO.FindProperty("face6Rotation").vector3Value = calibrator.face6Rotation;
        controllerSO.ApplyModifiedProperties();

        Debug.Log("配置已成功复制到Dice3DController！");
        EditorUtility.DisplayDialog("成功", "配置已成功复制到Dice3DController！", "确定");
    }
}
#endif
