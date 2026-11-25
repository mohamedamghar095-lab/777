using System.Collections.Generic;
using UnityEngine;

namespace Bralaia
{
    public sealed class BralaiaGame : MonoBehaviour
    {
        private BralaiaWorld world;
        private RegionData selectedRegion;
        private readonly Dictionary<string, RegionTileView> tiles = new Dictionary<string, RegionTileView>();
        private Camera gameCamera;
        private bool is3D;
        private Vector3 cameraTarget = new Vector3(0f, 0f, -0.5f);
        private float zoom = 13f;
        private Vector2 logScroll;
        private GUIStyle titleStyle, bodyStyle, buttonStyle, panelStyle;

        private void Awake()
        {
            world = new BralaiaWorld();
            selectedRegion = world.GetRegion("center");
            BuildWorld();
            BuildCamera();
            RefreshTiles();
        }

        private void BuildWorld()
        {
            var lightObject = new GameObject("Sunlight");
            var sun = lightObject.AddComponent<Light>();
            sun.type = LightType.Directional;
            sun.intensity = 1.15f;
            lightObject.transform.rotation = Quaternion.Euler(50f, -30f, 0f);

            var ground = GameObject.CreatePrimitive(PrimitiveType.Cube);
            ground.name = "Bralaia Map Background";
            ground.transform.position = new Vector3(0f, -0.7f, -0.5f);
            ground.transform.localScale = new Vector3(16f, 0.3f, 12f);
            var groundRenderer = ground.GetComponent<Renderer>();
            groundRenderer.material.color = new Color(0.12f, 0.16f, 0.18f);
            Destroy(ground.GetComponent<Collider>());

            foreach (var region in world.Regions)
            {
                var tile = GameObject.CreatePrimitive(PrimitiveType.Cube);
                tile.name = "Region - " + region.Name;
                tile.transform.position = new Vector3(region.Grid.x * 2.45f, 0f, region.Grid.y * 2.45f);
                tile.transform.localScale = new Vector3(2.28f, 0.45f, 2.28f);
                var view = tile.AddComponent<RegionTileView>();
                view.Initialize(this, region);
                tiles.Add(region.Id, view);
            }
        }

        private void BuildCamera()
        {
            var cameraObject = new GameObject("Bralaia Camera");
            gameCamera = cameraObject.AddComponent<Camera>();
            cameraObject.AddComponent<AudioListener>();
            gameCamera.clearFlags = CameraClearFlags.SolidColor;
            gameCamera.backgroundColor = new Color(0.07f, 0.10f, 0.14f);
            gameCamera.fieldOfView = 50f;
            ApplyCameraMode();
        }

        private void Update()
        {
            if (gameCamera == null) return;
            float speed = Mathf.Max(4f, zoom * 0.5f) * Time.unscaledDeltaTime;
            if (Input.GetKey(KeyCode.A) || Input.GetKey(KeyCode.LeftArrow)) cameraTarget.x -= speed;
            if (Input.GetKey(KeyCode.D) || Input.GetKey(KeyCode.RightArrow)) cameraTarget.x += speed;
            if (Input.GetKey(KeyCode.W) || Input.GetKey(KeyCode.UpArrow)) cameraTarget.z += speed;
            if (Input.GetKey(KeyCode.S) || Input.GetKey(KeyCode.DownArrow)) cameraTarget.z -= speed;
            cameraTarget.x = Mathf.Clamp(cameraTarget.x, -8f, 8f);
            cameraTarget.z = Mathf.Clamp(cameraTarget.z, -8f, 6f);
            float wheel = Input.mouseScrollDelta.y;
            if (Mathf.Abs(wheel) > 0.01f) zoom = Mathf.Clamp(zoom - wheel, 6f, 24f);
            ApplyCameraMode();
        }

        public void SelectRegion(string id)
        {
            selectedRegion = world.GetRegion(id);
            RefreshTiles();
        }

        private void RefreshTiles()
        {
            foreach (var region in world.Regions)
                if (tiles.TryGetValue(region.Id, out var view)) view.Refresh(region, selectedRegion == region);
        }

        private void ApplyCameraMode()
        {
            if (gameCamera == null) return;
            if (is3D)
            {
                gameCamera.orthographic = false;
                gameCamera.transform.position = cameraTarget + new Vector3(0f, zoom * 1.05f, -zoom * 1.05f);
                gameCamera.transform.rotation = Quaternion.Euler(42f, 0f, 0f);
                gameCamera.fieldOfView = 48f;
            }
            else
            {
                gameCamera.orthographic = true;
                gameCamera.orthographicSize = zoom * 0.68f;
                gameCamera.transform.position = cameraTarget + new Vector3(0f, zoom + 4f, 0f);
                gameCamera.transform.rotation = Quaternion.Euler(90f, 0f, 0f);
            }
        }

