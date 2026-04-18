using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Audio;
public class AudioController : MonoBehaviour
{
    public bool musicMute; //mutear el musica
    public bool soundMute; //mutear sonido
    public bool masterMute; //mutea el master
    public float previousMusic = 0; //inicio de la musica a este valor
    public float previousSound = 0; //iniio del sonido en este valor
    public float previuosMaster = 0; //inicio del sonido maestro a este valor
    public AudioMixer audioMixer; //Llamamos al audiomixer

    void Start()
    {
        audioMixer = Resources.Load<AudioMixer>("MainMixed");
        //recoge el audio mixer de la carpeta de resources
    }
    public float GetLevel(string bus)
    {
        float value;
        bool result = audioMixer.GetFloat(bus, out value);
        if (result)
        {
            return value;
        }
        else
        {
            return 0f;
        }
    }
    public void MasterVolume(Slider volume)
    {
        audioMixer.SetFloat("Master", volume.value);
    }
    public void SonidosVolume(Slider volume)
    {
        audioMixer.SetFloat("Sonidos", volume.value);
    }
    public void MusicaVolume(Slider volume)
    {
        audioMixer.SetFloat("Musica", volume.value);
    }
    public void MasterMute()
    {
        if (masterMute)
        {
            masterMute = false;
            audioMixer.SetFloat("Master", previuosMaster);
        }
        else
        {
            masterMute = true;
            previuosMaster = GetLevel("Master");
            audioMixer.SetFloat("Master", -80);
        }
    }
    public void SonidosMute()
    {
        if (soundMute)
        {
            soundMute = false;
            audioMixer.SetFloat("Sonidos", previousSound);
        }
        else
        {
            soundMute = true;
            previousSound = GetLevel("Sonidos");
            audioMixer.SetFloat("Sonidos", -80);
        }
    }
    public void MusicaMute()
    {
        if (musicMute)
        {
            musicMute = false;
            audioMixer.SetFloat("Musica", previousMusic);
        }
        else
        {
            musicMute = true;
            previousMusic = GetLevel("Musica");
            audioMixer.SetFloat("Musica", -80);
        }
    }
}