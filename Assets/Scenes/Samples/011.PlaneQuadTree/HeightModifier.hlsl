//UNITY_SHADER_NO_UPGRADE
#ifndef HEIGHTMODIFIER_HLSL
#define HEIGHTMODIFIER_HLSL

#include "Packages/jp.keijiro.noiseshader/Shader/ClassicNoise3D.hlsl"

void HeightModifier_float(float3 vOS, float3 vWS, float heightScale, out float3 vOutOS)
{
	float height = ClassicNoise(vWS);
	vOS.y = height * heightScale;
	vOutOS = vOS;
}

#endif //HEIGHTMODIFIER_HLSL