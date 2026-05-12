using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class MainMenuManager : MonoBehaviour
{
    [Header("��������")]
    [SerializeField] private string gameSceneName = "New"; // Ҫ���ص���Ϸ��������

    [Header("��ť���ã���ѡ�������ק���Զ����ң�")]
    [SerializeField] private Button startButton;
    [SerializeField] private Button quitButton;

    private void Start()
    {
        // ���δ�ֶ���ק��ť���Զ��������Ʋ���
        if (startButton == null)
            startButton = GameObject.Find("StartButton")?.GetComponent<Button>();
        if (quitButton == null)
            quitButton = GameObject.Find("QuitButton")?.GetComponent<Button>();

        // �󶨼����¼�
        if (startButton != null)
            startButton.onClick.AddListener(OnStartGame);
        else
            Debug.LogError("δ�ҵ���ʼ��ť����ȷ����ť����Ϊ StartButton ���ֶ���ק��ֵ��");

        if (quitButton != null)
            quitButton.onClick.AddListener(OnQuitGame);
        else
            Debug.LogError("δ�ҵ��˳���ť����ȷ����ť����Ϊ QuitButton ���ֶ���ק��ֵ��");
    }

    /// <summary>
    /// ��ʼ��Ϸ������ָ������
    /// </summary>
    public void OnStartGame()
    {
        if (!string.IsNullOrEmpty(gameSceneName))
            SceneManager.LoadScene(gameSceneName);
        else
            Debug.LogError("����д��Ϸ�������ƣ�");
    }

    /// <summary>
    /// �˳���Ϸ����������
    /// </summary>
    public void OnQuitGame()
    {
        Debug.Log("�˳���Ϸ");

#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }
}