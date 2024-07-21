using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class IDTriggerEnter : IDComponent
{
    public MainScript mainScript;

    public void OnTriggerEnter2D(Collider2D collision)
    {
        mainScript.triggerEnterEvents.Add(new TriggerEvent { id = id, collider = collision });
    }
}
