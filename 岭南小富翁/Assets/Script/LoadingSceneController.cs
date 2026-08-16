using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using UnityEngine.EventSystems;

/// <summary>
/// 加载场景控制器：在 start 与 New 场景之间显示加载进度条，
/// 异步加载完成后提示"点击继续"，玩家点击任意按键即进入 New 场景。
/// 运行时自动构建/修复 Canvas、进度条与文字，无需依赖 Inspector 配置。
/// </summary>
public class LoadingSceneController : MonoBehaviour
{
    [Header("目标场景")]
    [Tooltip("加载完成后要进入的场景名称")]
    public string targetSceneName = "New";

    [Header("UI 引用（可选，为空则自动生成）")]
    public Image progressBar;
    public Text progressText;
    public GameObject continuePrompt;
    public GameObject loadingPrompt;

    [Header("进度条平滑过渡")]
    public float progressSmoothSpeed = 5f;

    [Header("最小显示时长")]
    [Tooltip("加载界面最少显示的秒数，避免场景太小一闪而过")]
    public float minimumDisplaySeconds = 2f;

    private AsyncOperation loadOperation;
    private bool isLoadComplete = false;
    private float displayedProgress = 0f;
    private float startTime;
    private Sprite whiteSprite;
    private Font defaultFont;

    void Awake()
    {
        EnsureGraphicsAssets();
        EnsureUISetup();
    }

    void Start()
    {
        startTime = Time.time;
        if (continuePrompt != null) continuePrompt.SetActive(false);
        if (loadingPrompt != null) loadingPrompt.SetActive(true);
        if (progressBar != null) progressBar.fillAmount = 0f;
        if (progressText != null) progressText.text = "0%";

        StartCoroutine(LoadTargetSceneAsync());
    }

    // 确保运行时存在基础美术资源：白色 Sprite + 字体
    void EnsureGraphicsAssets()
    {
        if (whiteSprite == null)
        {
            Texture2D tex = new Texture2D(4, 4, TextureFormat.RGBA32, false);
            Color32[] px = new Color32[16];
            for (int i = 0; i < 16; i++) px[i] = new Color32(255, 255, 255, 255);
            tex.SetPixels32(px);
            tex.Apply();
            whiteSprite = Sprite.Create(tex, new Rect(0, 0, 4, 4), new Vector2(0.5f, 0.5f), 100f);
        }

        if (defaultFont == null)
        {
            defaultFont = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            if (defaultFont == null)
            {
                Font[] fonts = Resources.FindObjectsOfTypeAll<Font>();
                if (fonts != null && fonts.Length > 0) defaultFont = fonts[0];
            }
        }
    }

    // 自动创建 Canvas 与 UI 子元素，并补齐引用
    void EnsureUISetup()
    {
        Camera cam = Camera.main;
        if (cam == null) cam = FindObjectOfType<Camera>();
        if (cam != null)
        {
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0.06f, 0.06f, 0.08f, 1f);
        }

        Canvas canvas = FindObjectOfType<Canvas>();
        if (canvas == null)
        {
            GameObject canvasGO = new GameObject("Canvas", typeof(RectTransform));
            canvas = canvasGO.AddComponent<Canvas>();
            canvasGO.AddComponent<CanvasScaler>();
            canvasGO.AddComponent<GraphicRaycaster>();
        }

        // Screen Space Overlay —— 最简单可靠的 UI 渲染方式
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 100;

        CanvasScaler scaler = canvas.GetComponent<CanvasScaler>();
        if (scaler != null)
        {
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.matchWidthOrHeight = 0.5f;
        }

        if (FindObjectOfType<EventSystem>() == null)
        {
            GameObject esGO = new GameObject("EventSystem");
            esGO.AddComponent<EventSystem>();
            esGO.AddComponent<StandaloneInputModule>();
        }

        Transform canvasTf = canvas.transform;

        // 1. 背景：铺满整个 Canvas
        Image bgImg = FindChildOrCreate<Image>(canvasTf, "Background", stretch: true);
        bgImg.sprite = whiteSprite;
        bgImg.type = Image.Type.Simple;
        bgImg.color = new Color(0.06f, 0.06f, 0.08f, 1f);

