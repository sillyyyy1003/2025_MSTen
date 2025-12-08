#include "../HexCellData.hlsl"

//------------------------------------------------------------
float4 _HoverColor; // hover 高亮颜色
float4 _ClickColor; // click 高亮颜色
float4 _RightClickColor;	// 右键高光颜色
float4 _FinalColorMultiply; // rgb = 最终乘算颜色，a=启用程度(0~1)

//------------------------------------------------------------
// 从网格顶点的三个相邻六边形单元中提取数据，并计算顶点的地形与可见性信息
//------------------------------------------------------------
void GetVertexCellData_float(
	float3 Indices, // 顶点关联的三个六边形单元索引
	float3 Weights, // 顶点相对于每个单元的权重（插值用）
	bool EditMode, // 是否处于编辑模式（决定数据读取方式）
	out float4 Terrain, // 输出地形信息：x,y,z=每个单元的地形类型索引，w=最大海拔（乘30缩放）
	out float4 Visibility // 输出可见性信息：x,y,z=三个单元的可见度，w=加权平均后的探索度
)
{
	// 获取三个单元的数据（每个float4含有多个通道信息）
	float4 cell0 = GetCellData(Indices, 0, EditMode);
	float4 cell1 = GetCellData(Indices, 1, EditMode);
	float4 cell2 = GetCellData(Indices, 2, EditMode);

	// ---------------- 地形信息 ----------------
	Terrain.x = cell0.w; // 单元0的地形类型
	Terrain.y = cell1.w; // 单元1的地形类型
	Terrain.z = cell2.w; // 单元2的地形类型
	Terrain.w = max(max(cell0.b, cell1.b), cell2.b) * 30.0; // 取三个单元最高的地形高度值，乘30缩放

	// ---------------- 可见性信息 ----------------
	Visibility.x = cell0.x; // 单元0的可见性（通常为0~1）
	Visibility.y = cell1.x;
	Visibility.z = cell2.x;

	// 调整可见度范围：最低0.25，最高1
	Visibility.xyz = lerp(0.25, 1, Visibility.xyz);

	// 计算探索度（通常用于迷雾或探索系统）
	// 按权重混合三个单元的探索值（cell.y通道）
	Visibility.w = cell0.y * Weights.x + cell1.y * Weights.y + cell2.y * Weights.z;
}


//------------------------------------------------------------
// 采样对应地形纹理，并根据单元权重与可见度计算加权颜色
//------------------------------------------------------------
float4 GetTerrainColor(
	UnityTexture2DArray TerrainTextures, // 地形纹理数组
	float3 WorldPosition, // 当前片元的世界坐标
	float4 Terrain, // 地形类型信息
	float3 Weights, // 对三个单元的权重
	float4 Visibility, // 对应单元的可见性信息
	int index // 当前计算的单元索引（0~2）
)
{
	// 构建三维纹理采样坐标（xz为地表坐标，y层为地形类型索引）
	//float3 uvw = float3(WorldPosition.xz * (2 * TILING_SCALE), Terrain[index]);
	float TilingFactor = 2.0; // 缩小采样密度一半
	float3 uvw = float3(WorldPosition.xz * (2 * TILING_SCALE * TilingFactor), Terrain[index]);

	// 从2DArray中采样指定层的纹理
	float4 c = TerrainTextures.Sample(TerrainTextures.samplerstate, uvw);

	// 颜色乘以权重与可见度，得到加权颜色
	return c * (Weights[index] * Visibility[index]);
}


//------------------------------------------------------------
// 应用地格网线（Hex格子轮廓），距离中心在 0.965~1 之间时加深颜色 80%
//------------------------------------------------------------
float3 ApplyGrid(float3 baseColor, HexGridData h)
{
	return baseColor * (0.2 + 0.8 * h.Smoothstep10(0.965));
}

