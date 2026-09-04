Shader "WanderingCity/StylizedSky"
{
 Properties {
  _Zenith("Zenith",Color)=(.12,.36,.66,1)
  _Horizon("Horizon",Color)=(.72,.84,.86,1)
  _Haze("Horizon haze",Range(0,1))=.28
  _Coverage("Cloud coverage",Range(0,1))=.53
  _Softness("Cloud softness",Range(.01,.5))=.12
  _CloudSpeed("Cloud movement",Float)=.008
  _Exposure("Exposure",Float)=1
 }
 SubShader {
  Tags {"Queue"="Background" "RenderType"="Background" "PreviewType"="Skybox" "RenderPipeline"="UniversalPipeline"}
  Cull Off ZWrite Off
  Pass {
   HLSLPROGRAM
   #pragma vertex vert
   #pragma fragment frag
   #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
   #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
   float4 _Zenith,_Horizon; float _Haze,_Coverage,_Softness,_CloudSpeed,_Exposure;
   struct V {float4 position:SV_POSITION; float3 direction:TEXCOORD0;};
   V vert(float4 p:POSITION) {V o;o.position=TransformObjectToHClip(p.xyz);o.direction=p.xyz;return o;}
   float hash(float2 p){return frac(sin(dot(p,float2(127.1,311.7)))*43758.5453);}
   float noise(float2 p){float2 i=floor(p),f=frac(p);f=f*f*(3-2*f);return lerp(lerp(hash(i),hash(i+float2(1,0)),f.x),lerp(hash(i+float2(0,1)),hash(i+1),f.x),f.y);}
   half4 frag(V i):SV_Target {
    float3 d=normalize(i.direction);
    float3 col=lerp(_Horizon.rgb,_Zenith.rgb,pow(saturate(d.y),.55));
    col=lerp(col,_Horizon.rgb,_Haze*exp(-abs(d.y)*12));
    float sun=saturate(dot(d,GetMainLight().direction));
    col+=float3(1,.69,.3)*(pow(sun,180)*.3+pow(sun,1800)*.75);
    float2 p=d.xz/max(.15,d.y)*1.2+float2(_Time.y*_CloudSpeed,0);
    float n=noise(p)*.58+noise(p*2.1)*.28+noise(p*4.3)*.14;
    float cloud=smoothstep(1-_Coverage-_Softness,1-_Coverage+_Softness,n)*smoothstep(.015,.18,d.y);
    float3 cloudColor=lerp(float3(.52,.65,.76),float3(1,.96,.85),smoothstep(.35,.8,n));
    col=lerp(col,cloudColor,cloud*.9);
    return half4(col*_Exposure,1);
   }
   ENDHLSL
  }
 }
}
