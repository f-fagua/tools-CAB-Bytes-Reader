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

        string[] filters = { "Text files", "txt" };
        string filePath = EditorUtility.OpenFilePanelWithFilters("Select a file", "", filters);
        
        //Get YAML lines
        using (StreamReader reader = new StreamReader(filePath))
        {
            while (reader.ReadLine() is { } line)
            {
                if ((line.Contains("ID") && line.Contains("Texture2D")))
                {
                    Debug.Log(line);
                    s_IsSerializingTextures = true;
                    textureData.Add("-");
                }
                else if ((line.Contains("ID") && !line.Contains("Texture2D")))
                {
                    s_IsSerializingTextures = false;
                }
                else if ((LineContainsKey(line, out var key)) && s_IsSerializingTextures)
                {
                    textureData.Add($"  {key}: {GetValue(line)}" );
                }
            }
        }
        
        var yamlFilePath = EditorUtility.SaveFilePanel(
            "Save your YAML file",         // Title of the dialog
            "Assets/Data/YAML",        // Default directory
            "TexturesYAML.txt",     // Default file name
            "txt"                     // Default file extension
        );

        if (string.IsNullOrEmpty(yamlFilePath)) 
            return;
        
        File.WriteAllLines(yamlFilePath, textureData.ToArray());
        Debug.Log("File saved at: " + yamlFilePath);
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
        
        string[] filters = { "Text files", "txt" };
        string filePath = EditorUtility.OpenFilePanelWithFilters("Select a file", "Assets/Data/YAML", filters);

        Debug.Log("Here filepath: " + filePath);
        filePath = filePath.Substring(Application.dataPath.Length - "Assets".Length);
        var directoryName = Path.GetDirectoryName(filePath);
        
        if (string.IsNullOrEmpty(directoryName)) return;
        
        var outDirectory = Path.Combine(directoryName, "Comparators");
        Directory.CreateDirectory(outDirectory);
        
        filters = new []{ "Ress files", ".resS" };
        string resSFilePathA = EditorUtility.OpenFilePanelWithFilters("Select a file", "Assets", filters);
        string resSFilePathB = EditorUtility.OpenFilePanelWithFilters("Select a file", "Assets", filters);
        
        //Get YAML lines
        using (StreamReader reader = new StreamReader(filePath))
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
                        CreateAsset(cabReaderA, outDirectory);
                        CreateAsset(cabReaderB, outDirectory);
                        
                        CreateImageComparator(cabReaderA, cabReaderB, index, outDirectory);
                    }
                    
                    cabReaderA = ScriptableObject.CreateInstance<ImageCabReader>();
                    cabReaderA.FilePath = resSFilePathA;
                    
                    cabReaderB = ScriptableObject.CreateInstance<ImageCabReader>();
                    cabReaderB.FilePath = resSFilePathB;
                    
                    
                }
                else if (LineContainsKey(line, out var key, true) && cabReaderA != null)
                {
                    SetValue(cabReaderA, key, GetValueFromYAML(line), "A", index);
                    SetValue(cabReaderB, key, GetValueFromYAML(line), "B", index);
                }
            }
            
            if (cabReaderA != null && cabReaderB != null)
            {
                CreateAsset(cabReaderA, outDirectory);
                CreateAsset(cabReaderB, outDirectory);
                        
                CreateImageComparator(cabReaderA, cabReaderB, ++index, outDirectory);
                
                CreateBundleComparator(outDirectory);
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

    private static void CreateAsset(ImageCabReader cabReader, string outDirectory)
    {
        AssetDatabase.CreateAsset(cabReader, outDirectory + "/" + cabReader.name + ".asset");
        AssetDatabase.SaveAssets();
  
        EditorUtility.FocusProjectWindow();
        Selection.activeObject = cabReader;
    }
    
    private static void CreateImageComparator(ImageCabReader cabReaderA, ImageCabReader cabReaderB, int index, string outDirectory)
    {
        var comparator = ScriptableObject.CreateInstance<PairComparator>();
        comparator.name = "Comparator_" + (index-1);
        comparator.ImageA = cabReaderA;
        comparator.ImageB = cabReaderB;

        AssetDatabase.CreateAsset(comparator, outDirectory + "/" + comparator.name + ".asset");
        AssetDatabase.SaveAssets();
  
        EditorUtility.FocusProjectWindow();
        Selection.activeObject = comparator;
        
        s_Comparators.Add(comparator); 
    }

    private static void CreateBundleComparator(string outDirectory)
    {
        var comparator = ScriptableObject.CreateInstance<BundleComparator>();
        comparator.name = "BundleComparator";
        comparator.PairsToCheck = s_Comparators.ToArray();

        AssetDatabase.CreateAsset(comparator, outDirectory + "/" + comparator.name + ".asset");
        AssetDatabase.SaveAssets();
  
        EditorUtility.FocusProjectWindow();
        Selection.activeObject = comparator;
    }
}
