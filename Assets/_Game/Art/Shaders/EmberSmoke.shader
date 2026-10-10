Shader "Last Light/Embers and smoke"
{
    Properties { _Mode("Smoke",Float)=0 _DstBlend("Destination blend",Float)=1 }
    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" "RenderPipeline"="UniversalPipeline" }
        Pass
        {
            Blend SrcAlpha [_DstBlend]
            ZWrite Off Cull Off
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_fog
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            struct A { float4 position:POSITION;float4 color:COLOR;float2 uv:TEXCOORD0; };
            struct V { float4 position:SV_POSITION;float4 color:COLOR;float2 uv:TEXCOORD0;float fog:TEXCOORD1; };
            CBUFFER_START(UnityPerMaterial)
            float _Mode;
            CBUFFER_END
            float _NortonTime;
            V vert(A i){V o;o.position=TransformObjectToHClip(i.position.xyz);o.color=i.color;o.uv=i.uv;o.fog=ComputeFogFactor(o.position.z);return o;}
            half4 frag(V i):SV_Target
            {
                float2 p=i.uv*2-1;float r=dot(p,p);
                float noise=.78+.22*sin(p.x*17+sin(p.y*13+_NortonTime*1.4)*2)*sin(p.y*11-_NortonTime*2);
                float alpha=saturate(1-r)*saturate(1-r)*noise*i.color.a;
                float3 c=i.color.rgb*(_Mode>.5?1:1.8+2*saturate(1-r));
                if(_Mode<.5)
                {
                    float width=lerp(.85,.08,i.uv.y)+.08*sin(i.uv.y*19+_NortonTime*8);
                    float bend=.14*sin(i.uv.y*9-_NortonTime*6);
                    float tongue=saturate(1-abs(p.x+bend)/max(.05,width));
                    alpha=tongue*tongue*smoothstep(0,.12,i.uv.y)*(1-i.uv.y)*i.color.a*1.8;
                    c=lerp(float3(2.6,1.5,.35),i.color.rgb*2.8,i.uv.y);
                }
                c=MixFog(c,i.fog);return half4(c,alpha);
            }
            ENDHLSL
        }
    }
}
