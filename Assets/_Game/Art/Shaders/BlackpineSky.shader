Shader "Last Light/Blackpine night sky"
{
    Properties
    {
        _Horizon("Horizon", Color) = (.055,.095,.12,1)
        _Zenith("Zenith", Color) = (.006,.018,.035,1)
        _MoonDirection("Moon direction", Vector) = (.3,.6,.5,0)
    }
    SubShader
    {
        Tags { "Queue"="Background" "RenderType"="Background" "PreviewType"="Skybox" "RenderPipeline"="UniversalPipeline" }
        Cull Off ZWrite Off
        Pass
        {
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            CBUFFER_START(UnityPerMaterial)
                float4 _Horizon, _Zenith, _MoonDirection;
            CBUFFER_END
            struct Attributes { float4 positionOS : POSITION; };
            struct Varyings { float4 positionCS : SV_POSITION; float3 direction : TEXCOORD0; };
            Varyings Vert(Attributes input)
            {
                Varyings output;
                output.positionCS = TransformObjectToHClip(input.positionOS.xyz);
                output.direction = input.positionOS.xyz;
                return output;
            }
            half4 Frag(Varyings input) : SV_Target
            {
                float3 d = normalize(input.direction);
                float height = pow(saturate(d.y), .55);
                float3 color = lerp(_Horizon.rgb, _Zenith.rgb, height);
                float alignment = dot(d, normalize(_MoonDirection.xyz));
                float disk = smoothstep(.99965,.99988,alignment);
                float halo = pow(saturate(alignment),180)*.06;
                color += float3(.55,.68,.78)*(disk*.58+halo*.4);
                float2 uv = float2(atan2(d.x,d.z)/6.283185+0.5,asin(d.y)/3.14159+0.5)*450;
                float2 cell = floor(uv);
                float hash = frac(sin(dot(cell,float2(127.1,311.7)))*43758.5453);
                float star = (1-smoothstep(.015,.09,length(frac(uv)-.5)))*step(.988,hash)*smoothstep(.1,.45,d.y);
                color += star*float3(.035,.042,.048);
                return half4(color,1);
            }
            ENDHLSL
        }
    }
}
