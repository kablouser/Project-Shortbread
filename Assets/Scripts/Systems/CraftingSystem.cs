using UnityEngine;
using TMPro;
using UnityEngine.UI;
using System.Collections.Generic;
using Unity.VisualScripting.FullSerializer;

[System.Serializable]
public struct ForEachElement<T>
{
    public const int LENGTH = 4;
    public T fire, earth, air, water;

    public T this[int i]
    {
        get => i switch
        {
            0 => fire,
            1 => earth,
            2 => air,
            3 => water,
            _ => default,
        };

        set
        {
            switch (i)
            {
                case 0: fire = value; break;
                case 1: earth = value; break;
                case 2: air = value; break;
                case 3: water = value; break;
                default: break;
            };
        }
    }
}

[System.Serializable]
public struct CraftingResource
{
    public int value;
    public TextMeshProUGUI text;
    public Sprite pickupSprite;

    public void AddValue(int add, MainScript main, bool updateCraftingUI = true)
    {
        SetValue(value + add, main, updateCraftingUI);
    }

    public void SetValue(int newValue, MainScript main, bool updateCraftingUI = true)
    {
        if ((0 < value) != (0 < newValue))
        {
            text.transform.parent.gameObject.SetActive(0 < newValue);
        }

        value = newValue;

        if (0 < value)
        {
            text.text = value.ToString();
        }
    }

    public void Start()
    {
        text.transform.parent.gameObject.SetActive(0 < value);

        if (0 < value)
        {
            text.text = value.ToString();
        }
    }
}

[System.Serializable]
public struct CraftItem
{
    public bool isCrafted;
    public Texture icon;
    public ForEachElement<int> costs;
    [TextArea(1,3)]
    public string description;
    public StatType upgradeType;
    public float upgradeValue;
}

[System.Serializable]
public struct CraftingSystem
{
    public TextMeshProUGUI interactPrompt;
    public Transform craftingStation;
    public float interactDistance;

    public ForEachElement<CraftingResource> resources;
    public CraftItem[] items;

    public GameObject craftingPanel;

    public UpgradeIcon upgradeIconUIPrefab;
    public List<UpgradeIcon> upgradeIconUIs;
    public RectTransform upgradedItemsContainer;
    public ElementSelectorUI[] elementSelectorsUI;
    public CraftButtonUI craftButtonUI;

    private bool isExitMenuThisFrame;

    public void Start(MainScript mainScript)
    {
        isExitMenuThisFrame = false;

        resources.fire.Start();
        resources.earth.Start();
        resources.air.Start();
        resources.water.Start();

        foreach(ElementSelectorUI selector in elementSelectorsUI)
        {
            selector.Init(mainScript);
        }

        craftButtonUI.Init(mainScript);

        //craftItemUIs.Reserve(items.Length);
        //RectTransform listParent = craftItemUIsLayout.GetComponent<RectTransform>();
        //for (int i = 0; i < items.Length; i++)
        //{
        //    craftItemUIs.Add(Object.Instantiate(craftItemUIPrefab, listParent).GetComponent<CraftItemUI>());
        //}

        //UpdateCraftItemUIs(UpdateCraftItemUIMode.Init, mainScript, true);
    }

    public void Update(MainScript mainScript)
    {
        bool inRange = Vector3.SqrMagnitude(craftingStation.position - mainScript.player.transform.position) <= interactDistance * interactDistance;

        interactPrompt.enabled =
            !craftingPanel.activeSelf &&
            inRange &&
            (mainScript.gameState <= GameState.Survive);

        //if (craftingPanel.activeSelf)
        //{
        //    RectTransform listParent = craftItemUIsLayout.GetComponent<RectTransform>();

        //    listParent.sizeDelta = new Vector2
        //    {
        //        x = listParent.sizeDelta.x,
        //        // only works when active :(
        //        y = craftItemUIsLayout.preferredHeight
        //    };

        //    if (lastSelected != mainScript.eventSystem.currentSelectedGameObject)
        //    {
        //        lastSelected = mainScript.eventSystem.currentSelectedGameObject;
        //        if (lastSelected)
        //        {
        //            // trial and error code:

        //            // transform after the listParent
        //            RectTransform selectedRoot = (RectTransform)lastSelected.transform.GetParentUntil(listParent);

        //            Vector2 listParentAnchorPos = listParent.anchoredPosition;

        //            float viewHeight = scrollRect.GetComponent<RectTransform>().rect.height;

        //            Vector2 viewRange = new Vector2(listParentAnchorPos.y, listParentAnchorPos.y + viewHeight);

        //            float minPos =
        //                -(selectedRoot.anchoredPosition.y + selectedRoot.sizeDelta.y / 2);
        //            float maxPos =
        //                -(selectedRoot.anchoredPosition.y - selectedRoot.sizeDelta.y / 2);

        //            if (minPos < viewRange.x)
        //            {
        //                listParentAnchorPos.y = -(selectedRoot.anchoredPosition.y + selectedRoot.sizeDelta.y / 2);
        //            }
        //            if (viewRange.y < maxPos)
        //            {
        //                listParentAnchorPos.y = -viewHeight -
        //                    (selectedRoot.anchoredPosition.y - selectedRoot.sizeDelta.y / 2);
        //            }

        //            listParent.anchoredPosition = listParentAnchorPos;
        //        }
        //    }
        //}

        if (interactPrompt.enabled && mainScript.inputSystem.isInteractDown && !isExitMenuThisFrame)
        {
            EnterMenu(mainScript);
        }
        else if (craftingPanel.activeSelf && (mainScript.inputSystem.isEscapeDown || !inRange))
        {
            ExitMenu(mainScript);
        }

        isExitMenuThisFrame = false;
    }

