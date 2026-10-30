using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using ArabicSupport;

public class ArabicFix : MonoBehaviour
{

    public bool showTashkeel = true;
    public bool useHinduNumbers = true;
    public bool ignoreSelectedLanguage = false;

   
    public virtual void FixText()
    {
        if (!LocalizationManager.instance.IsArabicStyleLanguage() && !ignoreSelectedLanguage)
        {
            return;
        }
        string fixedText = ArabicFixer.Fix(GetComponent<Text>().text, showTashkeel, useHinduNumbers);
        gameObject.GetComponent<Text>().text = fixedText;
    }
    public virtual void FixText(string _text)
    {
        if (!LocalizationManager.instance.IsArabicStyleLanguage() && !ignoreSelectedLanguage)
        {
            gameObject.GetComponent<Text>().text = _text;
            return;
        }
        string fixedText = ArabicFixer.Fix(_text, showTashkeel, useHinduNumbers);

        gameObject.GetComponent<Text>().text = fixedText;
    }
}
