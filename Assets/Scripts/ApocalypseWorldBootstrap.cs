using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Builds a lightweight apocalyptic town set, a derailed train wreck, and a mobile-friendly minimap at runtime.
/// This keeps the scene editable while still giving the level a stronger visual identity without external art packs.
/// </summary>
[DefaultExecutionOrder(-60)]
public class ApocalypseWorldBootstrap : MonoBehaviour
{
    [Header("Scene References")]
    [SerializeField] private string playerTag = "Player";
    [SerializeField] private Transform player;
    [SerializeField] private Canvas targetCanvas;
    [SerializeField] private Terrain targetTerrain;

    [Header("Town Layout")]
    [SerializeField] private Vector3 townCenterOffset = new Vector3(34f, 0f, 18f);
    [SerializeField] private Vector2 townFootprint = new Vector2(118f, 86f);
    [SerializeField] private int buildingsPerSide = 4;
    [SerializeField] private int wreckedCarCount = 7;

    [Header("Mini Map")]
    [SerializeField] private float minimapSize = 186f;
    [SerializeField] private float minimapMargin = 18f;
    [SerializeField] private float minimapHeight = 68f;
    [SerializeField] private float minimapOrthographicSize = 32f;

    private Transform runtimeRoot;
    private Camera minimapCamera;
    private RenderTexture minimapTexture;
    private RectTransform playerArrow;
    private Image playerArrowImage;

    private Material asphaltMaterial;
    private Material dustMaterial;
    private Material concreteMaterial;
    private Material rustMaterial;
    private Material metalMaterial;
    private Material darkMaterial;

    private void Start()
    {
        ResolveReferences();
        EnsureMainCameraTag();
        ApplyAtmosphere();
        BuildTownIfNeeded();
        BuildMiniMapIfNeeded();
    }

    private void LateUpdate()
    {
        if (player == null || minimapCamera == null)
            return;

        Vector3 focus = player.position;
        minimapCamera.transform.position = new Vector3(focus.x, focus.y + minimapHeight, focus.z);
        minimapCamera.transform.rotation = Quaternion.Euler(90f, 0f, 0f);

        if (playerArrow != null)
            playerArrow.localEulerAngles = new Vector3(0f, 0f, -player.eulerAngles.y);
    }

    private void OnDestroy()
    {
        if (minimapTexture != null)
            minimapTexture.Release();
    }

    private void ResolveReferences()
    {
        if (player == null)
        {
            GameObject playerObject = GameObject.FindGameObjectWithTag(playerTag);
            if (playerObject != null)
                player = playerObject.transform;
        }

        if (targetTerrain == null)
            targetTerrain = Terrain.activeTerrain != null ? Terrain.activeTerrain : FindFirstObjectByType<Terrain>();

        if (targetCanvas == null)
        {
            Canvas[] canvases = FindObjectsByType<Canvas>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
            foreach (Canvas canvas in canvases)
            {
                if (canvas.renderMode != RenderMode.WorldSpace && canvas.transform.parent == null)
                {
                    targetCanvas = canvas;
                    break;
                }
            }
        }

        if (runtimeRoot == null)
        {
            Transform existing = transform.Find("ApocalypseRuntime");
            if (existing != null)
                runtimeRoot = existing;
        }

        if (runtimeRoot == null)
        {
            GameObject root = new GameObject("ApocalypseRuntime");
            root.transform.SetParent(transform, false);
            runtimeRoot = root.transform;
        }
    }

    private void EnsureMainCameraTag()
    {
        Camera cam = Camera.main;
        if (cam == null)
            cam = FindFirstObjectByType<Camera>();

        if (cam != null && cam.tag != "MainCamera")
            cam.tag = "MainCamera";
    }

    private void ApplyAtmosphere()
    {
        RenderSettings.fog = true;
        RenderSettings.fogMode = FogMode.Linear;
        RenderSettings.fogStartDistance = 8f;
        RenderSettings.fogEndDistance = 145f;
        RenderSettings.fogColor = new Color(0.49f, 0.43f, 0.36f, 1f);
        RenderSettings.ambientSkyColor = new Color(0.34f, 0.30f, 0.28f, 1f);
        RenderSettings.ambientEquatorColor = new Color(0.23f, 0.21f, 0.18f, 1f);
        RenderSettings.ambientGroundColor = new Color(0.11f, 0.10f, 0.08f, 1f);

        Light light = FindFirstObjectByType<Light>();
        if (light != null && light.type == LightType.Directional)
        {
            light.color = new Color(0.95f, 0.82f, 0.68f, 1f);
            light.intensity = 0.82f;
            light.transform.rotation = Quaternion.Euler(38f, -26f, 0f);
        }
    }

