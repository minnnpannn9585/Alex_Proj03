using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Plays line-by-line dialogue and draws the "press E" prompt.
/// The view is built at runtime so the scene does not need a hand-authored canvas.
/// </summary>
[DefaultExecutionOrder(50)]
public class DialogueManager : MonoBehaviour
{
    public static DialogueManager Instance { get; private set; }

    [SerializeField] private float charactersPerSecond = 42f;

    private Canvas canvas;
    private GameObject panel;
    private GameObject namePlate;
    private Text nameText;
    private Text bodyText;
    private Text hintText;
    private GameObject controlsHint;
    private GameObject promptRoot;
    private Text promptText;
    private RectTransform promptRect;

    private string[] lines = System.Array.Empty<string>();
    private string speakerName = string.Empty;
    private string fullText = string.Empty;
    private int index;
    private float shownChars;
    private bool lineComplete;
    private int openedFrame = -1;
    private bool blockUntilRelease;
    private bool promptVisible;
    private Vector3 promptWorld;
    private string promptLabel = string.Empty;

    public bool IsOpen { get; private set; }
    public bool CanStartInteraction => !IsOpen && !blockUntilRelease;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        BuildUi();
        panel.SetActive(false);
        promptRoot.SetActive(false);
    }

    private void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }
    }

    private void Update()
    {
        if (blockUntilRelease && !Input.GetKey(KeyCode.E))
        {
            blockUntilRelease = false;
        }

        if (!IsOpen)
        {
            return;
        }

        TickTypewriter();

        if (Time.frameCount == openedFrame || !PressedAdvance())
        {
            return;
        }

        if (!lineComplete)
        {
            CompleteLine();
            return;
        }

        Advance();
    }

    private void LateUpdate()
    {
        if (!promptVisible || promptRoot == null || canvas == null)
        {
            return;
        }

        Camera camera = Camera.main;
        if (camera == null)
        {
            promptRoot.SetActive(false);
            return;
        }

        Vector3 screen = camera.WorldToScreenPoint(promptWorld);
        if (screen.z < 0f)
        {
            promptRoot.SetActive(false);
            return;
        }

        promptRoot.SetActive(true);
        RectTransform canvasRect = canvas.GetComponent<RectTransform>();
        if (RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRect, screen, null, out Vector2 local))
        {
            promptRect.anchoredPosition = local + new Vector2(0f, 18f);
        }
    }

    public void StartDialogue(string speaker, string[] source)
    {
        List<string> collected = new List<string>();
        if (source != null)
        {
            for (int i = 0; i < source.Length; i++)
            {
                if (!string.IsNullOrWhiteSpace(source[i]))
                {
                    collected.Add(source[i].Trim());
                }
            }
        }

        if (collected.Count == 0 || panel == null)
        {
            return;
        }

        lines = collected.ToArray();
        speakerName = speaker ?? string.Empty;
        index = 0;
        IsOpen = true;
        openedFrame = Time.frameCount;
        HidePrompt();
        if (controlsHint != null)
        {
            controlsHint.SetActive(false);
        }

        panel.SetActive(true);
        ShowCurrentLine();
    }

    public void ShowPrompt(Vector3 worldPosition, string prompt)
    {
        if (IsOpen || promptRoot == null)
        {
            return;
        }

        promptWorld = worldPosition;
        string label = "按 E  " + prompt;
        if (promptLabel != label)
        {
            promptLabel = label;
            promptText.text = label;
        }

        promptVisible = true;
        promptRoot.SetActive(true);
    }

    public void HidePrompt()
    {
        promptVisible = false;
        promptLabel = string.Empty;
        if (promptRoot != null)
        {
            promptRoot.SetActive(false);
        }
    }

    private void ShowCurrentLine()
    {
        fullText = lines[index];
        shownChars = 0f;
        lineComplete = false;
        bodyText.text = string.Empty;
        nameText.text = speakerName;
        namePlate.SetActive(!string.IsNullOrEmpty(speakerName));
        hintText.gameObject.SetActive(false);
    }

    private void TickTypewriter()
    {
        if (lineComplete)
        {
            PulseHint();
            return;
        }

        shownChars += charactersPerSecond * Time.deltaTime;
        int count = Mathf.Clamp(Mathf.FloorToInt(shownChars), 0, fullText.Length);
        bodyText.text = fullText.Substring(0, count);
        if (count >= fullText.Length)
        {
            CompleteLine();
        }
    }

    private void CompleteLine()
    {
        lineComplete = true;
        shownChars = fullText.Length;
        bodyText.text = fullText;
        hintText.gameObject.SetActive(true);
    }

    private void PulseHint()
    {
        Color color = hintText.color;
        color.a = 0.4f + 0.6f * Mathf.PingPong(Time.time * 1.4f, 1f);
        hintText.color = color;
    }

    private void Advance()
    {
        index++;
        if (index >= lines.Length)
        {
            Close();
            return;
        }

        ShowCurrentLine();
    }

    private void Close()
    {
        IsOpen = false;
        panel.SetActive(false);
        blockUntilRelease = Input.GetKey(KeyCode.E);
    }

    private static bool PressedAdvance()
    {
        return Input.GetKeyDown(KeyCode.E)
            || Input.GetKeyDown(KeyCode.Space)
            || Input.GetKeyDown(KeyCode.Return)
            || Input.GetMouseButtonDown(0);
    }

    private void BuildUi()
    {
        Font font = LoadFont();

        GameObject canvasGo = new GameObject("DialogueCanvas");
        canvasGo.transform.SetParent(transform, false);
        canvas = canvasGo.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 100;

        CanvasScaler scaler = canvasGo.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;

        controlsHint = CreateTextObject("ControlsHint", canvasGo.transform, font, 28, FontStyle.Normal, new Color(1f, 0.95f, 0.82f, 0.92f), TextAnchor.MiddleCenter);
        RectTransform hintRect = controlsHint.GetComponent<RectTransform>();
        hintRect.anchorMin = new Vector2(0.5f, 1f);
        hintRect.anchorMax = new Vector2(0.5f, 1f);
        hintRect.pivot = new Vector2(0.5f, 1f);
        hintRect.sizeDelta = new Vector2(900f, 48f);
        hintRect.anchoredPosition = new Vector2(0f, -24f);
        controlsHint.GetComponent<Text>().text = "WASD 移动     靠近后按 E 交互";

        panel = CreateImageObject("Panel", canvasGo.transform, new Color(0.07f, 0.06f, 0.09f, 0.94f));
        RectTransform panelRect = panel.GetComponent<RectTransform>();
        panelRect.anchorMin = new Vector2(0.07f, 0f);
        panelRect.anchorMax = new Vector2(0.93f, 0f);
        panelRect.pivot = new Vector2(0.5f, 0f);
        panelRect.offsetMin = new Vector2(0f, 36f);
        panelRect.offsetMax = new Vector2(0f, 286f);
        Outline panelOutline = panel.AddComponent<Outline>();
        panelOutline.effectColor = new Color(0.86f, 0.72f, 0.42f, 0.95f);
        panelOutline.effectDistance = new Vector2(2.5f, -2.5f);

        namePlate = CreateImageObject("NamePlate", panel.transform, new Color(0.29f, 0.16f, 0.09f, 0.98f));
        RectTransform nameRect = namePlate.GetComponent<RectTransform>();
        nameRect.anchorMin = new Vector2(0f, 1f);
        nameRect.anchorMax = new Vector2(0f, 1f);
        nameRect.pivot = new Vector2(0f, 0f);
        nameRect.anchoredPosition = new Vector2(28f, -8f);
        nameRect.sizeDelta = new Vector2(280f, 52f);

        GameObject nameObject = CreateTextObject("Name", namePlate.transform, font, 28, FontStyle.Bold, new Color(1f, 0.93f, 0.75f, 1f), TextAnchor.MiddleLeft);
        Stretch(nameObject.GetComponent<RectTransform>(), 16f, 4f, -16f, -4f);
        nameText = nameObject.GetComponent<Text>();

        GameObject bodyObject = CreateTextObject("Body", panel.transform, font, 32, FontStyle.Normal, new Color(0.96f, 0.95f, 0.92f, 1f), TextAnchor.UpperLeft);
        Stretch(bodyObject.GetComponent<RectTransform>(), 36f, 48f, -36f, -20f);
        bodyText = bodyObject.GetComponent<Text>();
        bodyText.horizontalOverflow = HorizontalWrapMode.Wrap;
        bodyText.verticalOverflow = VerticalWrapMode.Truncate;

        GameObject continueObject = CreateTextObject("ContinueHint", panel.transform, font, 22, FontStyle.Normal, new Color(0.9f, 0.82f, 0.62f, 1f), TextAnchor.MiddleRight);
        RectTransform continueRect = continueObject.GetComponent<RectTransform>();
        continueRect.anchorMin = new Vector2(1f, 0f);
        continueRect.anchorMax = new Vector2(1f, 0f);
        continueRect.pivot = new Vector2(1f, 0f);
        continueRect.sizeDelta = new Vector2(320f, 36f);
        continueRect.anchoredPosition = new Vector2(-28f, 14f);
        hintText = continueObject.GetComponent<Text>();
        hintText.text = "E / 空格 继续";
        continueObject.SetActive(false);

        promptRoot = CreateImageObject("Prompt", canvasGo.transform, new Color(0.05f, 0.05f, 0.07f, 0.9f));
        promptRect = promptRoot.GetComponent<RectTransform>();
        promptRect.anchorMin = new Vector2(0.5f, 0.5f);
        promptRect.anchorMax = new Vector2(0.5f, 0.5f);
        promptRect.pivot = new Vector2(0.5f, 0f);
        promptRect.sizeDelta = new Vector2(220f, 48f);

        ContentSizeFitter fitter = promptRoot.AddComponent<ContentSizeFitter>();
        fitter.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
        fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        HorizontalLayoutGroup layout = promptRoot.AddComponent<HorizontalLayoutGroup>();
        layout.padding = new RectOffset(18, 18, 8, 8);
        layout.childAlignment = TextAnchor.MiddleCenter;
        layout.childControlWidth = true;
        layout.childControlHeight = true;
        layout.childForceExpandWidth = false;
        layout.childForceExpandHeight = false;

        GameObject promptLabelObject = CreateTextObject("Label", promptRoot.transform, font, 26, FontStyle.Bold, new Color(1f, 0.92f, 0.55f, 1f), TextAnchor.MiddleCenter);
        promptText = promptLabelObject.GetComponent<Text>();
        promptText.horizontalOverflow = HorizontalWrapMode.Overflow;
        promptText.verticalOverflow = VerticalWrapMode.Overflow;
    }

    private static Font LoadFont()
    {
        try
        {
            Font osFont = Font.CreateDynamicFontFromOSFont(
                new[] { "Microsoft YaHei UI", "Microsoft YaHei", "SimHei", "SimSun", "PingFang SC", "Noto Sans CJK SC" },
                32);
            if (osFont != null)
            {
                return osFont;
            }
        }
        catch (Exception)
        {
            // A player build without those OS fonts falls back to the built-in font.
        }

        return Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
    }

    private static GameObject CreateImageObject(string name, Transform parent, Color color)
    {
        GameObject go = new GameObject(name);
        go.transform.SetParent(parent, false);
        Image image = go.AddComponent<Image>();
        image.color = color;
        image.raycastTarget = false;
        return go;
    }

    private static GameObject CreateTextObject(string name, Transform parent, Font font, int size, FontStyle style, Color color, TextAnchor alignment)
    {
        GameObject go = new GameObject(name);
        go.transform.SetParent(parent, false);
        Text text = go.AddComponent<Text>();
        text.font = font;
        text.fontSize = size;
        text.fontStyle = style;
        text.color = color;
        text.alignment = alignment;
        text.raycastTarget = false;
        text.supportRichText = false;
        text.horizontalOverflow = HorizontalWrapMode.Wrap;
        text.verticalOverflow = VerticalWrapMode.Overflow;

        Outline outline = go.AddComponent<Outline>();
        outline.effectColor = new Color(0f, 0f, 0f, 0.85f);
        outline.effectDistance = new Vector2(1.2f, -1.2f);
        return go;
    }

    private static void Stretch(RectTransform rect, float left, float bottom, float right, float top)
    {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = new Vector2(left, bottom);
        rect.offsetMax = new Vector2(right, top);
    }
}
