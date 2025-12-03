using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 
/// </summary>
public class ChangeMaterial : MonoBehaviour
{
    public bool bIsChanged;
    public Material OutlineMat;
    private Material DefaultMat;

    private Renderer thisRender;
    private Material[] originalMaterials;
    private Material[] outlineMaterials;

    void Start()
    {
      

    }
    public void InitMat()
    {
        thisRender = GetComponent<Renderer>();
        if (thisRender == null)
        {
            return;
        }

        DefaultMat = thisRender.material;
        originalMaterials = thisRender.materials;

        outlineMaterials = new Material[originalMaterials.Length + 1];
        for (int i = 0; i < originalMaterials.Length; i++)
        {
            outlineMaterials[i] = originalMaterials[i];
        }
        outlineMaterials[originalMaterials.Length] = OutlineMat;
        Debug.Log("material init");
    }
    public void Outline()
    {
        if (bIsChanged || thisRender == null || OutlineMat == null)
        {
            return;
        }
        Debug.Log("Show outline");
        bIsChanged = true;
        thisRender.materials = outlineMaterials;
    }

    public void Default()
    {
        if (!bIsChanged || thisRender == null) return;

        bIsChanged = false;
        thisRender.materials = originalMaterials;
    }

   
}