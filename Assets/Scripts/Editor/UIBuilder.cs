using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using StealthGame;

public static class UIBuilder
{
    // Palette
    static readonly Color BgDeep      = new(0.04f, 0.06f, 0.10f, 0.92f);
    static readonly Color CyanPrimary = new(0.00f, 0.85f, 1.00f, 1.00f);
    static readonly Color CyanDim     = new(0.00f, 0.63f, 0.73f, 0.55f);
    static readonly Color IceText     = new(0.70f, 0.94f, 1.00f, 1.00f);
    static readonly Color NeonGreen   = new(0.00f, 1.00f, 0.60f, 1.00f);
    static readonly Color DangerRed   = new(1.00f, 0.16f, 0.10f, 1.00f);
    static readonly Color BtnNormal   = new(0.05f, 0.15f, 0.20f, 0.85f);
    static readonly Color BtnHover    = new(0.00f, 0.85f, 1.00f, 0.18f);
    static readonly Color BtnPressed  = new(0.00f, 0.63f, 0.73f, 0.35f);

    static Font _font;

    [MenuItem("Stealth/\U0001f3d7 Build Scene UI")]
    static void BuildUI()
    {
        if (Object.FindAnyObjectByType<Canvas>() != null)
        {
            EditorUtility.DisplayDialog("UI Builder",
                "A Canvas already exists.\nDelete it first, then re-run.", "OK");
            return;
        }

        _font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

        var canvasGo = new GameObject("Canvas", typeof(RectTransform));
        var canvas = canvasGo.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 10;
        var scaler = canvasGo.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.matchWidthOrHeight = 0.5f;
        canvasGo.AddComponent<GraphicRaycaster>();

        if (Object.FindAnyObjectByType<EventSystem>() == null)
        {
            var esGo = new GameObject("EventSystem");
            esGo.AddComponent<EventSystem>();
            esGo.AddComponent<StandaloneInputModule>();
            Undo.RegisterCreatedObjectUndo(esGo, "Create EventSystem");
        }

        var ct = canvasGo.transform;
        BuildHUD(ct);
        var gameOverGo = BuildGameOverScreen(ct);
        var winGo      = BuildWinScreen(ct);
        BuildMainMenu(ct);

        var gm = Object.FindAnyObjectByType<GameManager>();
        if (gm != null)
        {
            var gmSO = new SerializedObject(gm);
            gmSO.FindProperty("gameOverScreen").objectReferenceValue = gameOverGo;
            gmSO.FindProperty("winScreen").objectReferenceValue      = winGo;
            gmSO.ApplyModifiedProperties();
            Debug.Log("[UIBuilder] GameManager refs wired.");
        }

        Undo.RegisterCreatedObjectUndo(canvasGo, "Build Scene UI");
        EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
        Debug.Log("[UIBuilder] Done. Drag your GameStateChannel asset into GameManager and HUD inspectors if not auto-wired.");
    }

    // ---------------------------------------------------------------
    //  PANEL BUILDERS
    // ---------------------------------------------------------------

    static void BuildHUD(Transform canvas)
    {
        var root = Stretch(canvas, "HUD");

        // Label — sits above the bar
        var lbl = Txt(root, "NoiseMeterLabel", "NOISE LVL", 13, CyanPrimary,
                      TextAnchor.MiddleLeft, FontStyle.Bold);
        Anchor(lbl.rectTransform, Vector2.up, Vector2.up,
               new Vector2(20, -22), new Vector2(160, -6));

        // Background bar
        var bgImg = Img(root, "NoiseMeterBG", CyanDim);
        Anchor(bgImg.rectTransform, Vector2.up, Vector2.up,
               new Vector2(20, -58), new Vector2(300, -28));

        // Fill bar — cyan horizontal fill
        var fillImg = Img(root, "NoiseMeterFill", CyanPrimary);
        fillImg.type       = Image.Type.Filled;
        fillImg.fillMethod = Image.FillMethod.Horizontal;
        fillImg.fillAmount = 0.6f;
        Anchor(fillImg.rectTransform, Vector2.up, Vector2.up,
               new Vector2(20, -58), new Vector2(300, -28));

        // Border around bar
        Border(bgImg.rectTransform, CyanDim, 1f);

        var hud = root.gameObject.AddComponent<HUD>();
        var so  = new SerializedObject(hud);
        so.FindProperty("noiseMeter").objectReferenceValue = fillImg;

        // Auto-wire GameStateChannel if it already exists in the project
        var guids = AssetDatabase.FindAssets("t:GameStateEventChannelSO");
        if (guids.Length > 0)
        {
            var channel = AssetDatabase.LoadAssetAtPath<GameStateEventChannelSO>(
                AssetDatabase.GUIDToAssetPath(guids[0]));
            if (channel != null)
                so.FindProperty("gameStateChannel").objectReferenceValue = channel;
        }

        so.ApplyModifiedProperties();
    }

