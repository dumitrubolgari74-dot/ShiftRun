#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

[InitializeOnLoad]
static class NewGoiPlayModeFix
{
    static NewGoiPlayModeFix()
    {
        EditorApplication.playModeStateChanged += OnPlayModeChanged;
    }

    static void OnPlayModeChanged(PlayModeStateChange state)
    {
        if (state == PlayModeStateChange.ExitingPlayMode)
        {
            RestoreAllHammers();
            Selection.activeObject = null;
            return;
        }

        if (state == PlayModeStateChange.EnteredEditMode)
        {
            EditorApplication.delayCall += () =>
            {
                RestoreAllHammers();
                Selection.activeObject = null;
            };
        }
    }

    static void RestoreAllHammers()
    {
        var controllers = Object.FindObjectsByType<NewGoiController>(FindObjectsSortMode.None);
        foreach (var ctrl in controllers)
        {
            if (ctrl != null)
                ctrl.RestoreHammerParent();
        }
    }
}
#endif
