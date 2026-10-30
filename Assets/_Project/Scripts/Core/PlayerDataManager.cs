using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerDataManager : MonoBehaviour
{
    public static PlayerDataManager instance;

    public PlayerData playerData;
    bool isDataInitialized=false;
    private void Awake()
    {
        if(instance == null)
        {
            instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    #region Currancy
    public void UpdateCash(int _val)
    {
        playerData.playerCash += _val;
        if (_val < 0)
        {
            UpdatePlayerSpending(Mathf.Abs(_val));
        }
    }
    public void UpdatePlayerSpending(int _val)
    {
        if (playerData.playerTotalSpending > -1)
        {
            playerData.playerTotalSpending += _val;

            if (playerData.playerTotalSpending >= 500)
            {
              playerData.playerTotalSpending = -1;
             // StartCoroutine(MonetizationManager.instance.StartDroneDelivery());
            }
        }
    }
    public void UpdateBlitz(int _val)
    {
        playerData.playerBlitz += _val;
    }
    public void UpdateXP(int _val)
    {
        playerData.playerXP += _val;

    }

    public void UpdatePlayerCountry(int _index)
    {
        playerData.countryIndex = _index;
    }
    public void UpdateHealth(int _val)
    {
        print("Healthhhh   "+_val);
        int health = playerData.playerHealth;
        if(playerData.playerHealth <= 0&&_val<0)
        {
            playerData.playerHealth = 0;
            return;
        }
        if(playerData.playerHealth > 100)
        {
            playerData.playerHealth = 100;
            return;
        }
        if(health + _val>=100)
        {
            print("This iiiiiii");
            playerData.playerHealth = 100;
            return ;
        }
        playerData.playerHealth += _val;
    }
    public void UpdateLevel(int _val)
    {
        playerData.playerLevel += _val;
    }
    #endregion
    public void LoadPlayerData()
    {
        playerData = (PlayerData)SerializationManager.LoadFile("Player");

        if (playerData == null)
        {
            playerData = (PlayerData)SerializationManager.Load("Player");
            if (playerData == null)
            {
                playerData = new PlayerData();
            }
        }

        // For existing users without health data
        if (!PlayerPrefs.HasKey("HealthSystemInitialized"))
        {
            if (playerData.playerHealth <= 0 || playerData.maxHealth <= 0)
            {
                playerData.playerHealth = 100;
                playerData.maxHealth = 100;
            }

            PlayerPrefs.SetInt("HealthSystemInitialized", 1);
            PlayerPrefs.Save();
        }


        if (!PlayerPrefs.HasKey("PlayerCountry"))
        {
            playerData.countryIndex = -1;
        }
        // for existing user defaults
        if (playerData.playerLevel <= 0) playerData.playerLevel = 1;
        if (playerData.playerBlitz < 0) playerData.playerBlitz = 100;
        if (playerData.playerXP <= 0) playerData.playerXP = 0;

        isDataInitialized = true;
    }

    public void SavePlayerData()
    {
        if (isDataInitialized)
        {
            SerializationManager.Save(playerData, "Player");
           // CanteenCounterUI.instance.SaveCart();
        }
    }
    private void OnApplicationPause(bool pause)
    {
        if (pause)
        {
            if (GameController.instance == null)
            {
                SavePlayerData();
            }
        }
    }

    private void OnApplicationQuit()
    {
        if (GameController.instance == null)
        {
            SavePlayerData();
        }
    }
}

[System.Serializable]
public class PlayerData
{
    public string playerName = "Enter Name";
    public int playerCash;
    public int playerBlitz;
    public int playerXP;
    public int playerHealth;
    public int maxHealth;
    public int playerLevel;
    public int playerOrderRecieveCount;
    public int playerRewardGivenState;  //Used Only To Identify Users before new env
    public int selectedQualitySettings;
    public int selectedLanguage;
    public int playerTotalSpending;
    public bool qualityPnlDisplayed;
    public bool initialLocalizationSet;
    public Vector3 playerPosition;
    public Quaternion playerRotation;
    public string utcforwelcomebackreward;
    public int countryIndex;

    public PlayerData()
    {
  ///revised cash according to the document
        playerCash = 10000;
        playerBlitz = 100;
        playerXP = 0;
        playerHealth = 100;
        maxHealth = 100;
        playerLevel = 1;
        selectedLanguage = 0;
        playerRewardGivenState = 1;
        playerOrderRecieveCount = 0;
        selectedQualitySettings = 0;
        qualityPnlDisplayed = false;
        initialLocalizationSet = true;
        playerPosition = new Vector3(0, 0, 0);
        playerRotation = Quaternion.identity;
        countryIndex = -1;
    }
}