    private void BuildTownIfNeeded()
    {
        if (player == null)
        {
            Debug.LogWarning("[ApocalypseWorldBootstrap] Could not find the Player. Town generation skipped.", this);
            return;
        }

        if (runtimeRoot.Find("Town") != null)
            return;

        CreateMaterials();

        Random.State previousState = Random.state;
        Random.InitState(441199);

        GameObject town = new GameObject("Town");
        town.transform.SetParent(runtimeRoot, false);

        Vector3 townCenter = ClampToTerrain(player.position + townCenterOffset);

        BuildRoadNetwork(town.transform, townCenter);
        BuildBuildingStrips(town.transform, townCenter);
        BuildWreckedCars(town.transform, townCenter);
        BuildTrainWreck(town.transform, townCenter);
        BuildTownDebris(town.transform, townCenter);

        Random.state = previousState;
    }

    private void BuildMiniMapIfNeeded()
    {
        if (targetCanvas == null || player == null)
        {
            Debug.LogWarning("[ApocalypseWorldBootstrap] Could not find the gameplay Canvas or Player. Minimap skipped.", this);
            return;
        }

        if (targetCanvas.transform.Find("MiniMapRoot") != null)
            return;

        GameObject root = new GameObject("MiniMapRoot", typeof(RectTransform), typeof(Image));
        root.transform.SetParent(targetCanvas.transform, false);

        RectTransform rootRect = root.GetComponent<RectTransform>();
        rootRect.anchorMin = new Vector2(1f, 1f);
        rootRect.anchorMax = new Vector2(1f, 1f);
        rootRect.pivot = new Vector2(1f, 1f);
        rootRect.sizeDelta = new Vector2(minimapSize, minimapSize);
        rootRect.anchoredPosition = new Vector2(-minimapMargin, -minimapMargin);

        Image frame = root.GetComponent<Image>();
        frame.color = new Color(0.08f, 0.08f, 0.08f, 0.86f);

        GameObject mapGo = new GameObject("MiniMapImage", typeof(RectTransform), typeof(RawImage));
        mapGo.transform.SetParent(root.transform, false);
        RectTransform mapRect = mapGo.GetComponent<RectTransform>();
        mapRect.anchorMin = Vector2.zero;
        mapRect.anchorMax = Vector2.one;
        mapRect.offsetMin = new Vector2(10f, 10f);
        mapRect.offsetMax = new Vector2(-10f, -10f);

        RawImage raw = mapGo.GetComponent<RawImage>();
        raw.color = Color.white;

        GameObject arrowGo = new GameObject("PlayerArrow", typeof(RectTransform), typeof(Image));
        arrowGo.transform.SetParent(root.transform, false);
        playerArrow = arrowGo.GetComponent<RectTransform>();
        playerArrow.anchorMin = new Vector2(0.5f, 0.5f);
        playerArrow.anchorMax = new Vector2(0.5f, 0.5f);
        playerArrow.pivot = new Vector2(0.5f, 0.5f);
        playerArrow.sizeDelta = new Vector2(16f, 28f);
        playerArrow.anchoredPosition = Vector2.zero;

        playerArrowImage = arrowGo.GetComponent<Image>();
        playerArrowImage.sprite = CreateSolidSprite();
        playerArrowImage.color = new Color(0.95f, 0.21f, 0.16f, 1f);

        GameObject cameraGo = new GameObject("MiniMapCamera");
        cameraGo.transform.SetParent(runtimeRoot, false);
        minimapCamera = cameraGo.AddComponent<Camera>();
        minimapCamera.orthographic = true;
        minimapCamera.orthographicSize = minimapOrthographicSize;
        minimapCamera.clearFlags = CameraClearFlags.SolidColor;
        minimapCamera.backgroundColor = new Color(0.23f, 0.22f, 0.2f, 1f);
        minimapCamera.nearClipPlane = 0.3f;
        minimapCamera.farClipPlane = 180f;
        minimapCamera.allowHDR = false;
        minimapCamera.allowMSAA = false;
        minimapCamera.depth = -20f;
        minimapCamera.cullingMask = ~(1 << 5);

        minimapTexture = new RenderTexture(256, 256, 16, RenderTextureFormat.ARGB32);
        minimapTexture.name = "MiniMapTexture_Runtime";
        minimapTexture.Create();
        minimapCamera.targetTexture = minimapTexture;
        raw.texture = minimapTexture;

        LateUpdate();
    }

