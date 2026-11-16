using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Editor helpers:
///   Tools > VFX > Add Lightning Storm      - lightning system in the scene (+ toggle in the V menu)
///   Tools > VFX > Create Y Bot Death Burst - builds a death-explosion prefab and assigns it to every MonsterScript
/// Save the scene afterward.
/// </summary>
public static class ExtraVFXSetup
{
    const string PrefabFolder = "Assets/Scripts/VFX/Prefabs";

    // =====================================================================  Lightning

    [MenuItem("Tools/VFX/Add Lightning Storm")]
    public static void AddLightning()
    {
        var boltMat = WeatherVFXSetup.GetOrCreateMaterial("LightningBolt_Mat", new Color(0.85f, 0.92f, 1f, 1f), true, null);
        var glowMat = WeatherVFXSetup.GetOrCreateMaterial("LightningGlow_Mat", new Color(0.7f, 0.8f, 1f, 1f), true);

        var root = new GameObject("VFX_LightningStorm");
        Undo.RegisterCreatedObjectUndo(root, "Add Lightning Storm");
        var storm = root.AddComponent<LightningStorm>();

        // Bolt
        var boltGO = new GameObject("Bolt");
        boltGO.transform.SetParent(root.transform, false);
        var line = boltGO.AddComponent<LineRenderer>();
        line.useWorldSpace = true;
        line.sharedMaterial = boltMat;
        line.widthCurve = new AnimationCurve(new Keyframe(0f, 0.6f), new Keyframe(1f, 0.25f));
        line.numCapVertices = 2;
        line.numCornerVertices = 2;
        line.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        line.receiveShadows = false;
        line.positionCount = 0;
        line.enabled = false;

        // Glow burst in the clouds: a few huge soft particles.
        var glow = NewBurstSystem("SkyGlow", root.transform, glowMat);
        var gm = glow.main;
        gm.startLifetime = new ParticleSystem.MinMaxCurve(0.3f, 0.5f);
        gm.startSpeed = 0f;
        gm.startSize = new ParticleSystem.MinMaxCurve(18f, 30f);
        gm.startColor = new Color(0.75f, 0.85f, 1f, 0.5f);
        glow.emission.SetBursts(new[] { new ParticleSystem.Burst(0f, 6) });
        var gs = glow.shape; gs.shapeType = ParticleSystemShapeType.Sphere; gs.radius = 8f;
        FadeOut(glow);

        // Sparks where it hits the ground.
        var sparks = NewBurstSystem("GroundSparks", root.transform, glowMat);
        var sm = sparks.main;
        sm.startLifetime = new ParticleSystem.MinMaxCurve(0.4f, 0.9f);
        sm.startSpeed = new ParticleSystem.MinMaxCurve(4f, 11f);
        sm.startSize = new ParticleSystem.MinMaxCurve(0.06f, 0.14f);
        sm.startColor = new Color(0.8f, 0.9f, 1f, 1f);
        sm.gravityModifier = 1.5f;
        sparks.emission.SetBursts(new[] { new ParticleSystem.Burst(0f, 50) });
        var ss = sparks.shape; ss.shapeType = ParticleSystemShapeType.Hemisphere; ss.radius = 0.3f;
        ss.rotation = new Vector3(-90f, 0f, 0f);
        AddBounce(sparks, 0.3f);
        Stretch(sparks, 0.05f);
        FadeOut(sparks);

        storm.bolt = line;
        storm.skyGlow = glow;
        storm.groundSparks = sparks;
        var rainGO = GameObject.Find("VFX_Rain");
        if (rainGO) storm.rain = rainGO.GetComponent<ParticleSystem>();

        AddLightningToggleToMenu(storm);

        Selection.activeGameObject = root;
        Debug.Log("[VFX] Lightning added. Save the scene, press Play, and press L to force a strike.");
    }

