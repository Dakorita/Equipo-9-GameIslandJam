using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Localization;

[System.Serializable]
public class VNDialogueLine
{
    public int characterIndex;

    [TextArea(3, 5)]
    public string text;

    public LocalizedString localizedText;
    public bool useLocalization;

    [Header("Voz 'opcional'")]
    public AudioClip voiceClip;

    public List<int> visibleCharacters;
}   