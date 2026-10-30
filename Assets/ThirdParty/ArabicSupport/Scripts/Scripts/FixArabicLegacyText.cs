using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using ArabicSupport;
using UnityEngine.UI;
public class FixArabicLegacyText : ArabicFix
{
    //public bool ignoreSelectedLanguage = false;

    public override void FixText(string _text)
    {
        if(!LocalizationManager.instance.IsArabicStyleLanguage() && !ignoreSelectedLanguage)
        {
            gameObject.GetComponent<Text>().text = _text;
            return;
        }
        string fixedText = ArabicFixer.Fix(_text, showTashkeel, useHinduNumbers);

        gameObject.GetComponent<Text>().text = fixedText;
    }
    public override void FixText()
    {
        if (!LocalizationManager.instance.IsArabicStyleLanguage() && !ignoreSelectedLanguage)
        {
            return;
        }
        string fixedText = ArabicFixer.Fix(GetComponent<Text>().text, showTashkeel, useHinduNumbers);
        gameObject.GetComponent<Text>().text = fixedText;

    }
}
