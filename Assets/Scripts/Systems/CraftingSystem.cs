using UnityEngine;
using TMPro;
using UnityEngine.UI;
using System.Collections.Generic;

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

        if (updateCraftingUI)
        {
            main.craftingSystem.UpdateCraftItemUIs(CraftingSystem.UpdateCraftItemUIMode.IsCrafted, main);
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
    public UpgradeType upgradeType;
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

    public GameObject craftItemUIPrefab;
    public GameObject craftingPanel;
    public VerticalLayoutGroup craftItemUIsLayout;
    public ScrollRect scrollRect;
    public Color costGood, costBad;

    // same order as items
    public List<CraftItemUI> craftItemUIs;

    private GameObject lastSelected;

    public void Start(MainScript mainScript)
    {
        resources.fire.Start();
        resources.earth.Start();
        resources.air.Start();
        resources.water.Start();

        craftItemUIs.Reserve(items.Length);
        RectTransform listParent = craftItemUIsLayout.GetComponent<RectTransform>();
        for (int i = 0; i < items.Length; i++)
        {
            craftItemUIs.Add(Object.Instantiate(craftItemUIPrefab, listParent).GetComponent<CraftItemUI>());
        }

        UpdateCraftItemUIs(UpdateCraftItemUIMode.Init, mainScript, true);
    }

    public void Update(MainScript mainScript)
    {
        bool inRange = Vector3.SqrMagnitude(craftingStation.position - mainScript.player.transform.position) <= interactDistance * interactDistance;

        interactPrompt.enabled =
            !craftingPanel.activeSelf &&
            inRange &&
            (mainScript.gameState == GameState.Tutorial || mainScript.gameState == GameState.Survive);

        if (craftingPanel.activeSelf)
        {
            RectTransform listParent = craftItemUIsLayout.GetComponent<RectTransform>();

            listParent.sizeDelta = new Vector2
            {
                x = listParent.sizeDelta.x,
                // only works when active :(
                y = craftItemUIsLayout.preferredHeight
            };

            if (lastSelected != mainScript.eventSystem.currentSelectedGameObject)
            {
                lastSelected = mainScript.eventSystem.currentSelectedGameObject;
                if (lastSelected)
                {
                    // trial and error code:

                    // transform after the listParent
                    RectTransform selectedRoot = (RectTransform)lastSelected.transform.GetParentUntil(listParent);

                    Vector2 listParentAnchorPos = listParent.anchoredPosition;

                    float viewHeight = scrollRect.GetComponent<RectTransform>().rect.height;

                    Vector2 viewRange = new Vector2(listParentAnchorPos.y, listParentAnchorPos.y + viewHeight);

                    float minPos =
                        -(selectedRoot.anchoredPosition.y + selectedRoot.sizeDelta.y / 2);
                    float maxPos =
                        -(selectedRoot.anchoredPosition.y - selectedRoot.sizeDelta.y / 2);

                    if (minPos < viewRange.x)
                    {
                        listParentAnchorPos.y = -(selectedRoot.anchoredPosition.y + selectedRoot.sizeDelta.y / 2);
                    }
                    if (viewRange.y < maxPos)
                    {
                        listParentAnchorPos.y = -viewHeight -
                            (selectedRoot.anchoredPosition.y - selectedRoot.sizeDelta.y / 2);
                    }

                    listParent.anchoredPosition = listParentAnchorPos;
                }
            }
        }

        if (interactPrompt.enabled && mainScript.inputSystem.isInteractDown)
        {
            EnterMenu(mainScript);
        }
        else if (craftingPanel.activeSelf && (mainScript.inputSystem.isEscapeDown || !inRange))
        {
            ExitMenu(mainScript);
        }
    }

    public void EnterMenu(MainScript mainScript)
    {
        UpdateCraftItemUIs(UpdateCraftItemUIMode.IsCrafted, mainScript, true);
        craftingPanel.SetActive(true);
        interactPrompt.enabled = false;
        Time.timeScale = 0f;
        mainScript.eventSystem.SetSelectedGameObject(mainScript.eventSystem.firstSelectedGameObject = craftItemUIs[0].craftButton.gameObject);
    }

    public void ExitMenu(MainScript mainScript)
    {
        craftingPanel.SetActive(false);
        Time.timeScale = 1.0f;
        mainScript.eventSystem.firstSelectedGameObject = null;
        mainScript.eventSystem.SetSelectedGameObject(null);

        if (mainScript.gameState == GameState.Tutorial)
        {
            // start game
            mainScript.gameState = GameState.Survive;
            mainScript.gameStartText.enabled = true;
            mainScript.gameStartText.CrossFadeAlpha(0.0f, 3.0f, true);
        }
    }

    public enum UpdateCraftItemUIMode { Init, IsCrafted};
    public void UpdateCraftItemUIs(UpdateCraftItemUIMode mode, MainScript mainScript, bool updateOnUIClosed = false)
    {
        if (!updateOnUIClosed && !craftingPanel.activeSelf)
            return;

        for (int i = 0; i < items.Length; i++)
        {
            if (i < craftItemUIs.Count)
            {
                switch (mode)
                {
                    case UpdateCraftItemUIMode.Init:
                        craftItemUIs[i].Init(items[i], i, mainScript); break;
                    case UpdateCraftItemUIMode.IsCrafted:
                        craftItemUIs[i].UpdateIsCrafted(items[i], this); break;
                }
            }
            else
            {
                Debug.LogError("items and craftItemUIs needs to be same size");
                break;
            }
        }
    }

    public void CraftItem(MainScript mainScript, int i)
    {
        if (!(0 <= i && i < items.Length))
            return;

        ref CraftItem craftItem = ref items[i];
        if (craftItem.isCrafted)
            return;

        bool enoughElements = true;
        for (int e = 0; e < ForEachElement<int>.LENGTH; e++)
        {
            if (resources[e].value < craftItem.costs[e])
            {
                enoughElements = false;
                break;
            }
        }

        if (!enoughElements)
            return;

        mainScript.upgradeSystem.ApplyUpgrade(mainScript, ref mainScript.player, craftItem.upgradeType, craftItem.upgradeValue);
        mainScript.audioSystem.PlayVFXAtLocation(mainScript.audioSystem.craftingCompleteVFX, mainScript.player.transform.position);

        craftItem.isCrafted = true;
        resources.fire.AddValue(-craftItem.costs[0], mainScript, false);
        resources.earth.AddValue(-craftItem.costs[1], mainScript, false);
        resources.air.AddValue(-craftItem.costs[2], mainScript, false);
        resources.water.AddValue(-craftItem.costs[3], mainScript, false);
        UpdateCraftItemUIs(UpdateCraftItemUIMode.IsCrafted, mainScript);
    }

}
