using UnityEngine;

namespace NeonClash
{
    public sealed class NeonClashBootstrap : MonoBehaviour
    {
        [SerializeField] private GameContentCatalog catalog;
        [SerializeField] private FighterDefinition kael;
        [SerializeField] private FighterDefinition zara;

        private void Awake()
        {
            Application.targetFrameRate = 60;
            QualitySettings.vSyncCount = 0;

            if (kael == null || zara == null)
            {
                Debug.LogError("Neon Clash requires Kael and Zara fighter definition assets.");
                enabled = false;
                return;
            }

            CreateCamera();
            string reason = "No content catalog assigned.";
            if (catalog != null && catalog.IsComplete(out reason))
            {
                NeonClashFrontEnd frontEnd = gameObject.AddComponent<NeonClashFrontEnd>();
                frontEnd.Initialize(catalog);
                return;
            }

            if (catalog != null) Debug.LogWarning("Content catalog is incomplete: " + reason);
            CreateStage(null);
            NeonClashMatch match = gameObject.AddComponent<NeonClashMatch>();
            match.Initialize(kael, zara);
        }

        private static Camera CreateCamera()
        {
            GameObject cameraObject = new GameObject("Main Camera");
            cameraObject.tag = "MainCamera";
            Camera camera = cameraObject.AddComponent<Camera>();
            camera.orthographic = true;
            camera.orthographicSize = 5.25f;
            camera.transform.position = new Vector3(0f, 1.2f, -10f);
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(0.025f, 0.035f, 0.09f);
            return camera;
        }

        public static void CreateStage(StageDefinition stage)
        {
            GameObject existing = GameObject.Find(StagePresentation.RootName);
            if (existing != null) Destroy(existing);
            StagePresentation.Build(stage);
        }
    }
}
