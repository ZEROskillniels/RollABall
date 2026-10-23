using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class RollABallSceneBuilder
{
    private const string ScenePath = "Assets/Scenes/rollaball.unity";
    private const string PrefabPath = "Assets/Prefabs/Pick Up.prefab";

    [MenuItem("Tools/RollABall/Build Assignment Scene")]
    public static void Build()
    {
        Scene scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
        EnsureFolder("Assets/Prefabs");
        EnsureFolder("Assets/Textures");
        EnsureTag("PickUp");

        Material groundMaterial = CreateMaterial("Background", Color.white, 0.08f, 0.55f,
            CreateTexture("PinballPlayfield", TexturePattern.Ground), new Vector2(4f, 4f));
        Material wallMaterial = CreateMaterial("WallMaterial", Color.white, 0.72f, 0.78f,
            CreateTexture("PinballRail", TexturePattern.Rail), Vector2.one);
        Material playerMaterial = CreateMaterial("PlayerMaterial", new Color(0.82f, 0.9f, 0.98f), 1f, 0.96f,
            null, Vector2.one);
        Material pickupMaterial = CreateMaterial("PickUpMaterial", new Color(1f, 0.92f, 0.28f), 0.35f, 0.78f,
            CreateTexture("PickupFacets", TexturePattern.Pickup), Vector2.one);
        Material neonMaterial = CreateMaterial("NeonRailMaterial", new Color(0.06f, 0.75f, 1f), 0f, 0.75f,
            null, Vector2.one);
        neonMaterial.SetColor("_EmissionColor", new Color(0.05f, 0.85f, 1f) * 2f);
        neonMaterial.EnableKeyword("_EMISSION");
        EditorUtility.SetDirty(neonMaterial);
        Material trailMaterial = CreateMaterial("PinballTrailMaterial", new Color(0.35f, 0.9f, 1f), 0f, 0f,
            null, Vector2.one);

        GameObject ground = GameObject.Find("Ground");
        ground.transform.SetPositionAndRotation(Vector3.zero, Quaternion.identity);
        ground.transform.localScale = new Vector3(2f, 1f, 2f);
        ground.GetComponent<Renderer>().sharedMaterial = groundMaterial;

        GameObject player = GameObject.Find("Player");
        player.transform.SetPositionAndRotation(new Vector3(0f, 2f, 0f), Quaternion.identity);
        player.transform.localScale = Vector3.one;
        player.GetComponent<Renderer>().sharedMaterial = playerMaterial;
        TrailRenderer trail = player.GetComponent<TrailRenderer>();
        if (trail == null) trail = player.AddComponent<TrailRenderer>();
        trail.sharedMaterial = trailMaterial;
        trail.time = 0.3f;
        trail.startWidth = 0.18f;
        trail.endWidth = 0f;
        trail.minVertexDistance = 0.06f;
        trail.emitting = true;
        Gradient trailColors = new Gradient();
        trailColors.SetKeys(
            new[] { new GradientColorKey(new Color(0.55f, 0.95f, 1f), 0f), new GradientColorKey(new Color(0.05f, 0.5f, 1f), 1f) },
            new[] { new GradientAlphaKey(0.65f, 0f), new GradientAlphaKey(0f, 1f) });
        trail.colorGradient = trailColors;
        Transform ballLight = player.transform.Find("Pinball Ball Light");
        if (ballLight == null)
        {
            GameObject lightObject = new GameObject("Pinball Ball Light");
            ballLight = lightObject.transform;
            ballLight.SetParent(player.transform);
            ballLight.localPosition = Vector3.zero;
        }
        Light ballGlow = ballLight.GetComponent<Light>();
        if (ballGlow == null) ballGlow = ballLight.gameObject.AddComponent<Light>();
        ballGlow.type = LightType.Point;
        ballGlow.color = new Color(0.55f, 0.9f, 1f);
        ballGlow.intensity = 1.5f;
        ballGlow.range = 3f;
        Rigidbody rb = player.GetComponent<Rigidbody>();
        if (rb == null) rb = player.AddComponent<Rigidbody>();
        rb.interpolation = RigidbodyInterpolation.Interpolate;
        rb.collisionDetectionMode = CollisionDetectionMode.Continuous;
        PlayerController controller = player.GetComponent<PlayerController>();
        if (controller == null) controller = player.AddComponent<PlayerController>();
        controller.speed = 18f;
        AudioSource audio = player.GetComponent<AudioSource>();
        if (audio == null) audio = player.AddComponent<AudioSource>();
        audio.playOnAwake = false;
        audio.spatialBlend = 0f;

        GameObject camera = GameObject.Find("Main Camera");
        camera.transform.SetPositionAndRotation(new Vector3(0f, 10f, 10f), Quaternion.Euler(45f, 180f, 0f));
        Camera sceneCamera = camera.GetComponent<Camera>();
        sceneCamera.clearFlags = CameraClearFlags.SolidColor;
        sceneCamera.backgroundColor = new Color(0.015f, 0.025f, 0.055f);
        CameraController cameraController = camera.GetComponent<CameraController>();
        if (cameraController == null) cameraController = camera.AddComponent<CameraController>();
        cameraController.player = player;
        cameraController.smoothTime = 0.18f;

        GameObject sunlight = GameObject.Find("Directional Light");
        sunlight.transform.rotation = Quaternion.Euler(50f, 60f, 0f);
        sunlight.GetComponent<Light>().intensity = 1.25f;

        BuildWalls(wallMaterial, neonMaterial);
        BuildInvisibleSafetyWalls();
        BuildPickups(pickupMaterial);
        BuildLights();

        DestroyIfPresent("Game Manager");
        GameObject manager = new GameObject("Game Manager");
        manager.AddComponent<GameManager>().levelDuration = 180f;

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Selection.activeGameObject = player;
        Debug.Log("RollABall Pinball scene built: 12 Pick Ups, 12 visible rails, 4 additional invisible safety walls and bonus systems.");
    }

    private static void BuildWalls(Material material, Material neonMaterial)
    {
        DestroyIfPresent("Walls");
        GameObject root = new GameObject("Walls");

        CreateWall(root.transform, "West Wall", new Vector3(-10f, 0f, 0f), new Vector3(0.5f, 2f, 20.5f), material, neonMaterial);
        CreateWall(root.transform, "East Wall", new Vector3(10f, 0f, 0f), new Vector3(0.5f, 2f, 20.5f), material, neonMaterial);
        CreateWall(root.transform, "North Wall", new Vector3(0f, 0f, 10f), new Vector3(20.5f, 2f, 0.5f), material, neonMaterial);
        CreateWall(root.transform, "South Wall", new Vector3(0f, 0f, -10f), new Vector3(20.5f, 2f, 0.5f), material, neonMaterial);

        // Two separated segments per lane leave a clear passage for the ball.
        CreateWall(root.transform, "Maze Wall 1", new Vector3(-6f, 0f, -6.5f), new Vector3(0.35f, 1.5f, 6f), material, neonMaterial);
        CreateWall(root.transform, "Maze Wall 2", new Vector3(-6f, 0f, 4.5f), new Vector3(0.35f, 1.5f, 10f), material, neonMaterial);
        CreateWall(root.transform, "Maze Wall 3", new Vector3(-2f, 0f, -4.5f), new Vector3(0.35f, 1.5f, 10f), material, neonMaterial);
        CreateWall(root.transform, "Maze Wall 4", new Vector3(-2f, 0f, 6.5f), new Vector3(0.35f, 1.5f, 6f), material, neonMaterial);
        CreateWall(root.transform, "Maze Wall 5", new Vector3(2f, 0f, -6.5f), new Vector3(0.35f, 1.5f, 6f), material, neonMaterial);
        CreateWall(root.transform, "Maze Wall 6", new Vector3(2f, 0f, 4.5f), new Vector3(0.35f, 1.5f, 10f), material, neonMaterial);
        CreateWall(root.transform, "Maze Wall 7", new Vector3(6f, 0f, -4.5f), new Vector3(0.35f, 1.5f, 10f), material, neonMaterial);
        CreateWall(root.transform, "Maze Wall 8", new Vector3(6f, 0f, 6.5f), new Vector3(0.35f, 1.5f, 6f), material, neonMaterial);
    }

    private static void CreateWall(Transform parent, string name, Vector3 position, Vector3 scale, Material material, Material neonMaterial)
    {
        GameObject wall = GameObject.CreatePrimitive(PrimitiveType.Cube);
        wall.name = name;
        wall.transform.SetParent(parent);
        wall.transform.position = position;
        wall.transform.localScale = scale;
        wall.GetComponent<Renderer>().sharedMaterial = material;
        // Keep the metal panel pattern at a similar world-space size on every wall face.
        Mesh mesh = Object.Instantiate(wall.GetComponent<MeshFilter>().sharedMesh);
        Vector3[] vertices = mesh.vertices;
        Vector3[] normals = mesh.normals;
        Vector2[] uv = new Vector2[vertices.Length];
        for (int i = 0; i < vertices.Length; i++)
        {
            Vector3 vertex = vertices[i];
            Vector3 normal = normals[i];
            if (Mathf.Abs(normal.x) > 0.5f)
                uv[i] = new Vector2(vertex.z * scale.z / 2f, vertex.y * scale.y / 2f);
            else if (Mathf.Abs(normal.z) > 0.5f)
                uv[i] = new Vector2(vertex.x * scale.x / 2f, vertex.y * scale.y / 2f);
            else
                uv[i] = new Vector2(vertex.x * scale.x / 2f, vertex.z * scale.z / 2f);
        }
        mesh.uv = uv;
        wall.GetComponent<MeshFilter>().sharedMesh = mesh;

        GameObject trim = GameObject.CreatePrimitive(PrimitiveType.Cube);
        trim.name = "Neon Edge";
        trim.transform.SetParent(wall.transform);
        trim.transform.localPosition = new Vector3(0f, 0.5f + 0.04f / scale.y, 0f);
        trim.transform.localScale = scale.x > scale.z
            ? new Vector3(1f, 0.08f / scale.y, 0.08f / scale.z)
            : new Vector3(0.08f / scale.x, 0.08f / scale.y, 1f);
        trim.GetComponent<Renderer>().sharedMaterial = neonMaterial;
        Object.DestroyImmediate(trim.GetComponent<BoxCollider>());
    }

    private static void BuildInvisibleSafetyWalls()
    {
        DestroyIfPresent("ADDITIONAL - Invisible Safety Walls");
        GameObject root = new GameObject("ADDITIONAL - Invisible Safety Walls");
        CreateInvisibleWall(root.transform, "West Safety Collider", new Vector3(-10.75f, 0.75f, 0f), new Vector3(0.6f, 4f, 22.5f));
        CreateInvisibleWall(root.transform, "East Safety Collider", new Vector3(10.75f, 0.75f, 0f), new Vector3(0.6f, 4f, 22.5f));
        CreateInvisibleWall(root.transform, "North Safety Collider", new Vector3(0f, 0.75f, 10.75f), new Vector3(22.5f, 4f, 0.6f));
        CreateInvisibleWall(root.transform, "South Safety Collider", new Vector3(0f, 0.75f, -10.75f), new Vector3(22.5f, 4f, 0.6f));
    }

    private static void CreateInvisibleWall(Transform parent, string name, Vector3 position, Vector3 size)
    {
        GameObject safetyWall = new GameObject(name);
        safetyWall.transform.SetParent(parent);
        safetyWall.transform.position = position;
        safetyWall.AddComponent<BoxCollider>().size = size;
    }

    private static void BuildPickups(Material material)
    {
        DestroyIfPresent("Pick Ups");

        GameObject template = GameObject.CreatePrimitive(PrimitiveType.Cube);
        template.name = "Pick Up";
        template.tag = "PickUp";
        template.transform.position = new Vector3(0f, 0.5f, 0f);
        template.transform.rotation = Quaternion.Euler(45f, 45f, 45f);
        template.transform.localScale = Vector3.one * 0.5f;
        template.GetComponent<BoxCollider>().isTrigger = true;
        template.GetComponent<Renderer>().sharedMaterial = material;
        template.AddComponent<Rotator>();

        GameObject prefab = PrefabUtility.SaveAsPrefabAsset(template, PrefabPath);
        Object.DestroyImmediate(template);

        GameObject root = new GameObject("Pick Ups");
        Vector2[] positions =
        {
            new Vector2(-8f, -7f), new Vector2(-8f, 0f), new Vector2(-8f, 7f),
            new Vector2(-4f, -6f), new Vector2(-4f, 2f), new Vector2(-4f, 7f),
            new Vector2(0f, -7f), new Vector2(0f, 0f), new Vector2(0f, 7f),
            new Vector2(4f, -6f), new Vector2(4f, 6f), new Vector2(8f, 0f)
        };

        for (int i = 0; i < positions.Length; i++)
        {
            GameObject pickup = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
            pickup.name = $"Pick Up {i + 1:00}";
            pickup.transform.SetParent(root.transform);
            pickup.transform.position = new Vector3(positions[i].x, 0.5f, positions[i].y);
        }
    }

    private static void BuildLights()
    {
        DestroyIfPresent("Bonus Lights");
        GameObject root = new GameObject("Bonus Lights");
        CreateLight(root.transform, "Blue Point Light", LightType.Point, new Vector3(-6f, 4f, -5f), new Color(0.15f, 0.65f, 1f), 3.5f, 12f);
        CreateLight(root.transform, "Amber Point Light", LightType.Point, new Vector3(5f, 4f, 6f), new Color(1f, 0.52f, 0.15f), 3.5f, 12f);
        Light spot = CreateLight(root.transform, "Violet Spot Light", LightType.Spot, new Vector3(0f, 9f, 0f), new Color(0.65f, 0.38f, 1f), 5f, 18f);
        spot.spotAngle = 75f;
        spot.transform.rotation = Quaternion.Euler(90f, 0f, 0f);
    }

    private static Light CreateLight(Transform parent, string name, LightType type, Vector3 position, Color color, float intensity, float range)
    {
        GameObject lightObject = new GameObject(name);
        lightObject.transform.SetParent(parent);
        lightObject.transform.position = position;
        Light light = lightObject.AddComponent<Light>();
        light.type = type;
        light.color = color;
        light.intensity = intensity;
        light.range = range;
        return light;
    }

    private static Material CreateMaterial(string name, Color color, float metallic, float smoothness, Texture2D texture, Vector2 tiling)
    {
        string path = $"Assets/Materials/{name}.mat";
        Material material = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (material == null)
        {
            Shader shader = Shader.Find("Universal Render Pipeline/Lit");
            material = new Material(shader);
            AssetDatabase.CreateAsset(material, path);
        }

        material.SetColor("_BaseColor", color);
        material.SetFloat("_Metallic", metallic);
        material.SetFloat("_Smoothness", smoothness);
        material.SetTexture("_BaseMap", texture);
        material.SetTexture("_MainTex", texture);
        material.SetTextureScale("_BaseMap", tiling);
        EditorUtility.SetDirty(material);
        return material;
    }

    private enum TexturePattern { Ground, Rail, Pickup }

    private static Texture2D CreateTexture(string name, TexturePattern pattern)
    {
        const int size = 256;
        string path = $"Assets/Textures/{name}.png";
        Texture2D image = new Texture2D(size, size, TextureFormat.RGBA32, true);

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float grain = (Mathf.PerlinNoise(x * 0.12f, y * 0.12f) - 0.5f) * 0.07f;
                Color color;
                switch (pattern)
                {
                    case TexturePattern.Ground:
                        float radius = Vector2.Distance(new Vector2(x, y), new Vector2(128f, 128f));
                        bool ring = Mathf.Abs(radius - 58f) < 3f || Mathf.Abs(radius - 92f) < 2f;
                        bool target = Mathf.Abs(x - 128) + Mathf.Abs(y - 128) < 15;
                        color = target ? new Color(0.8f, 0.22f, 0.5f)
                            : ring ? new Color(0.08f, 0.48f, 0.62f)
                            : new Color(0.035f, 0.065f, 0.12f);
                        break;
                    case TexturePattern.Rail:
                        bool neonLine = (y > 26 && y < 34) || (y > 214 && y < 222);
                        bool panelSeam = x % 64 < 3;
                        bool rivet = (x % 64 > 29 && x % 64 < 35) && (y < 8 || y > 248);
                        color = neonLine ? new Color(0.05f, 0.9f, 1f)
                            : rivet ? new Color(0.76f, 0.86f, 0.91f)
                            : panelSeam ? new Color(0.02f, 0.04f, 0.08f)
                            : new Color(0.13f, 0.19f, 0.26f);
                        break;
                    default:
                        bool facet = ((x + y) / 28) % 2 == 0;
                        color = facet ? new Color(1f, 0.95f, 0.4f) : new Color(0.9f, 0.58f, 0.12f);
                        break;
                }
                image.SetPixel(x, y, color + new Color(grain, grain, grain, 0f));
            }
        }

        image.Apply(true);
        File.WriteAllBytes(path, image.EncodeToPNG());
        Object.DestroyImmediate(image);
        AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);
        TextureImporter importer = (TextureImporter)AssetImporter.GetAtPath(path);
        importer.wrapMode = TextureWrapMode.Repeat;
        importer.filterMode = FilterMode.Bilinear;
        importer.SaveAndReimport();
        return AssetDatabase.LoadAssetAtPath<Texture2D>(path);
    }

    private static void EnsureTag(string tag)
    {
        Object tagManager = AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset")[0];
        SerializedObject serialized = new SerializedObject(tagManager);
        SerializedProperty tags = serialized.FindProperty("tags");
        for (int i = 0; i < tags.arraySize; i++)
            if (tags.GetArrayElementAtIndex(i).stringValue == tag)
                return;

        tags.InsertArrayElementAtIndex(tags.arraySize);
        tags.GetArrayElementAtIndex(tags.arraySize - 1).stringValue = tag;
        serialized.ApplyModifiedProperties();
    }

    private static void EnsureFolder(string path)
    {
        if (!AssetDatabase.IsValidFolder(path))
            AssetDatabase.CreateFolder("Assets", Path.GetFileName(path));
    }

    private static void DestroyIfPresent(string name)
    {
        GameObject existing = GameObject.Find(name);
        if (existing != null)
            Object.DestroyImmediate(existing);
    }

    public static void CapturePreview()
    {
        EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
        Camera camera = GameObject.Find("Main Camera").GetComponent<Camera>();
        RenderTexture target = new RenderTexture(1280, 720, 24);
        Texture2D image = new Texture2D(1280, 720, TextureFormat.RGB24, false);
        camera.targetTexture = target;
        RenderTexture.active = target;
        camera.Render();
        image.ReadPixels(new Rect(0, 0, 1280, 720), 0, 0);
        image.Apply();
        string path = Path.Combine(Path.GetTempPath(), "rollaball-pinball-preview.png");
        File.WriteAllBytes(path, image.EncodeToPNG());
        camera.targetTexture = null;
        RenderTexture.active = null;
        Object.DestroyImmediate(image);
        Object.DestroyImmediate(target);
        Debug.Log("Pinball preview saved to " + path);
    }
}
