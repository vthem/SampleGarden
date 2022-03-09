//UNITY_SHADER_NO_UPGRADE
#ifndef HEIGHTMODIFIER_HLSL
#define HEIGHTMODIFIER_HLSL

#include "Packages/jp.keijiro.noiseshader/Shader/ClassicNoise3D.hlsl"

#define ARRAY_SIZE 1023
// left, up, right, down
float4 depthDeltaData[ARRAY_SIZE];

int indexMask[44] = {
	//  0  1  2  3  4  5  6  7  8  9  10
		0, 0, 0, 0, 0, 0, 0, 0, 0, 0,  0 // depthDelta = 0
		0, 1, 0, 1, 0, 1, 0, 1, 0, 1,  0 // depthDelta = 1
		0, 1, 1, 1, 1, 0, 1, 1, 1, 1,  0 // depthDelta = 2
		0, 1, 1, 1, 1, 1, 1, 1, 1, 1,  0 // depthDelta = 3
	};

void HeightModifier_float(float3 vOS, float3 vWS, float heightVScale, float heightHScale, out float3 vOutOS)
{
	float height = 0.0;
#if UNITY_ANY_INSTANCING_ENABLED
	height = ClassicNoise(vWS * heightHScale);
	float4 depthDelta = depthDeltaData[unity_InstanceID];
	if (vOS.x + 4 <= 0.001)
	{
		height = 0;
	}
	//height += unity_InstanceID;
#endif
	vOS.y = height * heightVScale;
	vOutOS = vOS;
}

#endif //HEIGHTMODIFIER_HLSL