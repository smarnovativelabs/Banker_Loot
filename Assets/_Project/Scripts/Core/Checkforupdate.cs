using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
public class Checkforupdate : MonoBehaviour
{
    public static Checkforupdate instance;
    public string currentVersion;  // Set this from the current build
    public string requiredVersion = ""; // Set this from the server or hardcoded
    public string latestVersion = "";   // Set this from the server or hardcoded
    public string updatePanelTextString;
    public GameObject updateRequiredPanel;  // Reference to the "Update Required" UI panel
    public GameObject laterButton;
    public Text updateText;
    void Awake()
    {
        instance = this;
    }
    void Start()
    {
        currentVersion = Application.version;
        StartCoroutine(CheckVersionAndDisplayPanel());
    }

    IEnumerator CheckVersionAndDisplayPanel()
    {
        while (string.IsNullOrEmpty(requiredVersion))
        {
            yield return null;
        
        }
        while (string.IsNullOrEmpty(latestVersion))
        {
            yield return null;
          
        }

        // Compare current version with required version
        int versionComparison = CompareVersions(currentVersion, requiredVersion);

        if (versionComparison < 0)  // currentVersion < requiredVersion
        {
            // Show "Update Required" panel
            updateRequiredPanel.SetActive(true);
            //updateOptionalPanel.SetActive(false);
            laterButton.SetActive(false);
            updateText.text = updatePanelTextString;
            print("must update");
        }
        else if (versionComparison >= 0 && CompareVersions(currentVersion, latestVersion) < 0)  // currentVersion >= requiredVersion but < latestVersion
        {
            // Show "Optional Update" panel
            updateRequiredPanel.SetActive(true);
            laterButton.SetActive(true);
            updateText.text = updatePanelTextString;
            print("not not  update");
        }
        else
        {
            // No update needed, hide both panels
            updateRequiredPanel.SetActive(false);
            laterButton.SetActive(false);
        }
    }

    public void hideOptionalUpdatePanel()
    {
        updateRequiredPanel.SetActive(false);
    }

    public void OnUpdateClick()
    {
        Application.OpenURL("https://play.google.com/store/apps/details?id=com.peri.food.processing.simulator.game.factory");
    }

    /// <summary>
    /// Compares two version strings (e.g., "1.0.1" and "1.1.0").
    /// Returns:
    /// -1 if versionA < versionB
    ///  0 if versionA == versionB
    ///  1 if versionA > versionB
    /// </summary>
    int CompareVersions(string versionA, string versionB)
    {
        string[] vA = versionA.Split('.');
        string[] vB = versionB.Split('.');

        // Compare major, minor, and patch versions
        for (int i = 0; i < Mathf.Max(vA.Length, vB.Length); i++)
        {
            int a = (i < vA.Length) ? int.Parse(vA[i]) : 0;
            int b = (i < vB.Length) ? int.Parse(vB[i]) : 0;

            if (a < b)
            {
                return -1;  // versionA is smaller
            }
            if (a > b)
            {
                return 1;   // versionA is greater
            }
        }

        return 0;  // versions are equal
    }
}
