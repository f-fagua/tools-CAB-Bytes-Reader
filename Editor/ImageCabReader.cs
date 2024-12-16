using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

[CreateAssetMenu(fileName = "ImageCabReader", menuName = "Unity Support/Image CAB Reader", order = 1)]
public class ImageCabReader : ScriptableObject
{
    [SerializeField]
    private string m_FilePath;

    [SerializeField]
    private string m_ImageName;
    
    [SerializeField]
    private int m_Offset;

    [SerializeField]
    private int m_Size;

    [SerializeField]
    public int m_Width;
    
    [SerializeField]
    public int m_Height;
    
    private byte[] m_BytesReaded;

    public byte[] BytesReaded => m_BytesReaded;

    public string CabPath => m_FilePath;
    
    public string ImageName
    {
        get => m_ImageName;
        set => m_ImageName = value;
    }

    public int Width
    {
        get => m_Width;
        set => m_Width = value;
    }

    public int Height
    {
        get => m_Height;
        set => m_Height = value;
    }

    public int Offset
    {
        get => m_Offset;
        set => m_Offset = value;
    }

    public int Size
    {
        get => m_Size;
        set => m_Size = value;
    }

    public string FilePath
    {
        get => m_FilePath;
        set => m_FilePath = value;
    }

    public void ReadBytes()
    {
        using (FileStream fs = new FileStream(m_FilePath, FileMode.Open, FileAccess.Read))
        {
            fs.Seek(m_Offset, SeekOrigin.Begin);
            m_BytesReaded = new byte[m_Size];
            fs.Read(m_BytesReaded, 0, m_BytesReaded.Length);
        }
        //Debug.Log($"Read Bytes from {CabPath}/{ImageName} Complete.");
    }

    public static bool AreEqual(ImageCabReader imageA, ImageCabReader imageB, out List<int> differences)
    {
        //Debug.Log($"Reading bytes from {imageA.CabPath}/{imageA.ImageName}");
        imageA.ReadBytes();
        //Debug.Log($"Reading bytes from {imageB.CabPath}/{imageB.ImageName}");
        imageB.ReadBytes();
        
        differences = new List<int>();
        var m = MathF.Max(imageA.BytesReaded.Length, imageB.BytesReaded.Length);
        
        for (int i = 0; i < m; i++)
        {
            if (imageA.BytesReaded[i] != imageB.BytesReaded[i])
            {
                differences.Add(i);
            }
        }

        return differences.Count == 0;
    }

    [MenuItem("Unity Support/Create Textures", false, 4)]
    public static void CreateTextures()
    {
        var cabReaders = Selection.objects;

        foreach (var cabReaderObject in cabReaders)
        {
            var cabReader = cabReaderObject as ImageCabReader;

            CreateTexture(cabReader);
        }
    }

    private static void CreateTexture(ImageCabReader cabReader)
    {
        Texture2D texture = new Texture2D(cabReader.Width, cabReader.Height);
        
        texture.LoadImage(cabReader.BytesReaded);

        texture.Apply();
        
        byte[] bytes = texture.EncodeToPNG();
        
        //AssetDatabase.CreateAsset(texture, "Assets/Data/" + cabReader.name + ".png");
        
        System.IO.File.WriteAllBytes("Assets/Data/" + cabReader.name + ".png", bytes);
        
        AssetDatabase.Refresh(ImportAssetOptions.ForceUpdate);
    }
}
