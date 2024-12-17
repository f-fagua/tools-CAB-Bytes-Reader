using UnityEditor;
using UnityEngine;
using System.Collections.Generic;
using System.IO;

public class ReaderWindow : EditorWindow
{
    private static string s_KeyName   = "m_Name";
    private static string s_KeyWidth  = "m_Width";
    private static string s_KeyHeight = "m_Height";
    private static string s_KeyOffset = "offset";
    private static string s_KeySize   = "size";

    private static string[] k_keys = new[] { s_KeyName, s_KeyWidth, s_KeyHeight, s_KeyOffset, s_KeySize };
    
    private List<TextureItem> m_TextureItems = new List<TextureItem>();
    private Vector2 scrollPosition;

    private float m_AreEqualColumnWidth = 60f;
    private float m_NameColumnWidth = 450f;
    private float m_WitdthColumnWidth = 60f;
    private float m_HeightColumnWidth = 60f;
    private float m_OffsetColumnWidth = 90f;
    private float m_SizeColumnWidth = 75f;
    private float m_DifferentBytesColumnWidth = 75f;
    
    private const float columnMinWidth = 50f;
    
    private ProcessProgress m_ProcessProgress;
    
    [MenuItem("Window/Unity Support/Open Comparator Window")]
    public static void ShowWindow()
    {
        GetWindow<ReaderWindow>("Textures List");
    }

    private void OnEnable()
    {
        m_ProcessProgress = ProcessProgress.Initializing;
    }

    private void OnGUI()
    {
        GUILayout.Label("Items", EditorStyles.boldLabel);

        if (m_ProcessProgress.HasFlag(ProcessProgress.Initializing))
        {
            PaintBundlePanel();
        }

        if (m_ProcessProgress.HasFlag(ProcessProgress.HasSelectedBundle))
        {
            PaintResSSelectors();
        }

        if (m_ProcessProgress.HasFlag(ProcessProgress.HasSelectedResSA) &&
            m_ProcessProgress.HasFlag(ProcessProgress.HasSelectedResSB))
        {
            PaintComparatorPanel();    
        }
        
        /*
        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("Add Item"))
        {
            m_TextureItems.Add(new TextureItem("New Item", 0, 0, 0, 0, 0));
        }
        if (GUILayout.Button("Remove Last Item"))
        {
            if (m_TextureItems.Count > 0)
            {
                m_TextureItems.RemoveAt(m_TextureItems.Count - 1);
            }
        }
        if (GUILayout.Button("Clear All Items"))
        {
            m_TextureItems.Clear();
        }
        EditorGUILayout.EndHorizontal();
        */
        
        PaintTableHeader();

        // Begin scroll view for the list of items
        scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);

        foreach (var item in m_TextureItems)
        {
            //var guiLayout = GetGui;
            EditorGUILayout.BeginHorizontal();
            var areEqual = (item.differentBytes == 0);
            EditorGUILayout.TextField("" + areEqual, GetGuiStyle(areEqual), GUILayout.Width(m_AreEqualColumnWidth));
            EditorGUILayout.TextField(item.name, GetGuiStyle(areEqual), GUILayout.Width(m_NameColumnWidth));
            EditorGUILayout.TextField("" + item.width, GetGuiStyle(areEqual), GUILayout.Width(m_WitdthColumnWidth));
            EditorGUILayout.TextField("" + item.height, GetGuiStyle(areEqual), GUILayout.Width(m_HeightColumnWidth));
            EditorGUILayout.TextField("" + item.offset, GetGuiStyle(areEqual), GUILayout.Width(m_OffsetColumnWidth));
            EditorGUILayout.TextField("" + item.size, GetGuiStyle(areEqual), GUILayout.Width(m_SizeColumnWidth));
            EditorGUILayout.TextField("" + item.differentBytes, GetGuiStyle(areEqual), GUILayout.Width(m_DifferentBytesColumnWidth));
            //EditorGUILayout.TextField("" + item.differentBytes, GUILayout.Width(m_DifferentBytesColumnWidth));
            EditorGUILayout.EndHorizontal();
        }