    /// Adds a "Lightning" toggle to the existing Weather Settings menu by cloning the Dust toggle.
    static void AddLightningToggleToMenu(LightningStorm storm)
    {
        var menu = Object.FindAnyObjectByType<WeatherSettingsMenu>(FindObjectsInactive.Include);
        if (menu == null || menu.dustToggle == null)
        {
            Debug.Log("[VFX] No Weather Settings menu found; skipping the menu toggle.");
            return;
        }
        if (menu.lightningToggle != null)
        {
            Undo.RecordObject(menu, "Add Lightning Storm");
            menu.lightning = storm;
            return;
        }

        var dust = menu.dustToggle.gameObject;
        var clone = Object.Instantiate(dust, dust.transform.parent);
        Undo.RegisterCreatedObjectUndo(clone, "Add Lightning Storm");
        clone.name = "LightningToggle";
        clone.transform.SetSiblingIndex(dust.transform.GetSiblingIndex() + 1);
        var label = clone.GetComponentInChildren<TMPro.TMP_Text>();
        if (label) label.text = "Lightning";

        // Make the panel taller to fit the new row.
        var panel = dust.transform.parent as RectTransform;
        if (panel != null)
        {
            Undo.RecordObject(panel, "Add Lightning Storm");
            panel.sizeDelta += new Vector2(0f, 62f);
        }

        Undo.RecordObject(menu, "Add Lightning Storm");
        menu.lightningToggle = clone.GetComponent<Toggle>();
        menu.lightning = storm;
        EditorUtility.SetDirty(menu);
    }

    // =====================================================================  Y Bot death burst

    [MenuItem("Tools/VFX/Create Y Bot Death Burst")]
    public static void CreateDeathBurst()
    {
        var sparkMat = WeatherVFXSetup.GetOrCreateMaterial("BurstSpark_Mat", new Color(1f, 0.75f, 0.35f, 1f), true);
        var smokeMat = WeatherVFXSetup.GetOrCreateMaterial("BurstSmoke_Mat", new Color(0.35f, 0.35f, 0.38f, 1f), false);
        var debrisMat = WeatherVFXSetup.GetOrCreateMaterial("BurstDebris_Mat", Color.white, false, null);

        // Root = bright core flash.
        var root = new GameObject("VFX_YBotDeathBurst");
        var flash = root.AddComponent<ParticleSystem>();
        ConfigureBurst(flash, root.GetComponent<ParticleSystemRenderer>(), sparkMat);
        var fm = flash.main;
        fm.startLifetime = new ParticleSystem.MinMaxCurve(0.12f, 0.2f);
        fm.startSpeed = 0f;
        fm.startSize = new ParticleSystem.MinMaxCurve(2.5f, 4f);
        fm.startColor = new Color(1f, 0.9f, 0.7f, 1f);
        flash.emission.SetBursts(new[] { new ParticleSystem.Burst(0f, 3) });
        FadeOut(flash);

        // Sparks: fast, stretched, bounce on the floor.
        var sparks = NewBurstSystem("Sparks", root.transform, sparkMat);
        var sm = sparks.main;
        sm.startLifetime = new ParticleSystem.MinMaxCurve(0.5f, 1.1f);
        sm.startSpeed = new ParticleSystem.MinMaxCurve(6f, 14f);
        sm.startSize = new ParticleSystem.MinMaxCurve(0.04f, 0.1f);
        sm.startColor = new Color(1f, 0.85f, 0.4f, 1f);
        sm.gravityModifier = 1.2f;
        sparks.emission.SetBursts(new[] { new ParticleSystem.Burst(0f, 70) });
        var ss = sparks.shape; ss.shapeType = ParticleSystemShapeType.Sphere; ss.radius = 0.2f;
        AddBounce(sparks, 0.4f);
        Stretch(sparks, 0.04f);
        var sCol = sparks.colorOverLifetime; sCol.enabled = true;
        sCol.color = Gradient2(new Color(1f, 0.95f, 0.6f), new Color(1f, 0.3f, 0.1f), 1f, 0f);

        // Smoke: slow, grows, rises, fades.
        var smoke = NewBurstSystem("Smoke", root.transform, smokeMat);
        var smm = smoke.main;
        smm.startLifetime = new ParticleSystem.MinMaxCurve(1.5f, 2.5f);
        smm.startSpeed = new ParticleSystem.MinMaxCurve(0.5f, 2f);
        smm.startSize = new ParticleSystem.MinMaxCurve(1.2f, 2.2f);
        smm.startRotation = new ParticleSystem.MinMaxCurve(0f, Mathf.PI * 2f);
        smm.startColor = new Color(0.3f, 0.3f, 0.33f, 0.7f);
        smm.gravityModifier = -0.05f;
        smoke.emission.SetBursts(new[] { new ParticleSystem.Burst(0f, 14) });
        var smShape = smoke.shape; smShape.shapeType = ParticleSystemShapeType.Sphere; smShape.radius = 0.5f;
        var smSize = smoke.sizeOverLifetime; smSize.enabled = true;
        smSize.size = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.EaseInOut(0f, 0.6f, 1f, 1.6f));
        var smRot = smoke.rotationOverLifetime; smRot.enabled = true;
        smRot.z = new ParticleSystem.MinMaxCurve(-0.6f, 0.6f);
        FadeOut(smoke);