    static GameObject BuildGameOverScreen(Transform canvas)
    {
        var root = Stretch(canvas, "GameOverScreen");
        var cg   = root.gameObject.AddComponent<CanvasGroup>();
        cg.alpha = 0; cg.interactable = false; cg.blocksRaycasts = false;
        root.gameObject.SetActive(false);

        Img(root, "Overlay", new Color(0.02f, 0.03f, 0.06f, 0.88f));

        var panel = Img(root, "Panel", BgDeep).rectTransform;
        Anchor(panel, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
               new Vector2(-320, -200), new Vector2(320, 200));
        Border(panel, DangerRed);

        var header = Txt(panel, "HeaderText", "MISSION COMPROMISED", 38,
                         DangerRed, TextAnchor.MiddleCenter, FontStyle.Bold);
        Anchor(header.rectTransform, new Vector2(0, 1), new Vector2(1, 1),
               new Vector2(20, -100), new Vector2(-20, -30));
        AddOutline(header.gameObject, DangerRed);

        var sub = Txt(panel, "SubText", "AGENT CAPTURED", 18,
                      IceText, TextAnchor.MiddleCenter, FontStyle.Normal);
        Anchor(sub.rectTransform, new Vector2(0, 1), new Vector2(1, 1),
               new Vector2(20, -138), new Vector2(-20, -106));

        var retryBtn = Btn(panel, "RetryButton", "[ RETRY MISSION ]",
                           new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                           new Vector2(-304, -80), new Vector2(-14, -30));
        var menuBtn  = Btn(panel, "MainMenuButton", "[ MAIN MENU ]",
                           new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                           new Vector2(14, -80), new Vector2(304, -30));

        var comp = root.gameObject.AddComponent<GameOverScreen>();
        var so   = new SerializedObject(comp);
        so.FindProperty("retryButton").objectReferenceValue    = retryBtn;
        so.FindProperty("mainMenuButton").objectReferenceValue = menuBtn;
        so.ApplyModifiedProperties();

        return root.gameObject;
    }

    static GameObject BuildWinScreen(Transform canvas)
    {
        var root = Stretch(canvas, "WinScreen");
        var cg   = root.gameObject.AddComponent<CanvasGroup>();
        cg.alpha = 0; cg.interactable = false; cg.blocksRaycasts = false;
        root.gameObject.SetActive(false);

        Img(root, "Overlay", new Color(0.00f, 0.04f, 0.03f, 0.88f));

        var panel = Img(root, "Panel", BgDeep).rectTransform;
        Anchor(panel, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
               new Vector2(-300, -190), new Vector2(300, 190));
        Border(panel, NeonGreen);

        var header = Txt(panel, "HeaderText", "MISSION COMPLETE", 38,
                         NeonGreen, TextAnchor.MiddleCenter, FontStyle.Bold);
        Anchor(header.rectTransform, new Vector2(0, 1), new Vector2(1, 1),
               new Vector2(20, -95), new Vector2(-20, -25));
        AddOutline(header.gameObject, NeonGreen);

        var sub = Txt(panel, "SubText", "OBJECTIVE SECURED", 18,
                      IceText, TextAnchor.MiddleCenter, FontStyle.Normal);
        Anchor(sub.rectTransform, new Vector2(0, 1), new Vector2(1, 1),
               new Vector2(20, -130), new Vector2(-20, -99));

        var menuBtn = Btn(panel, "MainMenuButton", "[ MAIN MENU ]",
                          new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                          new Vector2(-280, -80), new Vector2(-10, -30));
        var quitBtn = Btn(panel, "QuitButton", "[ QUIT ]",
                          new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                          new Vector2(10, -80), new Vector2(280, -30));

        var comp = root.gameObject.AddComponent<WinScreen>();
        var so   = new SerializedObject(comp);
        so.FindProperty("mainMenuButton").objectReferenceValue = menuBtn;
        so.FindProperty("quitButton").objectReferenceValue     = quitBtn;
        so.ApplyModifiedProperties();

        return root.gameObject;
    }

