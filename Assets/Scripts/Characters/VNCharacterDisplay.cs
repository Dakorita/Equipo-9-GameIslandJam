using UnityEngine;
using UnityEngine.UI;

public class VNCharacterDisplay : MonoBehaviour
{
    public VNCharacterSlot slot;
    public Image characterImage;

    public void ShowCharacter(Sprite sprite)
    {
        characterImage.sprite = sprite;
        characterImage.enabled = true;
    }

    public void Hide()
    {
        characterImage.enabled = false;
    }

    public void SetDimmed(bool dimmed)
    {
        Color c = characterImage.color;
        c.a = dimmed ? 0.5f : 1f;
        characterImage.color = c;
    }
}