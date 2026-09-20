Shader "YesterdayMap/UI/ExplorationLoadingGauge"
{
    Properties
    {
        [PerRendererData] _MainTex ("Source Artwork", 2D) = "white" {}
        _TrackColor ("Track Color", Color) = (0.17, 0.155, 0.135, 1)
        _FillColor ("Fill Color", Color) = (0.42, 0.41, 0.39, 1)
        _GaugeRect ("Gauge Rect", Vector) = (0.046, 0.0934, 0.442, 0.048)
        _Progress ("Progress", Range(0, 1)) = 0
        _MaskLow ("Mask Low", Range(0, 1)) = 0.018
        _MaskHigh ("Mask High", Range(0, 1)) = 0.12
        _ContrastLow ("Contrast Low", Range(0, 1)) = 0.008
        _ContrastHigh ("Contrast High", Range(0, 1)) = 0.06
        _BaselineY ("Baseline Y In Gauge Rect", Range(0, 1)) = 0.32
        _BaselineHalfThickness ("Baseline Half Thickness", Range(0.01, 0.2)) = 0.05
        _PulseStart ("Pulse Start", Range(0, 1)) = 0.90
        _PulseEnd ("Pulse End", Range(0, 1)) = 0.96

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
        Tags
        {
            "Queue" = "Transparent"
            "IgnoreProjector" = "True"
            "RenderType" = "Transparent"
            "PreviewType" = "Plane"
            "CanUseSpriteAtlas" = "True"
        }

        Stencil
        {
            Ref [_Stencil]
            Comp [_StencilComp]
            Pass [_StencilOp]
            ReadMask [_StencilReadMask]
            WriteMask [_StencilWriteMask]
        }

        Cull Off
        Lighting Off
        ZWrite Off
        ZTest [unity_GUIZTestMode]
        Blend SrcAlpha OneMinusSrcAlpha
        ColorMask [_ColorMask]

        Pass
        {
            Name "GaugeOverlay"

            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 2.0
            #pragma multi_compile_local _ UNITY_UI_CLIP_RECT
            #pragma multi_compile_local _ UNITY_UI_ALPHACLIP

            #include "UnityCG.cginc"
            #include "UnityUI.cginc"

            struct appdata_t
            {
                float4 vertex : POSITION;
                float4 color : COLOR;
                float2 texcoord : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct v2f
            {
                float4 vertex : SV_POSITION;
                fixed4 color : COLOR;
                float2 texcoord : TEXCOORD0;
                float4 worldPosition : TEXCOORD1;
                UNITY_VERTEX_OUTPUT_STEREO
            };

            sampler2D _MainTex;
            float4 _MainTex_TexelSize;
            fixed4 _TrackColor;
            fixed4 _FillColor;
            float4 _GaugeRect;
            float _Progress;
            float _MaskLow;
            float _MaskHigh;
            float _ContrastLow;
            float _ContrastHigh;
            float _BaselineY;
            float _BaselineHalfThickness;
            float _PulseStart;
            float _PulseEnd;
            float4 _ClipRect;

            v2f vert(appdata_t input)
            {
                v2f output;
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);
                output.worldPosition = input.vertex;
                output.vertex = UnityObjectToClipPos(input.vertex);
                output.texcoord = input.texcoord;
                output.color = input.color;
                return output;
            }

            float GaugeLuminance(fixed3 color)
            {
                return dot(color, fixed3(0.2126, 0.7152, 0.0722));
            }

            fixed4 frag(v2f input) : SV_Target
            {
                float2 uv = input.texcoord;
                float2 rectMin = _GaugeRect.xy;
                float2 rectMax = rectMin + _GaugeRect.zw;
                float inside = step(rectMin.x, uv.x) * step(uv.x, rectMax.x) *
                               step(rectMin.y, uv.y) * step(uv.y, rectMax.y);

                fixed4 source = tex2D(_MainTex, uv);
                float sourceLuma = GaugeLuminance(source.rgb);
                float2 sampleStep = _MainTex_TexelSize.xy * 6.0;
                float upperLuma = GaugeLuminance(tex2D(_MainTex, uv + float2(0, sampleStep.y)).rgb);
                float lowerLuma = GaugeLuminance(tex2D(_MainTex, uv - float2(0, sampleStep.y)).rgb);
                float leftLuma = GaugeLuminance(tex2D(_MainTex, uv - float2(sampleStep.x, 0)).rgb);
                float rightLuma = GaugeLuminance(tex2D(_MainTex, uv + float2(sampleStep.x, 0)).rgb);
                float verticalContrast = sourceLuma - (upperLuma + lowerLuma) * 0.5;
                float horizontalContrast = sourceLuma - (leftLuma + rightLuma) * 0.5;
                float localContrast = max(verticalContrast, horizontalContrast);

                float brightnessMask = smoothstep(_MaskLow, _MaskHigh, sourceLuma);
                float contrastMask = smoothstep(_ContrastLow, _ContrastHigh, localContrast);
                float sourceMask = saturate(brightnessMask * contrastMask * 3.0);

                float2 gaugeUv = (uv - rectMin) / max(_GaugeRect.zw, float2(0.0001, 0.0001));
                float baselineDistance = abs(gaugeUv.y - _BaselineY);
                float baselineMask = 1.0 - smoothstep(
                    _BaselineHalfThickness * 0.65,
                    _BaselineHalfThickness,
                    baselineDistance);
                float pulseFade = 0.015;
                float pulseRegion = smoothstep(_PulseStart, _PulseStart + pulseFade, gaugeUv.x) *
                                    (1.0 - smoothstep(_PulseEnd - pulseFade, _PulseEnd, gaugeUv.x));

                // The source contains a baked partial fill. A fitted baseline
                // completes its dark tail, while source contrast preserves the
                // exact ECG pulse instead of replacing it with a generic line.
                // Use the fitted baseline for every straight section so artwork
                // variations cannot make one loading screen look thicker than
                // another. Blend to the source mask only where the ECG bends.
                float pulseMask = max(sourceMask, baselineMask * 0.15);
                float gaugeMask = inside * lerp(baselineMask, pulseMask, pulseRegion);

                float progressEdge = rectMin.x + _GaugeRect.z * saturate(_Progress);
                float filled = step(uv.x, progressEdge) * step(0.0001, _Progress);
                fixed4 target = lerp(_TrackColor, _FillColor, filled);

                // Preserve a small amount of the source hue and texture while
                // normalizing the baked partial bar into track/fill states.
                float targetLuma = max(GaugeLuminance(target.rgb), 0.001);
                fixed3 normalizedSource = saturate(source.rgb * (targetLuma / max(sourceLuma, 0.001)));
                fixed3 styledColor = lerp(target.rgb, normalizedSource, 0.35);
                fixed4 output = fixed4(styledColor, gaugeMask * target.a) * input.color;

                #ifdef UNITY_UI_CLIP_RECT
                output.a *= UnityGet2DClipping(input.worldPosition.xy, _ClipRect);
                #endif

                #ifdef UNITY_UI_ALPHACLIP
                clip(output.a - 0.001);
                #endif

                return output;
            }
            ENDCG
        }
    }
}