    static void BuildMainMenu(Transform canvas)
    {
        var root = Stretch(canvas, "MainMenu");

        Img(root, "Background", new Color(0.03f, 0.05f, 0.09f, 1f));
        BracketCorners(root, CyanDim);

        var title = Txt(root, "Title", "STEALTH AI SANDBOX", 52,
                        CyanPrimary, TextAnchor.MiddleCenter, FontStyle.Bold);
        Anchor(title.rectTransform, new Vector2(0, 0.5f), new Vector2(1, 0.5f),
               new Vector2(0, 80), new Vector2(0, 160));
        AddOutline(title.gameObject, CyanPrimary);

        var sub = Txt(root, "Subtitle", "ARIVERSE STUDIO  //  CLASSIFIED", 16,
                      CyanDim, TextAnchor.MiddleCenter, FontStyle.Normal);
        Anchor(sub.rectTransform, new Vector2(0, 0.5f), new Vector2(1, 0.5f),
               new Vector2(0, 48), new Vector2(0, 78));

        var div = Img(root, "Divider", CyanDim);
        div.rectTransform.anchorMin = new Vector2(0.3f, 0.5f);
        div.rectTransform.anchorMax = new Vector2(0.7f, 0.5f);
        div.rectTransform.offsetMin = new Vector2(0, 36);
        div.rectTransform.offsetMax = new Vector2(0, 38);

        var playBtn = Btn(root, "PlayButton", "[ INITIATE MISSION ]",
                          new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                          new Vector2(-200, -30), new Vector2(200, 20));
        var quitBtn = Btn(root, "QuitButton", "[ TERMINATE ]",
                          new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                          new Vector2(-160, -90), new Vector2(160, -40));

        var ver = Txt(root, "Version", "v0.1.0  //  BUILD 2026", 10,
                      new Color(0.4f, 0.55f, 0.6f, 0.45f),
                      TextAnchor.LowerRight, FontStyle.Normal);
        Anchor(ver.rectTransform, new Vector2(1, 0), new Vector2(1, 0),
               new Vector2(-200, 8), new Vector2(-12, 22));

        var comp = root.gameObject.AddComponent<MainMenu>();
        var so   = new SerializedObject(comp);
        so.FindProperty("playButton").objectReferenceValue = playBtn;
        so.FindProperty("quitButton").objectReferenceValue = quitBtn;
        so.ApplyModifiedProperties();
    }

    // ---------------------------------------------------------------
    //  PRIMITIVE HELPERS
    // ---------------------------------------------------------------

    static RectTransform Stretch(Transform parent, string name)
    {
        var go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        var rt = (RectTransform)go.transform;
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = rt.offsetMax = Vector2.zero;
        return rt;
    }

    static Image Img(RectTransform parent, string name, Color color)
    {
        var go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        var rt = (RectTransform)go.transform;
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = rt.offsetMax = Vector2.zero;
        var img = go.AddComponent<Image>();
        img.color = color;
        return img;
    }

    static Text Txt(RectTransform parent, string name, string content,
                    int size, Color color, TextAnchor anchor, FontStyle style)
    {
        var go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        var t = go.AddComponent<Text>();
        t.text = content; t.font = _font; t.fontSize = size;
        t.color = color;  t.alignment = anchor; t.fontStyle = style;
        t.resizeTextForBestFit = false;
        t.supportRichText = false;
        return t;
    }

