using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

/// <summary>
/// Editor helper: Tools > UI > Add Weather UI.
/// Builds a screen-space settings menu and a world-space info sign in the open scene,
/// wired to VFX_Rain / VFX_FloatingDust (run Tools > VFX first). Save the scene afterward.
/// </summary>
public static class WeatherUISetup
{
    static DefaultControls.Resources s_Res;

    [MenuItem("Tools/UI/Add Weather UI")]
    public static void AddWeatherUI()
    {
        s_Res = new DefaultControls.Resources
        {
            standard = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd"),
            background = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/Background.psd"),
            knob = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/Knob.psd"),
            checkmark = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/Checkmark.psd"),
        };

        var rainGO = GameObject.Find("VFX_Rain");
        var dustGO = GameObject.Find("VFX_FloatingDust");
        var rain = rainGO ? rainGO.GetComponent<ParticleSystem>() : null;
        var dust = dustGO ? dustGO.GetComponent<ParticleSystem>() : null;
        if (rain == null)
            Debug.LogWarning("[UI] VFX_Rain not found. Run Tools > VFX > Add Rain Weather first, or assign it on the menu by hand.");

        EnsureEventSystem();
        BuildScreenMenu(rain, dust);
        BuildWorldSign(rain, dust);
        Debug.Log("[UI] Weather UI added. Save the scene (Ctrl+S), press Play, and press V.");
    }

    // ------------------------------------------------------------ Screen-space menu

    static void BuildScreenMenu(ParticleSystem rain, ParticleSystem dust)
    {
        var canvasGO = new GameObject("UI_WeatherMenuCanvas", typeof(RectTransform));
        Undo.RegisterCreatedObjectUndo(canvasGO, "Add Weather UI");
        var canvas = canvasGO.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 50;
        var scaler = canvasGO.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.matchWidthOrHeight = 0.5f;
        canvasGO.AddComponent<GraphicRaycaster>();

        // Dimmed full-screen backdrop = the menu root that gets toggled on/off.
        var root = NewUI("MenuRoot", canvasGO.transform);
        Stretch(root);
        root.gameObject.AddComponent<Image>().color = new Color(0f, 0f, 0f, 0.55f);

        // Centered panel.
        var panel = NewUI("Panel", root);
        panel.anchorMin = panel.anchorMax = panel.pivot = new Vector2(0.5f, 0.5f);
        panel.sizeDelta = new Vector2(620, 520);
        var panelImg = panel.gameObject.AddComponent<Image>();
        panelImg.sprite = s_Res.standard;
        panelImg.type = Image.Type.Sliced;
        panelImg.color = new Color(0.12f, 0.15f, 0.2f, 0.95f);
        var layout = panel.gameObject.AddComponent<VerticalLayoutGroup>();
        layout.padding = new RectOffset(48, 48, 36, 36);
        layout.spacing = 18;
        layout.childControlWidth = layout.childControlHeight = true;
        layout.childForceExpandWidth = true;
        layout.childForceExpandHeight = false;

        var title = NewText("Title", panel, "Weather Settings", 52, TextAlignmentOptions.Center);
        title.fontStyle = FontStyles.Bold;
        SetHeight(title.gameObject, 80);

        var rainToggle = NewToggle(panel, "Rain");
        var dustToggle = NewToggle(panel, "Floating Dust");

        var sliderLabel = NewText("IntensityLabel", panel, "Rain Intensity", 30, TextAlignmentOptions.Left);
        SetHeight(sliderLabel.gameObject, 40);

        var sliderGO = DefaultControls.CreateSlider(s_Res);
        sliderGO.name = "RainIntensitySlider";
        sliderGO.transform.SetParent(panel, false);
        SetHeight(sliderGO, 30);
        var slider = sliderGO.GetComponent<Slider>();
        slider.minValue = 0f;
        slider.maxValue = 7000f;
        slider.value = rain != null ? rain.emission.rateOverTime.constant : 3500f;

        var buttonGO = DefaultControls.CreateButton(s_Res);
        buttonGO.name = "ResumeButton";
        buttonGO.transform.SetParent(panel, false);
        SwapToTMP(buttonGO, 34, TextAlignmentOptions.Center, Color.black);
        SetHeight(buttonGO, 70);

        var hint = NewText("Hint", panel, "Press V to close", 22, TextAlignmentOptions.Center);
        hint.color = new Color(1f, 1f, 1f, 0.6f);
        SetHeight(hint.gameObject, 30);

        var menu = canvasGO.AddComponent<WeatherSettingsMenu>();
        menu.menuRoot = root.gameObject;
        menu.rainToggle = rainToggle;
        menu.dustToggle = dustToggle;
        menu.rainIntensitySlider = slider;
        menu.resumeButton = buttonGO.GetComponent<Button>();
        menu.rain = rain;
        menu.dust = dust;

        buttonGO.GetComponentInChildren<TMP_Text>().text = "Resume";
    }

    // ------------------------------------------------------------ World-space sign

