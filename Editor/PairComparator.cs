using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "CAB Image Pair", menuName = "Unity Support/CAB Image Pair (to compare)", order = 2)]
public class PairComparator : ScriptableObject
{
    [SerializeField]
    private ImageCabReader m_ImageA;
    
    [SerializeField]
    private ImageCabReader m_ImageB;

    public ImageCabReader ImageA
    {
        get => m_ImageA;
        set => m_ImageA = value;
    }

    public ImageCabReader ImageB
    {
        get => m_ImageB;
        set => m_ImageB = value;
    }

    public void Compare(out List<int> differences)
    {
        var msj =
            $"Images \n- {m_ImageA.CabPath}/{m_ImageA.ImageName} and \n- {m_ImageB.CabPath}/{m_ImageB.ImageName} \nare ";

        if (ImageCabReader.AreEqual(m_ImageA, m_ImageB, out differences))
        {
            msj += "equal!";
            Debug.Log(msj);
        }
        else
        {   
            msj += $"different Found {differences.Count} different bytes.";
            Debug.LogWarning(msj);
        }
    }
}