    public void EnterMenu(MainScript mainScript)
    {
        craftingPanel.SetActive(true);
        interactPrompt.enabled = false;
        Time.timeScale = 0f;

        GameObject firstButton = elementSelectorsUI[0].button.gameObject;
        mainScript.eventSystem.firstSelectedGameObject = firstButton;
        mainScript.eventSystem.SetSelectedGameObject(firstButton);

        foreach (ElementSelectorUI selector in elementSelectorsUI)
        {
            selector.ResetUI();
        }
        craftButtonUI.ResetUI();
    }

    public void ExitMenu(MainScript mainScript)
    {
        craftingPanel.SetActive(false);
        Time.timeScale = 1.0f;
        mainScript.eventSystem.firstSelectedGameObject = null;
        mainScript.eventSystem.SetSelectedGameObject(null);

        if (mainScript.gameState < GameState.Survive)
        {
            // start game
            mainScript.SetGameState(GameState.Survive);
        }

        isExitMenuThisFrame = true;
    }

    public enum UpdateCraftItemUIMode { Init, IsCrafted};

    public void CraftItem(MainScript mainScript)
    {
        // FIRST FIND WHAT THE ELEMENTS WOULD CRAFT
        ForEachElement<int> usedElements = new ForEachElement<int>();
        foreach (ElementSelectorUI selector in elementSelectorsUI)
        {
            if(selector.selectedResource > -1 && selector.selectedResource < ForEachElement<int>.LENGTH)
            {
                usedElements[selector.selectedResource] += 1;
                if(resources[selector.selectedResource].value < usedElements[selector.selectedResource])
                {
                    craftButtonUI.resultText.SetText("Insufficient elements");
                    return;
                }
            }
        }

        int craftItemIndex = -1;
        for(int i = 0; i < items.Length; i++)
        {
            bool enoughElements = true;
            for (int e = 0; e < ForEachElement<int>.LENGTH; e++)
            {
                // needs exact number of elements for crafting
                if(usedElements[e] != items[i].costs[e])
                {
                    enoughElements = false;
                    break;
                }
            }

            if(enoughElements)
            {
                craftItemIndex = i;
                break;
            }
        }

        if(craftItemIndex == -1)
        {
            craftButtonUI.resultText.SetText("Nothing happened");
            return;
        }

        // BACK OUT IF ALREADY CRAFTED
        ref CraftItem craftItem = ref items[craftItemIndex];
        if(craftItem.isCrafted)
        {
            craftButtonUI.resultText.SetText("Already discovered");
            return;
        }

        // CRAFT ITEM AND RESET ALL SELECTORS 
        mainScript.upgradeSystem.ApplyUpgrade(mainScript, ref mainScript.player, craftItem.upgradeType, craftItem.upgradeValue);
        mainScript.audioSystem.PlayVFX(mainScript.audioSystem.craftingCompleteVFX);

        craftItem.isCrafted = true;
        resources.fire.AddValue(-craftItem.costs[0], mainScript, false);
        resources.earth.AddValue(-craftItem.costs[1], mainScript, false);
        resources.air.AddValue(-craftItem.costs[2], mainScript, false);
        resources.water.AddValue(-craftItem.costs[3], mainScript, false);

        UpgradeIcon newIcon = Object.Instantiate(upgradeIconUIPrefab, upgradedItemsContainer);
        newIcon.Init(craftItem);
        upgradeIconUIs.Add(newIcon);

        craftButtonUI.resultText.SetText(craftItem.description);

        foreach (ElementSelectorUI selector in elementSelectorsUI)
        {
            selector.ResetUI();
        }
    }
}
