using UnityEngine;

namespace Bralaia
{
    public sealed class RegionTileView : MonoBehaviour
    {
        public string RegionId { get; private set; }
        private BralaiaGame game;
        private Renderer tileRenderer;
        private Material tileMaterial;

        public void Initialize(BralaiaGame owner, RegionData data)
        {
            game = owner;
            RegionId = data.Id;
            tileRenderer = GetComponent<Renderer>();
            tileMaterial = new Material(Shader.Find("Standard"));
            tileRenderer.material = tileMaterial;
            Refresh(data, false);
        }

        public void Refresh(RegionData data, bool selected)
        {
            if (data == null || tileRenderer == null) return;
            Color color = data.Owner == BralaiaWorld.PlayerFaction ? new Color(0.20f, 0.62f, 0.42f) :
                data.Owner == "مستقلة" ? new Color(0.55f, 0.53f, 0.44f) :
                data.Owner == "اتحاد السهوب" ? new Color(0.72f, 0.37f, 0.25f) : new Color(0.28f, 0.48f, 0.72f);
            if (selected) color = Color.Lerp(color, Color.yellow, 0.38f);
            tileMaterial.color = color;
        }

        private void OnMouseDown()
        {
            if (game != null) game.SelectRegion(RegionId);
        }

        private void OnDestroy()
        {
            if (tileMaterial != null) Destroy(tileMaterial);
        }
    }
}
