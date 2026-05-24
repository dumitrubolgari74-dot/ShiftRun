using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Триггер перехода на следующую сцену. Collider2D → Is Trigger.
/// Игрок (Rigidbody2D + Collider2D) заходит в зону — загрузка сцены.
/// </summary>
[RequireComponent(typeof(Collider2D))]
public class LevelExitTrigger : MonoBehaviour
{
    [Header("Куда перейти")]
    [Tooltip("Имя сцены из File → Build Settings.")]
    public string nextSceneName;

    [Tooltip("Если задан (>0), грузится build index вместо имени.")]
    public int nextSceneBuildIndex = -1;

    [Header("Кто может активировать")]
    public string playerTag = "Player";

    [Tooltip("Если пусто — только объект с playerTag.")]
    public Transform playerRoot;

    [Header("Поведение")]
    public bool loadOnce = true;
    public float reloadDelay;

    bool _triggered;

    void Reset()
    {
        var col = GetComponent<Collider2D>();
        col.isTrigger = true;
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (_triggered && loadOnce)
            return;

        if (!IsPlayer(other))
            return;

        _triggered = true;

        if (reloadDelay > 0f)
            Invoke(nameof(LoadNextScene), reloadDelay);
        else
            LoadNextScene();
    }

    bool IsPlayer(Collider2D other)
    {
        if (playerRoot != null)
            return other.transform == playerRoot || other.transform.IsChildOf(playerRoot);

        if (!string.IsNullOrEmpty(playerTag))
        {
            if (other.CompareTag(playerTag))
                return true;

            var rb = other.GetComponentInParent<Rigidbody2D>();
            return rb != null && rb.CompareTag(playerTag);
        }

        return other.GetComponentInParent<Rigidbody2D>() != null;
    }

    void LoadNextScene()
    {
        if (nextSceneBuildIndex >= 0)
        {
            if (nextSceneBuildIndex >= SceneManager.sceneCountInBuildSettings)
            {
                Debug.LogError($"[LevelExit] Build index {nextSceneBuildIndex} не в Build Settings.");
                _triggered = false;
                return;
            }

            SceneManager.LoadScene(nextSceneBuildIndex);
            return;
        }

        if (string.IsNullOrEmpty(nextSceneName))
        {
            Debug.LogError("[LevelExit] Не задано nextSceneName.", this);
            _triggered = false;
            return;
        }

        SceneManager.LoadScene(nextSceneName);
    }
}
