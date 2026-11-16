using UnityEditor;
using UnityEngine;

/// <summary>
/// Editor helper: Tools > VFX > Add Rain Weather / Add Floating Dust.
/// Builds real ParticleSystem components in the open scene so every setting can be
/// inspected and tweaked in the Inspector afterward. Save the scene after running it.
/// </summary>
public static class WeatherVFXSetup
{
    const string MaterialFolder = "Assets/Scripts/VFX/Materials";
    const string DotTexturePath = "Assets/FPS/Art/Textures/VFX/Dot.png";

    // ---------------------------------------------------------------- Rain

    [MenuItem("Tools/VFX/Add Rain Weather")]
    public static void AddRain()
    {
        var rainMat = GetOrCreateMaterial("Rain_Mat", new Color(0.75f, 0.85f, 1f, 0.6f), additive: false);
        var splashMat = GetOrCreateMaterial("RainSplash_Mat", new Color(0.85f, 0.9f, 1f, 0.8f), additive: false);

        // Root: the rain emitter, follows the player.
        var rainGO = new GameObject("VFX_Rain");
        Undo.RegisterCreatedObjectUndo(rainGO, "Add Rain Weather");
        rainGO.AddComponent<WeatherFollow>();
        var rain = rainGO.AddComponent<ParticleSystem>();
        rain.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

        var main = rain.main;
        main.duration = 5f;
        main.loop = true;
        main.prewarm = true;                       // already raining when the level starts
        main.startLifetime = 1.5f;
        main.startSpeed = new ParticleSystem.MinMaxCurve(18f, 24f);
        main.startSize = new ParticleSystem.MinMaxCurve(0.03f, 0.06f);
        main.startColor = new Color(0.75f, 0.85f, 1f, 0.55f);
        main.gravityModifier = 0.5f;
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.maxParticles = 10000;

        var emission = rain.emission;
        emission.rateOverTime = 3500f;

        // A wide flat box above the player, rotated so particles shoot straight down.
        var shape = rain.shape;
        shape.shapeType = ParticleSystemShapeType.Box;
        shape.rotation = new Vector3(90f, 0f, 0f);
        shape.scale = new Vector3(50f, 50f, 1f);

        // Slight wind so it doesn't look perfectly vertical.
        var vel = rain.velocityOverLifetime;
        vel.enabled = true;
        vel.space = ParticleSystemSimulationSpace.World;
        vel.x = new ParticleSystem.MinMaxCurve(1.5f, 2.5f);
        vel.y = new ParticleSystem.MinMaxCurve(0f, 0f);
        vel.z = new ParticleSystem.MinMaxCurve(0.5f, 1f);

        // Drops die when they hit the level geometry...
        var collision = rain.collision;
        collision.enabled = true;
        collision.type = ParticleSystemCollisionType.World;
        collision.mode = ParticleSystemCollisionMode.Collision3D;
        collision.quality = ParticleSystemCollisionQuality.Medium;
        collision.lifetimeLoss = 1f;
        collision.bounce = 0f;
        collision.radiusScale = 0.5f;

        // Stretched billboards turn round dots into streaks.
        var rainRenderer = rainGO.GetComponent<ParticleSystemRenderer>();
        rainRenderer.renderMode = ParticleSystemRenderMode.Stretch;
        rainRenderer.velocityScale = 0.06f;
        rainRenderer.lengthScale = 1f;
        rainRenderer.sharedMaterial = rainMat;
        rainRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;

        // ...and spawn a splash sub-emitter where they land.
        var splashGO = new GameObject("VFX_RainSplash");
        splashGO.transform.SetParent(rainGO.transform, false);
        var splash = splashGO.AddComponent<ParticleSystem>();
        splash.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

        var sMain = splash.main;
        sMain.duration = 0.5f;
        sMain.loop = false;
        sMain.playOnAwake = false;
        sMain.startLifetime = new ParticleSystem.MinMaxCurve(0.2f, 0.35f);
        sMain.startSpeed = new ParticleSystem.MinMaxCurve(1f, 2.5f);
        sMain.startSize = new ParticleSystem.MinMaxCurve(0.03f, 0.07f);
        sMain.startColor = new Color(0.85f, 0.9f, 1f, 0.8f);
        sMain.gravityModifier = 1f;
        sMain.simulationSpace = ParticleSystemSimulationSpace.World;
        sMain.maxParticles = 4000;

        var sEmission = splash.emission;
        sEmission.rateOverTime = 0f;
        sEmission.SetBursts(new[] { new ParticleSystem.Burst(0f, 2, 4) });

        var sShape = splash.shape;
        sShape.shapeType = ParticleSystemShapeType.Hemisphere;
        sShape.radius = 0.05f;
        sShape.rotation = new Vector3(-90f, 0f, 0f); // dome points up

        var sSize = splash.sizeOverLifetime;
        sSize.enabled = true;
        sSize.size = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.Linear(0f, 1f, 1f, 0f));

