Shader "Last Light/Intro interior"
{
    Properties
    {
        _BaseMap("Wear and colour",2D)="white"{}
        _BaseColor("Tint",Color)=(.3,.32,.3,1)
        _BumpMap("Surface normal",2D)="bump"{}
        _BumpScale("Normal strength",Range(0,2))=.5
        _Smoothness("Smoothness",Range(0,1))=.15
        _Metallic("Metallic",Range(0,1))=0
        _Pattern("Grain / woven fabric",Range(0,1))=0
        _GrainScale("Grain density",Float)=120
        _GrainDepth("Relief",Range(0,.3))=.12
        _Cull("Cull",Float)=2
    }
    SubShader
    {
        Tags {"RenderPipeline"="UniversalPipeline" "RenderType"="Opaque"}
        Pass
        {
            Name "ForwardLit"
            Tags {"LightMode"="UniversalForward"}
            Cull [_Cull]
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #pragma multi_compile_instancing
            #pragma multi_compile_fog
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE _MAIN_LIGHT_SHADOWS_SCREEN
            #pragma multi_compile _ _ADDITIONAL_LIGHTS
            #pragma multi_compile_fragment _ _ADDITIONAL_LIGHT_SHADOWS
            #pragma multi_compile_fragment _ _SHADOWS_SOFT
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
            TEXTURE2D(_BaseMap);SAMPLER(sampler_BaseMap);
            TEXTURE2D(_BumpMap);SAMPLER(sampler_BumpMap);
            CBUFFER_START(UnityPerMaterial)
            float4 _BaseMap_ST,_BaseColor;
            float _BumpScale,_Smoothness,_Metallic,_Pattern,_GrainScale,_GrainDepth;
            CBUFFER_END
            struct A {float4 p:POSITION;float3 n:NORMAL;float4 t:TANGENT;float2 uv:TEXCOORD0;UNITY_VERTEX_INPUT_INSTANCE_ID};
            struct V {float4 p:SV_POSITION;float2 uv:TEXCOORD0;float3 world:TEXCOORD1;half3 n:TEXCOORD2;half4 tangent:TEXCOORD3;float4 shadow:TEXCOORD4;half fog:TEXCOORD5;UNITY_VERTEX_INPUT_INSTANCE_ID UNITY_VERTEX_OUTPUT_STEREO};
            V Vert(A a)
            {
                V v=(V)0;UNITY_SETUP_INSTANCE_ID(a);UNITY_TRANSFER_INSTANCE_ID(a,v);UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(v);
                VertexPositionInputs p=GetVertexPositionInputs(a.p.xyz);VertexNormalInputs n=GetVertexNormalInputs(a.n,a.t);
                v.p=p.positionCS;v.world=p.positionWS;v.uv=TRANSFORM_TEX(a.uv,_BaseMap);v.n=n.normalWS;
                v.tangent=half4(n.tangentWS,a.t.w*GetOddNegativeScale());v.shadow=GetShadowCoord(p);v.fog=ComputeFogFactor(p.positionCS.z);return v;
            }
            float Hash(float2 p){return frac(sin(dot(p,float2(127.1,311.7)))*43758.5453);}
            float Noise(float2 p)
            {
                float2 cell=floor(p),f=frac(p);f=f*f*(3-2*f);
                return lerp(lerp(Hash(cell),Hash(cell+float2(1,0)),f.x),lerp(Hash(cell+float2(0,1)),Hash(cell+1),f.x),f.y);
            }
            float Relief(float2 p)
            {
                float grain=.72*Noise(p)+.28*Noise(p*2.4);
                float weave=.5+.5*sin(p.x*6.283185)*sin(p.y*6.283185);
                return lerp(grain,weave,_Pattern);
            }
            half4 Frag(V v):SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(v);UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(v);
                float2 p=v.uv*_GrainScale;float aa=saturate(1-max(fwidth(p.x),fwidth(p.y))*1.4);
                float h=Relief(p),dx=(Relief(p+float2(.04,0))-h)/.04,dy=(Relief(p+float2(0,.04))-h)/.04;
                half3 baseNormal=UnpackNormalScale(SAMPLE_TEXTURE2D(_BumpMap,sampler_BumpMap,v.uv),_BumpScale);
                half3 detail=normalize(half3(baseNormal.xy-half2(dx,dy)*_GrainDepth*aa,baseNormal.z));
                half3 n=normalize(v.n),t=normalize(v.tangent.xyz),b=cross(n,t)*v.tangent.w;
                InputData d=(InputData)0;d.positionWS=v.world;d.normalWS=normalize(t*detail.x+b*detail.y+n*detail.z);
                d.viewDirectionWS=GetWorldSpaceNormalizeViewDir(v.world);d.shadowCoord=v.shadow;d.shadowMask=1;d.bakedGI=SampleSH(d.normalWS);d.normalizedScreenSpaceUV=GetNormalizedScreenSpaceUV(v.p);
                SurfaceData s=(SurfaceData)0;s.albedo=SAMPLE_TEXTURE2D(_BaseMap,sampler_BaseMap,v.uv).rgb*_BaseColor.rgb*lerp(1,.85+.3*h,aa);
                s.alpha=1;s.metallic=_Metallic;s.smoothness=saturate(_Smoothness+(h-.5)*.12*aa);s.occlusion=1;s.normalTS=detail;
                half4 colour=UniversalFragmentPBR(d,s);colour.rgb=MixFog(colour.rgb,v.fog);return colour;
            }
            ENDHLSL
        }
        UsePass "Universal Render Pipeline/Lit/ShadowCaster"
        UsePass "Universal Render Pipeline/Lit/DepthOnly"
    }
}
