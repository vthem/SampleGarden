//UNITY_SHADER_NO_UPGRADE
#ifndef HEIGHTMODIFIER_HLSL
#define HEIGHTMODIFIER_HLSL

#include "Packages/jp.keijiro.noiseshader/Shader/ClassicNoise3D.hlsl"
#include "Assets/Scenes/Samples/010.WormMesh/WormMesh.hlsl"

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

float _rootQuadSize;
float _heightVScale;
float _heightHScale;
float _width;
float _radius;

void WormModifier(float3 vWS, out float3 vOutWS, out float3 normal)
{
	vOutWS = vWS; normal = float3(0, 1, 0); return;

	float angle = (vWS.x / _width) * 2 * PI; // (5 + vOS.x) * 0.1 * PI * 0.5;
	float3 pOnPath = float3(0, 0, vWS.z);
	float3 tan = float3(0, 0, 1);
	float4 q = fromToRotation(float3(0, 0, 1), tan);
	float3 pOnCircleDir = rotateWithQuaternion(float3(cos(angle), sin(angle), 0), q);
    //vOutWS = float3(vWS.x, pOnPath.y, vWS.z); // + pOnCircle;
	vOutWS = pOnPath + pOnCircleDir * _radius;
	//vOutWS = pOnPath + pOnCircleDir * ClassicNoise(vOutWS * _heightHScale) * _heightVScale + pOnCircleDir * _radius;
	//return vOutWS;

	// compute normal
	normal = -pOnCircleDir;
}

//void ComputeVertexWS(float3 vWS, float3, )
//{
//	vWS = WormModifier(vWS);
//	//vWS.y = ClassicNoise(vWS * _heightHScale);
//	return vWS;
//}

void ComputeSeamParameter(int d, int dn, float xz, out float lb, out float ub, out float t)
{
	float step_dn = 0.1 * _rootQuadSize / pow(2, (d - dn));

	ub = xz + step_dn - xz % step_dn;
	lb = ub - step_dn;
	t = (xz - lb) / (ub - lb);
}

void ComputeVertexInterpolation(float3 lb, float3 ub, float t, out float3 vOutWS, out float3 normal)
{
	float3 ub_vWS, ub_normal, lb_vWS, lb_normal;
	WormModifier(ub, ub_vWS, ub_normal);
	WormModifier(lb, lb_vWS, lb_normal);
	vOutWS = lerp(lb_vWS, ub_vWS, t);
	normal = lerp(lb_normal, ub_normal, t);
}

void HeightModifier_float(float3 vOS, out float3 vOutOS)
{
	float height = 0.0;
#if UNITY_ANY_INSTANCING_ENABLED

	InstanceData data = _PerInstanceData[unity_InstanceID];

	float3 vWS = mul(unity_ObjectToWorld, float4(vOS, 1)).xyz;
	float3 vOutWS = float3(0, 0, 0);
	//if (unity_InstanceID == 7 || unity_InstanceID == 10)
	{
		int dn_left = (data.depthInfo >> 6 * 0) & 0x0000003f;
		int dn_up = (data.depthInfo >> 6 * 1) & 0x0000003f;
		int dn_right = (data.depthInfo >> 6 * 2) & 0x0000003f;
		int dn_bottom = (data.depthInfo >> 6 * 3) & 0x0000003f;
		int d = (data.depthInfo >> 6 * 4) & 0x0000003f;
		
		float ub, lb, t;
		float3 ub_vWS;
		float3 lb_vWS;

		if (round(vOS.z) == 0 && dn_bottom > 0)
		{			
			ComputeSeamParameter(d, dn_bottom, vWS.x, lb, ub, t);

			ub_vWS = float3(ub, vWS.yz);
			lb_vWS = float3(lb, vWS.yz);
		}
		else if (round(vOS.z) == 10 && dn_up > 0)
		{
			ComputeSeamParameter(d, dn_up, vWS.x, lb, ub, t);

			ub_vWS = float3(ub, vWS.yz);
			lb_vWS = float3(lb, vWS.yz);
		}
		else if (round(vOS.x) == 0 && dn_left > 0)
		{
			ComputeSeamParameter(d, dn_left, vWS.z, lb, ub, t);
			
			ub_vWS = float3(vWS.xy, ub);
			lb_vWS = float3(vWS.xy, lb);
		}
		else if (round(vOS.x) == 10 && dn_right > 0)
		{
			ComputeSeamParameter(d, dn_right, vWS.z, lb, ub, t);
			
			ub_vWS = float3(vWS.xy, ub);
			lb_vWS = float3(vWS.xy, lb);
		}
		else
		{
			ub_vWS = vWS;
			lb_vWS = vWS;
			t = 1.0;
		}		
		float3 normal;
		ComputeVertexInterpolation(lb_vWS, ub_vWS, t, vOutWS, normal);
		vOutWS += normal * ClassicNoise(vOutWS * _heightHScale) * _heightVScale;
	}	
	vOutOS = mul(unity_WorldToObject, float4(vOutWS, 1)).xyz;
#else
	vOutOS = vOS;
#endif
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