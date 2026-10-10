Shader "Last Light/Blood on lens"
{
    Properties { _MainTex("Texture",2D)="white"{} _Color("Tint",Color)=(1,1,1,1) }
    SubShader
    {
        Tags { "Queue"="Overlay" "RenderType"="Transparent" "RenderPipeline"="UniversalPipeline" }
        Blend SrcAlpha OneMinusSrcAlpha Cull Off ZWrite Off ZTest Always
        Pass
        {
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            CBUFFER_START(UnityPerMaterial)
            half4 _Color;
            CBUFFER_END
            struct A {float4 positionOS:POSITION;float2 uv:TEXCOORD0;half4 color:COLOR;};
            struct V {float4 positionCS:SV_POSITION;float2 uv:TEXCOORD0;half4 color:COLOR;};
            V Vert(A i){V o;o.positionCS=TransformObjectToHClip(i.positionOS.xyz);o.uv=i.uv;o.color=i.color*_Color;return o;}
            float H(float2 p){return frac(sin(dot(p,float2(127.1,311.7)))*43758.54);}
            half4 Frag(V i):SV_Target
            {
                float2 p=(i.uv-.5)*float2(1.15,1);
                float edge=smoothstep(.26,.6,length(p)+.055*sin(i.uv.x*38)*sin(i.uv.y*27));
                float2 warped=i.uv+float2(sin(i.uv.y*19),sin(i.uv.x*23))*.035;
                float2 cell=floor(warped*float2(16,11)),f=frac(warped*float2(16,11));
                float random=H(cell);float2 centre=float2(.2+.6*H(cell+1),.3+.4*H(cell+3));
                float radius=.025+random*random*.16;
                float droplet=1-smoothstep(radius,radius+.027,length((f-centre)*float2(1,1.3+random)));
                droplet*=step(.65,random)*smoothstep(.14,.42,length(p));
                float trail=pow(saturate(1-abs(f.x-centre.x)*23),2)*step(f.y,centre.y)*step(.86,random)*edge;
                float a=saturate(edge*.9+droplet*.8+trail*.5)*i.color.a;
                half3 blood=lerp(half3(.023,.0007,.0015),half3(.115,.002,.004),saturate(droplet*.38+edge*.3));
                return half4(blood,a);
            }
            ENDHLSL
        }
    }
}
