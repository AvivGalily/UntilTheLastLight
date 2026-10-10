Shader "Last Light/Crawler skin"
{
    Properties
    {
        _BaseMap("Skin",2D)="white"{}
        _BaseColor("Tint",Color)=(1,1,1,1)
        _BumpMap("Skin pores",2D)="bump"{}
        _BumpScale("Normal strength",Range(0,2))=.75
        _AnatomyMap("Rest-pose human skin atlas",2D)="white"{}
        _UseSkinAtlas("Use anatomical UV2 atlas",Range(0,1))=0
        _WorldSkin("Static human skin projection",Range(0,1))=0
        _CadaverDecay("Cadaver discoloration",Range(0,1))=0
        _CinderDecay("Necrotic veins and mottled tissue",Range(0,1))=0
        _CharAmount("Burned tissue",Range(0,1))=0
        _BurnAway("Ash dissolution",Range(0,1))=0
        _FogFade("Concealment in distant mist",Range(0,1))=0
        _SkinTileScale("Human skin repeats per metre",Float)=3.2
        _MetallicGlossMap("Skin mask",2D)="white"{}
        _OcclusionMap("Occlusion",2D)="white"{}
        _OcclusionStrength("Occlusion strength",Range(0,1))=.55
        _Smoothness("Smoothness",Range(0,1))=1
        _SkinSpecular("Dielectric reflectance",Range(.01,.08))=.04
        _Subsurface("Warm skin light diffusion",Range(0,.5))=0
        _HighlightRolloff("Close flashlight highlight rolloff",Range(0,3))=1.4
        _IlluminanceRolloff("Human surface close-light response",Range(0,3))=0
        _Cutoff("Cutoff",Range(0,1))=.5
        _AlphaClip("Hair cutout",Float)=0
        _BloodAmount("Blood and bruising",Range(0,1))=0
        _Lacerations("Visible cuts",Range(0,1))=0
        _Wound0("Wound centre / radius",Vector)=(0,0,0,.2)
        _Wound1("Secondary wound / radius",Vector)=(0,0,0,.1)
    }
    SubShader
    {
        Tags{"RenderType"="Opaque" "RenderPipeline"="UniversalPipeline" "Queue"="Geometry"}
        HLSLINCLUDE
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
        CBUFFER_START(UnityPerMaterial)
        float4 _BaseMap_ST;half4 _BaseColor;
        half _BumpScale,_OcclusionStrength,_Smoothness,_HighlightRolloff,_Cutoff;
        half _UseSkinAtlas,_SkinSpecular,_Subsurface;
        half _WorldSkin,_SkinTileScale,_CadaverDecay,_CinderDecay,_CharAmount,_BurnAway,_FogFade;
        half _IlluminanceRolloff;
        half _BloodAmount,_Lacerations,_AlphaClip;float4 _Wound0,_Wound1;
        CBUFFER_END
        TEXTURE2D(_BaseMap);SAMPLER(sampler_BaseMap);
        TEXTURE2D(_BumpMap);SAMPLER(sampler_BumpMap);
        TEXTURE2D(_AnatomyMap);SAMPLER(sampler_AnatomyMap);
        TEXTURE2D(_MetallicGlossMap);SAMPLER(sampler_MetallicGlossMap);
        TEXTURE2D(_OcclusionMap);SAMPLER(sampler_OcclusionMap);
        float Hash(float2 p){return frac(sin(dot(p,float2(127.1,311.7)))*43758.5453);}
        float Noise(float2 p)
        {
            float2 cell=floor(p),f=frac(p);f=f*f*(3-2*f);
            return lerp(lerp(Hash(cell),Hash(cell+float2(1,0)),f.x),lerp(Hash(cell+float2(0,1)),Hash(cell+1),f.x),f.y);
        }
        float Ash(float2 uv){return Noise(uv*57)*.65+Noise(uv*137)*.35;}
        void ClipSurface(float2 uv)
        {
            if(_AlphaClip>.5)clip(SAMPLE_TEXTURE2D(_BaseMap,sampler_BaseMap,uv).a-_Cutoff);
            if(_BurnAway>0)clip(Ash(uv)-_BurnAway);
            if(_FogFade>0)clip(Ash(uv)-_FogFade);
        }
        struct DepthInput{float4 positionOS:POSITION;float3 normalOS:NORMAL;float2 uv:TEXCOORD0;UNITY_VERTEX_INPUT_INSTANCE_ID};
        struct DepthOutput{float4 positionCS:SV_POSITION;float2 uv:TEXCOORD0;half3 normalWS:TEXCOORD1;UNITY_VERTEX_INPUT_INSTANCE_ID};
        DepthOutput DepthVert(DepthInput i)
        {
            DepthOutput o=(DepthOutput)0;UNITY_SETUP_INSTANCE_ID(i);UNITY_TRANSFER_INSTANCE_ID(i,o);
            o.positionCS=TransformObjectToHClip(i.positionOS.xyz);o.uv=TRANSFORM_TEX(i.uv,_BaseMap);o.normalWS=TransformObjectToWorldNormal(i.normalOS);return o;
        }
        ENDHLSL
        Pass
        {
            Name "ForwardLit"
            Tags{"LightMode"="UniversalForward"}
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE _MAIN_LIGHT_SHADOWS_SCREEN
            #pragma multi_compile _ _ADDITIONAL_LIGHTS_VERTEX _ADDITIONAL_LIGHTS
            #pragma multi_compile_fragment _ _ADDITIONAL_LIGHT_SHADOWS
            #pragma multi_compile_fragment _ _SHADOWS_SOFT
            #pragma multi_compile_fog
            #pragma multi_compile_instancing
            #define _SPECULAR_SETUP 1
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
            struct Attributes{float4 positionOS:POSITION;float3 normalOS:NORMAL;float4 tangentOS:TANGENT;float2 uv:TEXCOORD0;float2 atlasUV:TEXCOORD1;UNITY_VERTEX_INPUT_INSTANCE_ID};
            struct Varyings{float4 positionCS:SV_POSITION;float3 positionWS:TEXCOORD0;half3 normalWS:TEXCOORD1;half4 tangentWS:TEXCOORD2;float2 uv:TEXCOORD3;float4 shadow:TEXCOORD4;half fog:TEXCOORD5;float2 atlasUV:TEXCOORD6;UNITY_VERTEX_INPUT_INSTANCE_ID};
            Varyings Vert(Attributes input)
            {
                Varyings o=(Varyings)0;UNITY_SETUP_INSTANCE_ID(input);UNITY_TRANSFER_INSTANCE_ID(input,o);
                VertexPositionInputs p=GetVertexPositionInputs(input.positionOS.xyz);VertexNormalInputs n=GetVertexNormalInputs(input.normalOS,input.tangentOS);
                o.positionCS=p.positionCS;o.positionWS=p.positionWS;o.normalWS=n.normalWS;
                o.tangentWS=half4(n.tangentWS,input.tangentOS.w*GetOddNegativeScale());o.uv=TRANSFORM_TEX(input.uv,_BaseMap);
                o.atlasUV=input.atlasUV;o.shadow=GetShadowCoord(p);o.fog=ComputeFogFactor(p.positionCS.z);return o;
            }
            half3 SkinDiffusion(Light light,InputData d,half3 albedo,half thickness)
            {
                // A bounded approximation of epidermal diffusion. Use the same
                // distance/shadow attenuation as PBR: never glow in darkness or
                // send flashlight light through an opaque wall.
                half ndl=dot(d.normalWS,light.direction);
                half wrap=max(0,saturate((ndl+.35h)/1.35h)-saturate(ndl));
                half forward=pow(saturate(dot(-light.direction,d.viewDirectionWS)),4)*thickness*.12h;
                return albedo*half3(1,.32h,.22h)*light.color*(wrap+forward)*_Subsurface
                    *light.distanceAttenuation*light.shadowAttenuation;
            }
            half4 Frag(Varyings i):SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(i);
                ClipSurface(i.uv);float ash=Ash(i.uv);
                half3 normalTS=UnpackNormalScale(SAMPLE_TEXTURE2D(_BumpMap,sampler_BumpMap,i.uv),_BumpScale);
                half3 bitangent=i.tangentWS.w*cross(i.normalWS,i.tangentWS.xyz);
                InputData d=(InputData)0;d.positionWS=i.positionWS;
                d.normalWS=NormalizeNormalPerPixel(TransformTangentToWorld(normalTS,half3x3(i.tangentWS.xyz,bitangent,i.normalWS)));
                d.viewDirectionWS=GetWorldSpaceNormalizeViewDir(i.positionWS);d.shadowCoord=i.shadow;
                d.bakedGI=SampleSH(d.normalWS);d.shadowMask=half4(1,1,1,1);d.normalizedScreenSpaceUV=GetNormalizedScreenSpaceUV(i.positionCS);
                half4 mask=SAMPLE_TEXTURE2D(_MetallicGlossMap,sampler_MetallicGlossMap,i.uv);
                SurfaceData s=(SurfaceData)0;
                half3 tileColor=SAMPLE_TEXTURE2D(_BaseMap,sampler_BaseMap,i.uv).rgb;
                half3 atlasColor=SAMPLE_TEXTURE2D(_AnatomyMap,sampler_AnatomyMap,i.atlasUV).rgb;
                s.albedo=lerp(tileColor,atlasColor,_UseSkinAtlas)*_BaseColor.rgb;s.alpha=1;
                if(_WorldSkin>.5)
                {
                    half3 weights=pow(abs(normalize(i.normalWS)),4);weights/=max(.001h,weights.x+weights.y+weights.z);
                    float3 p=i.positionWS*_SkinTileScale;
                    half3 skinX=SAMPLE_TEXTURE2D(_BaseMap,sampler_BaseMap,p.zy).rgb;
                    half3 skinY=SAMPLE_TEXTURE2D(_BaseMap,sampler_BaseMap,p.xz).rgb;
                    half3 skinZ=SAMPLE_TEXTURE2D(_BaseMap,sampler_BaseMap,p.xy).rgb;
                    s.albedo=(skinX*weights.x+skinY*weights.y+skinZ*weights.z)*_BaseColor.rgb;
                }
                s.specular=_SkinSpecular.xxx;s.metallic=0;s.smoothness=mask.a*_Smoothness;
                if(_CadaverDecay>0)
                {
                    // Anatomical UVs hold the discoloration on animated skin.
                    float patch=.5+.5*sin(i.uv.x*61+sin(i.uv.y*47)*2)*sin(i.uv.y*53+i.uv.x*13);
                    float vein=1-smoothstep(.018,.09,abs(sin(i.uv.x*173+sin(i.uv.y*67)*2)));
                    float grey=dot(s.albedo,float3(.299,.587,.114));
                    half3 pallor=grey*half3(.68,.75,.72);
                    s.albedo=lerp(s.albedo,pallor,_CadaverDecay*.85);
                    s.albedo*=1-_CadaverDecay*(patch*.12+vein*.055);
                    s.albedo=lerp(s.albedo,s.albedo*half3(.78,.62,.74),_CadaverDecay*patch*.18);
                }
                if(_BloodAmount>0)
                {
                    float3 p=i.positionWS-_Wound0.xyz;
                    float grains=.5+.5*sin(p.x*211+sin(p.y*71)*4)*sin(p.z*193+p.y*101);
                    float organic=sin(p.x*47+sin(p.z*32))*sin(p.z*39+p.y*24)*.13+sin(p.y*91+p.x*60)*.06;
                    float wound=min(length(p)/max(.01,_Wound0.w),length(i.positionWS-_Wound1.xyz)/max(.01,_Wound1.w));
                    half bruise=(1-smoothstep(.65,1.7,wound+organic))*.6*_BloodAmount;
                    half stain=(1-smoothstep(.28,1.02,wound+organic))*_BloodAmount;
                    s.albedo=lerp(s.albedo,s.albedo*half3(.48,.25,.32),bruise);
                    s.albedo=lerp(s.albedo,lerp(half3(.025,.0015,.002),half3(.085,.003,.005),grains)*(.8+.2*abs(sin(p.x*71))),stain);
                    s.smoothness=lerp(s.smoothness,.58+.10*grains,stain);s.specular=lerp(s.specular,.035,stain);
                    float3 q0=p/max(.01,_Wound0.w),q1=(i.positionWS-_Wound1.xyz)/max(.01,_Wound1.w);
                    float3 q=length(q0)<length(q1)?q0:q1;
                    half slit=1-smoothstep(.018,.060,abs(q.y+q.x*.42+sin(q.x*17)*.025));
                    half edge=(1-smoothstep(.16,.78,abs(q.x)))*(1-smoothstep(.50,1.10,length(q)));
                    half cut=slit*edge*_Lacerations*_BloodAmount;
                    s.albedo=lerp(s.albedo,half3(.11,.0018,.006),cut);s.smoothness=lerp(s.smoothness,.62,cut);
                }
                if(_CinderDecay>0)
                {
                    float mottling=Noise(i.uv*37)*.65+Noise(i.uv*89)*.35;
                    float vein=1-smoothstep(.025,.12,abs(sin(i.uv.x*97+Noise(i.uv*19)*9+i.uv.y*21)));
                    float bruise=smoothstep(.45,.78,mottling);
                    s.albedo=lerp(s.albedo,s.albedo*half3(.53,.34,.46),bruise*_CinderDecay*.68);
                    s.albedo=lerp(s.albedo,s.albedo*half3(.42,.46,.58),vein*_CinderDecay*.35);
                    s.smoothness=lerp(s.smoothness,.38,bruise*_CinderDecay);
                }
                s.albedo=lerp(s.albedo,half3(.011,.007,.006)*(ash*.65+.35),_CharAmount);
                s.smoothness=lerp(s.smoothness,.08,_CharAmount);
                s.normalTS=normalTS;s.occlusion=lerp(1,SAMPLE_TEXTURE2D(_OcclusionMap,sampler_OcclusionMap,i.uv).g,_OcclusionStrength);
                half4 color=UniversalFragmentPBR(d,s);
                if(_IlluminanceRolloff>0)
                {
                    Light main=GetMainLight(d.shadowCoord,d.positionWS,d.shadowMask);
                    half irradiance=max(main.color.r,max(main.color.g,main.color.b))*main.distanceAttenuation*main.shadowAttenuation;
                    #if defined(_ADDITIONAL_LIGHTS)
                    uint lightCount=GetAdditionalLightsCount();
                    LIGHT_LOOP_BEGIN(lightCount)
                        Light source=GetAdditionalLight(lightIndex,d.positionWS,d.shadowMask);
                        irradiance+=max(source.color.r,max(source.color.g,source.color.b))*source.distanceAttenuation*source.shadowAttenuation;
                    LIGHT_LOOP_END
                    #endif
                    // Compress incident light, not the albedo pattern. Dark hair,
                    // closed eye creases and pigment retain their relative tones.
                    color.rgb/=1+max(0,irradiance-.8h)*_IlluminanceRolloff;
                }
                if(_Subsurface>0)
                {
                    color.rgb+=SkinDiffusion(GetMainLight(d.shadowCoord,d.positionWS,d.shadowMask),d,s.albedo,mask.b);
                    #if defined(_ADDITIONAL_LIGHTS)
                    uint count=GetAdditionalLightsCount();
                    LIGHT_LOOP_BEGIN(count)
                        color.rgb+=SkinDiffusion(GetAdditionalLight(lightIndex,d.positionWS,d.shadowMask),d,s.albedo,mask.b);
                    LIGHT_LOOP_END
                    #endif
                }
                // Preserve skin hue/detail under a close spotlight before the scene's ACES grade.
                half peak=max(color.r,max(color.g,color.b));color.rgb/=1+max(0,peak-.45h)*_HighlightRolloff;
                if(_BurnAway>0)color.rgb+=half3(1.7,.24,.015)*(1-smoothstep(0,.055,ash-_BurnAway))*(1-_BurnAway);
                color.rgb=MixFog(color.rgb,i.fog);return color;
            }
            ENDHLSL
        }
        UsePass "Universal Render Pipeline/Lit/ShadowCaster"
        Pass
        {
            Name "DepthOnly" Tags{"LightMode"="DepthOnly"} ZWrite On ColorMask R
            HLSLPROGRAM
            #pragma vertex DepthVert
            #pragma fragment DepthFrag
            #pragma multi_compile_instancing
            half DepthFrag(DepthOutput i):SV_Target{UNITY_SETUP_INSTANCE_ID(i);ClipSurface(i.uv);return 0;}
            ENDHLSL
        }
        Pass
        {
            Name "DepthNormals" Tags{"LightMode"="DepthNormals"} ZWrite On
            HLSLPROGRAM
            #pragma vertex DepthVert
            #pragma fragment NormalFrag
            #pragma multi_compile_instancing
            half4 NormalFrag(DepthOutput i):SV_Target{UNITY_SETUP_INSTANCE_ID(i);ClipSurface(i.uv);return half4(normalize(i.normalWS),0);}
            ENDHLSL
        }
    }
}
