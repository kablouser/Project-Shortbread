using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class IndentAttribute : PropertyAttribute
{
    public int indents;
    public string label;
    public IndentAttribute(int indents = 1, string label = null)
    {
        this.indents = indents;
        this.label = label;
    }
}
