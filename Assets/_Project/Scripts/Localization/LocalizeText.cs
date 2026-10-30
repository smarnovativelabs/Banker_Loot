using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class LocalizeText : MonoBehaviour
{
    [HideInInspector]public Text text;
    public string textToDisplay;
    public long callbackCounter;
    public bool isInitialized;
    public void Start()
    {
        text = GetComponent<Text>();
        if(!isInitialized)
            textToDisplay = text.text;
        UpdateText(textToDisplay);
        
        callbackCounter = LocalizationManager.instance.RegisterCallback(OnUpdateLanguage);
    }
   
    private void OnEnable()
    {
        //RegisterEvent();
        //UpdateText(textToDisplay);
    }
    public void UpdateText(string _text)
    {
        
        textToDisplay = _text;
        isInitialized = true;
        string _localizedText = LocalizationManager.instance.LocalizeText(textToDisplay);
        if (string.IsNullOrEmpty(_localizedText))
        {
            _localizedText = _text;
        }
        if (GetComponent<ArabicFix>())
        {
            GetComponent<ArabicFix>().FixText(_localizedText);
        }
        else
        {
            text.text = _localizedText;
        }
    }
    public void OnUpdateLanguage()
    {
        UpdateText(textToDisplay);
    }
    public void OnDestroy()
    {
        LocalizationManager.instance.UnRegisterCallback(callbackCounter);
    }
}