    private void BuildRoadNetwork(Transform parent, Vector3 townCenter)
    {
        Transform roads = new GameObject("Roads").transform;
        roads.SetParent(parent, false);

        CreateRoad(roads, townCenter, new Vector3(townFootprint.x, 0.28f, 9.5f), 0f);
        CreateRoad(roads, townCenter, new Vector3(9.5f, 0.28f, townFootprint.y), 0f);
        CreateRoad(roads, townCenter + new Vector3(0f, 0f, 22f), new Vector3(townFootprint.x * 0.68f, 0.24f, 6.5f), 0f);
        CreateRoad(roads, townCenter + new Vector3(22f, 0f, 0f), new Vector3(6.5f, 0.24f, townFootprint.y * 0.72f), 0f);
    }

    private void BuildBuildingStrips(Transform parent, Vector3 townCenter)
    {
        Transform districts = new GameObject("Buildings").transform;
        districts.SetParent(parent, false);

        float span = townFootprint.x - 24f;
        float step = buildingsPerSide > 1 ? span / (buildingsPerSide - 1) : 0f;
        float northZ = townCenter.z + 18f;
        float southZ = townCenter.z - 18f;

        for (int i = 0; i < buildingsPerSide; i++)
        {
            float x = townCenter.x - (span * 0.5f) + step * i;
            CreateRuinedBuilding(districts, new Vector3(x, 0f, northZ), Random.Range(10f, 14f), Random.Range(9f, 13f), Random.Range(10f, 18f), 180f);
            CreateRuinedBuilding(districts, new Vector3(x, 0f, southZ), Random.Range(9f, 13f), Random.Range(9f, 12f), Random.Range(8f, 16f), 0f);
        }

        CreateRuinedBuilding(districts, townCenter + new Vector3(-28f, 0f, 34f), 16f, 11f, 13f, 210f);
        CreateRuinedBuilding(districts, townCenter + new Vector3(31f, 0f, -29f), 15f, 10f, 12f, 25f);
    }

    private void BuildWreckedCars(Transform parent, Vector3 townCenter)
    {
        Transform cars = new GameObject("WreckedCars").transform;
        cars.SetParent(parent, false);

        for (int i = 0; i < wreckedCarCount; i++)
        {
            float laneX = Random.Range(-townFootprint.x * 0.32f, townFootprint.x * 0.32f);
            float laneZ = (i % 2 == 0)
                ? townCenter.z + Random.Range(-4.5f, 4.5f)
                : townCenter.z + Random.Range(-townFootprint.y * 0.28f, townFootprint.y * 0.28f);

            Vector3 carPos = (i % 2 == 0)
                ? new Vector3(townCenter.x + laneX, 0f, laneZ)
                : new Vector3(townCenter.x + Random.Range(-4.5f, 4.5f), 0f, laneZ);

            float yaw = i % 2 == 0 ? 90f + Random.Range(-35f, 35f) : Random.Range(-35f, 35f);
            bool overturned = i % 3 == 0;
            CreateWreckedCar(cars, carPos, yaw, overturned);
        }
    }

    private void BuildTrainWreck(Transform parent, Vector3 townCenter)
    {
        Transform train = new GameObject("TrainWreck").transform;
        train.SetParent(parent, false);

        Vector3 railCenter = townCenter + new Vector3(8f, 0f, -26f);
        CreateRailTrack(train, railCenter, 48f, 18f);

        CreateTrainCar(train, railCenter + new Vector3(-10f, 0f, 0f), new Vector3(3.6f, 3.2f, 10.5f), new Vector3(0f, 18f, 0f));
        CreateTrainCar(train, railCenter + new Vector3(2f, 0f, 1.2f), new Vector3(3.4f, 3f, 9.6f), new Vector3(12f, -10f, 86f));
        CreateTrainCar(train, railCenter + new Vector3(14f, 0f, -2f), new Vector3(3.2f, 2.8f, 9.2f), new Vector3(-6f, 12f, 8f));
    }

