using UnityEditor;
using UnityEngine;
using System.Collections.Generic;

public class ReaderWindow : EditorWindow
{
    private class Item
    {
        public string name;
        public int value;

        public Item(string name, int value)
        {
            this.name = name;
            this.value = value;
        }
    }

    private List<Item> items = new List<Item>();
    private Vector2 scrollPosition;

    private float nameColumnWidth = 100f;
    private float valueColumnWidth = 50f;
    private const float columnMinWidth = 50f;

    [MenuItem("Unity Support/Window/Item List")]
    public static void ShowWindow()
    {
        GetWindow<ReaderWindow>("Item List");
    }

    private void OnEnable()
    {
        items.Add(new Item("Sword", 10));
        items.Add(new Item("Shield", 15));
        items.Add(new Item("Potion", 5));
    }

    private void OnGUI()
    {
        GUILayout.Label("Items", EditorStyles.boldLabel);

        // Add buttons above the item list
        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("Add Item"))
        {
            items.Add(new Item("New Item", 0));
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
        GUILayout.Label("Name", EditorStyles.boldLabel, GUILayout.Width(nameColumnWidth));

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