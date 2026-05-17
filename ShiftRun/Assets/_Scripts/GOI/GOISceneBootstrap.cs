using UnityEngine;

/// <summary>
/// Play Mode: spawns GOI player, level blocks, and camera follow if not already present.
/// Assign a GameSettings asset in the inspector on the bootstrap object.
/// Do not use in the same scene as <see cref="GravityControl"/> (it overwrites global Physics2D.gravity).
/// For packaged levels (<c>Level_*.unity</c>), instantiate the player prefab only after resolving gravity / saves logic.
/// </summary>
public class GOISceneBootstrap : MonoBehaviour
{
    [SerializeField] GameSettings settings;
    [SerializeField] Vector3 playerSpawn = new Vector3(0f, 2f, 0f);
    [SerializeField] bool buildLevel = true;

    void Awake()
    {
        if (settings == null)
        {
            Debug.LogWarning("[GOI] GameSettings not assigned on GOISceneBootstrap.");
            return;
        }

        if (FindObjectOfType<GOIPlayerRootMarker>() != null)
            return;

        var cam = Camera.main;
        if (cam == null)
        {
            var camGo = new GameObject("Main Camera");
            camGo.tag = "MainCamera";
            cam = camGo.AddComponent<Camera>();
            cam.orthographic = true;
            cam.orthographicSize = 6f;
            cam.transform.position = new Vector3(0f, 1.5f, -10f);
        }

        if (!cam.TryGetComponent<SmoothCamera>(out var smooth))
            smooth = cam.gameObject.AddComponent<SmoothCamera>();

        if (buildLevel)
            GOIHierarchyBuilder.BuildPrototypePlatforms(settings.prototypeLevelStone, settings.prototypeLevelIce);

        var player = GOIHierarchyBuilder.BuildPlayer(settings, playerSpawn, cam);
        smooth.Configure(settings, player.transform);
    }
}