//------------------------------------------------------------
// 应用高亮边缘：距离中心 0.68~0.8 之间添加白色亮边
//------------------------------------------------------------
float3 ApplyHighlight(float3 baseColor, HexGridData h)
{
	//return saturate(h.SmoothstepRange(0.68, 0.8) + baseColor.rgb);
	
	// 高亮强度（0~1），可自行调节
	float intensity = h.SmoothstepRange(0.68, 0.8);

    // 目标绿色（可换成任意色）
	float3 highlightColor = float3(0.2, 1.0, 0.2);

    // 将颜色往绿色混合（Lerp）
	float3 result = lerp(baseColor, highlightColor, intensity);

	return saturate(result);
}

//------------------------------------------------------------
// 应用高亮边缘：距离中心 0.68~0.8 之间添加对应颜色两边
//------------------------------------------------------------
float3 ApplyHighlightColor(float3 baseColor, float4 highlightColor, HexGridData h)
{
	float t = h.SmoothstepRange(0.68, 0.8); // 高亮边缘范围
	return lerp(baseColor, highlightColor.rgb, t * highlightColor.a);
}


//------------------------------------------------------------
// 根据水面与地表高度差添加蓝色调滤镜（模拟水下变蓝效果）
// 最大影响范围为 15 单位深度
//------------------------------------------------------------
float3 ColorizeSubmergence(float3 baseColor, float surfaceY, float waterY)
{
	float submergence = waterY - max(surfaceY, 0); // 水面高于地表的深度
	float3 colorFilter = float3(0.25, 0.25, 0.75); // 蓝色色调
	float filterRange = 1.0 / 15.0; // 影响范围（线性衰减）
	return baseColor * lerp(1.0, colorFilter, saturate(submergence * filterRange));
}


//------------------------------------------------------------
// 片元阶段计算：合成地形颜色、网格、蓝色滤镜、高亮等
//------------------------------------------------------------
void GetFragmentData_float(
	UnityTexture2DArray TerrainTextures, // 地形纹理数组
	float3 WorldPosition, // 当前片元的世界坐标
	float4 Terrain, // 顶点插值得到的地形信息
	float4 Visibility, // 顶点插值得到的可见性信息
	float3 Weights, // 权重（对应三个单元）
	bool ShowGrid, // 是否显示网格线
	out float3 BaseColor, // 输出最终颜色
	out float Exploration // 输出探索度（用于迷雾系统等）
)
{
	// 分别采样三个单元的地形纹理并叠加加权结果
	float4 c =
		GetTerrainColor(TerrainTextures, WorldPosition, Terrain, Weights, Visibility, 0) +
		GetTerrainColor(TerrainTextures, WorldPosition, Terrain, Weights, Visibility, 1) +
		GetTerrainColor(TerrainTextures, WorldPosition, Terrain, Weights, Visibility, 2);

	// 对颜色应用水下蓝色滤镜
	BaseColor = ColorizeSubmergence(c.rgb, WorldPosition.y, Terrain.w);

	// 获取六边形格子数据，用于网格绘制与高亮
	HexGridData hgd = GetHexGridData(WorldPosition.xz);

	// 如果开启网格显示，则绘制格线（深色调）
	if (ShowGrid)
	{
		BaseColor = ApplyGrid(BaseColor, hgd);
	}
		

	// Hover 高亮
	if (hgd.IsHoverHighlighted())
	{
		BaseColor = ApplyHighlightColor(BaseColor, _HoverColor, hgd);
	}

	// Click 高亮
	if (hgd.IsClickHighlighted())
	{
		BaseColor = ApplyHighlightColor(BaseColor, _ClickColor, hgd);
	}
	
	//// 右键高亮
	//if (hgd.IsRightClickHighlighted())
	//{
	//	BaseColor = ApplyHighlightColor(BaseColor, _RightClickColor, hgd);
	//}
	
	// 路径高亮
	if (hgd.IsPathHighlighted())
	{
		BaseColor = ApplyHighlightColor(BaseColor, _PathColor, hgd);
	}
	else
	{
		BaseColor *= lerp(float3(1, 1, 1), _FinalColorMultiply.rgb, _FinalColorMultiply.a);
	}
	
	// 输出探索度值（用于雾、可见性等系统）
	Exploration = Visibility.w;
}
