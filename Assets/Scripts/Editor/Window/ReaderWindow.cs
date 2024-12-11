using UnityEditor;
using UnityEngine;
using System.Collections.Generic;
using System.IO;
using UnityEditor.AddressableAssets.Build.Layout;

public class ReaderWindow : EditorWindow
{
    private static string s_KeyName   = "m_Name";
    private static string s_KeyWidth  = "m_Width";
    private static string s_KeyHeight = "m_Height";
    private static string s_KeyOffset = "offset";
    private static string s_KeySize   = "size";

    private static string[] k_keys = new[] { s_KeyName, s_KeyWidth, s_KeyHeight, s_KeyOffset, s_KeySize };
    
    private List<TextureItem> items = new List<TextureItem>();
    private Vector2 scrollPosition;

    private float nameColumnWidth = 100f;
    private float valueColumnWidth = 50f;
    private const float columnMinWidth = 50f;

    [MenuItem("Unity Support/Window/Item List")]
    public static void ShowWindow()
    {
        GetWindow<ReaderWindow>("Textures List");
    }

    private void OnEnable()
    {
        items.Add(new TextureItem("Sword", 10));
        items.Add(new TextureItem("Shield", 15));
        items.Add(new TextureItem("Potion", 5));
    }

    private void OnGUI()
    {
        GUILayout.Label("Items", EditorStyles.boldLabel);

        PaintBundlePanel();
        PaintResSSelectors();
        PaintComparatorPanel();
        
        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("Add Item"))
        {
            items.Add(new TextureItem("New Item", 0));
        }
        if (GUILayout.Button("Remove Last Item"))
        {
            if (items.Count > 0)
            {
                items.RemoveAt(items.Count - 1);
            }
        }
        if (GUILayout.Button("Clear All Items"))
        {
            items.Clear();
        }
        EditorGUILayout.EndHorizontal();

        // Add headers with resizable columns
        EditorGUILayout.BeginHorizontal();
        GUILayout.Label("Texture Name", EditorStyles.boldLabel, GUILayout.Width(nameColumnWidth));

        // Resizing handle for the Name column
        ResizeColumn(ref nameColumnWidth);

        GUILayout.Label("Value", EditorStyles.boldLabel, GUILayout.Width(valueColumnWidth));

        // Resizing handle for the Value column
        ResizeColumn(ref valueColumnWidth);

        GUILayout.FlexibleSpace();
        EditorGUILayout.EndHorizontal();