    private void BuildTownDebris(Transform parent, Vector3 townCenter)
    {
        Transform debris = new GameObject("Debris").transform;
        debris.SetParent(parent, false);

        for (int i = 0; i < 16; i++)
        {
            Vector3 pos = townCenter + new Vector3(
                Random.Range(-townFootprint.x * 0.45f, townFootprint.x * 0.45f),
                0f,
                Random.Range(-townFootprint.y * 0.45f, townFootprint.y * 0.45f));

            pos = ClampToTerrain(pos);
            pos.y += 0.18f;

            GameObject chunk = CreateCube("Rubble", debris, pos, new Vector3(Random.Range(0.5f, 1.8f), Random.Range(0.2f, 0.8f), Random.Range(0.5f, 1.6f)), concreteMaterial, false);
            chunk.transform.rotation = Quaternion.Euler(Random.Range(-12f, 18f), Random.Range(0f, 360f), Random.Range(-14f, 14f));
        }

        for (int i = 0; i < 6; i++)
        {
            Vector3 postPos = townCenter + new Vector3(
                Random.Range(-townFootprint.x * 0.45f, townFootprint.x * 0.45f),
                0f,
                Random.Range(-townFootprint.y * 0.4f, townFootprint.y * 0.4f));

            postPos = ClampToTerrain(postPos);
            GameObject pole = CreateCylinder("BrokenPole", debris, postPos + new Vector3(0f, 1.7f, 0f), new Vector3(0.22f, 1.7f, 0.22f), metalMaterial, true);
            pole.transform.rotation = Quaternion.Euler(Random.Range(-18f, 18f), Random.Range(0f, 360f), Random.Range(-14f, 14f));
        }
    }

    private void CreateRoad(Transform parent, Vector3 center, Vector3 scale, float yaw)
    {
        center = ClampToTerrain(center);
        center.y += 0.04f;

        GameObject road = CreateCube("Road", parent, center, scale, asphaltMaterial, false);
        road.transform.rotation = Quaternion.Euler(0f, yaw, 0f);

        for (int i = 0; i < 6; i++)
        {
            Vector3 crackPos = center + new Vector3(
                Random.Range(-scale.x * 0.45f, scale.x * 0.45f),
                0.03f,
                Random.Range(-scale.z * 0.4f, scale.z * 0.4f));

            Vector3 crackScale = new Vector3(Random.Range(0.3f, 1.2f), 0.05f, Random.Range(1.4f, 4.2f));
            GameObject crack = CreateCube("Crack", parent, crackPos, crackScale, darkMaterial, false);
            crack.transform.rotation = Quaternion.Euler(0f, Random.Range(0f, 180f), 0f);
        }
    }

    private void CreateRuinedBuilding(Transform parent, Vector3 center, float width, float depth, float height, float yaw)
    {
        center = ClampToTerrain(center);
        GameObject root = new GameObject("RuinedBuilding");
        root.transform.SetParent(parent, false);
        root.transform.position = center;
        root.transform.rotation = Quaternion.Euler(0f, yaw, 0f);

        CreateCube("BaseBlock", root.transform, center + new Vector3(0f, height * 0.28f, 0f), new Vector3(width, height * 0.56f, depth), concreteMaterial, true);
        CreateCube("UpperBlock", root.transform, center + new Vector3(width * -0.08f, height * 0.69f, depth * 0.07f), new Vector3(width * 0.78f, height * 0.34f, depth * 0.75f), rustMaterial, true);

        GameObject facade = CreateCube("FacadeSlab", root.transform, center + new Vector3(0f, height * 0.43f, depth * 0.45f), new Vector3(width * 0.78f, height * 0.42f, 0.55f), darkMaterial, false);
        facade.transform.rotation = Quaternion.Euler(Random.Range(-4f, 4f), yaw + Random.Range(-6f, 6f), Random.Range(-10f, 10f));

        GameObject roof = CreateCube("RoofFragment", root.transform, center + new Vector3(width * 0.08f, height * 0.98f, depth * -0.06f), new Vector3(width * 0.62f, 0.35f, depth * 0.52f), metalMaterial, false);
        roof.transform.rotation = Quaternion.Euler(Random.Range(-18f, -6f), yaw + Random.Range(-12f, 12f), Random.Range(-16f, 16f));

        for (int i = 0; i < 4; i++)
        {
            Vector3 debrisPos = center + new Vector3(
                Random.Range(-width * 0.44f, width * 0.44f),
                Random.Range(0.15f, 0.8f),
                Random.Range(-depth * 0.44f, depth * 0.44f));

            GameObject debris = CreateCube("WallDebris", root.transform, debrisPos, new Vector3(Random.Range(0.4f, 1.5f), Random.Range(0.2f, 0.75f), Random.Range(0.4f, 1.4f)), concreteMaterial, false);
            debris.transform.rotation = Quaternion.Euler(Random.Range(-18f, 18f), Random.Range(0f, 360f), Random.Range(-18f, 18f));
        }
    }

