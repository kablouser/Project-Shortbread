using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class CraftItemUI : MonoBehaviour
{
    public GameObject root;
    public RawImage image;
    public TextMeshProUGUI description;
    public ForEachElement<TextMeshProUGUI> costDisplays;
    public Button craftButton;
    public TextMeshProUGUI buttonText;

    public int craftItemI;

    public void Init(in CraftItem craftItem, int inCraftItemI, MainScript mainScript)
    {
        if (craftItem.icon == null)
        {
            image.texture = null;
            image.enabled = false;
        }
        else
        {
            image.texture = craftItem.icon;
            image.enabled = true;
        }

        description.text = craftItem.description;
        
        for (int i = 0; i < ForEachElement<TextMeshProUGUI>.LENGTH; i++)
        {
            int cost = craftItem.costs[i];
            if (0 < cost)
            {
                costDisplays[i].text = cost.ToString();
                costDisplays[i].transform.parent.gameObject.SetActive(true);
            }
            else
            {
                costDisplays[i].text = string.Empty;
                costDisplays[i].transform.parent.gameObject.SetActive(false);
            }
        }

        craftItemI = inCraftItemI;
        craftButton.onClick.AddListener(() => mainScript.craftingSystem.CraftItem(mainScript, craftItemI));

        UpdateIsCrafted(craftItem, mainScript.craftingSystem);
    }

    public void UpdateIsCrafted(in CraftItem craftItem, in CraftingSystem craftingSystem)
    {
        bool allCostGood = true;
        for (int i = 0; i < ForEachElement<TextMeshProUGUI>.LENGTH; i++)
        {
            bool costGood = craftItem.costs[i] <= craftingSystem.resources[i].value;
            costDisplays[i].color = costGood ?
                craftingSystem.costGood :
                craftingSystem.costBad;

            allCostGood &= costGood;
        }

        craftButton.interactable = allCostGood && !craftItem.isCrafted;
        buttonText.text = craftItem.isCrafted ? "Acquired" : "Fuse";
    }
}