        // Begin scroll view for the list of items
        scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);

        foreach (var item in items)
        {
            EditorGUILayout.BeginHorizontal();
            item.name = EditorGUILayout.TextField(item.name, GUILayout.Width(nameColumnWidth));
            item.value = EditorGUILayout.IntField(item.value, GUILayout.Width(valueColumnWidth));
            EditorGUILayout.EndHorizontal();
        }

        EditorGUILayout.EndScrollView();
    }

    private string m_BundleFilePath;
    private string m_YamlFilePath;
    
    private void PaintBundlePanel()
    {
        // Add buttons above the item list
        EditorGUILayout.BeginHorizontal();
        GUILayout.Label("Bundle to Inspect:", GUILayout.MaxWidth(105));
        GUILayout.TextField(m_BundleFilePath, GUILayout.MinWidth(360));
        if (GUILayout.Button("Select the Bundle", GUILayout.MinWidth(150)))
        {
            SelectBundleFile();
        }
        EditorGUILayout.EndHorizontal();
        
        EditorGUILayout.BeginHorizontal();
        GUILayout.Label("Textures YAML File at: ", GUILayout.Width(130));
        GUILayout.TextField(m_YamlFilePath);
        EditorGUILayout.EndHorizontal();
    }

    private void SelectBundleFile()
    {
        var isSerializingTextures = false;
        
        List<string> textureData = new List<string>();
        textureData.Add("Textures:");

        string[] filters = { "Text files", "txt" };
        m_BundleFilePath = EditorUtility.OpenFilePanelWithFilters("Select a file", "", filters);
        
        //Get YAML lines
        using (StreamReader reader = new StreamReader(m_BundleFilePath))
        {
            while (reader.ReadLine() is { } line)
            {
                if ((line.Contains("ID") && line.Contains("Texture2D")))
                {
                    Debug.Log(line);
                    isSerializingTextures = true;
                    textureData.Add("-");
                }
                else if ((line.Contains("ID") && !line.Contains("Texture2D")))
                {
                    isSerializingTextures = false;
                }
                else if ((LineContainsKey(line, out var key)) && isSerializingTextures)
                {
                    textureData.Add($"  {key}: {GetValue(line)}" );
                }
            }
        }
        
        m_YamlFilePath = EditorUtility.SaveFilePanel(
            "Save your YAML file",         // Title of the dialog
            "Assets/Data/YAML",        // Default directory
            "TexturesYAML.txt",     // Default file name
            "txt"                     // Default file extension
        );

        if (string.IsNullOrEmpty(m_YamlFilePath)) 
            return;
        
        File.WriteAllLines(m_YamlFilePath, textureData.ToArray());
        Debug.Log("File saved at: " + m_YamlFilePath);
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

    private string m_ResSFileAPath;
    private string m_ResSFileBPath;
    
    private void PaintResSSelectors()
    {
        EditorGUILayout.BeginHorizontal();
        GUILayout.Label("ResS File A:", GUILayout.MaxWidth(75));
        GUILayout.TextField(m_ResSFileAPath, GUILayout.MinWidth(420));
        
        string[] filters = new []{ "Ress files", ".resS" };
        
        if (GUILayout.Button("Select the Ress File A", GUILayout.MinWidth(80)))
        {
            m_ResSFileAPath = EditorUtility.OpenFilePanelWithFilters("Select a file", "Assets", filters);
        }
        EditorGUILayout.EndHorizontal();
        
        EditorGUILayout.BeginHorizontal();
        GUILayout.Label("ResS File B:", GUILayout.MaxWidth(75));
        GUILayout.TextField(m_ResSFileBPath, GUILayout.MinWidth(420));
        if (GUILayout.Button("Select the Ress File B", GUILayout.MinWidth(80)))
        {
            m_ResSFileBPath = EditorUtility.OpenFilePanelWithFilters("Select a file", "Assets", filters);
        }
        EditorGUILayout.EndHorizontal();
    }

    private string m_ComparatorAssetPath;
    
    private void PaintComparatorPanel()
    {
        EditorGUILayout.BeginHorizontal();
        GUILayout.Label("Comparator Asset:", GUILayout.MaxWidth(115));
        GUILayout.TextField(m_ComparatorAssetPath, GUILayout.MinWidth(420));
        if (GUILayout.Button("Create the Comparator Asset", GUILayout.MinWidth(160)))
        {
            CreateScriptableObjects();
        }
        EditorGUILayout.EndHorizontal();
        
        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("Compare Bundles", GUILayout.MinWidth(160)))
        {
            CompareBundles();
        }
        EditorGUILayout.EndHorizontal();
    }

    public void CompareBundles()
    {
        var comparator = AssetDatabase.LoadAssetAtPath<BundleComparator>(m_ComparatorAssetPath);
        
        if (comparator != null) 
            comparator.Compare();
        else
            Debug.Log("No comparator selected");
    }
    
    private List<PairComparator> m_Comparators = new List<PairComparator>();
    
    private void CreateScriptableObjects()
    {
        var isSerializingTextures = false;

        string filePath = m_YamlFilePath;
        Debug.Log("Here filepath: " + filePath);
        filePath = filePath.Substring(Application.dataPath.Length - "Assets".Length);
        var directoryName = Path.GetDirectoryName(filePath);
        
        if (string.IsNullOrEmpty(directoryName)) return;
        
        var outDirectory = Path.Combine(directoryName, "Comparators");
        Directory.CreateDirectory(outDirectory);

        string resSFilePathA = m_ResSFileAPath;
        string resSFilePathB = m_ResSFileBPath;
        
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
    
    private void CreateAsset(ImageCabReader cabReader, string outDirectory)
    {
        AssetDatabase.CreateAsset(cabReader, outDirectory + "/" + cabReader.name + ".asset");
        AssetDatabase.SaveAssets();
  
        EditorUtility.FocusProjectWindow();
        Selection.activeObject = cabReader;
    }
    
    private void CreateImageComparator(ImageCabReader cabReaderA, ImageCabReader cabReaderB, int index, string outDirectory)
    {
        var comparator = ScriptableObject.CreateInstance<PairComparator>();
        comparator.name = "Comparator_" + (index-1);
        comparator.ImageA = cabReaderA;
        comparator.ImageB = cabReaderB;

        AssetDatabase.CreateAsset(comparator, outDirectory + "/" + comparator.name + ".asset");
        AssetDatabase.SaveAssets();
  
        EditorUtility.FocusProjectWindow();
        Selection.activeObject = comparator;
        
        m_Comparators.Add(comparator); 
    }
    
    private void ResizeColumn(ref float columnWidth)
    {
        Rect resizeHandleRect = GUILayoutUtility.GetLastRect();
        resizeHandleRect.x += resizeHandleRect.width;
        resizeHandleRect.width = 5f;

        EditorGUIUtility.AddCursorRect(resizeHandleRect, MouseCursor.ResizeHorizontal);

        if (Event.current != null)
        {
            if (Event.current.type == EventType.MouseDown && resizeHandleRect.Contains(Event.current.mousePosition))
            {
                GUIUtility.hotControl = GUIUtility.GetControlID(FocusType.Passive);
                Event.current.Use();
            }

            if (GUIUtility.hotControl == GUIUtility.GetControlID(FocusType.Passive))
            {
                if (Event.current.type == EventType.MouseDrag)
                {
                    columnWidth += Event.current.delta.x;
                    columnWidth = Mathf.Max(columnMinWidth, columnWidth);
                    Event.current.Use();
                }
                else if (Event.current.type == EventType.MouseUp)
                {
                    GUIUtility.hotControl = 0;
                }
            }
        }
    }
    
    private void CreateBundleComparator(string outDirectory)
    {
        var comparator = ScriptableObject.CreateInstance<BundleComparator>();
        comparator.name = "BundleComparator";
        comparator.PairsToCheck = m_Comparators.ToArray();

        m_ComparatorAssetPath = outDirectory + "/" + comparator.name + ".asset";
        AssetDatabase.CreateAsset(comparator, m_ComparatorAssetPath);
        AssetDatabase.SaveAssets();
  
        EditorUtility.FocusProjectWindow();
        Selection.activeObject = comparator;
    }
}