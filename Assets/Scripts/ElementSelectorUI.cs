using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class ElementSelectorUI : MonoBehaviour
{
    public Button button;
    public RawImage elementImage;
    public int selectedResource;

    public void Init(MainScript mainScript)
    {
        button.onClick.AddListener(() =>
        {
            SelectNextElement(mainScript.craftingSystem.resources);
            mainScript.audioSystem.PlayVFX(mainScript.audioSystem.craftingVFX);
        });
    }

    public void ResetUI()
    {
        selectedResource = -1;
        elementImage.texture = null;
        elementImage.enabled = false;
    }

    public void SelectNextElement(in ForEachElement<CraftingResource> resources)
    {
        for (int e = 1; e < ForEachElement<int>.LENGTH + 1; e++)
        {
            int indexToCheck = selectedResource + e;
            if (indexToCheck >= ForEachElement<int>.LENGTH)
            {
                indexToCheck -= ForEachElement<int>.LENGTH;
            }

            if (resources[indexToCheck].value > 0)
            {
                elementImage.texture = resources[indexToCheck].pickupSprite.texture;
                elementImage.enabled = true;
                selectedResource = indexToCheck;
                return;
            }
        }
        ResetUI();
    }
}
