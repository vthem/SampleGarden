//UNITY_SHADER_NO_UPGRADE
#ifndef HEIGHTMODIFIER_HLSL
#define HEIGHTMODIFIER_HLSL

#include "Packages/jp.keijiro.noiseshader/Shader/ClassicNoise3D.hlsl"

#define ARRAY_SIZE 1023
float4 depthDeltaData[ARRAY_SIZE];

void HeightModifier_float(float3 vOS, float3 vWS, float heightVScale, float heightHScale, out float3 vOutOS)
{
	float height = 0.0;
#if UNITY_ANY_INSTANCING_ENABLED
	height = ClassicNoise(vWS * heightHScale);
	//height += unity_InstanceID;
#endif
	vOS.y = height * heightVScale;
	vOutOS = vOS;
}

#endif //HEIGHTMODIFIER_HLSL