        // 2. 进度条（居中偏下，基于 1920x1080 参考分辨率）
        Image bar = progressBar;
        if (bar == null || bar.transform.parent != canvasTf)
        {
            bar = FindChildOrCreate<Image>(canvasTf, "ProgressBar", stretch: false);
            RectTransform brt = bar.rectTransform;
            brt.anchorMin = new Vector2(0.5f, 0.5f);
            brt.anchorMax = new Vector2(0.5f, 0.5f);
            brt.pivot = new Vector2(0.5f, 0.5f);
            brt.anchoredPosition = new Vector2(0, -200);
            brt.sizeDelta = new Vector2(1000, 40);
            progressBar = bar;
        }
        bar.sprite = whiteSprite;
        bar.type = Image.Type.Filled;
        bar.fillMethod = Image.FillMethod.Horizontal;
        bar.fillOrigin = 0;
        bar.color = new Color(1f, 0.75f, 0.25f, 1f);
        bar.fillAmount = 0f;

        // 3. 进度百分比文本（进度条下方）
        Text pt = progressText;
        if (pt == null || pt.transform.parent != canvasTf)
        {
            pt = FindChildOrCreate<Text>(canvasTf, "ProgressText", stretch: false);
            RectTransform prt = pt.rectTransform;
            prt.anchorMin = new Vector2(0.5f, 0.5f);
            prt.anchorMax = new Vector2(0.5f, 0.5f);
            prt.pivot = new Vector2(0.5f, 0.5f);
            prt.anchoredPosition = new Vector2(0, -260);
            prt.sizeDelta = new Vector2(300, 60);
            progressText = pt;
        }
        pt.font = defaultFont;
        pt.fontSize = 40;
        pt.alignment = TextAnchor.MiddleCenter;
        pt.color = Color.white;
        pt.text = "0%";
        pt.horizontalOverflow = HorizontalWrapMode.Overflow;
        pt.verticalOverflow = VerticalWrapMode.Overflow;

        // 4. 加载提示（上半部居中）
        Text lpText;
        if (loadingPrompt == null || loadingPrompt.transform.parent != canvasTf)
        {
            GameObject go = FindChildOrCreateGO(canvasTf, "LoadingPrompt");
            lpText = EnsureComponent<Text>(go);
            RectTransform lrt = lpText.rectTransform;
            lrt.anchorMin = new Vector2(0.5f, 0.5f);
            lrt.anchorMax = new Vector2(0.5f, 0.5f);
            lrt.pivot = new Vector2(0.5f, 0.5f);
            lrt.anchoredPosition = new Vector2(0, 100);
            lrt.sizeDelta = new Vector2(600, 80);
            loadingPrompt = go;
        }
        else
        {
            lpText = loadingPrompt.GetComponent<Text>();
            if (lpText == null) lpText = loadingPrompt.AddComponent<Text>();
        }
        lpText.font = defaultFont;
        lpText.fontSize = 50;
        lpText.alignment = TextAnchor.MiddleCenter;
        lpText.color = Color.white;
        lpText.text = "加载中...";
        lpText.horizontalOverflow = HorizontalWrapMode.Overflow;
        lpText.verticalOverflow = VerticalWrapMode.Overflow;

