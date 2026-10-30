using System.Linq;
using TMPro;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

// One-time tool: turns the single volume slider in each menu into
// General / Music / SFX sliders wired to MixLevels.
public static class VolumeSlidersSetup
{
    private static readonly string[] scenes =
    {
        "Assets/Scenes/MainMenu.unity",
        "Assets/Scenes/Level1.unity",
        "Assets/Scenes/Level1Android.unity",
    };

    [MenuItem("Tools/Audio/Create Volume Sliders")]
    public static void Run()
    {
        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
        string startScene = EditorSceneManager.GetActiveScene().path;

        foreach (string path in scenes)
        {
            var scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Single);
            if (SetupScene())
            {
                EditorSceneManager.MarkSceneDirty(scene);
                EditorSceneManager.SaveScene(scene);
            }
        }

        if (!string.IsNullOrEmpty(startScene)) EditorSceneManager.OpenScene(startScene);
    }

    private static bool SetupScene()
    {
        Slider general = Object.FindObjectsOfType<Slider>(true).FirstOrDefault(s => GetMixLevels(s) != null);
        if (general == null)
        {
            Debug.LogWarning("Volume sliders: no slider wired to MixLevels in " + EditorSceneManager.GetActiveScene().name);
            return false;
        }

        MixLevels mix = GetMixLevels(general);
        var mixObject = new SerializedObject(mix);
        if (mixObject.FindProperty("musicSlider").objectReferenceValue != null)
        {
            Debug.Log("Volume sliders: already set up in " + EditorSceneManager.GetActiveScene().name);
            return false;
        }

        RectTransform generalRect = (RectTransform)general.transform;
        TMP_Text generalLabel = FindLabel(general);

        float labelGap = generalLabel != null
            ? Mathf.Abs(((RectTransform)generalLabel.transform).anchoredPosition.y - generalRect.anchoredPosition.y)
            : 0f;
        float rowHeight = labelGap + generalRect.rect.height + 10f;

        // Rows stack around the original slider position: General above, Music in place, SFX below
        Move(generalRect, generalLabel, rowHeight);
        Rewire(general, mix, mix.SetMasterVolume);
        general.name = "GeneralSlider";
        if (generalLabel != null)
        {
            generalLabel.text = "General";
            generalLabel.name = "GeneralText";
        }

        Slider music = Duplicate(general, generalLabel, -rowHeight, "Music", mix, mix.SetMusicVolume);
        Slider sfx = Duplicate(general, generalLabel, -2f * rowHeight, "SFX", mix, mix.SetSfxVolume);

        mixObject.FindProperty("slider").objectReferenceValue = general;
        mixObject.FindProperty("musicSlider").objectReferenceValue = music;
        mixObject.FindProperty("sfxSlider").objectReferenceValue = sfx;
        mixObject.ApplyModifiedProperties();

        Debug.Log("Volume sliders: created in " + EditorSceneManager.GetActiveScene().name + ". Check their layout.");
        return true;
    }

    private static Slider Duplicate(Slider source, TMP_Text sourceLabel, float offsetY, string title, MixLevels mix, UnityAction<float> setter)
    {
        Slider slider = Object.Instantiate(source, source.transform.parent);
        slider.name = title + "Slider";
        slider.transform.SetSiblingIndex(source.transform.GetSiblingIndex() + 1);
        Move((RectTransform)slider.transform, null, offsetY);
        Rewire(slider, mix, setter);
        Undo.RegisterCreatedObjectUndo(slider.gameObject, "Create volume slider");

        if (sourceLabel != null && sourceLabel.transform.parent == source.transform.parent)
        {
            TMP_Text label = Object.Instantiate(sourceLabel, sourceLabel.transform.parent);
            label.name = title + "Text";
            label.text = title;
            Move((RectTransform)label.transform, null, offsetY);
            Undo.RegisterCreatedObjectUndo(label.gameObject, "Create volume label");
        }
        return slider;
    }

    private static void Move(RectTransform rect, TMP_Text label, float offsetY)
    {
        rect.anchoredPosition += new Vector2(0f, offsetY);
        if (label != null && label.transform.parent == rect.parent)
            ((RectTransform)label.transform).anchoredPosition += new Vector2(0f, offsetY);
    }

    // Replaces the MixLevels listener, keeping any other listeners on the slider
    private static void Rewire(Slider slider, MixLevels mix, UnityAction<float> setter)
    {
        for (int i = slider.onValueChanged.GetPersistentEventCount() - 1; i >= 0; i--)
        {
            if (slider.onValueChanged.GetPersistentTarget(i) is MixLevels)
                UnityEventTools.RemovePersistentListener(slider.onValueChanged, i);
        }
        UnityEventTools.AddPersistentListener(slider.onValueChanged, setter);
    }

    private static MixLevels GetMixLevels(Slider slider)
    {
        for (int i = 0; i < slider.onValueChanged.GetPersistentEventCount(); i++)
        {
            if (slider.onValueChanged.GetPersistentTarget(i) is MixLevels mix) return mix;
        }
        return null;
    }

    // The "Volume" / "music" text next to the slider
    private static TMP_Text FindLabel(Slider slider)
    {
        return slider.transform.parent.GetComponentsInChildren<TMP_Text>(true)
            .Where(t => t.transform.parent == slider.transform.parent)
            .FirstOrDefault(t =>
            {
                string text = t.text.ToLower();
                return text.Contains("volume") || text.Contains("music");
            });
    }
}
