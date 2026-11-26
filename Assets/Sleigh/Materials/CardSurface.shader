// Inspired by Cyanilux/Cards' noise dissolve and periodic Shine graph.
// UI adaptation uses card-local UV1, including the existing bitmap font layers.
Shader "Sleigh/UI Card Surface"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}
        _Color ("Tint", Color) = (1,1,1,1)
        _ShinePeriod ("Shine Period Seconds", Float) = 4.8
        _ShineWidth ("Shine Width", Range(.01,.4)) = .14
        _ShineStrength ("Shine Strength", Range(0,1)) = .48
        _DissolveColor ("Dissolve Edge", Color) = (1,.62,.18,1)
        [HideInInspector] _PreviewTime ("Preview Time", Float) = -1
        _StencilComp ("Stencil Comparison", Float) = 8
        _Stencil ("Stencil ID", Float) = 0
        _StencilOp ("Stencil Operation", Float) = 0
        _StencilWriteMask ("Stencil Write Mask", Float) = 255
        _StencilReadMask ("Stencil Read Mask", Float) = 255
        _ColorMask ("Color Mask", Float) = 15
        [Toggle(UNITY_UI_ALPHACLIP)] _UseUIAlphaClip ("Use Alpha Clip", Float) = 0
    }
    SubShader
    {
        Tags { "Queue"="Transparent" "IgnoreProjector"="True" "RenderType"="Transparent" "CanUseSpriteAtlas"="True" }
        Stencil { Ref [_Stencil] Comp [_StencilComp] Pass [_StencilOp] ReadMask [_StencilReadMask] WriteMask [_StencilWriteMask] }
        Cull Off Lighting Off ZWrite Off ZTest [unity_GUIZTestMode]
        Blend SrcAlpha OneMinusSrcAlpha
        ColorMask [_ColorMask]
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 3.0
            #pragma multi_compile_local _ UNITY_UI_CLIP_RECT
            #pragma multi_compile_local _ UNITY_UI_ALPHACLIP
            #include "UnityCG.cginc"
            #include "UnityUI.cginc"
            struct appdata
            {
                float4 vertex : POSITION;
                fixed4 color : COLOR;
                float2 texcoord : TEXCOORD0;
                float4 surface : TEXCOORD1;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };
            struct v2f
            {
                float4 vertex : SV_POSITION;
                fixed4 color : COLOR;
                float2 texcoord : TEXCOORD0;
                float4 worldPosition : TEXCOORD1;
                float4 surface : TEXCOORD2;
                UNITY_VERTEX_OUTPUT_STEREO
            };
            sampler2D _MainTex;
            fixed4 _Color, _TextureSampleAdd, _DissolveColor;
            float4 _ClipRect;
            float _ShinePeriod, _ShineWidth, _ShineStrength, _PreviewTime;
            v2f vert(appdata v)
            {
                v2f o;
                UNITY_SETUP_INSTANCE_ID(v);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);
                o.worldPosition = v.vertex; o.vertex = UnityObjectToClipPos(v.vertex);
                o.texcoord = v.texcoord; o.color = v.color * _Color; o.surface = v.surface;
                return o;
            }
            float Hash(float2 p) { return frac(sin(dot(p, float2(127.1,311.7))) * 43758.5453); }
            float Noise(float2 p)
            {
                float2 cell = floor(p), f = frac(p); f = f * f * (3 - 2 * f);
                return lerp(lerp(Hash(cell), Hash(cell + float2(1,0)), f.x),
                    lerp(Hash(cell + float2(0,1)), Hash(cell + 1), f.x), f.y);
            }
            fixed4 frag(v2f i) : SV_Target
            {
                fixed4 c = (tex2D(_MainTex, i.texcoord) + _TextureSampleAdd) * i.color;
                float2 uv = i.surface.xy;
                float progress = i.surface.z;
                if (progress > 0)
                {
                    clip(.9999 - progress);
                    // Quantize to the card's logical pixels, keeping a torn-paper edge.
                    float2 pixel = floor(uv * float2(66,88)) / float2(66,88);
                    float field = saturate(pixel.y * .45 + Noise(pixel * float2(9,12)) * .55);
                    float remaining = field - lerp(-.12, 1.12, progress);
                    clip(remaining);
                    float edge = 1 - smoothstep(0, .075, remaining);
                    c.rgb = lerp(c.rgb, _DissolveColor.rgb, edge * .95);
                }
                else if (i.surface.w >= 0)
                {
                    float clock = _PreviewTime >= 0 ? _PreviewTime : _Time.y;
                    float cycle = frac(clock / max(.5, _ShinePeriod) + i.surface.w);
                    float center = lerp(-.3, 1.75, saturate(cycle / .26));
                    float band = 1 - smoothstep(_ShineWidth * .2, _ShineWidth, abs(uv.x * .85 + uv.y * .55 - center));
                    c.rgb = lerp(c.rgb, fixed3(1,1,1), band * _ShineStrength * step(cycle, .26));
                }
                #ifdef UNITY_UI_CLIP_RECT
                c.a *= UnityGet2DClipping(i.worldPosition.xy, _ClipRect);
                #endif
                #ifdef UNITY_UI_ALPHACLIP
                clip(c.a - .001);
                #endif
                return c;
            }
            ENDCG
        }
    }
}
