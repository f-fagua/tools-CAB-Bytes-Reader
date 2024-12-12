using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TextureItem
{
    public string name { get; set; }
    

    public int width { get; set; }
    
    public int height { get; set; }
    
    public int offset { get; set; }

    public int size { get; set; }

    public TextureItem(string name, int width, int height, int offset, int size)
    {
        this.name = name;
        this.width = width;
        this.height = height;
        this.offset = offset;
        this.size = size;
        this.size = size;
    }
}
