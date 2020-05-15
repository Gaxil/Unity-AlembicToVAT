// Made with Amplify Shader Editor
// Available at the Unity Asset Store - http://u3d.as/y3X 
Shader "VATMaterial"
{
	Properties
	{
		_Timeposition("Time position", Range( 0 , 1)) = 0
		_Color("Color", Color) = (1,0.5773492,0,0)
		_Metalness("Metalness", Range( 0 , 1)) = 0
		_Roughness("Roughness", Range( 0 , 1)) = 0
		_Framecount("Frame count", Float) = 240
		_VAT_positions("VAT_positions", 2D) = "white" {}
		_VAT_normals("VAT_normals", 2D) = "white" {}
		[HideInInspector] __dirty( "", Int ) = 1
	}

	SubShader
	{
		Tags{ "RenderType" = "Opaque"  "Queue" = "Geometry+0" }
		Cull Back
		CGPROGRAM
		#pragma target 3.0
		#pragma surface surf Standard keepalpha addshadow fullforwardshadows vertex:vertexDataFunc 
		struct Input
		{
			half filler;
		};

		uniform sampler2D _VAT_positions;
		uniform float4 _VAT_positions_TexelSize;
		uniform float _Framecount;
		uniform float _Timeposition;
		uniform sampler2D _VAT_normals;
		uniform float4 _Color;
		uniform float _Metalness;
		uniform float _Roughness;

		void vertexDataFunc( inout appdata_full v, out Input o )
		{
			UNITY_INITIALIZE_OUTPUT( Input, o );
			float2 appendResult9 = (float2(( _VAT_positions_TexelSize.x * ( ( _Framecount - 1.0 ) * _Timeposition ) ) , 0.0));
			float2 temp_output_21_0 = ( appendResult9 + v.texcoord1.xy );
			v.vertex.xyz += tex2Dlod( _VAT_positions, float4( temp_output_21_0, 0, 0.0) ).rgb;
			v.normal = tex2Dlod( _VAT_normals, float4( temp_output_21_0, 0, 0.0) ).rgb;
		}

		void surf( Input i , inout SurfaceOutputStandard o )
		{
			o.Albedo = _Color.rgb;
			o.Metallic = _Metalness;
			o.Smoothness = ( 1.0 - _Roughness );
			o.Alpha = 1;
		}

		ENDCG
	}
	Fallback "Diffuse"
	CustomEditor "ASEMaterialInspector"
}
/*ASEBEGIN
Version=17101
1923;0;1848;1171;2343.338;644.0337;1;True;False
Node;AmplifyShaderEditor.RangedFloatNode;14;-1836.69,-67.99374;Inherit;False;Property;_Framecount;Frame count;4;0;Create;True;0;0;False;0;240;240;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleSubtractOpNode;17;-1580.609,-20.29099;Inherit;False;2;0;FLOAT;0;False;1;FLOAT;1;False;1;FLOAT;0
Node;AmplifyShaderEditor.TexturePropertyNode;22;-1783.033,-632.879;Inherit;True;Property;_VAT_positions;VAT_positions;5;0;Create;True;0;0;False;0;None;e78b4fe27cda70d4cb58ac31349678b7;False;white;Auto;Texture2D;0;1;SAMPLER2D;0
Node;AmplifyShaderEditor.RangedFloatNode;8;-1779.555,69.79008;Inherit;False;Property;_Timeposition;Time position;0;0;Create;True;0;0;False;0;0;0.396;0;1;0;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;15;-1349.54,-23.81801;Inherit;False;2;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.TexelSizeNode;24;-1511.805,-514.0825;Inherit;False;-1;1;0;SAMPLER2D;;False;5;FLOAT4;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;25;-1157.735,-29.28227;Inherit;False;2;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.TextureCoordinatesNode;7;-841.2699,209.621;Inherit;False;1;-1;2;3;2;SAMPLER2D;;False;0;FLOAT2;1,1;False;1;FLOAT2;0,0;False;5;FLOAT2;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.DynamicAppendNode;9;-964.1415,-24.70842;Inherit;False;FLOAT2;4;0;FLOAT;0;False;1;FLOAT;0;False;2;FLOAT;0;False;3;FLOAT;0;False;1;FLOAT2;0
Node;AmplifyShaderEditor.TexturePropertyNode;23;-1773.106,-385.3825;Inherit;True;Property;_VAT_normals;VAT_normals;6;0;Create;True;0;0;False;0;None;cfb837e3442efaf42a08ff258ce1a8b3;False;white;Auto;Texture2D;0;1;SAMPLER2D;0
Node;AmplifyShaderEditor.RangedFloatNode;12;-205,-219;Inherit;False;Property;_Roughness;Roughness;3;0;Create;True;0;0;False;0;0;0;0;1;0;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleAddOpNode;21;-517.7681,114.606;Inherit;False;2;2;0;FLOAT2;0,0;False;1;FLOAT2;0,0;False;1;FLOAT2;0
Node;AmplifyShaderEditor.SamplerNode;1;-197,130;Inherit;True;Property;_NormalVAT;NormalVAT;0;0;Create;True;0;0;False;0;None;None;True;0;False;white;Auto;False;Object;-1;Auto;Texture2D;6;0;SAMPLER2D;;False;1;FLOAT2;0,0;False;2;FLOAT;0;False;3;FLOAT2;0,0;False;4;FLOAT2;0,0;False;5;FLOAT;1;False;5;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.SamplerNode;2;-195,-65;Inherit;True;Property;_PositionVAT;PositionVAT;1;0;Create;True;0;0;False;0;None;None;True;0;False;white;Auto;False;Object;-1;Auto;Texture2D;6;0;SAMPLER2D;;False;1;FLOAT2;0,0;False;2;FLOAT;0;False;3;FLOAT2;0,0;False;4;FLOAT2;0,0;False;5;FLOAT;1;False;5;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.RangedFloatNode;11;-102,-333;Inherit;False;Property;_Metalness;Metalness;2;0;Create;True;0;0;False;0;0;0;0;1;0;1;FLOAT;0
Node;AmplifyShaderEditor.ColorNode;10;-35,-509;Inherit;False;Property;_Color;Color;1;0;Create;True;0;0;False;0;1,0.5773492,0,0;1,0.5773492,0,0;True;0;5;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.OneMinusNode;13;65,-217;Inherit;False;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.FractNode;20;-1548.473,189.4616;Inherit;False;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleTimeNode;19;-1784.743,216.9211;Inherit;False;1;0;FLOAT;1;False;1;FLOAT;0
Node;AmplifyShaderEditor.StandardSurfaceOutputNode;0;331,-326;Float;False;True;2;ASEMaterialInspector;0;0;Standard;VATMaterial;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;Back;0;False;-1;0;False;-1;False;0;False;-1;0;False;-1;False;0;Opaque;0.5;True;True;0;False;Opaque;;Geometry;All;14;all;True;True;True;True;0;False;-1;False;0;False;-1;255;False;-1;255;False;-1;0;False;-1;0;False;-1;0;False;-1;0;False;-1;0;False;-1;0;False;-1;0;False;-1;0;False;-1;False;2;15;10;25;False;0.5;True;0;0;False;-1;0;False;-1;0;0;False;-1;0;False;-1;0;False;-1;0;False;-1;0;False;0;0,0,0,0;VertexOffset;True;False;Cylindrical;False;Relative;0;;-1;-1;-1;-1;0;False;0;0;False;-1;-1;0;False;-1;0;0;0;False;0.1;False;-1;0;False;-1;16;0;FLOAT3;0,0,0;False;1;FLOAT3;0,0,0;False;2;FLOAT3;0,0,0;False;3;FLOAT;0;False;4;FLOAT;0;False;5;FLOAT;0;False;6;FLOAT3;0,0,0;False;7;FLOAT3;0,0,0;False;8;FLOAT;0;False;9;FLOAT;0;False;10;FLOAT;0;False;13;FLOAT3;0,0,0;False;11;FLOAT3;0,0,0;False;12;FLOAT3;0,0,0;False;14;FLOAT4;0,0,0,0;False;15;FLOAT3;0,0,0;False;0
WireConnection;17;0;14;0
WireConnection;15;0;17;0
WireConnection;15;1;8;0
WireConnection;24;0;22;0
WireConnection;25;0;24;1
WireConnection;25;1;15;0
WireConnection;9;0;25;0
WireConnection;21;0;9;0
WireConnection;21;1;7;0
WireConnection;1;0;23;0
WireConnection;1;1;21;0
WireConnection;2;0;22;0
WireConnection;2;1;21;0
WireConnection;13;0;12;0
WireConnection;20;0;19;0
WireConnection;0;0;10;0
WireConnection;0;3;11;0
WireConnection;0;4;13;0
WireConnection;0;11;2;0
WireConnection;0;12;1;0
ASEEND*/
//CHKSM=EB17460BB238E09E5E1CB86CBA41BE399E5321C4