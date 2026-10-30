
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.UI;

// Player volume settings: General (Master), Music and SFX.
// The balance between groups is set in the AudioMixer; a slider at
// full volume plays its group at that mixer level.
public class MixLevels : MonoBehaviour
{
    private const string MasterKey = "MasterVolume";
    private const string MusicKey = "MusicVolume";
    private const string SfxKey = "SfxVolume";

    [SerializeField] private AudioMixer masterMixer;
    // General volume slider
    [SerializeField] private Slider slider;
    [SerializeField] private Slider musicSlider;
    [SerializeField] private Slider sfxSlider;

    // Each exposed parameter's level in the mixer, read before any slider changes it
    private static readonly Dictionary<string, float> mixLevels = new Dictionary<string, float>();

    private void Start()
    {
        // Level snapshot: level music playing, boss track silent
        masterMixer.FindSnapshot("Level").TransitionTo(0f);

        Init(slider, "MasterVol", MasterKey);
        Init(musicSlider, "MusicVol", MusicKey);
        Init(sfxSlider, "SfxVol", SfxKey);
    }

    public void SetMasterVolume(float volume) => SetVolume("MasterVol", MasterKey, volume);
    public void SetMusicVolume(float volume) => SetVolume("MusicVol", MusicKey, volume);
    public void SetSfxVolume(float volume) => SetVolume("SfxVol", SfxKey, volume);

    // Old name of SetMasterVolume, still used by sliders not yet rewired
    public void SetMusicLvl(float volume) => SetMasterVolume(volume);

    public void SaveSliderVolume()
    {
        PlayerPrefs.Save();
    }

    private void Init(Slider volumeSlider, string param, string key)
    {
        if (!mixLevels.ContainsKey(param))
        {
            masterMixer.GetFloat(param, out float mixDb);
            mixLevels[param] = mixDb;
        }

        float volume = PlayerPrefs.GetFloat(key, 1f);
        SetVolume(param, key, volume);
        if (volumeSlider != null) volumeSlider.value = volume;
    }

    // Slider is linear (0.0001..1), the mixer works in dB
    private void SetVolume(string param, string key, float volume)
    {
        volume = Mathf.Clamp(volume, 0.0001f, 1f);
        mixLevels.TryGetValue(param, out float mixDb);
        masterMixer.SetFloat(param, mixDb + Mathf.Log10(volume) * 20f);
        PlayerPrefs.SetFloat(key, volume);
    }

}

