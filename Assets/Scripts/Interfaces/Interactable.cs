
using UnityEngine;

public abstract class Interactable : MonoBehaviour
{
    public bool interactOneTime;
    public bool interacted;
    public float buttonOffset;
    // To Interact automatically when entity get closer
    public bool closerToInteract;
    public AudioClip sound;

    private bool soundPlayed;

    public virtual void Interact()
    {
            PlaySound();
            Interacted();
    }
    public virtual void Interacted()
    {
        if (interactOneTime)
        {
            interacted = true;
        }
    }

    public virtual void PlaySound()
    {
        if (sound != null && !soundPlayed)
        {
            soundPlayed = true;
            GameObject audioGm = new GameObject("Interactable Sound");
            AudioSource source = audioGm.AddComponent<AudioSource>();
            if (SoundManager.instance != null) source.outputAudioMixerGroup = SoundManager.instance.SfxGroup;
            source.PlayOneShot(sound);
            Destroy(audioGm, 2f);
        }
    }
}
