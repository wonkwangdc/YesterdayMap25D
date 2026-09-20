Shader "YesterdayMap/WorldEventOutline"
{
    Properties
    {
        _OutlineColor ("Outline Color", Color) = (1, 1, 1, 1)
        _OutlineAlpha ("Outline Alpha", Range(0, 1)) = 0
        _OutlineWidth ("Outline Width (Pixels)", Range(0, 12)) = 4
    }

    SubShader
    {
        Tags
        {
            "Queue" = "Transparent+20"
            "RenderType" = "Transparent"
            "IgnoreProjector" = "True"
        }

        Pass
        {
            Name "EventOutline"
            Cull Front
            ZWrite Off
            ZTest LEqual
            Blend SrcAlpha One

            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 3.0

            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                float3 normal : NORMAL;
            };

            struct v2f
            {
                float4 position : SV_POSITION;
            };

            fixed4 _OutlineColor;
            float _OutlineAlpha;
            float _OutlineWidth;

            v2f vert(appdata input)
            {
                v2f output;
                float4 clipPosition = UnityObjectToClipPos(input.vertex);
                float3 viewNormal = normalize(mul(
                    (float3x3)UNITY_MATRIX_IT_MV,
                    input.normal));
                float2 direction = viewNormal.xy;
                float directionLength = max(length(direction), 0.0001);
                direction /= directionLength;

                float2 pixelToClip = float2(
                    2.0 / _ScreenParams.x,
                    2.0 / _ScreenParams.y);
                clipPosition.xy += direction * pixelToClip *
                    _OutlineWidth * clipPosition.w;
                output.position = clipPosition;
                return output;
            }

            fixed4 frag(v2f input) : SV_Target
            {
                return fixed4(
                    _OutlineColor.rgb,
                    _OutlineColor.a * _OutlineAlpha);
            }
            ENDCG
        }
    }

    Fallback Off
}
