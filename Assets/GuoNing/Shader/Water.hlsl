#ifndef CUSTOM_WATER_INCLUDED
#define CUSTOM_WATER_INCLUDED

// --- 定义缩放因子 ---
#define TILING_SCALE 0.1

// --- Foam ---
float Foam(float shore, float2 worldXZ, float time, Texture2D noiseTex, SamplerState noiseSampler)
{
	shore = sqrt(shore) * 0.9;
	//float foamWeight = smoothstep(0.0, 1.0, shore);

	float2 noiseUV = worldXZ + time * 0.25;
	float4 noise = noiseTex.Sample(noiseSampler, noiseUV * (2 * TILING_SCALE));

	float distortion1 = noise.x * (1 - shore);
	float foam1 = sin((shore + distortion1) * 10 - time);
	//float foam1 = sin((foamWeight + distortion1) * 10 - time);
	foam1 *= foam1;

	float distortion2 = noise.y * (1 - shore);
	float foam2 = sin((shore + distortion2) * 10 + time + 2);
	//float foam2 = sin((foamWeight + distortion2) * 10 + time + 2);
	foam2 *= foam2 * 0.7;

	return max(foam1, foam2) * shore;
}

// --- River ---
float River(float2 riverUV, float time, Texture2D noiseTex, SamplerState noiseSampler)
{
	float2 uv = riverUV;
	uv.x = uv.x * 0.0625 + time * 0.005;
	uv.y -= time * 0.25;
	float4 noise = noiseTex.Sample(noiseSampler, uv);

	float2 uv2 = riverUV;
	uv2.x = uv2.x * 0.0625 - time * 0.0052;
	uv2.y -= time * 0.23;
	float4 noise2 = noiseTex.Sample(noiseSampler, uv2);

	return noise.r * noise2.w;
}

// --- Waves ---
float Waves(float2 worldXZ, float time, Texture2D noiseTex, SamplerState noiseSampler)
{
	float2 uv1 = worldXZ;
	uv1.y += time;
	float4 noise1 = noiseTex.Sample(noiseSampler, uv1 * (3 * TILING_SCALE));

	float2 uv2 = worldXZ;
	uv2.x += time;
	float4 noise2 = noiseTex.Sample(noiseSampler, uv2 * (3 * TILING_SCALE));

	float blendWave = sin((worldXZ.x + worldXZ.y) * 0.1 + (noise1.y + noise2.z) + time);
	blendWave *= blendWave;

	float waves =
        lerp(noise1.z, noise1.w, blendWave) +
        lerp(noise2.x, noise2.y, blendWave);

	return smoothstep(0.75, 2, waves);
}

#endif
