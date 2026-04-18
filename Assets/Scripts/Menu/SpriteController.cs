using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
public class SpriteController : MonoBehaviour
{
    public GameObject idioma;
    public Sprite englishSprite;
    public Sprite spanishSprite;
    public LocaleSelector localeSelector;
    public bool isSpanish;
    void Start()
    {
        isSpanish = localeSelector.isSpanish;
        idioma = gameObject;
        Sprite Load(string imageName, string spriteName)
        {
            Sprite[] all = Resources.LoadAll<Sprite>(imageName);
            foreach (var s in all)
            {
                if (s.name == spriteName)
                {
                    return s;
                }
            }
            return null;
        }
        englishSprite = Load("idiomas", "english");
        spanishSprite = Load("idiomas", "spanish");
    }
    public void ChangeBool()
    {
        isSpanish = !isSpanish;
    }
    void Update()
    {
        if (isSpanish)
        {
            idioma.GetComponent<Image>().sprite = spanishSprite;
        }
        else
        {
            idioma.GetComponent<Image>().sprite = englishSprite;
        }
    }
}
