//UNITY_SHADER_NO_UPGRADE
#ifndef HEIGHTMODIFIER_HLSL
#define HEIGHTMODIFIER_HLSL

#include "Packages/jp.keijiro.noiseshader/Shader/ClassicNoise3D.hlsl"

#define ARRAY_SIZE 200
// left, up, right, down
float4 depthDeltaData[ARRAY_SIZE];
float4x4 _objectToWorld[ARRAY_SIZE];

void HeightModifier_float(float3 vOS, float heightVScale, float heightHScale, out float3 vOutOS)
{
	const float indexMask[44] = {
	//  0  1  2  3  4  5  6  7  8  9  10
		0, 0, 0, 0, 0, 0, 0, 0, 0, 0,  0, // depthDelta = 0
		0, 1, 0, 1, 0, 1, 0, 1, 0, 1,  0, // depthDelta = 1
		0, 1, 1, 1, 1, 0, 1, 1, 1, 1,  0, // depthDelta = 2
		0, 1, 1, 1, 1, 1, 1, 1, 1, 1,  0, // depthDelta = 3
	};

	const float indexMin[44] = {
	//  0  1  2  3  4  5  6  7  8  9  10
		0, 0, 0, 0, 0, 0, 0, 0, 0, 0,  0, // depthDelta = 0
		0, 0, 0, 2, 0, 4, 0, 6, 0, 8,  0, // depthDelta = 1
		0, 1, 1, 1, 1, 0, 1, 1, 1, 1,  0, // depthDelta = 2
		0, 1, 1, 1, 1, 1, 1, 1, 1, 1,  0, // depthDelta = 3
	};

	const float indexMax[44] = {
	//  0  1  2  3  4  5  6  7  8  9  10
		0, 0, 0, 0, 0, 0, 0, 0, 0, 0,  0, // depthDelta = 0
		0, 2, 0, 4, 0, 6, 0, 8, 0, 10,  0, // depthDelta = 1
		0, 1, 1, 1, 1, 0, 1, 1, 1, 1,  0, // depthDelta = 2
		0, 1, 1, 1, 1, 1, 1, 1, 1, 1,  0, // depthDelta = 3
	};

	float height = 0.0;
#if UNITY_ANY_INSTANCING_ENABLED
	//float3 vWS = mul(_objectToWorld[unity_InstanceID], vOS);
	float3 vWS = mul(_objectToWorld[unity_InstanceID], float4(vOS.xyz, 1)).xyz;
	height = ClassicNoise(vWS * heightHScale);
	//height = ClassicNoise(mvOS * heightHScale);
	//float4 depthDelta = depthDeltaData[unity_InstanceID];
	//if (depthDelta.r - 1.0 <= 0.001)
	//{
		//height = indexMask[round(depthDelta.r * 11 + vOS.x + 5)];
		//height = round(round(depthDelta.r) * 11 + round(vOS.x + 5));
		//height = round(depthDelta.r);
		//height = round(vOS.x + 5.0) / 10.0;
	//	//height = vOS.x + 5;
	//}
	//height += unity_InstanceID;
	//if (unity_InstanceID != 4)
		//height = 0;
#endif
	vOS.y = height * heightVScale;
	vOutOS = vOS;
}

#endif //HEIGHTMODIFIER_HLSL