        // Debris: little blue robot chunks (mesh particles) that tumble and bounce.
        var debris = NewBurstSystem("Debris", root.transform, debrisMat);
        var dm = debris.main;
        dm.startLifetime = new ParticleSystem.MinMaxCurve(2f, 3f);
        dm.startSpeed = new ParticleSystem.MinMaxCurve(4f, 8f);
        dm.startSize = new ParticleSystem.MinMaxCurve(0.08f, 0.2f);
        dm.startRotation3D = true;
        dm.startRotationX = new ParticleSystem.MinMaxCurve(0f, Mathf.PI * 2f);
        dm.startRotationY = new ParticleSystem.MinMaxCurve(0f, Mathf.PI * 2f);
        dm.startRotationZ = new ParticleSystem.MinMaxCurve(0f, Mathf.PI * 2f);
        dm.startColor = new ParticleSystem.MinMaxGradient(new Color(0.15f, 0.35f, 0.85f), new Color(0.6f, 0.7f, 0.9f));
        dm.gravityModifier = 1.5f;
        debris.emission.SetBursts(new[] { new ParticleSystem.Burst(0f, 16) });
        var dShape = debris.shape; dShape.shapeType = ParticleSystemShapeType.Hemisphere; dShape.radius = 0.3f;
        dShape.rotation = new Vector3(-90f, 0f, 0f);
        var dRot = debris.rotationOverLifetime; dRot.enabled = true; dRot.separateAxes = true;
        dRot.x = new ParticleSystem.MinMaxCurve(-6f, 6f);
        dRot.y = new ParticleSystem.MinMaxCurve(-6f, 6f);
        dRot.z = new ParticleSystem.MinMaxCurve(-6f, 6f);
        var dCol = debris.collision;
        dCol.enabled = true; dCol.type = ParticleSystemCollisionType.World;
        dCol.bounce = 0.5f; dCol.dampen = 0.3f; dCol.lifetimeLoss = 0f;
        var dRend = debris.GetComponent<ParticleSystemRenderer>();
        dRend.renderMode = ParticleSystemRenderMode.Mesh;
        dRend.mesh = Resources.GetBuiltinResource<Mesh>("Cube.fbx");
        var dFade = debris.colorOverLifetime; dFade.enabled = true;
        dFade.color = Gradient2(Color.white, Color.white, 1f, 0f, 0.85f);

        // Shockwave: a flat disc that expands along the ground.
        var wave = NewBurstSystem("Shockwave", root.transform, sparkMat);
        wave.transform.localPosition = new Vector3(0f, -1.1f, 0f); // near the floor (spawned 1.2m up)
        var wm = wave.main;
        wm.startLifetime = 0.45f;
        wm.startSpeed = 0f;
        wm.startSize = 1f;
        wm.startColor = new Color(0.4f, 0.75f, 1f, 0.8f);
        wave.emission.SetBursts(new[] { new ParticleSystem.Burst(0f, 1) });
        var wShape = wave.shape; wShape.enabled = false;
        var wSize = wave.sizeOverLifetime; wSize.enabled = true;
        wSize.size = new ParticleSystem.MinMaxCurve(8f, AnimationCurve.EaseInOut(0f, 0.1f, 1f, 1f));
        wave.GetComponent<ParticleSystemRenderer>().renderMode = ParticleSystemRenderMode.HorizontalBillboard;
        FadeOut(wave);

