using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
public class InAppBtn : MonoBehaviour
{
    public Text headingTxt;
    public Text rewardDesc;
    public Text priceTxt;
    public Image displayImg;
    public Transform valueTagCntnr;
    public InAppProduct productToBuy;

    public virtual void OnInitializeIAPBtn(InAppProduct _product)
    {

    }
    public virtual void OnSuccessPurchase()
    {

    }
}
