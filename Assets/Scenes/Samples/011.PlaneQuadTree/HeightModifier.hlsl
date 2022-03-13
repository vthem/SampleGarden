//UNITY_SHADER_NO_UPGRADE
#ifndef HEIGHTMODIFIER_HLSL
#define HEIGHTMODIFIER_HLSL

#include "Packages/jp.keijiro.noiseshader/Shader/ClassicNoise3D.hlsl"

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
	float4x4 m;
};


#define ARRAY_SIZE 200
// left, up, right, down
//float4 depthDeltaData[ARRAY_SIZE];
//float4x4 _objectToWorld[ARRAY_SIZE];

#ifdef UNITY_PROCEDURAL_INSTANCING_ENABLED

StructuredBuffer<InstanceData> _PerInstanceData;

void vertInstancingMatrices(inout float4x4 objectToWorld, out float4x4 worldToObject) {
	InstanceData data = _PerInstanceData[unity_InstanceID];

	objectToWorld = data.m;

	//float3 pos = float4(2, 0, 0, 2);
	//objectToWorld._11_21_31_41 = float4(pos.w, 0, 0, 0);
    //objectToWorld._12_22_32_42 = float4(0, pos.w, 0, 0);
    //objectToWorld._13_23_33_43 = float4(0, 0, pos.w, 0);
    //objectToWorld._14_24_34_44 = float4(pos.xyz, 1);

    worldToObject = objectToWorld;
    worldToObject._14_24_34 *= -1;
    worldToObject._11_22_33 = 1.0f / worldToObject._11_22_33;
}

void vertInstancingSetup() {
	vertInstancingMatrices(unity_ObjectToWorld, unity_WorldToObject);
}
#endif

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

	InstanceData data = _PerInstanceData[unity_InstanceID];
	float4x4 objectToWorld = data.m;

	float3 vWS = mul(objectToWorld, float4(vOS, 1)).xyz;
	//float3 vWS = mul(objectToWorld[unity_InstanceID], float4(vOS.xyz, 1)).xyz;
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