        // Orange light flash.
        var lightGO = new GameObject("FlashLight");
        lightGO.transform.SetParent(root.transform, false);
        var light = lightGO.AddComponent<Light>();
        light.type = LightType.Point;
        light.color = new Color(1f, 0.6f, 0.3f);
        light.range = 10f;
        light.intensity = 8f;
        light.shadows = LightShadows.None;

        var life = root.AddComponent<VFXBurstLifetime>();
        life.lifetime = 4f;
        life.flashLight = light;

        // Save as a prefab.
        if (!AssetDatabase.IsValidFolder(PrefabFolder))
            AssetDatabase.CreateFolder("Assets/Scripts/VFX", "Prefabs");
        string path = $"{PrefabFolder}/VFX_YBotDeathBurst.prefab";
        var prefab = PrefabUtility.SaveAsPrefabAsset(root, path);
        Object.DestroyImmediate(root);

        // Hook it up to every Y Bot (MonsterScript) in the open scene.
        int count = 0;
        foreach (var monster in Object.FindObjectsByType<MonsterScript>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            Undo.RecordObject(monster, "Assign Death VFX");
            monster.deathVFX = prefab;
            PrefabUtility.RecordPrefabInstancePropertyModifications(monster);
            count++;
        }
        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());

        Selection.activeObject = prefab;
        EditorGUIUtility.PingObject(prefab);
        Debug.Log($"[VFX] Saved {path} and assigned it to {count} MonsterScript object(s). Save the scene.");
    }

    // =====================================================================  Helpers

    static ParticleSystem NewBurstSystem(string name, Transform parent, Material mat)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        var ps = go.AddComponent<ParticleSystem>();
        ConfigureBurst(ps, go.GetComponent<ParticleSystemRenderer>(), mat);
        return ps;
    }

    /// One-shot defaults: no looping, no constant emission, world space, no shadows.
    static void ConfigureBurst(ParticleSystem ps, ParticleSystemRenderer r, Material mat)
    {
        ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        var main = ps.main;
        main.duration = 1f;
        main.loop = false;
        main.playOnAwake = true;
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        var em = ps.emission;
        em.rateOverTime = 0f;
        r.sharedMaterial = mat;
        r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        r.receiveShadows = false;
    }

    static void FadeOut(ParticleSystem ps)
    {
        var col = ps.colorOverLifetime;
        col.enabled = true;
        col.color = Gradient2(Color.white, Color.white, 1f, 0f);
    }

    static void AddBounce(ParticleSystem ps, float bounce)
    {
        var c = ps.collision;
        c.enabled = true;
        c.type = ParticleSystemCollisionType.World;
        c.quality = ParticleSystemCollisionQuality.Medium;
        c.bounce = bounce;
        c.dampen = 0.3f;
        c.lifetimeLoss = 0.2f;
    }

    static void Stretch(ParticleSystem ps, float velocityScale)
    {
        var r = ps.GetComponent<ParticleSystemRenderer>();
        r.renderMode = ParticleSystemRenderMode.Stretch;
        r.velocityScale = velocityScale;
        r.lengthScale = 1f;
    }

    static Gradient Gradient2(Color a, Color b, float alphaStart, float alphaEnd, float holdUntil = 0f)
    {
        var g = new Gradient();
        var alphas = holdUntil > 0f
            ? new[] { new GradientAlphaKey(alphaStart, 0f), new GradientAlphaKey(alphaStart, holdUntil), new GradientAlphaKey(alphaEnd, 1f) }
            : new[] { new GradientAlphaKey(alphaStart, 0f), new GradientAlphaKey(alphaEnd, 1f) };
        g.SetKeys(new[] { new GradientColorKey(a, 0f), new GradientColorKey(b, 1f) }, alphas);
        return g;
    }
}
