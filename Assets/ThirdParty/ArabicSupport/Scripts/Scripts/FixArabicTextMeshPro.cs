using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using ArabicSupport;
using TMPro;
public class FixArabicTextMeshPro : ArabicFix
{
    //public bool ignoreSelectedLanguage = false;

    public override void FixText(string _text)
    {
        if (!LocalizationManager.instance.IsArabicStyleLanguage() && !ignoreSelectedLanguage)
        {
            gameObject.GetComponent<TextMeshProUGUI>().text = _text;
            return;
        }
        string fixedText = ArabicFixer.Fix(_text, showTashkeel, useHinduNumbers);

        gameObject.GetComponent<TextMeshProUGUI>().text = fixedText;
    }
    public override void FixText()
    {
        if (!LocalizationManager.instance.IsArabicStyleLanguage() && !ignoreSelectedLanguage)
        {
            return;
        }
        string fixedText = ArabicFixer.Fix(GetComponent<TextMeshProUGUI>().text, showTashkeel, useHinduNumbers);
        gameObject.GetComponent<TextMeshProUGUI>().text = fixedText;

    }
}
