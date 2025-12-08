using DG.Tweening;
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
    public void UnitDead(System.Action onFinished)
    {
        Debug.Log("执行单位新死亡特效 "+DefaultMat.name);
        if (thisRender == null)
            thisRender = GetComponent<Renderer>();

        if (thisRender == null)
        {
            Debug.LogError("ChangeMaterial: 找不到 Renderer");
            onFinished?.Invoke();
            return;
        }

        // 取当前材质
        // 确保初始值设置
        DefaultMat.SetFloat("_Float", -1f);
        // 创建 DOTween 动画：_Float 从 0 → 1
        DOTween.To(
            () => DefaultMat.GetFloat("_Float"),
            x => DefaultMat.SetFloat("_Float", x),
            1f,
            1f // 播放时间 1 秒
        )
        .SetEase(Ease.Linear)
        .OnComplete(() =>
        {
            Debug.Log("死亡特效播放完成");
            onFinished?.Invoke();
        });
    }

}