        private void OnGUI()
        {
            EnsureStyles();
            float scale = Mathf.Clamp(Screen.width / 1280f, 0.78f, 1.25f);
            GUI.matrix = Matrix4x4.TRS(Vector3.zero, Quaternion.identity, new Vector3(scale, scale, 1f));
            float width = Screen.width / scale;
            float height = Screen.height / scale;
            GUI.Box(new Rect(12, 12, width - 24, 58), GUIContent.none, panelStyle);
            GUI.Label(new Rect(28, 20, 280, 40), "برالايا | عصر الممالك", titleStyle);
            GUI.Label(new Rect(330, 24, 630, 32), "الدور " + world.Turn + "   |   الذهب " + world.Treasury + "   |   الطعام " + world.TotalFood + "   |   القوى البشرية " + world.Manpower + "   |   الاستقرار " + world.Stability, bodyStyle);
            if (GUI.Button(new Rect(width - 215, 22, 90, 38), is3D ? "عرض 2D" : "عرض 3D", buttonStyle)) is3D = !is3D;
            if (GUI.Button(new Rect(width - 116, 22, 96, 38), "الدور التالي", buttonStyle)) { world.NextTurn(); RefreshTiles(); }

            float panelY = 84f;
            float panelHeight = Mathf.Max(260f, height - 250f);
            GUI.Box(new Rect(12, panelY, 245, panelHeight), GUIContent.none, panelStyle);
            GUILayout.BeginArea(new Rect(26, panelY + 12, 217, panelHeight - 24));
            GUILayout.Label("المناطق", titleStyle);
            foreach (var region in world.Regions)
            {
                string marker = region == selectedRegion ? "▶ " : "";
                if (GUILayout.Button(marker + region.Name + "  ·  " + ShortOwner(region.Owner), buttonStyle, GUILayout.Height(30))) SelectRegion(region.Id);
            }
            GUILayout.Space(10);
            GUILayout.Label("WASD / الأسهم: تحريك الخريطة\nعجلة الفأرة: تقريب وإبعاد", bodyStyle);
            GUILayout.EndArea();

            float rightX = width - 270f;
            GUI.Box(new Rect(rightX, panelY, 258, panelHeight), GUIContent.none, panelStyle);
            DrawRegionPanel(rightX + 14f, panelY + 12f, 230f, panelHeight - 24f);

            float logY = height - 154f;
            GUI.Box(new Rect(12, logY, width - 24, 142), GUIContent.none, panelStyle);
            GUILayout.BeginArea(new Rect(26, logY + 8, width - 52, 126));
            GUILayout.Label("سجل الأحداث", titleStyle);
            logScroll = GUILayout.BeginScrollView(logScroll, GUILayout.Height(82));
            foreach (var message in world.EventLog) GUILayout.Label("• " + message, bodyStyle);
            GUILayout.EndScrollView();
            GUILayout.EndArea();
            GUI.matrix = Matrix4x4.identity;
        }

        private void DrawRegionPanel(float x, float y, float width, float height)
        {
            GUILayout.BeginArea(new Rect(x, y, width, height));
            if (selectedRegion == null) { GUILayout.Label("اختر منطقة", titleStyle); GUILayout.EndArea(); return; }
            GUILayout.Label(selectedRegion.Name, titleStyle);
            GUILayout.Label("الحاكم: " + selectedRegion.Owner, bodyStyle);
            GUILayout.Label("السكان: " + selectedRegion.Population + " ألف", bodyStyle);
            GUILayout.Label("الجيش: " + selectedRegion.Army + " جندي", bodyStyle);
            GUILayout.Label("التنمية: " + selectedRegion.Development, bodyStyle);
            GUILayout.Space(8);
            bool playerOwns = selectedRegion.IsOwnedBy(BralaiaWorld.PlayerFaction);
            GUI.enabled = playerOwns;
            if (GUILayout.Button("تجنيد 40 جنديًا — 80 ذهب", buttonStyle, GUILayout.Height(36))) { world.Recruit(selectedRegion); RefreshTiles(); }
            if (GUILayout.Button("تطوير الزراعة — 100 ذهب", buttonStyle, GUILayout.Height(36))) { world.DevelopFarms(selectedRegion); RefreshTiles(); }
            GUI.enabled = true;
            if (GUILayout.Button("جباية الضرائب", buttonStyle, GUILayout.Height(36))) world.CollectTaxes();
            GUILayout.Space(8);
            GUILayout.Label("المناطق المجاورة", bodyStyle);
            foreach (var neighborId in selectedRegion.Neighbors)
            {
                var neighbor = world.GetRegion(neighborId);
                if (neighbor != null && GUILayout.Button(neighbor.Name + "  ·  " + ShortOwner(neighbor.Owner), buttonStyle, GUILayout.Height(28))) SelectRegion(neighbor.Id);
            }
            GUILayout.EndArea();
        }

        private string ShortOwner(string owner)
        {
            if (owner == BralaiaWorld.PlayerFaction) return "لك";
            if (owner == "مستقلة") return "مستقلة";
            if (owner == "اتحاد السهوب") return "السهوب";
            return "الساحل";
        }

        private void EnsureStyles()
        {
            if (titleStyle != null) return;
            titleStyle = new GUIStyle(GUI.skin.label) { fontSize = 20, fontStyle = FontStyle.Bold, normal = { textColor = new Color(0.96f, 0.83f, 0.49f) } };
            bodyStyle = new GUIStyle(GUI.skin.label) { fontSize = 14, wordWrap = true, normal = { textColor = new Color(0.92f, 0.93f, 0.94f) } };
            buttonStyle = new GUIStyle(GUI.skin.button) { fontSize = 13, wordWrap = true, normal = { textColor = Color.white } };
            panelStyle = new GUIStyle(GUI.skin.box) { normal = { background = MakeTexture(new Color(0.08f, 0.12f, 0.16f, 0.94f)) } };
        }

        private Texture2D MakeTexture(Color color)
        {
            var texture = new Texture2D(1, 1);
            texture.SetPixel(0, 0, color);
            texture.Apply();
            return texture;
        }
    }
}
