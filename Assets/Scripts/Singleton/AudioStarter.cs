using UnityEngine;

public class AudioStarter : MonoBehaviour
{
    public enum MusicOption
    {
        Menu,
        Gameplay,
        Exito,
        Fracaso
    }
    public MusicOption musicOption;
    void Start()
    {
        AudioManager.instance.PlayMusic(musicOption.ToString());
    }
}
