//UNITY_SHADER_NO_UPGRADE
#ifndef MYHLSLINCLUDE_INCLUDED
#define MYHLSLINCLUDE_INCLUDED

// from https://github.com/sugi-cho/First-Compute-Shader/blob/master/Assets/FirstCompute/Quaternion.cginc
#define PI2 6.28318530718
// Quaternion multiplication.
// http://mathworld.wolfram.com/Quaternion.html
float4 qmul(float4 q1, float4 q2)
{
    return float4(
        q2.xyz * q1.w + q1.xyz * q2.w + cross(q1.xyz, q2.xyz),
        q1.w * q2.w - dot(q1.xyz, q2.xyz)
    );
}

// Rotate a vector with a rotation quaternion.
// http://mathworld.wolfram.com/Quaternion.html
float3 rotateWithQuaternion(float3 v, float4 r)
{
    float4 r_c = r * float4(-1, -1, -1, 1);
    return qmul(r, qmul(float4(v, 0), r_c)).xyz;
}

float4 getAngleAxisRotation(float3 v, float3 axis, float angle){
	axis = normalize(axis);
	float s,c;
	sincos(angle,s,c);
	return float4(axis.x*s,axis.y*s,axis.z*s,c);
}

float3 rotateAngleAxis(float3 v, float3 axis, float angle){
	float4 q = getAngleAxisRotation(v,axis,angle);
	return rotateWithQuaternion(v,q);
}

float4 fromToRotation(float3 from, float3 to){
	float3
		v1 = normalize(from),
		v2 = normalize(to),
		cr = cross(v1,v2);
	float4 q = float4( cr,1+dot(v1,v2) );
	return normalize(q);
}

#define ARRAY_SIZE 512

float4 _PositionArray[ARRAY_SIZE];
float4 _ForwardArray[ARRAY_SIZE];
int _ArrayCount = 0;

float3 GetInterpolatedValue(float4 arr[ARRAY_SIZE], int count, float zWS)
{
	//float fIndex = lerp(0, count - 1, zWS);
	int index = int(floor(zWS));
	index = min(index, count - 1);
	float dec = zWS - index;
	return lerp(arr[index], arr[index + 1], dec);
}

float3 GetPosition(float zWS)
{
	return GetInterpolatedValue(_PositionArray, _ArrayCount, zWS);
}

float3 GetForward(float zWS)
{
	return GetInterpolatedValue(_ForwardArray, _ArrayCount, zWS);
}

void WormModifier_float(float3 vOS, float3 vWS, int count1d, out float3 vOutWS, out float3 normal)
{
	// compute position
	int vCount = count1d - 1;
	float angle = (5 + vOS.x) * 0.1 * PI * 0.5;
	//float angle = PI * 2 * vOS.x / vCount;
	float3 pOnPath = GetPosition(vWS.z);
	float3 tan = GetForward(vWS.z);
	float4 q = fromToRotation(float3(0, 0, 1), tan);
	float radius = 1;
	float3 pOnCircle = rotateWithQuaternion(float3(cos(angle) * radius, sin(angle) * radius, 0), q);
    //vOutWS = float3(vWS.x, pOnPath.y, vWS.z); // + pOnCircle;
	vOutWS = pOnPath + pOnCircle;

	// compute normal
	normal = -pOnCircle;
}

#endif //MYHLSLINCLUDE_INCLUDED