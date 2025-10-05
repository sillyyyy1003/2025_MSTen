using UnityEngine;

/// <summary>
/// ビジュアル要素を持つゲームオブジェクトの基底クラス
/// 2D/3D両対応
/// </summary>
public abstract class VisualGameObject : MonoBehaviour
{
    // ===== コンポーネントキャッシュ =====
    
    //サムネイル画像用
    protected SpriteRenderer thumbnailRenderer;
    // 2D用
    protected SpriteRenderer spriteRenderer;
    // 3D用
    protected MeshRenderer meshRenderer;
    protected MeshFilter meshFilter;
    // 共通
    protected Animator animator;

    protected virtual void SetupVisualComponents()
    {
        //サムネイル画像のSpriteレンダラーを取得
        Transform thumbnailTransform = transform.Find("Thumbnail");
        if (thumbnailTransform != null)
        {
            thumbnailRenderer = thumbnailTransform.GetComponent<SpriteRenderer>();
        }

        // 2D コンポーネントを取得
        spriteRenderer = GetComponent<SpriteRenderer>();
        
        // 3D コンポーネントを取得
        meshRenderer = GetComponent<MeshRenderer>();
        meshFilter = GetComponent<MeshFilter>();
        
        // Animator（オプション、2D/3D共通）
        animator = GetComponent<Animator>();
        
        // どちらも存在しない場合はエラー
        if (spriteRenderer == null && (meshRenderer == null || meshFilter == null))
        {
            Debug.LogError($"{gameObject.name}: SpriteRenderer または MeshRenderer/MeshFilter が必要です！");
        }
    }

    /// <summary>
    /// スプライトを適用（2D用）
    /// </summary>
    protected void ApplySprite(Sprite sprite, Color color)
    {
        if (spriteRenderer != null && sprite != null)
        {
            spriteRenderer.sprite = sprite;
            spriteRenderer.color = color;
        }
    }

    /// <summary>
    /// メッシュとマテリアルを適用（3D用）
    /// </summary>
    protected void ApplyMesh(Mesh mesh, Material material)
    {
        if (meshFilter != null && mesh != null)
        {
            meshFilter.mesh = mesh;
        }
        
        if (meshRenderer != null && material != null)
        {
            meshRenderer.material = material;
        }
    }

    /// <summary>
    /// 色を変更（2D/3D両対応）
    /// </summary>
    protected void SetColor2D(Color color)
    {
        if (spriteRenderer != null)
        {
            spriteRenderer.color = color;
        }
    }

    protected void SetColor3D(Color color)
    {
        if (meshRenderer != null)
        {
            // 3Dの場合はマテリアルのプロパティを変更
            meshRenderer.material.color = color;
        }
    }

    /// <summary>
    /// アニメーションをトリガー
    /// </summary>
    protected void TriggerAnimation(string triggerName)
    {
        if (animator != null)
        {
            animator.SetTrigger(triggerName);
        }
    }
}