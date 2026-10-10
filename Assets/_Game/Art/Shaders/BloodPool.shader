Shader "Last Light/Blood soaked soil"
{
    Properties { _BaseColor("Blood",Color)=(.07,.0018,.003,1) }
    SubShader
    {
        Tags {"Queue"="Transparent-20" "RenderType"="Transparent" "RenderPipeline"="UniversalPipeline"}
        Pass
        {
            Tags {"LightMode"="UniversalForward"}
            ZWrite Off Blend SrcAlpha OneMinusSrcAlpha Cull Off
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE
            #pragma multi_compile _ _ADDITIONAL_LIGHTS
            #pragma multi_compile_fragment _ _ADDITIONAL_LIGHT_SHADOWS
            #pragma multi_compile_fog
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
            CBUFFER_START(UnityPerMaterial) half4 _BaseColor; CBUFFER_END
            struct A {float4 vertex:POSITION;float2 uv:TEXCOORD0;};
            struct V {float4 position:SV_POSITION;float2 uv:TEXCOORD0;float3 world:TEXCOORD1;float4 shadow:TEXCOORD2;half fog:TEXCOORD3;};
            V vert(A a){V o;VertexPositionInputs p=GetVertexPositionInputs(a.vertex.xyz);o.position=p.positionCS;o.world=p.positionWS;o.uv=a.uv;o.shadow=GetShadowCoord(p);o.fog=ComputeFogFactor(p.positionCS.z);return o;}
            half4 frag(V v):SV_Target
            {
                float2 p=(v.uv-.5)*2;float turbulence=sin(p.x*19+sin(p.y*13))*sin(p.y*23)*.06+sin(p.x*59+p.y*43)*.017;
                half a=1-smoothstep(.72,.99,length(p)+turbulence);clip(a-.005);
                InputData d=(InputData)0;d.positionWS=v.world;d.normalWS=normalize(half3(.007*sin(p.x*55),1,.007*sin(p.y*45)));d.viewDirectionWS=GetWorldSpaceNormalizeViewDir(v.world);d.shadowCoord=v.shadow;d.shadowMask=1;d.bakedGI=SampleSH(d.normalWS);d.normalizedScreenSpaceUV=GetNormalizedScreenSpaceUV(v.position);
                SurfaceData s=(SurfaceData)0;s.albedo=_BaseColor.rgb*(.65+.35*a);s.alpha=1;s.smoothness=.58;s.occlusion=1;s.normalTS=half3(0,0,1);
                half3 color=UniversalFragmentPBR(d,s).rgb;color/=1+max(0,max(color.r,max(color.g,color.b))-.3)*2;
                color*=half3(1,.28,.35);
                return half4(MixFog(color,v.fog),a*.92);
            }
            ENDHLSL
        }
    }
}