        EditorGUILayout.EndScrollView();
    }

    private GUIStyle GetGuiStyle(bool areEqual)
    {
        var textFieldStyle = new GUIStyle(GUI.skin.textField);
        textFieldStyle.normal.textColor = Color.red;
        //textFieldStyle. = size;
        
        if (areEqual)
            textFieldStyle.normal.textColor = Color.white;
        
        return textFieldStyle;
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
        
        if (!string.IsNullOrEmpty(m_BundleFilePath))
            m_ProcessProgress |= ProcessProgress.HasSelectedBundle;
        else
            PaintCurrentInstruction("Select the txt Bundle to Inspect (made with the bin2text tool)");
    }

    private void PaintCurrentInstruction(string message)
    {
        EditorGUILayout.BeginHorizontal();
        var centeredLabelStyle = new GUIStyle(GUI.skin.label);
        centeredLabelStyle.alignment = TextAnchor.MiddleCenter; // Center align the text
        centeredLabelStyle.normal.textColor = Color.yellow;     // Set the text color to yellow
        GUILayout.Label(message, centeredLabelStyle);
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
        
        if (string.IsNullOrEmpty(m_ResSFileAPath))
        {
            PaintCurrentInstruction("Select the .resS file uncompressed with the WebExtract tool for the first bundle");
            return;    
        }
        
        EditorGUILayout.BeginHorizontal();
        GUILayout.Label("ResS File B:", GUILayout.MaxWidth(75));
        GUILayout.TextField(m_ResSFileBPath, GUILayout.MinWidth(420));
        if (GUILayout.Button("Select the Ress File B", GUILayout.MinWidth(80)))
        {
            m_ResSFileBPath = EditorUtility.OpenFilePanelWithFilters("Select a file", "Assets", filters);
        }
        EditorGUILayout.EndHorizontal();

        if (string.IsNullOrEmpty(m_ResSFileBPath))
        {
            PaintCurrentInstruction("Select the .resS file uncompressed with the WebExtract tool for the second bundle");
            return;    
        }

        if (string.IsNullOrEmpty(m_ResSFileAPath) || string.IsNullOrEmpty(m_ResSFileBPath)) return;
        m_ProcessProgress |= ProcessProgress.HasSelectedResSA;
        m_ProcessProgress |= ProcessProgress.HasSelectedResSB;
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

        if (!string.IsNullOrEmpty(m_ComparatorAssetPath))
        {
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Compare Bundles", GUILayout.MinWidth(160)))
            {
                CompareBundles();
            }
            EditorGUILayout.EndHorizontal();    
        }
        
        if (string.IsNullOrEmpty(m_ComparatorAssetPath))
            PaintCurrentInstruction("Create the asset comparators");
    }

    public void CompareBundles()
    {
        var comparator = AssetDatabase.LoadAssetAtPath<BundleComparator>(m_ComparatorAssetPath);
        
        if (comparator != null) 
            m_TextureItems = comparator.Compare();
        else
            Debug.Log("No comparator selected");
    }
    
    private List<PairComparator> m_Comparators = new List<PairComparator>();
    
    private void CreateScriptableObjects()
    {
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

    private void PaintTableHeader()
    {
        // Add headers with resizable columns
        EditorGUILayout.BeginHorizontal();
        
        GUILayout.Label("Equal?", EditorStyles.boldLabel, GUILayout.Width(m_AreEqualColumnWidth));
        ResizeColumn(ref m_AreEqualColumnWidth);
        
        GUILayout.Label("Texture Name", EditorStyles.boldLabel, GUILayout.Width(m_NameColumnWidth));
        ResizeColumn(ref m_NameColumnWidth);
        
        GUILayout.Label("Witdth", EditorStyles.boldLabel, GUILayout.Width(m_WitdthColumnWidth));
        ResizeColumn(ref m_WitdthColumnWidth);

        GUILayout.Label("Height", EditorStyles.boldLabel, GUILayout.Width(m_HeightColumnWidth));
        ResizeColumn(ref m_HeightColumnWidth);
        
        GUILayout.Label("Offset", EditorStyles.boldLabel, GUILayout.Width(m_OffsetColumnWidth));
        ResizeColumn(ref m_OffsetColumnWidth);
        
        GUILayout.Label("Size", EditorStyles.boldLabel, GUILayout.Width(m_SizeColumnWidth));
        ResizeColumn(ref m_SizeColumnWidth);

        GUILayout.Label("Different Bytes", EditorStyles.boldLabel, GUILayout.Width(m_DifferentBytesColumnWidth));
        ResizeColumn(ref m_DifferentBytesColumnWidth);
        
        GUILayout.FlexibleSpace();
        EditorGUILayout.EndHorizontal();
    }
}