using UnityEngine;

[RequireComponent(typeof(Collider))]
public class LevelExitTrigger : MonoBehaviour
{
    [Header("UI")]
    [Tooltip("Панель, которая откроется при входе в финальную зону.")]
    public GameObject finalPanel;

    [Header("Кто может активировать")]
    public string playerTag = "Player";

    [Tooltip("Если задан — срабатывает только на этот объект/его детей.")]
    public Transform playerRoot;

    [Header("Поведение")]
    public bool triggerOnce = true;
    public bool pauseGameOnFinal = true;
    public bool unlockCursorOnFinal = true;

    bool _triggered;

    void Reset()
    {
        var col = GetComponent<Collider>();
        col.isTrigger = true;
    }

    void Awake()
    {
        if (finalPanel == null)
        {
            Transform panel = transform.root.Find("FinalPanel")
                          ?? transform.root.Find("WinPanel")
                          ?? transform.root.Find("CongratulationsPanel");
            if (panel != null)
                finalPanel = panel.gameObject;
        }

        if (finalPanel != null)
            finalPanel.SetActive(false);
    }

    void OnTriggerEnter(Collider other)
    {
        if (_triggered && triggerOnce)
            return;

        if (!IsPlayer(other))
            return;

        _triggered = true;
        ShowFinalPanel();
    }

    bool IsPlayer(Collider other)
    {
        if (playerRoot != null)
            return other.transform == playerRoot || other.transform.IsChildOf(playerRoot);

        if (!string.IsNullOrEmpty(playerTag))
        {
            if (other.CompareTag(playerTag))
                return true;

            var rb = other.GetComponentInParent<Rigidbody>();
            return rb != null && rb.CompareTag(playerTag);
        }

        return other.GetComponentInParent<Rigidbody>() != null;
    }

    void ShowFinalPanel()
    {
        if (finalPanel != null)
            finalPanel.SetActive(true);

        if (pauseGameOnFinal)
            Time.timeScale = 0f;

        if (unlockCursorOnFinal)
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }
    }
}

[AddComponentMenu("Common/Trigger Zone Final")]
public class TriggerZoneFinal : LevelExitTrigger
{
}