    static void BuildWorldSign(ParticleSystem rain, ParticleSystem dust)
    {
        var signGO = new GameObject("UI_WorldWeatherSign", typeof(RectTransform));
        Undo.RegisterCreatedObjectUndo(signGO, "Add Weather UI");
        var canvas = signGO.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.WorldSpace;
        canvas.worldCamera = Camera.main;
        signGO.AddComponent<GraphicRaycaster>();

        // 800x560 canvas units * 0.005 = a 4m x 2.8m sign.
        var rt = (RectTransform)signGO.transform;
        rt.sizeDelta = new Vector2(800, 560);
        rt.localScale = Vector3.one * 0.005f;

        // Place it in front of the player.
        var player = GameObject.FindWithTag("Player");
        Transform anchor = player ? player.transform : (Camera.main ? Camera.main.transform : null);
        if (anchor != null)
        {
            Vector3 fwd = anchor.forward; fwd.y = 0f;
            if (fwd.sqrMagnitude < 0.01f) fwd = Vector3.forward;
            rt.position = anchor.position + fwd.normalized * 7f + Vector3.up * 2.5f;
            rt.rotation = Quaternion.LookRotation(fwd.normalized);
        }
        else if (SceneView.lastActiveSceneView != null)
        {
            rt.position = SceneView.lastActiveSceneView.pivot;
        }

        var bg = NewUI("Background", rt);
        Stretch(bg);
        var bgImg = bg.gameObject.AddComponent<Image>();
        bgImg.sprite = s_Res.standard;
        bgImg.type = Image.Type.Sliced;
        bgImg.color = new Color(0.08f, 0.12f, 0.18f, 0.8f);

        var title = NewText("Title", rt, "WEATHER STATION", 64, TextAlignmentOptions.Center);
        title.fontStyle = FontStyles.Bold;
        title.color = new Color(0.6f, 0.85f, 1f);
        var titleRT = title.rectTransform;
        titleRT.anchorMin = new Vector2(0, 1); titleRT.anchorMax = new Vector2(1, 1);
        titleRT.pivot = new Vector2(0.5f, 1);
        titleRT.sizeDelta = new Vector2(0, 110);
        titleRT.anchoredPosition = new Vector2(0, -20);

        var stats = NewText("Stats", rt, "Rain: ...", 48, TextAlignmentOptions.TopLeft);
        var statsRT = stats.rectTransform;
        statsRT.anchorMin = Vector2.zero; statsRT.anchorMax = Vector2.one;
        statsRT.offsetMin = new Vector2(60, 30);
        statsRT.offsetMax = new Vector2(-60, -140);

        var sign = signGO.AddComponent<WorldSpaceWeatherSign>();
        sign.statsText = stats;
        sign.rain = rain;
        sign.dust = dust;

        Selection.activeGameObject = signGO;
    }

    // ------------------------------------------------------------ Helpers

    internal static RectTransform NewUI(string name, Transform parent)
    {
        var go = new GameObject(name, typeof(RectTransform));
        go.layer = LayerMask.NameToLayer("UI");
        go.transform.SetParent(parent, false);
        return (RectTransform)go.transform;
    }

    internal static void Stretch(RectTransform rt)
    {
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = rt.offsetMax = Vector2.zero;
    }

    internal static TextMeshProUGUI NewText(string name, Transform parent, string text, float size, TextAlignmentOptions align)
    {
        var rt = NewUI(name, parent);
        var tmp = rt.gameObject.AddComponent<TextMeshProUGUI>();
        tmp.text = text;
        tmp.fontSize = size;
        tmp.alignment = align;
        tmp.color = Color.white;
        return tmp;
    }

    static Toggle NewToggle(Transform parent, string label)
    {
        var go = DefaultControls.CreateToggle(s_Res);
        go.name = label.Replace(" ", "") + "Toggle";
        go.transform.SetParent(parent, false);
        SetHeight(go, 44);

        // Bigger checkbox, label next to it.
        var bg = (RectTransform)go.transform.Find("Background");
        bg.sizeDelta = new Vector2(36, 36);
        bg.anchoredPosition = new Vector2(18, -22);
        var check = (RectTransform)bg.Find("Checkmark");
        Stretch(check);

        var lbl = SwapToTMP(go, 32, TextAlignmentOptions.MidlineLeft, Color.white);
        lbl.text = label;
        var lblRT = lbl.rectTransform;
        lblRT.offsetMin = new Vector2(60, 0);
        lblRT.offsetMax = Vector2.zero;

        var toggle = go.GetComponent<Toggle>();
        toggle.isOn = true;
        return toggle;
    }

    /// DefaultControls makes legacy Text; replace it with TextMeshPro so it looks crisp.
    static TextMeshProUGUI SwapToTMP(GameObject root, float size, TextAlignmentOptions align, Color color)
    {
        TextMeshProUGUI result = null;
        foreach (var t in root.GetComponentsInChildren<Text>(true))
        {
            var go = t.gameObject;
            string s = t.text;
            Object.DestroyImmediate(t);
            var tmp = go.AddComponent<TextMeshProUGUI>();
            tmp.text = s;
            tmp.fontSize = size;
            tmp.alignment = align;
            tmp.color = color;
            result = tmp;
        }
        return result;
    }

    static void SetHeight(GameObject go, float h)
    {
        var le = go.GetComponent<LayoutElement>();
        if (le == null) le = go.AddComponent<LayoutElement>();
        le.preferredHeight = h;
        le.minHeight = h;
    }

    static void EnsureEventSystem()
    {
        if (Object.FindAnyObjectByType<EventSystem>() != null) return;
        var es = new GameObject("EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));
        Undo.RegisterCreatedObjectUndo(es, "Add Weather UI");
    }
}
