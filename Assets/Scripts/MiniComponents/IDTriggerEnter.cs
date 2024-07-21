using UnityEngine;

public class IDTriggerEnter : IDComponent
{
    public MainScript mainScript;

    public void OnTriggerEnter2D(Collider2D collider)
    {
        mainScript.ProcessTriggerEnter(this, collider);
    }
}
