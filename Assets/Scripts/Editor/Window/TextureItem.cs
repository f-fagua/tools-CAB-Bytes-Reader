using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TextureItem
{
    public string name { get; set; }
    public int value { get; set; }

    public TextureItem(string name, int value)
    {
        this.name = name;
        this.value = value;
    }
}
