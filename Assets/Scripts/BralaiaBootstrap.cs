using UnityEngine;

namespace Bralaia
{
    /// <summary>Creates the playable prototype even when the opened scene is empty.</summary>
    public static class BralaiaBootstrap
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void CreateGame()
        {
            if (Object.FindObjectOfType<BralaiaGame>() != null)
                return;

            var root = new GameObject("Bralaia Game");
            Object.DontDestroyOnLoad(root);
            root.AddComponent<BralaiaGame>();
        }
    }
}
