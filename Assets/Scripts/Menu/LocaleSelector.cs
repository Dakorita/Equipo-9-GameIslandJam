using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.Localization.Settings;



public class LocaleSelector : MonoBehaviour
{
    private bool active = false;
    public bool isSpanish = true;
    public int localeID = 1;
    public void ChangeLocale()
    {

        if (isSpanish == true)
        {
            localeID = 0;
            isSpanish = false;
            Debug.Log("???");
        }
        else if (isSpanish == false)
        {
            localeID = 1;
            isSpanish = true;
            Debug.Log("elseif");
        }
        StartCoroutine(SetLocale(localeID));
    }
    IEnumerator SetLocale(int _localeID)
    {

        Debug.Log(localeID);
        yield return LocalizationSettings.InitializationOperation;
        LocalizationSettings.SelectedLocale = LocalizationSettings.AvailableLocales.Locales[_localeID];
    }
}