Shader "WanderingCity/StylizedWater"
{
 Properties { _Shallow("Shallows",Color)=(.25,.7,.65,1) _Deep("Depth",Color)=(.035,.2,.27,1) _Foam("Shore foam",Color)=(.85,.94,.87,1) }
 SubShader
 {
  Tags { "RenderPipeline"="UniversalPipeline" "Queue"="Transparent" "RenderType"="Transparent" }
  Pass
  {
   Tags { "LightMode"="UniversalForward" }
   Blend SrcAlpha OneMinusSrcAlpha ZWrite Off Cull Off
   HLSLPROGRAM
   #pragma vertex vert
   #pragma fragment frag
   #pragma multi_compile_fog
   #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
   #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
   #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareDepthTexture.hlsl"
   #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareOpaqueTexture.hlsl"
   CBUFFER_START(UnityPerMaterial)
   float4 _Shallow, _Deep, _Foam;
   CBUFFER_END
   struct V { float4 position:SV_POSITION; float3 world:TEXCOORD0; float eye:TEXCOORD1; float fog:TEXCOORD2; };
   V vert(float4 p:POSITION) { V o; o.world=TransformObjectToWorld(p.xyz); o.position=TransformWorldToHClip(o.world); o.eye=-TransformWorldToView(o.world).z; o.fog=ComputeFogFactor(o.position.z); return o; }
   half4 frag(V i):SV_Target
   {
    float2 uv=GetNormalizedScreenSpaceUV(i.position);
    float depth=LinearEyeDepth(SampleSceneDepth(uv),_ZBufferParams);
    float thickness=max(0,depth-i.eye);
    float t=_Time.y;
    float2 ripple=float2(sin(i.world.x*1.7+i.world.z*.9+t*1.2),cos(i.world.z*1.4-i.world.x*.6+t*.8))*.07;
    float3 n=normalize(float3(ripple.x,1,ripple.y));
    float2 offset=uv+ripple*.015*saturate(thickness);
    float refractedDepth=LinearEyeDepth(SampleSceneDepth(offset),_ZBufferParams);
    float3 scene=SampleSceneColor(refractedDepth>i.eye+.03?offset:uv);
    float3 tint=lerp(_Shallow.rgb,_Deep.rgb,saturate(thickness*.65));
    Light sun=GetMainLight(); float3 view=normalize(GetWorldSpaceViewDir(i.world));
    float spec=pow(saturate(dot(n,normalize(sun.direction+view))),96);
    float fresnel=pow(1-saturate(dot(n,view)),3);
    float foam=(1-smoothstep(.04,.45,thickness))*(.6+.4*sin(i.world.x*3+i.world.z*2+t*1.7));
    float3 color=lerp(scene,tint,.55+saturate(thickness)*.25)+sun.color*spec*.7+fresnel*.1;
    color=lerp(color,_Foam.rgb,saturate(foam*.8));
    return half4(MixFog(color,i.fog),.85);
   }
   ENDHLSL
  }
 }
}