    static Button Btn(RectTransform parent, string name, string label,
                      Vector2 anchorMin, Vector2 anchorMax,
                      Vector2 offsetMin, Vector2 offsetMax)
    {
        var go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        var rt = (RectTransform)go.transform;
        rt.anchorMin = anchorMin; rt.anchorMax = anchorMax;
        rt.offsetMin = offsetMin; rt.offsetMax = offsetMax;

        var bg = go.AddComponent<Image>();
        bg.color = BtnNormal;

        var btn = go.AddComponent<Button>();
        btn.colors = new ColorBlock
        {
            normalColor      = BtnNormal,
            highlightedColor = BtnHover,
            pressedColor     = BtnPressed,
            selectedColor    = BtnHover,
            disabledColor    = new Color(0.3f, 0.3f, 0.3f, 0.5f),
            colorMultiplier  = 1f,
            fadeDuration     = 0.1f,
        };
        btn.targetGraphic = bg;

        var tgo = new GameObject("Text", typeof(RectTransform));
        tgo.transform.SetParent(go.transform, false);
        var trt = (RectTransform)tgo.transform;
        trt.anchorMin = Vector2.zero; trt.anchorMax = Vector2.one;
        trt.offsetMin = new Vector2(6, 2); trt.offsetMax = new Vector2(-6, -2);
        var t = tgo.AddComponent<Text>();
        t.text = label; t.font = _font; t.fontSize = 16;
        t.color = CyanPrimary; t.alignment = TextAnchor.MiddleCenter;
        t.fontStyle = FontStyle.Bold;

        Border(rt, CyanDim, 1.5f);
        return btn;
    }

    static void Anchor(RectTransform rt, Vector2 anchorMin, Vector2 anchorMax,
                       Vector2 offsetMin, Vector2 offsetMax)
    {
        rt.anchorMin = anchorMin;
        rt.anchorMax = anchorMax;
        rt.pivot     = new Vector2((anchorMin.x + anchorMax.x) * 0.5f,
                                   (anchorMin.y + anchorMax.y) * 0.5f);
        rt.offsetMin = offsetMin;
        rt.offsetMax = offsetMax;
    }

    static void Border(RectTransform parent, Color color, float t = 2f)
    {
        Strip(parent, "BT", color, new Vector2(0, 1), new Vector2(1, 1), new Vector2(0, -t), new Vector2(0,  0));
        Strip(parent, "BB", color, new Vector2(0, 0), new Vector2(1, 0), new Vector2(0,  0), new Vector2(0,  t));
        Strip(parent, "BL", color, new Vector2(0, 0), new Vector2(0, 1), new Vector2(0,  0), new Vector2(t,  0));
        Strip(parent, "BR", color, new Vector2(1, 0), new Vector2(1, 1), new Vector2(-t, 0), new Vector2(0,  0));
    }

    static void BracketCorners(RectTransform parent, Color color)
    {
        const float len = 50f, thick = 2f, margin = 14f;
        // top-left
        Strip(parent, "TL_H", color, Vector2.up,          Vector2.up,
              new Vector2(margin,        -thick), new Vector2(margin + len, 0));
        Strip(parent, "TL_V", color, Vector2.up,          Vector2.up,
              new Vector2(margin,        -len),   new Vector2(margin + thick, 0));
        // top-right
        Strip(parent, "TR_H", color, Vector2.one,         Vector2.one,
              new Vector2(-(margin+len), -thick), new Vector2(-margin, 0));
        Strip(parent, "TR_V", color, Vector2.one,         Vector2.one,
              new Vector2(-(margin+thick), -len), new Vector2(-margin, 0));
        // bottom-left
        Strip(parent, "BL_H", color, Vector2.zero,        Vector2.zero,
              new Vector2(margin,        0),      new Vector2(margin + len,   thick));
        Strip(parent, "BL_V", color, Vector2.zero,        Vector2.zero,
              new Vector2(margin,        0),      new Vector2(margin + thick, len));
        // bottom-right
        Strip(parent, "BR_H", color, new Vector2(1, 0),   new Vector2(1, 0),
              new Vector2(-(margin+len), 0),      new Vector2(-margin,        thick));
        Strip(parent, "BR_V", color, new Vector2(1, 0),   new Vector2(1, 0),
              new Vector2(-(margin+thick), 0),    new Vector2(-margin,        len));
    }

    static void Strip(RectTransform parent, string name, Color color,
                      Vector2 aMin, Vector2 aMax, Vector2 oMin, Vector2 oMax)
    {
        var go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        var rt = (RectTransform)go.transform;
        rt.anchorMin = aMin; rt.anchorMax = aMax;
        rt.offsetMin = oMin; rt.offsetMax = oMax;
        go.AddComponent<Image>().color = color;
    }

    static void AddOutline(GameObject go, Color color)
    {
        var o = go.AddComponent<Outline>();
        o.effectColor    = new Color(color.r, color.g, color.b, 0.6f);
        o.effectDistance = new Vector2(1, -1);
    }
}
