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

        Material groundMaterial = CreateMaterial("Background", new Color(0.12f, 0.2f, 0.35f), 0.12f, 0.65f,
            CreateTexture("GroundGrid", TexturePattern.Ground), new Vector2(4f, 4f));
        Material wallMaterial = CreateMaterial("WallMaterial", Color.white, 0f, 0.3f,
            CreateTexture("BrickWall", TexturePattern.Brick), Vector2.one);
        Material playerMaterial = CreateMaterial("PlayerMaterial", new Color(1f, 0.75f, 0.25f), 0.45f, 0.68f,
            CreateTexture("PlayerPanels", TexturePattern.Player), Vector2.one);
        Material pickupMaterial = CreateMaterial("PickUpMaterial", new Color(1f, 0.92f, 0.28f), 0.35f, 0.78f,
            CreateTexture("PickupFacets", TexturePattern.Pickup), Vector2.one);

        GameObject ground = GameObject.Find("Ground");
        ground.transform.SetPositionAndRotation(Vector3.zero, Quaternion.identity);
        ground.transform.localScale = new Vector3(2f, 1f, 2f);
        ground.GetComponent<Renderer>().sharedMaterial = groundMaterial;

        GameObject player = GameObject.Find("Player");
        player.transform.SetPositionAndRotation(new Vector3(0f, 2f, 0f), Quaternion.identity);
        player.transform.localScale = Vector3.one;
        player.GetComponent<Renderer>().sharedMaterial = playerMaterial;
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
        CameraController cameraController = camera.GetComponent<CameraController>();
        if (cameraController == null) cameraController = camera.AddComponent<CameraController>();
        cameraController.player = player;
        cameraController.smoothTime = 0.18f;

        GameObject sunlight = GameObject.Find("Directional Light");
        sunlight.transform.rotation = Quaternion.Euler(50f, 60f, 0f);
        sunlight.GetComponent<Light>().intensity = 1.5f;

        BuildWalls(wallMaterial);
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
        Debug.Log("RollABall scene built: 12 Pick Ups, boundary walls, 8 maze walls, lights and bonus systems.");
    }

    private static void BuildWalls(Material material)
    {
        DestroyIfPresent("Walls");
        GameObject root = new GameObject("Walls");

        CreateWall(root.transform, "West Wall", new Vector3(-10f, 0f, 0f), new Vector3(0.5f, 2f, 20.5f), material);
        CreateWall(root.transform, "East Wall", new Vector3(10f, 0f, 0f), new Vector3(0.5f, 2f, 20.5f), material);
        CreateWall(root.transform, "North Wall", new Vector3(0f, 0f, 10f), new Vector3(20.5f, 2f, 0.5f), material);
        CreateWall(root.transform, "South Wall", new Vector3(0f, 0f, -10f), new Vector3(20.5f, 2f, 0.5f), material);

        // Two separated segments per lane leave a clear passage for the ball.
        CreateWall(root.transform, "Maze Wall 1", new Vector3(-6f, 0f, -6.5f), new Vector3(0.35f, 1.5f, 6f), material);
        CreateWall(root.transform, "Maze Wall 2", new Vector3(-6f, 0f, 4.5f), new Vector3(0.35f, 1.5f, 10f), material);
        CreateWall(root.transform, "Maze Wall 3", new Vector3(-2f, 0f, -4.5f), new Vector3(0.35f, 1.5f, 10f), material);
        CreateWall(root.transform, "Maze Wall 4", new Vector3(-2f, 0f, 6.5f), new Vector3(0.35f, 1.5f, 6f), material);
        CreateWall(root.transform, "Maze Wall 5", new Vector3(2f, 0f, -6.5f), new Vector3(0.35f, 1.5f, 6f), material);
        CreateWall(root.transform, "Maze Wall 6", new Vector3(2f, 0f, 4.5f), new Vector3(0.35f, 1.5f, 10f), material);
        CreateWall(root.transform, "Maze Wall 7", new Vector3(6f, 0f, -4.5f), new Vector3(0.35f, 1.5f, 10f), material);
        CreateWall(root.transform, "Maze Wall 8", new Vector3(6f, 0f, 6.5f), new Vector3(0.35f, 1.5f, 6f), material);
    }

    private static void CreateWall(Transform parent, string name, Vector3 position, Vector3 scale, Material material)
    {
        GameObject wall = GameObject.CreatePrimitive(PrimitiveType.Cube);
        wall.name = name;
        wall.transform.SetParent(parent);
        wall.transform.position = position;
        wall.transform.localScale = scale;
        wall.GetComponent<Renderer>().sharedMaterial = material;
        // Keep each brick about the same world-space size on every wall face.
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
        CreateLight(root.transform, "Blue Point Light", LightType.Point, new Vector3(-6f, 4f, -5f), new Color(0.15f, 0.65f, 1f), 8f, 12f);
        CreateLight(root.transform, "Amber Point Light", LightType.Point, new Vector3(5f, 4f, 6f), new Color(1f, 0.52f, 0.15f), 8f, 12f);
        Light spot = CreateLight(root.transform, "Violet Spot Light", LightType.Spot, new Vector3(0f, 9f, 0f), new Color(0.65f, 0.38f, 1f), 14f, 18f);
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

    private enum TexturePattern { Ground, Brick, Player, Pickup }

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
                        bool grid = x % 32 < 2 || y % 32 < 2;
                        color = grid ? new Color(0.18f, 0.46f, 0.68f) : new Color(0.17f, 0.25f, 0.39f);
                        break;
                    case TexturePattern.Brick:
                        int course = y / 32;
                        int brickX = (x + (course % 2) * 32) % 64;
                        int brickY = y % 32;
                        bool mortar = brickX < 3 || brickY < 3;
                        float variation = (Mathf.PerlinNoise((x / 64) * 3.7f, course * 2.3f) - 0.5f) * 0.12f;
                        float edgeShade = brickX < 7 || brickY < 7 ? -0.08f : 0f;
                        color = mortar
                            ? new Color(0.59f, 0.54f, 0.48f)
                            : new Color(0.66f + variation + edgeShade, 0.24f + variation * 0.4f + edgeShade * 0.4f, 0.16f + variation * 0.3f);
                        break;
                    case TexturePattern.Player:
                        bool panel = (x / 32 + y / 32) % 2 == 0;
                        color = panel ? new Color(1f, 0.78f, 0.28f) : new Color(0.9f, 0.34f, 0.08f);
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
}
