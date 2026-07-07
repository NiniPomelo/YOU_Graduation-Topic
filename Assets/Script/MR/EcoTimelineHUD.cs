using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class EcoTimelineHUD : MonoBehaviour
{
    public static EcoTimelineHUD Instance;

    [Header("Follow")]
    public Vector3 viewportAnchor = new Vector3(0.5f, 0.70f, 1.8f);
    public float followSmooth = 12f;
    public float rotationSmooth = 16f;

    [Header("Layout")]
    public Vector2 panelSize = new Vector2(720f, 88f);
    public float canvasScale = 0.0016f;

    [Header("Colors")]
    public Color panelColor = new Color(0.06f, 0.08f, 0.075f, 0.72f);
    public Color progressBackColor = new Color(1f, 1f, 1f, 0.18f);
    public Color progressFillColor = new Color(0.42f, 0.82f, 0.64f, 0.92f);
    public Color textColor = new Color(0.94f, 0.98f, 0.95f, 1f);

    private Canvas canvas;
    private RectTransform panelRect;
    private RectTransform progressFillRect;
    private TMP_Text stageText;
    private TMP_Text yearText;
    private TMP_Text timeText;
    private Camera targetCamera;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            BuildHud();
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void LateUpdate()
    {
        FindCameraIfNeeded();

        if (targetCamera == null || GameTimer.Instance == null)
        {
            if (canvas != null)
                canvas.enabled = false;
            return;
        }

        if (canvas != null && !canvas.enabled)
            canvas.enabled = true;

        FollowCamera();
        UpdateContent();
    }

    private void BuildHud()
    {
        GameObject canvasObject = new GameObject("EcoTimelineHUDCanvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        canvasObject.transform.SetParent(transform, false);

        canvas = canvasObject.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.WorldSpace;
        canvas.sortingOrder = 50;

        RectTransform canvasRect = canvasObject.GetComponent<RectTransform>();
        canvasRect.sizeDelta = panelSize;
        canvasRect.localScale = Vector3.one * canvasScale;

        CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();
        scaler.dynamicPixelsPerUnit = 10f;

        GameObject panelObject = new GameObject("TimelinePanel", typeof(RectTransform), typeof(Image));
        panelObject.transform.SetParent(canvasObject.transform, false);
        panelRect = panelObject.GetComponent<RectTransform>();
        panelRect.anchorMin = new Vector2(0.5f, 0.5f);
        panelRect.anchorMax = new Vector2(0.5f, 0.5f);
        panelRect.pivot = new Vector2(0.5f, 0.5f);
        panelRect.anchoredPosition = Vector2.zero;
        panelRect.sizeDelta = panelSize;

        Image panelImage = panelObject.GetComponent<Image>();
        panelImage.color = panelColor;

        stageText = CreateText("StageText", panelObject.transform, new Vector2(-238f, 12f), new Vector2(240f, 42f), 22, TextAlignmentOptions.Left);
        yearText = CreateText("YearText", panelObject.transform, new Vector2(0f, 14f), new Vector2(250f, 42f), 26, TextAlignmentOptions.Center);
        timeText = CreateText("TimeText", panelObject.transform, new Vector2(255f, 12f), new Vector2(180f, 42f), 22, TextAlignmentOptions.Right);

        GameObject progressBack = new GameObject("ProgressBack", typeof(RectTransform), typeof(Image));
        progressBack.transform.SetParent(panelObject.transform, false);
        RectTransform backRect = progressBack.GetComponent<RectTransform>();
        backRect.anchorMin = new Vector2(0.5f, 0.5f);
        backRect.anchorMax = new Vector2(0.5f, 0.5f);
        backRect.pivot = new Vector2(0.5f, 0.5f);
        backRect.anchoredPosition = new Vector2(0f, -24f);
        backRect.sizeDelta = new Vector2(650f, 10f);
        progressBack.GetComponent<Image>().color = progressBackColor;

        GameObject progressFill = new GameObject("ProgressFill", typeof(RectTransform), typeof(Image));
        progressFill.transform.SetParent(progressBack.transform, false);
        progressFillRect = progressFill.GetComponent<RectTransform>();
        progressFillRect.anchorMin = new Vector2(0f, 0.5f);
        progressFillRect.anchorMax = new Vector2(0f, 0.5f);
        progressFillRect.pivot = new Vector2(0f, 0.5f);
        progressFillRect.anchoredPosition = Vector2.zero;
        progressFillRect.sizeDelta = new Vector2(0f, 10f);
        progressFill.GetComponent<Image>().color = progressFillColor;
    }

    private TMP_Text CreateText(string objectName, Transform parent, Vector2 position, Vector2 size, int fontSize, TextAlignmentOptions alignment)
    {
        GameObject textObject = new GameObject(objectName, typeof(RectTransform), typeof(TextMeshProUGUI));
        textObject.transform.SetParent(parent, false);

        RectTransform rect = textObject.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = position;
        rect.sizeDelta = size;

        TMP_Text text = textObject.GetComponent<TMP_Text>();
        text.text = "";
        text.fontSize = fontSize;
        text.color = textColor;
        text.alignment = alignment;
        text.textWrappingMode = TextWrappingModes.NoWrap;
        text.overflowMode = TextOverflowModes.Ellipsis;

        return text;
    }

    private void FindCameraIfNeeded()
    {
        if (targetCamera != null && targetCamera.isActiveAndEnabled)
            return;

        targetCamera = Camera.main;
        if (targetCamera != null)
            return;

        Camera[] cameras = FindObjectsByType<Camera>(FindObjectsSortMode.None);
        for (int i = 0; i < cameras.Length; i++)
        {
            if (cameras[i] != null && cameras[i].isActiveAndEnabled)
            {
                targetCamera = cameras[i];
                return;
            }
        }
    }

    private void FollowCamera()
    {
        Transform canvasTransform = canvas.transform;
        Vector3 targetPosition = targetCamera.ViewportToWorldPoint(viewportAnchor);
        Quaternion targetRotation = Quaternion.LookRotation(canvasTransform.position - targetCamera.transform.position);

        canvasTransform.position = Vector3.Lerp(canvasTransform.position, targetPosition, Time.unscaledDeltaTime * followSmooth);
        canvasTransform.rotation = Quaternion.Slerp(canvasTransform.rotation, targetRotation, Time.unscaledDeltaTime * rotationSmooth);
    }

    private void UpdateContent()
    {
        GameTimer timer = GameTimer.Instance;
        KarmaSystem karma = KarmaSystem.Instance;

        float totalTime = Mathf.Max(1f, timer.totalTime);
        float elapsed = Mathf.Clamp(timer.ElapsedRealSeconds, 0f, totalTime);
        float elapsedYears = Mathf.Clamp(timer.ElapsedGameYears, 0f, timer.gameYearsPerRealMinute * totalTime / 60f);
        float maxYears = timer.gameYearsPerRealMinute * totalTime / 60f;
        float progress = Mathf.Clamp01(timer.ElapsedRealSeconds / totalTime);

        int minutes = Mathf.FloorToInt(elapsed / 60f);
        int seconds = Mathf.FloorToInt(elapsed % 60f);
        int displayYear = Mathf.FloorToInt(elapsedYears);
        int displayMaxYear = Mathf.RoundToInt(maxYears);
        int totalKarma = karma != null ? karma.GetTotalNegative() : 0;

        stageText.text = "Stage: " + GetEnvironmentalStage(totalKarma);
        yearText.text = "Year " + displayYear.ToString("00") + " / " + displayMaxYear.ToString("00");
        timeText.text = "Time " + minutes.ToString("00") + ":" + seconds.ToString("00");

        if (progressFillRect != null)
            progressFillRect.sizeDelta = new Vector2(650f * progress, 10f);
    }

    private string GetEnvironmentalStage(int totalKarma)
    {
        if (totalKarma <= 149)
            return "Stable";

        if (totalKarma <= 299)
            return "Stressed";

        if (totalKarma < 450)
            return "Critical";

        return "Collapse";
    }
}
