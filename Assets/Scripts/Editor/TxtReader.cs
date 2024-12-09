using System;
using System.Collections;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using UnityEditor;
using UnityEngine;

public class TxtReader : MonoBehaviour
{
    private static string s_ResSFilePathA = "Assets/CABs/A/CAB-21478c858c7420d60c173ae6bb7b91a9.resS";
    private static string s_ResSFilePathB = "Assets/CABs/B/CAB-21478c858c7420d60c173ae6bb7b91a9.resS";
    
    private static string s_InFilePath = "Assets/CABs/CAB-21478c858c7420d60c173ae6bb7b91a9.txt";
    private static string s_OutYAMLFilePath = "Assets/Data/Textures.txt";

    private static string s_ImageReaderFolder = "Assets/Data/";
    
    private static bool s_IsSerializingTextures = false;
    
    private static string s_KeyName   = "m_Name";
    private static string s_KeyWidth  = "m_Width";
    private static string s_KeyHeight = "m_Height";
    private static string s_KeyOffset = "offset";
    private static string s_KeySize   = "size";

    private static string[] k_keys = new[] { s_KeyName, s_KeyWidth, s_KeyHeight, s_KeyOffset, s_KeySize };

    private static List<PairComparator> s_Comparators = new List<PairComparator>();
    
    [MenuItem("Unity Support/Read Txt File (create YAML file)", false, 1)]
    public static void ReadFile()
    {
        s_IsSerializingTextures = false;
        
        List<string> textureData = new List<string>();
        textureData.Add("Textures:");
        
        //Get YAML lines
        using (StreamReader reader = new StreamReader(s_InFilePath))
        {
            string line;

            while ((line = reader.ReadLine()) != null)
            {
                
                if ((line.Contains("ID") && line.Contains("Texture2D")))
                {
                    Debug.Log(line);
                    //Begin texture
                    s_IsSerializingTextures = true;
                    textureData.Add("-");
                }
                else if ((line.Contains("ID") && !line.Contains("Texture2D")))
                {
                    s_IsSerializingTextures = false;
                }
                else if ((LineContainsKey(line, out var key)) && s_IsSerializingTextures)
                {
                    //TODO continuar aqui, mandar la key con el valor.
                    textureData.Add($"  {key}: {GetValue(line)}" );
                }
            }
        }
        
        File.WriteAllLines(s_OutYAMLFilePath, textureData.ToArray());
        
        
        //Get JSON Objects
        
    }

    private static bool LineContainsKey(string line, out string key, bool fromYAML = false )
    {
        key = null;
        foreach (var currentKey in k_keys)
        {
            if (line.Contains(currentKey))
            {
                if (fromYAML)
                {
                    key = currentKey;
                    return true;
                }
                else if ((line.Contains(s_KeySize) && line.Contains("unsigned")) || !line.Contains(s_KeySize))
                {
                    key = currentKey;
                    return true;    
                }
            }
        }
        
        return false;
    }
    
    private static string GetValue(string line)
    {
        return line.Split(' ')[1];
    }
    
    [MenuItem("Unity Support/Create Scriptable Objects", false, 2)]
    public static void CreateScriptableObjects()
    {
        s_IsSerializingTextures = false;

        s_Comparators = new List<PairComparator>();
        
        //Get YAML lines
        using (StreamReader reader = new StreamReader(s_OutYAMLFilePath))
        {
            string line;

            int index = 0;
            
            ImageCabReader cabReaderA = null;
            ImageCabReader cabReaderB = null;

            while ((line = reader.ReadLine()) != null)
            {
                
                if (line == "-")
                {
                    index++;
                    if (cabReaderA != null && cabReaderB != null)
                    {
                        CreateAsset(cabReaderA);
                        CreateAsset(cabReaderB);
                        
                        CreateImageComparator(cabReaderA, cabReaderB, index);
                    }
                    
                    cabReaderA = ScriptableObject.CreateInstance<ImageCabReader>();
                    cabReaderA.FilePath = s_ResSFilePathA;
                    
                    cabReaderB = ScriptableObject.CreateInstance<ImageCabReader>();
                    cabReaderB.FilePath = s_ResSFilePathB;
                    
                    
                }
                else if (LineContainsKey(line, out var key, true) && cabReaderA != null)
                {
                    SetValue(cabReaderA, key, GetValueFromYAML(line), "A", index);
                    SetValue(cabReaderB, key, GetValueFromYAML(line), "B", index);
                }
            }
            
            if (cabReaderA != null && cabReaderB != null)
            {
                CreateAsset(cabReaderA);
                CreateAsset(cabReaderB);
                        
                CreateImageComparator(cabReaderA, cabReaderB, ++index);
                
                CreateBundleComparator();
            }
        }
    }

    

    private static string GetValueFromYAML(string line)
    {
        return line.Split(':')[1].Trim();
    }

    private static void SetValue(ImageCabReader cabReader, string key, string value, string bundle, int index)
    {
        if (key == s_KeyName)
        {
            var realName =value.Substring(1, value.Length-2);
            cabReader.name = bundle + "_" + index + "_" + realName;
            cabReader.ImageName = realName;
        }
        if (key == s_KeyWidth)
        {
            cabReader.Width = int.Parse(value);
        }
        if (key == s_KeyHeight)
        {
            cabReader.Height = int.Parse(value);
        }
        if (key == s_KeyOffset)
        {
            cabReader.Offset = int.Parse(value);
        }
        if (key == s_KeySize)
        {
            cabReader.Size = int.Parse(value);
        }
    }

    private static void CreateAsset(ImageCabReader cabReader)
    {
        AssetDatabase.CreateAsset(cabReader, s_ImageReaderFolder + cabReader.name + ".asset");
        AssetDatabase.SaveAssets();
  
        EditorUtility.FocusProjectWindow();
        Selection.activeObject = cabReader;
    }
    
    private static void CreateImageComparator(ImageCabReader cabReaderA, ImageCabReader cabReaderB, int index)
    {
        var comparator = ScriptableObject.CreateInstance<PairComparator>();
        comparator.name = "Comparator_" + (index-1);
        comparator.ImageA = cabReaderA;
        comparator.ImageB = cabReaderB;

        AssetDatabase.CreateAsset(comparator, s_ImageReaderFolder + comparator.name + ".asset");
        AssetDatabase.SaveAssets();
  
        EditorUtility.FocusProjectWindow();
        Selection.activeObject = comparator;
        
        s_Comparators.Add(comparator); 
    }

    private static void CreateBundleComparator()
    {
        var comparator = ScriptableObject.CreateInstance<BundleComparator>();
        comparator.name = "BundleComparator";
        comparator.PairsToCheck = s_Comparators.ToArray();

        AssetDatabase.CreateAsset(comparator, s_ImageReaderFolder + comparator.name + ".asset");
        AssetDatabase.SaveAssets();
  
        EditorUtility.FocusProjectWindow();
        Selection.activeObject = comparator;
    }
}
