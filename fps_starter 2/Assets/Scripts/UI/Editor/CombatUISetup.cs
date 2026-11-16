using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Editor helper: Tools > UI > Add Combat UI.
/// Builds the stats HUD, hit marker, game-over stats screen (screen space)
/// and the floating damage number template (world space). Save the scene afterward.
/// </summary>
public static class CombatUISetup
{
    [MenuItem("Tools/UI/Add Combat UI")]
    public static void AddCombatUI()
    {
        var uiSprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd");

        // ---------- Screen-space canvas
        var canvasGO = new GameObject("UI_CombatCanvas", typeof(RectTransform));
        Undo.RegisterCreatedObjectUndo(canvasGO, "Add Combat UI");
        var canvas = canvasGO.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 40;
        var scaler = canvasGO.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.matchWidthOrHeight = 0.5f;
        canvasGO.AddComponent<CombatStats>();

        // ---------- Stats HUD (right side)
        var stats = WeatherUISetup.NewUI("StatsHUD", canvasGO.transform);
        stats.anchorMin = stats.anchorMax = stats.pivot = new Vector2(1f, 0.5f);
        stats.anchoredPosition = new Vector2(-30, 150);
        stats.sizeDelta = new Vector2(300, 180);
        var statsBg = stats.gameObject.AddComponent<Image>();
        statsBg.sprite = uiSprite;
        statsBg.type = Image.Type.Sliced;
        statsBg.color = new Color(0f, 0f, 0f, 0.45f);
        var statsText = WeatherUISetup.NewText("Text", stats, "TIME 0:00", 28, TextAlignmentOptions.MidlineLeft);
        WeatherUISetup.Stretch(statsText.rectTransform);
        statsText.rectTransform.offsetMin = new Vector2(20, 10);
        statsText.rectTransform.offsetMax = new Vector2(-20, -10);
        stats.gameObject.AddComponent<StatsHUD>().statsText = statsText;

        // ---------- Hit marker (center, an X made of 4 bars)
        var marker = WeatherUISetup.NewUI("HitMarker", canvasGO.transform);
        marker.anchorMin = marker.anchorMax = marker.pivot = new Vector2(0.5f, 0.5f);
        marker.sizeDelta = new Vector2(48, 48);
        var markerGroup = marker.gameObject.AddComponent<CanvasGroup>();
        markerGroup.blocksRaycasts = false;
        markerGroup.interactable = false;
        var bars = new Image[4];
        Vector2[] dirs = { new Vector2(1, 1), new Vector2(-1, 1), new Vector2(1, -1), new Vector2(-1, -1) };
        for (int i = 0; i < 4; i++)
        {
            var bar = WeatherUISetup.NewUI("Bar" + i, marker);
            bar.sizeDelta = new Vector2(4, 14);
            bar.anchoredPosition = dirs[i] * 11f;
            bar.localRotation = Quaternion.Euler(0, 0, dirs[i].x * dirs[i].y > 0 ? -45f : 45f);
            bars[i] = bar.gameObject.AddComponent<Image>();
            bars[i].raycastTarget = false;
        }
        var hitMarker = marker.gameObject.AddComponent<HitMarker>();
        hitMarker.group = markerGroup;
        hitMarker.bars = bars;

        // ---------- Game-over stats screen (own sorting so it draws over the fade-to-black)
        var over = WeatherUISetup.NewUI("GameOverStats", canvasGO.transform);
        WeatherUISetup.Stretch(over);
        var overCanvas = over.gameObject.AddComponent<Canvas>();
        overCanvas.overrideSorting = true;
        overCanvas.sortingOrder = 500;
        var overGroup = over.gameObject.AddComponent<CanvasGroup>();

        var box = WeatherUISetup.NewUI("Box", over);
        box.anchorMin = box.anchorMax = box.pivot = new Vector2(0.5f, 0.5f);
        box.sizeDelta = new Vector2(720, 520);
        var boxImg = box.gameObject.AddComponent<Image>();
        boxImg.sprite = uiSprite;
        boxImg.type = Image.Type.Sliced;
        boxImg.color = new Color(0.08f, 0.1f, 0.14f, 0.92f);

        var title = WeatherUISetup.NewText("Title", box, "YOU DIED", 72, TextAlignmentOptions.Center);
        title.fontStyle = FontStyles.Bold;
        var titleRT = title.rectTransform;
        titleRT.anchorMin = new Vector2(0, 1); titleRT.anchorMax = new Vector2(1, 1);
        titleRT.pivot = new Vector2(0.5f, 1);
        titleRT.sizeDelta = new Vector2(0, 120);
        titleRT.anchoredPosition = new Vector2(0, -30);

        var overStats = WeatherUISetup.NewText("Stats", box, "", 38, TextAlignmentOptions.TopLeft);
        var osRT = overStats.rectTransform;
        WeatherUISetup.Stretch(osRT);
        osRT.offsetMin = new Vector2(80, 40);
        osRT.offsetMax = new Vector2(-80, -170);
        overStats.lineSpacing = 20;

        var gameOver = canvasGO.AddComponent<GameOverStats>();
        gameOver.panel = over.gameObject;
        gameOver.group = overGroup;
        gameOver.titleText = title;
        gameOver.statsText = overStats;

        // ---------- World-space damage number template
        var dmgGO = new GameObject("UI_DamageNumberTemplate", typeof(RectTransform));
        Undo.RegisterCreatedObjectUndo(dmgGO, "Add Combat UI");
        var dmgCanvas = dmgGO.AddComponent<Canvas>();
        dmgCanvas.renderMode = RenderMode.WorldSpace;
        dmgCanvas.sortingOrder = 10;
        var dmgRT = (RectTransform)dmgGO.transform;
        dmgRT.sizeDelta = new Vector2(200, 80);
        dmgRT.localScale = Vector3.one * 0.01f; // 2m x 0.8m
        var dmgText = WeatherUISetup.NewText("Text", dmgRT, "25", 64, TextAlignmentOptions.Center);
        WeatherUISetup.Stretch(dmgText.rectTransform);
        dmgText.fontStyle = FontStyles.Bold;
        dmgText.raycastTarget = false;
        var dmgNumber = dmgGO.AddComponent<DamageNumber>();
        dmgNumber.text = dmgText;
        dmgGO.SetActive(false);

        canvasGO.AddComponent<DamageNumbers>().template = dmgNumber;

        Selection.activeGameObject = canvasGO;
        Debug.Log("[UI] Combat UI added. Save the scene (Ctrl+S) and press Play.");
    }
}
