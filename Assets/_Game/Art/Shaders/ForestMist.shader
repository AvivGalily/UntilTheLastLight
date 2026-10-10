Shader "Last Light/Low forest mist"
{
    Properties { _Tint("Mist tint",Color) = (.22,.26,.27,.4) }
    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" "RenderPipeline"="UniversalPipeline" }
        Blend SrcAlpha OneMinusSrcAlpha
        Cull Off ZWrite Off
        Pass
        {
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareDepthTexture.hlsl"
            CBUFFER_START(UnityPerMaterial)
                float4 _Tint;
            CBUFFER_END
            float _Dread;
            float _AtmosphereTime;
            float3 _MistBeamPosition,_MistBeamDirection;
            float4 _MistBeamParams;
            struct Attributes { float4 positionOS:POSITION; float2 uv:TEXCOORD0; };
            struct Varyings { float4 positionCS:SV_POSITION; float4 screen:TEXCOORD0; float2 uv:TEXCOORD1; float3 world:TEXCOORD2; };
            Varyings Vert(Attributes input)
            {
                Varyings output;
                output.world=TransformObjectToWorld(input.positionOS.xyz);
                output.positionCS=TransformWorldToHClip(output.world);
                output.screen=ComputeScreenPos(output.positionCS);output.uv=input.uv;
                return output;
            }
            float Hash(float2 p){return frac(sin(dot(p,float2(127.1,311.7)))*43758.5453);}
            float Noise(float2 p)
            {
                float2 i=floor(p),f=frac(p);f=f*f*(3-2*f);
                return lerp(lerp(Hash(i),Hash(i+float2(1,0)),f.x),lerp(Hash(i+float2(0,1)),Hash(i+1),f.x),f.y);
            }
            half4 Frag(Varyings input):SV_Target
            {
                float2 p=input.world.xz*.18+input.world.y*float2(.23,-.12)+float2(_AtmosphereTime*.025,_AtmosphereTime*.012);
                float cloud=Noise(p)*.5+Noise(p*2.03)*.30+Noise(p*4.07)*.15+Noise(p*8.11)*.05;
                float edge=saturate(1-length((input.uv-.5)*2));edge=smoothstep(0,.7,edge);
                float raw=SampleSceneDepth(input.screen.xy/input.screen.w);
                float depth=LinearEyeDepth(raw,_ZBufferParams);
                float intersection=saturate((depth-input.screen.w)*.9);
                float distance=length(input.world-_WorldSpaceCameraPos);
                float nearFade=smoothstep(2.5,7,distance),farFade=1-smoothstep(34,46,distance);
                float3 fromBeam=input.world-_MistBeamPosition;float beamDistance=length(fromBeam);
                float cone=smoothstep(_MistBeamParams.y,_MistBeamParams.y+.09,dot(normalize(fromBeam+1e-5),_MistBeamDirection));
                float glow=cone*saturate(1-beamDistance/max(.1,_MistBeamParams.x))*_MistBeamParams.z;
                half3 tint=_Tint.rgb+half3(.07,.064,.05)*glow;
                return half4(tint,edge*cloud*intersection*nearFade*farFade*_Tint.a*(1+_Dread*.45));
            }
            ENDHLSL
        }
    }
}
