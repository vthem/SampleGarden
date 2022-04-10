// This shader adds tessellation in URP
Shader "Example/URPUnlitShaderTessallated"
{

	// The properties block of the Unity shader. In this example this block is empty
	// because the output color is predefined in the fragment shader code.
	Properties
	{
		_Tess("Tessellation", Range(1, 32)) = 20
		_MaxTessDistance("Max Tess Distance", Range(1, 32)) = 20
		_Noise("Noise", 2D) = "gray" {}

	_Weight("Displacement Amount", Range(0, 1)) = 0
	}

		// The SubShader block containing the Shader code. 
		SubShader
	{
		// SubShader Tags define when and under which conditions a SubShader block or
		// a pass is executed.
		Tags{ "RenderType" = "Opaque" "RenderPipeline" = "UniversalRenderPipeline" }

		Pass
	{
		Tags{ "LightMode" = "UniversalForward" }


		// The HLSL code block. Unity SRP uses the HLSL language.
		HLSLPROGRAM
		// The Core.hlsl file contains definitions of frequently used HLSL
		// macros and functions, and also contains #include references to other
		// HLSL files (for example, Common.hlsl, SpaceTransforms.hlsl, etc.).
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"    
#include "CustomTessellation.hlsl"
#include "Packages/jp.keijiro.noiseshader/Shader/ClassicNoise3D.hlsl"


#pragma require tessellation
		// This line defines the name of the vertex shader. 
#pragma vertex TessellationVertexProgram
		// This line defines the name of the fragment shader. 
#pragma fragment frag
		// This line defines the name of the hull shader. 
#pragma hull hull
		// This line defines the name of the domain shader. 
#pragma domain domain






		sampler2D _Noise;
	float _Weight;

	// pre tesselation vertex program
	ControlPoint TessellationVertexProgram(Attributes v)
	{
		ControlPoint p;

		p.vertex = v.vertex;
		p.uv = v.uv;
		p.normal = v.normal;
		p.color = v.color;

		return p;
	}

	float3 ModifyVertex(float3 v)
	{
		//float Noise = ClassicNoise(mul(unity_ObjectToWorld, float4(v, 1)));
		float Noise = ClassicNoise(v);
		v.y = Noise * _Weight;
		return v;
	}

	// after tesselation
	Varyings vert(Attributes input)
	{
		Varyings output;
		//float Noise = tex2Dlod(_Noise, float4(input.uv, 0, 0)).r;
		float3 v = input.vertex.xyz;
		float3 vm = ModifyVertex(v);
		output.vertex = TransformObjectToHClip(vm);
		//output.color = input.color;

		float delta = 0.0001;
		float3 vz = float3(v.xy, v.z + delta);
		float3 vx = float3(v.x + delta, v.yz);
		vz = ModifyVertex(vz);
		vx = ModifyVertex(vx);
	
		//output.normal = input.normal;
		output.normal = normalize(cross(vz - vm, vx - vm));
		output.color = float4(output.normal, 1);
		//output.color = float4(1, 0, 0, 1);
		output.uv = input.uv;
		return output;
	}

	[UNITY_domain("tri")]
	Varyings domain(TessellationFactors factors, OutputPatch<ControlPoint, 3> patch, float3 barycentricCoordinates : SV_DomainLocation)
	{
		Attributes v;

#define DomainPos(fieldName) v.fieldName = \
				patch[0].fieldName * barycentricCoordinates.x + \
				patch[1].fieldName * barycentricCoordinates.y + \
				patch[2].fieldName * barycentricCoordinates.z;

			DomainPos(vertex)
			DomainPos(uv)
			DomainPos(color)
			DomainPos(normal)

			return vert(v);
	}

	// The fragment shader definition.            
	half4 frag(Varyings IN) : SV_Target
	{
		//half4 tex = tex2D(_Noise, IN.uv); return tex;

		return IN.color;
	}
		ENDHLSL
	}
	}
}