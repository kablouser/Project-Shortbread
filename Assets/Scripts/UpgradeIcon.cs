using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class UpgradeIcon : MonoBehaviour
{
    public GameObject root;
    public RawImage image;

    public void Init(in CraftItem craftItem)
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
    }
}
