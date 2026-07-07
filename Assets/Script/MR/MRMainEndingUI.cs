using UnityEngine;
using TMPro;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class MRMainEndingUI : MonoBehaviour
{
    [Header("Resource Panel")]
    public GameObject resourcePanel;

    [Header("Ending Panel")]
    public GameObject endingPanel;

    [Header("Text")]
    public TMP_Text titleText;
    public TMP_Text descriptionText;
    public TMP_Text summaryText;

    [Header("Restart Scene")]
    public string restartSceneName = "VR_Forest";

    [Header("VR Ray Input")]
    public Transform rayStartPoint;
    public OVRInput.Controller rayController = OVRInput.Controller.RTouch;
    public OVRInput.Button clickButton = OVRInput.Button.PrimaryIndexTrigger;
    public float buttonRayDistance = 15f;

    void Start()
    {
        if (endingPanel != null)
            endingPanel.SetActive(false);

        EnsureRayStartPoint();
        WireEndingButtons();
        ShowEndingIfNeeded();
    }

    void Update()
    {
        if (endingPanel == null || !endingPanel.activeSelf)
            return;

        EnsureRayStartPoint();

        if (OVRInput.GetDown(clickButton, rayController))
            TryClickButtonWithRay();
    }

    void ShowEndingIfNeeded()
    {
        if (GameEndingState.Instance == null) return;
        if (!GameEndingState.Instance.hasPendingEnding) return;

        if (resourcePanel != null)
            resourcePanel.SetActive(false);

        if (endingPanel != null)
        {
            endingPanel.SetActive(true);
            endingPanel.transform.SetAsLastSibling();

            RectTransform rt = endingPanel.GetComponent<RectTransform>();
            if (rt != null)
            {
                rt.anchoredPosition = Vector2.zero;
                rt.localScale = Vector3.one;
            }
        }

        if (titleText != null)
            titleText.text = GameEndingState.Instance.endingTitle;

        if (descriptionText != null)
            descriptionText.text = GameEndingState.Instance.endingDescription;

        if (summaryText != null)
        {
            summaryText.text =
                "\u7d50\u5c40\u985e\u578b\uff1a" + GameEndingState.Instance.endingType +
                "\n\u904a\u6232\u5e74\u6578\uff1a" + Mathf.FloorToInt(GameEndingState.Instance.elapsedGameYears) +
                "\n\u74b0\u5883\u968e\u6bb5\uff1a" + GameEndingState.Instance.environmentalStage +
                "\n\u539f\u59cb\u56e0\u679c\u503c\uff1a" + GameEndingState.Instance.totalBeforeRestoration +
                "\n\u5fa9\u80b2\u62b5\u6d88\uff1a-" + GameEndingState.Instance.restorationKarma +
                "\n\u6700\u7d42\u56e0\u679c\u503c\uff1a" + GameEndingState.Instance.totalNegative;
        }

        WireEndingButtons();
        GameEndingState.Instance.ClearEndingData();
    }

    void WireEndingButtons()
    {
        if (endingPanel == null) return;

        Button[] buttons = endingPanel.GetComponentsInChildren<Button>(true);
        for (int i = 0; i < buttons.Length; i++)
        {
            Button button = buttons[i];
            if (button == null) continue;

            string buttonName = button.gameObject.name.ToLower();
            TMP_Text labelText = button.GetComponentInChildren<TMP_Text>(true);
            string label = labelText != null ? labelText.text.ToLower() : "";

            if (buttonName.Contains("restart") || label.Contains("restart"))
            {
                button.onClick.RemoveListener(RestartGame);
                button.onClick.AddListener(RestartGame);
            }
            else if (buttonName.Contains("close") || buttonName.Contains("back") || label.Contains("close") || label.Contains("back"))
            {
                button.onClick.RemoveListener(BackToMain);
                button.onClick.AddListener(BackToMain);
            }
        }
    }

    void TryClickButtonWithRay()
    {
        if (rayStartPoint == null || endingPanel == null)
            return;

        Button[] buttons = endingPanel.GetComponentsInChildren<Button>(true);
        Button hitButton = null;
        float closestDistance = buttonRayDistance;
        Ray ray = new Ray(rayStartPoint.position, rayStartPoint.forward);

        for (int i = 0; i < buttons.Length; i++)
        {
            Button button = buttons[i];
            if (button == null || !button.gameObject.activeInHierarchy || !button.interactable)
                continue;

            RectTransform rect = button.transform as RectTransform;
            if (rect == null)
                continue;

            if (!TryGetRayRectHitDistance(ray, rect, out float distance))
                continue;

            if (distance < closestDistance)
            {
                closestDistance = distance;
                hitButton = button;
            }
        }

        if (hitButton == null)
            return;

        Debug.Log("Ending panel ray clicked: " + hitButton.gameObject.name);
        hitButton.onClick.Invoke();
    }

    bool TryGetRayRectHitDistance(Ray ray, RectTransform rect, out float distance)
    {
        distance = 0f;

        Plane plane = new Plane(rect.forward, rect.position);
        if (!plane.Raycast(ray, out distance))
            return false;

        if (distance < 0f || distance > buttonRayDistance)
            return false;

        Vector3 localPoint = rect.InverseTransformPoint(ray.GetPoint(distance));
        return rect.rect.Contains(new Vector2(localPoint.x, localPoint.y));
    }

    void EnsureRayStartPoint()
    {
        if (rayStartPoint != null)
            return;

        OVRCameraRig rig = Object.FindFirstObjectByType<OVRCameraRig>();
        if (rig != null && rig.rightHandAnchor != null)
        {
            rayStartPoint = rig.rightHandAnchor;
            return;
        }

        GameObject rightHand = GameObject.Find("RightHandAnchor");
        if (rightHand != null)
            rayStartPoint = rightHand.transform;
    }

    public void RestartGame()
    {
        if (SaveManager.Instance != null)
            SaveManager.Instance.DeleteSave();

        if (ResourceManager.Instance != null)
            ResourceManager.Instance.ResetAllResources();

        if (KarmaSystem.Instance != null)
            KarmaSystem.Instance.ResetKarma();

        if (GameEndingState.Instance != null)
            GameEndingState.Instance.ClearEndingData();

        if (GameTimer.Instance != null)
            GameTimer.Instance.ResetTimer();

        SceneManager.LoadScene(restartSceneName);
    }

    public void BackToMain()
    {
        if (endingPanel != null)
            endingPanel.SetActive(false);

        if (resourcePanel != null)
            resourcePanel.SetActive(true);
    }
}