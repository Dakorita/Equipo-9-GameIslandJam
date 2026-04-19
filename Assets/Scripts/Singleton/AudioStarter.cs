using UnityEngine;

public class AudioStarter : MonoBehaviour
{
    public enum MusicOption
    {
        Menu,
        Gameplay,
        gameplay2,
        Exito,
        Fracaso
    }
    public MusicOption musicOption;
    void Start()
    {
        AudioManager.instance.PlayMusic(musicOption.ToString());
    }
}
