using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

[CreateAssetMenu(fileName = "Bundle Comparator", menuName = "Unity Support/Bundle CAB Comparator", order = 3)]
public class BundleComparator : ScriptableObject
{
    [SerializeField]
    private PairComparator[] m_PairsToCheck;

    public PairComparator[] PairsToCheck
    {
        get => m_PairsToCheck;
        set => m_PairsToCheck = value;
    }

    public List<TextureItem> Compare()
    {
        var textureItems = new List<TextureItem>();
        
        foreach (var pair in m_PairsToCheck)
        {
            pair.Compare(out var differences);
            var textureItem = new TextureItem(
                pair.ImageA.ImageName,
                pair.ImageA.Width,
                pair.ImageA.Height,
                pair.ImageA.Offset,
                pair.ImageA.Size,
                differences.Count
                );
            textureItems.Add(textureItem);            
        }
        
        return textureItems;
    }


    [MenuItem("Unity Support/Compare Bundles", false, 3)]
    public static void CompareBundles()
    {
        var comparator = Selection.activeObject as BundleComparator;

        if (comparator != null) 
            comparator.Compare();
        else
            Debug.Log("No comparator selected");
    }
}
