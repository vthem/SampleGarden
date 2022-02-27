//UNITY_SHADER_NO_UPGRADE
#ifndef HEIGHTMODIFIER_HLSL
#define HEIGHTMODIFIER_HLSL

void HeightModifier_float(float3 vOS, float height, out float3 vOutOS)
{
	vOS.y = height;
	vOutOS = vOS;
}

#endif //HEIGHTMODIFIER_HLSL