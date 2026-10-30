
using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.UI;

// Master volume control. The balance between Music, SFX and UI is set
// on those groups in the AudioMixer, not here.
class MixLevels : MonoBehaviour
{
    private const string VolumeKey = "MasterVolume";

    [SerializeField] private AudioMixer masterMixer;
    [SerializeField] private Slider slider;

    private void Start()
    {
        // Level snapshot: level music playing, boss track silent
        masterMixer.FindSnapshot("Level").TransitionTo(0f);

        float volume = PlayerPrefs.GetFloat(VolumeKey, 1f);
        SetMusicLvl(volume);
        if (slider != null) slider.value = volume;
    }

    // Slider is linear (0.0001..1), the mixer works in dB
    public void SetMusicLvl(float volume)
    {
        volume = Mathf.Clamp(volume, 0.0001f, 1f);
        masterMixer.SetFloat("MasterVol", Mathf.Log10(volume) * 20f);
        PlayerPrefs.SetFloat(VolumeKey, volume);
    }

    public void SaveSliderVolume()
    {
        PlayerPrefs.Save();
    }

}

