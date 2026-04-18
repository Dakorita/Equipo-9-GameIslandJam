using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AudioManager : MonoBehaviour
{

    public static AudioManager instance;

    public AudioSource sfxSource;
    public AudioSource musicSource;
    private bool isActive = false;

    public Dictionary<string, AudioClip> sfxClips = new Dictionary<string, AudioClip>();
    public Dictionary<string, AudioClip> musicClips = new Dictionary<string, AudioClip>();

    private void Awake()
    {
        if (instance == null) instance = this;
        else { Destroy(gameObject); return; }
        DontDestroyOnLoad(gameObject);
        LoadSFXClips();
        LoadMusicClips();

    }
    private void LoadSFXClips()
    {
        sfxClips["Explosion"] = Resources.Load<AudioClip>("SFX/BigExplosion");
    }

    private void LoadMusicClips()
    {
        musicClips["Menu"] = Resources.Load<AudioClip>("Music/Menu");
        musicClips["Gameplay"] = Resources.Load<AudioClip>("Music/Gameplay");
        musicClips["Exito"] = Resources.Load<AudioClip>("Music/Exito");
        musicClips["Fracaso"] = Resources.Load<AudioClip>("Music/Fracaso");
    }

    public void PlaySFX(string clipName)
    {
        if (sfxClips.ContainsKey(clipName))
        {
            sfxSource.PlayOneShot(sfxClips[clipName]);
        }
        else Debug.LogWarning("El AudioClip " + clipName + " no se encontró en el diccionario de sfxClips.");
        
    }
    public void PlayMusic(string clipName)
    {
        if (musicClips.ContainsKey(clipName))
        {
            musicSource.clip = musicClips[clipName];
            musicSource.loop = true;
            musicSource.Play();
        }
        else Debug.LogWarning("El AudioClip " + clipName + " no se encontró en el diccionario de musicClips.");

    }

    public void PlaySFXWithCooldown(string clipName, float cooldownSeconds)
    {
        if (sfxClips.ContainsKey(clipName))
        {
            if (!isActive){
                StartCoroutine(SFXCooldownCoroutine(cooldownSeconds));
                sfxSource.PlayOneShot(sfxClips[clipName]);
            }
            
        }
        else Debug.LogWarning("El AudioClip " + clipName + " no se encontró en el diccionario de sfxClips.");
        //AudioManager.instance.PlaySFXWithCooldown("Saved", );
    }
    private IEnumerator SFXCooldownCoroutine(float cooldownSeconds)
    {
        isActive = true;
        yield return new WaitForSeconds(cooldownSeconds);
        isActive = false;
    }
}
