Shader "YesterdayMap/Environment/MeshyFurniturePBR"
{
    Properties
    {
        _MainTex ("Base Color", 2D) = "white" {}
        _MetallicTex ("Metallic", 2D) = "black" {}
        [Normal] _BumpMap ("Normal", 2D) = "bump" {}
        _RoughnessTex ("Roughness", 2D) = "white" {}
        _Color ("Tint", Color) = (1, 1, 1, 1)
        _NormalStrength ("Normal Strength", Range(0, 2)) = 0.85
        _MetallicStrength ("Metallic Strength", Range(0, 1)) = 0.8
        _SmoothnessStrength ("Smoothness Strength", Range(0, 1)) = 0.72
    }

    SubShader
    {
        Tags { "RenderType"="Opaque" "Queue"="Geometry" }
        LOD 300
        Cull Off

        CGPROGRAM
        #pragma surface surf Standard fullforwardshadows addshadow
        #pragma target 3.0

        sampler2D _MainTex;
        sampler2D _MetallicTex;
        sampler2D _BumpMap;
        sampler2D _RoughnessTex;
        fixed4 _Color;
        half _NormalStrength;
        half _MetallicStrength;
        half _SmoothnessStrength;

        struct Input { float2 uv_MainTex; };

        void surf(Input IN, inout SurfaceOutputStandard o)
        {
            fixed4 baseColor = tex2D(_MainTex, IN.uv_MainTex) * _Color;
            half metallic = tex2D(_MetallicTex, IN.uv_MainTex).r;
            half roughness = tex2D(_RoughnessTex, IN.uv_MainTex).r;
            fixed3 normal = UnpackNormal(tex2D(_BumpMap, IN.uv_MainTex));
            normal.xy *= _NormalStrength;

            o.Albedo = baseColor.rgb;
            o.Normal = normalize(normal);
            o.Metallic = saturate(metallic * _MetallicStrength);
            o.Smoothness = saturate((1.0h - roughness) * _SmoothnessStrength);
            o.Occlusion = 1.0h;
            o.Alpha = 1.0h;
        }
        ENDCG
    }
    FallBack "Standard"
}