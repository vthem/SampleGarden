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
	int nd; // neighbor delta
};


#define ARRAY_SIZE 200
// left, up, right, down

#ifdef UNITY_PROCEDURAL_INSTANCING_ENABLED

StructuredBuffer<InstanceData> _PerInstanceData;

void vertInstancingSetup() {
	InstanceData data = _PerInstanceData[unity_InstanceID];
	unity_ObjectToWorld = data.objectToWorld;
	unity_WorldToObject = data.worldToObject;
}
#endif

void HeightModifier_float(float3 vOS, float heightVScale, float heightHScale, out float3 vOutOS)
{
	float height = 0.0;
#if UNITY_ANY_INSTANCING_ENABLED

	InstanceData data = _PerInstanceData[unity_InstanceID];

	float3 vWS = mul(unity_ObjectToWorld, float4(vOS, 1)).xyz;
	height = ClassicNoise(vWS * heightHScale);
	if (unity_InstanceID == 7 || unity_InstanceID == 10)
	{
		if (round(vOS.z) == 0)
		{
			int delta = (data.nd >> 8*3) & 0x000000ff;
			int inter = pow(2, delta);
			float t = vWS.x % (0.125*inter);
			float lb = vWS.x - t; // lower bound
			float up = vWS.x + (0.125*inter); // upper bound
			
			float lb_height = ClassicNoise(float3(lb, vWS.yz) * heightHScale);
			float ub_height = ClassicNoise(float3(up, vWS.yz) * heightHScale);
			//height = lerp(lb_height, ub_height, t);
			//if ((vWS.x % (0.125*inter)) != 0)
			//{
			//	height = 0;
			//}
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