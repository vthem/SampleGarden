//UNITY_SHADER_NO_UPGRADE
#ifndef MYHLSLINCLUDE_INCLUDED
#define MYHLSLINCLUDE_INCLUDED

float4 _PositionArray[512];
int _PositionArrayCount = 0;

void MyFunction_float(float3 A, float3 B, out float3 Out)
{
    Out = A + B + _PositionArray[0];
}

#endif //MYHLSLINCLUDE_INCLUDED