        var splashRenderer = splashGO.GetComponent<ParticleSystemRenderer>();
        splashRenderer.sharedMaterial = splashMat;
        splashRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;

        var subs = rain.subEmitters;
        subs.enabled = true;
        subs.AddSubEmitter(splash, ParticleSystemSubEmitterType.Collision,
            ParticleSystemSubEmitterProperties.InheritNothing);

        rain.Play(true);
        Selection.activeGameObject = rainGO;
        Debug.Log("[VFX] Rain added. Save the scene (Ctrl+S) and press Play.");
    }

    // ---------------------------------------------------------------- Dust motes

    [MenuItem("Tools/VFX/Add Floating Dust")]
    public static void AddDust()
    {
        var dustMat = GetOrCreateMaterial("Dust_Mat", new Color(1f, 0.9f, 0.6f, 1f), additive: true);

        var dustGO = new GameObject("VFX_FloatingDust");
        Undo.RegisterCreatedObjectUndo(dustGO, "Add Floating Dust");
        var follow = dustGO.AddComponent<WeatherFollow>();
        follow.offset = new Vector3(0f, 1f, 0f);
        var dust = dustGO.AddComponent<ParticleSystem>();
        dust.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

        var main = dust.main;
        main.loop = true;
        main.prewarm = true;
        main.startLifetime = new ParticleSystem.MinMaxCurve(4f, 7f);
        main.startSpeed = new ParticleSystem.MinMaxCurve(0.05f, 0.2f);
        main.startSize = new ParticleSystem.MinMaxCurve(0.03f, 0.08f);
        main.startColor = new ParticleSystem.MinMaxGradient(
            new Color(1f, 0.95f, 0.75f, 1f), new Color(1f, 0.75f, 0.4f, 1f));
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.maxParticles = 1000;

        var emission = dust.emission;
        emission.rateOverTime = 60f;

        var shape = dust.shape;
        shape.shapeType = ParticleSystemShapeType.Box;
        shape.scale = new Vector3(20f, 6f, 20f);

        // Noise makes them drift and swirl instead of moving in straight lines.
        var noise = dust.noise;
        noise.enabled = true;
        noise.strength = 0.4f;
        noise.frequency = 0.3f;
        noise.scrollSpeed = 0.2f;

        // Fade in, then fade out.
        var col = dust.colorOverLifetime;
        col.enabled = true;
        var grad = new Gradient();
        grad.SetKeys(
            new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
            new[] { new GradientAlphaKey(0f, 0f), new GradientAlphaKey(0.8f, 0.25f),
                    new GradientAlphaKey(0.8f, 0.75f), new GradientAlphaKey(0f, 1f) });
        col.color = grad;

        var renderer = dustGO.GetComponent<ParticleSystemRenderer>();
        renderer.sharedMaterial = dustMat;
        renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;

        dust.Play(true);
        Selection.activeGameObject = dustGO;
        Debug.Log("[VFX] Floating dust added. Save the scene (Ctrl+S) and press Play.");
    }

    // ---------------------------------------------------------------- Helpers

    static Material GetOrCreateMaterial(string name, Color tint, bool additive)
    {
        string path = $"{MaterialFolder}/{name}.mat";
        var existing = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (existing != null) return existing;

        if (!AssetDatabase.IsValidFolder(MaterialFolder))
            AssetDatabase.CreateFolder("Assets/Scripts/VFX", "Materials");

        var shader = Shader.Find("Universal Render Pipeline/Particles/Unlit")
                     ?? Shader.Find("Particles/Standard Unlit");
        var mat = new Material(shader) { name = name };

        var tex = AssetDatabase.LoadAssetAtPath<Texture2D>(DotTexturePath);
        if (tex != null) mat.SetTexture("_BaseMap", tex);
        mat.SetColor("_BaseColor", tint);

        // URP particle shader: transparent surface, alpha or additive blending.
        mat.SetFloat("_Surface", 1f);
        mat.SetFloat("_Blend", additive ? 2f : 0f);
        mat.SetFloat("_SrcBlend", (float)UnityEngine.Rendering.BlendMode.SrcAlpha);
        mat.SetFloat("_DstBlend", additive
            ? (float)UnityEngine.Rendering.BlendMode.One
            : (float)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
        mat.SetFloat("_ZWrite", 0f);
        mat.SetOverrideTag("RenderType", "Transparent");
        mat.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
        mat.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent;

        AssetDatabase.CreateAsset(mat, path);
        AssetDatabase.SaveAssets();
        return mat;
    }
}
