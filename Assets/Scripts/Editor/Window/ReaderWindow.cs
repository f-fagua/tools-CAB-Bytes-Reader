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
}