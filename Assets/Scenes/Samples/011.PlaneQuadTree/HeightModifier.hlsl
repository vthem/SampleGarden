//UNITY_SHADER_NO_UPGRADE
#ifndef HEIGHTMODIFIER_HLSL
#define HEIGHTMODIFIER_HLSL

#include "Packages/jp.keijiro.noiseshader/Shader/ClassicNoise3D.hlsl"

// from https://gist.github.com/Cyanilux/4046e7bf3725b8f64761bf6cf54a16eb

// ----------------------------------------------------------------------------------

// Graph should contain Boolean Keyword, "PROCEDURAL_INSTANCING_ON", Global, Multi-Compile.
// Must have two Custom Functions in vertex stage. One is used to attach this file (see Instancing_float below),
// and another to set #pragma instancing_options :

// It must use the String mode as this cannot be defined in includes.
// Without this, you will get "UNITY_INSTANCING_PROCEDURAL_FUNC must be defined" Shader Error.
/*
Out = In;
#pragma instancing_options procedural:vertInstancingSetup
*/
// I've found this works fine, but it might make sense for the pragma to be defined outside of a function,
// so could also use this slightly hacky method too
/*
Out = In;
}
#pragma instancing_options procedural:vertInstancingSetup
void dummy(){
*/

// ----------------------------------------------------------------------------------

struct InstanceData {
	float4x4 objectToWorld; // object to world
	float4x4 worldToObject;
	int depthInfo; // packed depth information depth | left neighbor | up | right | down ..
};

#ifdef UNITY_PROCEDURAL_INSTANCING_ENABLED

StructuredBuffer<InstanceData> _PerInstanceData;

void vertInstancingSetup() {
	InstanceData data = _PerInstanceData[unity_InstanceID];
	unity_ObjectToWorld = data.objectToWorld;
	unity_WorldToObject = data.worldToObject;
}
#endif

float _quadWidth;
float _quadHeight;

void HeightModifier_float(float3 vOS, float heightVScale, float heightHScale, out float3 vOutOS)
{
	float height = 0.0;
#if UNITY_ANY_INSTANCING_ENABLED

	InstanceData data = _PerInstanceData[unity_InstanceID];

	float3 vWS = mul(unity_ObjectToWorld, float4(vOS, 1)).xyz;
	height = ClassicNoise(vWS * heightHScale);
	//height = 0;
	if (unity_InstanceID == 7 || unity_InstanceID == 10)
	{
		if (round(vOS.z) == 0 /*&& round(vOS.x) == 2*/)
		{
			int dn = (data.depthInfo >> 6 * 3) & 0x0000003f;
			int d = (data.depthInfo >> 6 * 4) & 0x0000003f;
			float step_d = _quadWidth / pow(2, d+1);
			float step_dn = _quadWidth / pow(2, (d - dn) + 1);

			float ub = vWS.x + step_dn % vWS.x;
			float3 ub_vWS = float3(ub, vWS.yz);
			float3 lb_vWS = float3(ub - step_dn, vWS.yz);

			float ub_height = ClassicNoise(ub_vWS * heightHScale);
			float lb_height = ClassicNoise(lb_vWS * heightHScale);

			float t_d = (step_dn - step_dn % vWS.x) / step_dn;
			height = lerp(lb_height, ub_height, t_d);
		}
	}
#endif
	//vOS.y = height * heightVScale;
	vWS.y = height * heightVScale;
	vOutOS = mul(unity_WorldToObject, float4(vWS, 1)).xyz;
}

// Obtain InstanceID. e.g. Can be used as a Seed into Random Range node to generate random data per instance
void GetInstanceID_float(out float Out){
	Out = 0;
	#ifndef SHADERGRAPH_PREVIEW
	#if UNITY_ANY_INSTANCING_ENABLED
	Out = unity_InstanceID;
	#endif
	#endif
}

// Just passes the position through, allows us to actually attach this file to the graph.
// Should be placed somewhere in the vertex stage, e.g. right before connecting the object space position.
void Instancing_float(float3 Position, out float3 Out){
	Out = Position;
}

#endif //HEIGHTMODIFIER_HLSL