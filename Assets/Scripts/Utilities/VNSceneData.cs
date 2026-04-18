using System.Collections.Generic;
using UnityEngine;

public class VNSceneData : MonoBehaviour
{
    [Header("Personajes que participan")]
    public List<VNCharacter> characters;

    [Header("Diálogos de la escena")]
    public List<VNDialogueLine> dialogues;
}