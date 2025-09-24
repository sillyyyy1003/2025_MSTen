using System.Drawing;
using UnityEngine;

// 创建六边形网格
[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer), typeof(MeshCollider))]
public class CreateCellMesh : MonoBehaviour
{
    [SerializeField] private float radius = 1f;

    void Start()
    {
        float height = 0.1f;
        // 创建 Mesh
        Mesh mesh = new Mesh();

        // 顶点数组：7个上顶点 + 7个下顶点 = 14个顶点
        Vector3[] vertices = new Vector3[14];

        // 上表面顶点 (0-6)
        vertices[0] = new Vector3(0, height / 2, 0); // 中心上顶点

        for (int i = 0; i < 6; i++)
        {
            float angle = 60f * i * Mathf.Deg2Rad - 30f * Mathf.Deg2Rad; // 从顶部开始
            vertices[i + 1] = new Vector3(
                Mathf.Cos(angle) * radius,
                height / 2,
                Mathf.Sin(angle) * radius
            );
        }

        // 下表面顶点 (7-13)
        vertices[7] = new Vector3(0, -height / 2, 0); // 中心下顶点

        for (int i = 0; i < 6; i++)
        {
            float angle = 60f * i * Mathf.Deg2Rad - 30f * Mathf.Deg2Rad;
            vertices[i + 8] = new Vector3(
                Mathf.Cos(angle) * radius,
                -height / 2,
                Mathf.Sin(angle) * radius
            );
        }

        // 三角形索引：上表面(6) + 下表面(6) + 侧面(12) = 24个三角形 × 3 = 72个索引
        int[] triangles = new int[72];

        int triangleIndex = 0;

        // 上表面三角形 (6个三角形)
        for (int i = 0; i < 6; i++)
        {
            triangles[triangleIndex++] = 0; // 中心上顶点
            triangles[triangleIndex++] = i + 1;
            triangles[triangleIndex++] = (i + 1) % 6 + 1;
        }

        // 下表面三角形 (6个三角形，注意顶点顺序要反转)
        for (int i = 0; i < 6; i++)
        {
            triangles[triangleIndex++] = 7; // 中心下顶点
            triangles[triangleIndex++] = (i + 1) % 6 + 8;
            triangles[triangleIndex++] = i + 8;
        }

        // 侧面四边形，每个四边形由2个三角形组成 (6个四边形 × 2个三角形 = 12个三角形)
        for (int i = 0; i < 6; i++)
        {
            int next = (i + 1) % 6;

            // 第一个三角形
            triangles[triangleIndex++] = i + 1;     // 上表面当前顶点
            triangles[triangleIndex++] = i + 8;     // 下表面当前顶点
            triangles[triangleIndex++] = next + 1;  // 上表面下一个顶点

            // 第二个三角形
            triangles[triangleIndex++] = next + 1;  // 上表面下一个顶点
            triangles[triangleIndex++] = i + 8;     // 下表面当前顶点
            triangles[triangleIndex++] = next + 8;  // 下表面下一个顶点
        }

        mesh.vertices = vertices;
        mesh.triangles = triangles;
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();

        // 设置组件
        MeshFilter meshFilter = GetComponent<MeshFilter>();
        if (meshFilter == null) meshFilter = gameObject.AddComponent<MeshFilter>();
        meshFilter.mesh = mesh;

        // 添加渲染器材质
        MeshRenderer renderer = GetComponent<MeshRenderer>();
        if (renderer == null) renderer = gameObject.AddComponent<MeshRenderer>();
            renderer.enabled = false;
        // 设置碰撞器
        MeshCollider collider = GetComponent<MeshCollider>();
        if (collider == null) collider = gameObject.AddComponent<MeshCollider>();

        collider.sharedMesh = mesh;
        collider.convex = true;

       
    }
}