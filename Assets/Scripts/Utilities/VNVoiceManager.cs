using UnityEngine;

public class VNVoiceManager : MonoBehaviour
{
    public AudioSource audioSource;
    public bool enableVoices = true;

    public void PlayVoice(AudioClip clip)
    {
        if (!enableVoices) return;
        if (clip == null) return;

        audioSource.Stop();
        audioSource.PlayOneShot(clip);
    }

    public void StopVoice()
    {
        if (audioSource.isPlaying)
            audioSource.Stop();
    }
}