    private void CreateWreckedCar(Transform parent, Vector3 center, float yaw, bool overturned)
    {
        center = ClampToTerrain(center);

        GameObject root = new GameObject("WreckedCar");
        root.transform.SetParent(parent, false);
        root.transform.position = center + new Vector3(0f, 0.4f, 0f);
        root.transform.rotation = Quaternion.Euler(overturned ? 90f : Random.Range(-8f, 8f), yaw, Random.Range(-10f, 10f));

        CreateCube("Body", root.transform, root.transform.position + new Vector3(0f, 0.05f, 0f), new Vector3(1.85f, 0.55f, 3.8f), rustMaterial, true);
        CreateCube("Cabin", root.transform, root.transform.position + new Vector3(0f, 0.58f, -0.15f), new Vector3(1.5f, 0.68f, 1.8f), metalMaterial, true);
        CreateCube("Hood", root.transform, root.transform.position + new Vector3(0f, 0.42f, 1.12f), new Vector3(1.55f, 0.2f, 1.05f), darkMaterial, false);

        CreateWheel(root.transform, new Vector3(-0.9f, -0.08f, 1.22f), overturned);
        CreateWheel(root.transform, new Vector3(0.9f, -0.08f, 1.22f), overturned);
        if (Random.value > 0.35f)
            CreateWheel(root.transform, new Vector3(-0.9f, -0.08f, -1.22f), overturned);
        if (Random.value > 0.2f)
            CreateWheel(root.transform, new Vector3(0.9f, -0.08f, -1.22f), overturned);
    }

    private void CreateTrainCar(Transform parent, Vector3 center, Vector3 bodySize, Vector3 euler)
    {
        center = ClampToTerrain(center);

        GameObject car = new GameObject("TrainCar");
        car.transform.SetParent(parent, false);
        car.transform.position = center + new Vector3(0f, bodySize.y * 0.5f, 0f);
        car.transform.rotation = Quaternion.Euler(euler);

        CreateCube("CarBody", car.transform, car.transform.position, bodySize, rustMaterial, true);
        CreateCube("Roof", car.transform, car.transform.position + new Vector3(0f, bodySize.y * 0.56f, 0f), new Vector3(bodySize.x * 0.88f, 0.26f, bodySize.z * 0.9f), metalMaterial, false);
        CreateCube("WindowBand", car.transform, car.transform.position + new Vector3(0f, 0.35f, 0f), new Vector3(bodySize.x * 1.02f, bodySize.y * 0.26f, bodySize.z * 0.72f), darkMaterial, false);

        CreateWheelSet(car.transform, new Vector3(0f, -bodySize.y * 0.46f, bodySize.z * 0.28f));
        CreateWheelSet(car.transform, new Vector3(0f, -bodySize.y * 0.46f, -bodySize.z * 0.28f));
    }

    private void CreateRailTrack(Transform parent, Vector3 center, float length, float yaw)
    {
        center = ClampToTerrain(center);
        Quaternion rotation = Quaternion.Euler(0f, yaw, 0f);

        Vector3 leftRail = center + rotation * new Vector3(-1.2f, 0.12f, 0f);
        Vector3 rightRail = center + rotation * new Vector3(1.2f, 0.12f, 0f);

        GameObject railA = CreateCube("RailLeft", parent, leftRail, new Vector3(0.22f, 0.12f, length), metalMaterial, false);
        railA.transform.rotation = rotation;
        GameObject railB = CreateCube("RailRight", parent, rightRail, new Vector3(0.22f, 0.12f, length), metalMaterial, false);
        railB.transform.rotation = rotation;

        for (int i = -7; i <= 7; i++)
        {
            Vector3 sleeperPos = center + rotation * new Vector3(0f, 0.05f, i * (length / 14f));
            GameObject sleeper = CreateCube("Sleeper", parent, sleeperPos, new Vector3(3.4f, 0.14f, 0.5f), darkMaterial, false);
            sleeper.transform.rotation = rotation;
        }
    }

    private void CreateWheel(Transform car, Vector3 localOffset, bool overturned)
    {
        GameObject wheel = CreateCylinder("Wheel", car, car.position, new Vector3(0.42f, 0.14f, 0.42f), darkMaterial, true);
        wheel.transform.localPosition = localOffset;
        wheel.transform.localRotation = Quaternion.Euler(90f, 0f, overturned ? 25f : 0f);
    }

