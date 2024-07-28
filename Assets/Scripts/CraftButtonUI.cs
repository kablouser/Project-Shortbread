using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class CraftButtonUI : MonoBehaviour
{
    public Button craftButton;
    public TMP_Text resultText;

    public void Init(MainScript mainScript)
    {
        craftButton.onClick.AddListener(() => mainScript.craftingSystem.CraftItem(mainScript));
    }

    public void ResetUI()
    {
        resultText.SetText(string.Empty);
    }
}