        // 5. 继续提示（居中，默认隐藏）
        Text cpText;
        if (continuePrompt == null || continuePrompt.transform.parent != canvasTf)
        {
            GameObject go = FindChildOrCreateGO(canvasTf, "ContinuePrompt");
            cpText = EnsureComponent<Text>(go);
            RectTransform crt = cpText.rectTransform;
            crt.anchorMin = new Vector2(0.5f, 0.5f);
            crt.anchorMax = new Vector2(0.5f, 0.5f);
            crt.pivot = new Vector2(0.5f, 0.5f);
            crt.anchoredPosition = new Vector2(0, 0);
            crt.sizeDelta = new Vector2(800, 100);
            continuePrompt = go;
        }
        else
        {
            cpText = continuePrompt.GetComponent<Text>();
            if (cpText == null) cpText = continuePrompt.AddComponent<Text>();
        }
        cpText.font = defaultFont;
        cpText.fontSize = 60;
        cpText.alignment = TextAnchor.MiddleCenter;
        cpText.color = new Color(1f, 0.9f, 0.2f, 1f);
        cpText.text = "点击继续";
        cpText.horizontalOverflow = HorizontalWrapMode.Overflow;
        cpText.verticalOverflow = VerticalWrapMode.Overflow;
    }

    T FindChildOrCreate<T>(Transform parent, string childName, bool stretch) where T : Component
    {
        GameObject go = FindChildOrCreateGO(parent, childName);
        T comp = go.GetComponent<T>();
        if (comp == null) comp = go.AddComponent<T>();
        if (stretch)
        {
            RectTransform rt = go.GetComponent<RectTransform>();
            if (rt != null)
            {
                rt.anchorMin = Vector2.zero;
                rt.anchorMax = Vector2.one;
                rt.offsetMin = Vector2.zero;
                rt.offsetMax = Vector2.zero;
                rt.pivot = new Vector2(0.5f, 0.5f);
            }
        }
        return comp;
    }

    GameObject FindChildOrCreateGO(Transform parent, string name)
    {
        Transform tf = parent.Find(name);
        if (tf != null) return tf.gameObject;
        GameObject go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        return go;
    }

    T EnsureComponent<T>(GameObject go) where T : Component
    {
        T c = go.GetComponent<T>();
        if (c == null) c = go.AddComponent<T>();
        return c;
    }

    IEnumerator LoadTargetSceneAsync()
    {
        loadOperation = SceneManager.LoadSceneAsync(targetSceneName);
        loadOperation.allowSceneActivation = false;

        bool canShowContinue = false;

        while (!loadOperation.isDone)
        {
            float elapsed = Time.time - startTime;
            float loadProgress = Mathf.Clamp01(loadOperation.progress / 0.9f);

            // 进度上限：加载进度与最小显示时间进度中较小的那个，保证界面至少显示 minimumDisplaySeconds
            float minDisplayProgress = Mathf.Clamp01(elapsed / minimumDisplaySeconds);
            float targetProgress = Mathf.Min(loadProgress, minDisplayProgress);

            displayedProgress = Mathf.Lerp(displayedProgress, targetProgress, Time.deltaTime * progressSmoothSpeed);
            if (displayedProgress > targetProgress) displayedProgress = targetProgress;
            UpdateProgressUI(displayedProgress);

            // 只有两个条件都满足才显示"点击继续"
            if (loadOperation.progress >= 0.9f
                && elapsed >= minimumDisplaySeconds
                && !canShowContinue)
            {
                canShowContinue = true;
                isLoadComplete = true;
                displayedProgress = 1f;
                UpdateProgressUI(1f);
                ShowContinuePrompt();
            }
            yield return null;
        }
    }

    void UpdateProgressUI(float progress)
    {
        if (progressBar != null) progressBar.fillAmount = progress;
        if (progressText != null) progressText.text = Mathf.RoundToInt(progress * 100f).ToString() + "%";
    }

    void ShowContinuePrompt()
    {
        if (loadingPrompt != null) loadingPrompt.SetActive(false);
        if (continuePrompt != null) continuePrompt.SetActive(true);
        Debug.Log("[Loading] 场景加载完成，点击任意键继续 -> " + targetSceneName);
    }

    void Update()
    {
        if (!isLoadComplete) return;
        if (Input.anyKeyDown || Input.GetMouseButtonDown(0) || Input.GetMouseButtonDown(1) || Input.GetMouseButtonDown(2))
        {
            ProceedToTargetScene();
        }
    }

    public void OnContinueButtonClicked()
    {
        ProceedToTargetScene();
    }

    void ProceedToTargetScene()
    {
        if (loadOperation != null && !loadOperation.allowSceneActivation)
        {
            Debug.Log("[Loading] 进入目标场景: " + targetSceneName);
            loadOperation.allowSceneActivation = true;
        }
    }
}