    private void CreateWheelSet(Transform parent, Vector3 localOffset)
    {
        GameObject left = CreateCylinder("WheelLeft", parent, parent.position, new Vector3(0.42f, 0.18f, 0.42f), darkMaterial, false);
        left.transform.localPosition = localOffset + new Vector3(-1.35f, 0f, 0f);
        left.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);

        GameObject right = CreateCylinder("WheelRight", parent, parent.position, new Vector3(0.42f, 0.18f, 0.42f), darkMaterial, false);
        right.transform.localPosition = localOffset + new Vector3(1.35f, 0f, 0f);
        right.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);

        CreateCube("Axle", parent, parent.position, new Vector3(2.8f, 0.15f, 0.18f), metalMaterial, false).transform.localPosition = localOffset;
    }

    private void CreateMaterials()
    {
        if (asphaltMaterial != null)
            return;

        Shader shader = Shader.Find("Universal Render Pipeline/Lit");
        if (shader == null)
            shader = Shader.Find("Standard");
        if (shader == null)
            shader = Shader.Find("Diffuse");

        asphaltMaterial = CreateMaterial(shader, new Color(0.13f, 0.13f, 0.14f, 1f));
        dustMaterial = CreateMaterial(shader, new Color(0.44f, 0.37f, 0.29f, 1f));
        concreteMaterial = CreateMaterial(shader, new Color(0.57f, 0.53f, 0.48f, 1f));
        rustMaterial = CreateMaterial(shader, new Color(0.41f, 0.22f, 0.14f, 1f));
        metalMaterial = CreateMaterial(shader, new Color(0.29f, 0.29f, 0.32f, 1f));
        darkMaterial = CreateMaterial(shader, new Color(0.08f, 0.08f, 0.08f, 1f));
    }

    private static Material CreateMaterial(Shader shader, Color color)
    {
        Material material = new Material(shader);
        material.color = color;
        return material;
    }

    private GameObject CreateCube(string name, Transform parent, Vector3 worldPosition, Vector3 scale, Material material, bool keepCollider)
    {
        GameObject go = GameObject.CreatePrimitive(PrimitiveType.Cube);
        go.name = name;
        go.transform.SetParent(parent, true);
        go.transform.position = worldPosition;
        go.transform.localScale = scale;
        ApplyMaterial(go, material);

        if (!keepCollider)
        {
            Collider collider = go.GetComponent<Collider>();
            if (collider != null)
                Destroy(collider);
        }

        return go;
    }

    private GameObject CreateCylinder(string name, Transform parent, Vector3 worldPosition, Vector3 scale, Material material, bool keepCollider)
    {
        GameObject go = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        go.name = name;
        go.transform.SetParent(parent, true);
        go.transform.position = worldPosition;
        go.transform.localScale = scale;
        ApplyMaterial(go, material);

        if (!keepCollider)
        {
            Collider collider = go.GetComponent<Collider>();
            if (collider != null)
                Destroy(collider);
        }

        return go;
    }

    private static void ApplyMaterial(GameObject go, Material material)
    {
        Renderer renderer = go.GetComponent<Renderer>();
        if (renderer != null)
            renderer.sharedMaterial = material;
    }

    private Vector3 ClampToTerrain(Vector3 worldPosition)
    {
        if (targetTerrain == null)
            return new Vector3(worldPosition.x, 0f, worldPosition.z);

        Vector3 terrainPos = targetTerrain.transform.position;
        Vector3 terrainSize = targetTerrain.terrainData.size;

        float x = Mathf.Clamp(worldPosition.x, terrainPos.x + 6f, terrainPos.x + terrainSize.x - 6f);
        float z = Mathf.Clamp(worldPosition.z, terrainPos.z + 6f, terrainPos.z + terrainSize.z - 6f);
        float y = targetTerrain.SampleHeight(new Vector3(x, 0f, z)) + terrainPos.y;
        return new Vector3(x, y, z);
    }

    private static Sprite CreateSolidSprite()
    {
        Texture2D texture = new Texture2D(1, 1, TextureFormat.RGBA32, false);
        texture.SetPixel(0, 0, Color.white);
        texture.Apply(false, true);
        return Sprite.Create(texture, new Rect(0f, 0f, 1f, 1f), new Vector2(0.5f, 0.5f), 100f);
    }
}
