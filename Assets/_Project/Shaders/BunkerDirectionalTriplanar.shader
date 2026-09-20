Shader "YesterdayMap/Environment/BunkerDirectionalTriplanar"
{
    Properties
    {
        _WallTex ("Wall Albedo", 2D) = "white" {}
        _WallNormal ("Wall Normal (Reserved)", 2D) = "bump" {}
        _FloorTex ("Floor Albedo", 2D) = "white" {}
        _FloorNormal ("Floor Normal (Reserved)", 2D) = "bump" {}
        _WallTint ("Wall Tint", Color) = (0.82, 0.85, 0.88, 1)
        _FloorTint ("Floor Tint", Color) = (0.72, 0.75, 0.78, 1)
        _WallTiling ("Wall Tiles Per Meter", Range(0.05, 2)) = 0.32
        _FloorTiling ("Floor Tiles Per Meter", Range(0.05, 2)) = 0.25
        _WallNormalStrength ("Wall Normal Strength (Reserved)", Range(0, 2)) = 0.9
        _FloorNormalStrength ("Floor Normal Strength (Reserved)", Range(0, 2)) = 0.65
        _FloorBlendStart ("Floor Blend Start", Range(0, 1)) = 0.55
        _FloorBlendEnd ("Floor Blend End", Range(0, 1)) = 0.82
        _Smoothness ("Smoothness", Range(0, 1)) = 0.14
        _Occlusion ("Occlusion", Range(0, 1)) = 0.92
    }

    SubShader
    {
        Tags { "RenderType" = "Opaque" "Queue" = "Geometry" }
        LOD 300

        CGPROGRAM
        #pragma surface surf Standard fullforwardshadows addshadow
        #pragma target 3.0

        sampler2D _WallTex;
        sampler2D _FloorTex;

        fixed4 _WallTint;
        fixed4 _FloorTint;
        half _WallTiling;
        half _FloorTiling;
        half _FloorBlendStart;
        half _FloorBlendEnd;
        half _Smoothness;
        half _Occlusion;

        struct Input
        {
            float3 worldPos;
            float3 worldNormal;
        };

        void surf(Input IN, inout SurfaceOutputStandard o)
        {
            float normalLength = max(length(IN.worldNormal), 0.0001);
            float3 geometricNormal = IN.worldNormal / normalLength;
            float signX = geometricNormal.x >= 0.0 ? 1.0 : -1.0;
            float signY = geometricNormal.y >= 0.0 ? 1.0 : -1.0;
            float signZ = geometricNormal.z >= 0.0 ? 1.0 : -1.0;

            float wallWeightX = abs(geometricNormal.x);
            float wallWeightZ = abs(geometricNormal.z);
            float wallWeightSum = max(wallWeightX + wallWeightZ, 0.0001);
            wallWeightX /= wallWeightSum;

            float2 wallUvX = float2(-signX * IN.worldPos.z, IN.worldPos.y) * _WallTiling;
            float2 wallUvZ = float2(signZ * IN.worldPos.x, IN.worldPos.y) * _WallTiling;
            float2 floorUv = float2(IN.worldPos.x, -signY * IN.worldPos.z) * _FloorTiling;

            fixed3 wallColorX = tex2D(_WallTex, wallUvX).rgb;
            fixed3 wallColorZ = tex2D(_WallTex, wallUvZ).rgb;
            fixed3 wallColor = lerp(wallColorZ, wallColorX, wallWeightX) * _WallTint.rgb;
            fixed3 floorColor = tex2D(_FloorTex, floorUv).rgb * _FloorTint.rgb;

            float floorBlend = smoothstep(
                _FloorBlendStart,
                max(_FloorBlendStart + 0.001, _FloorBlendEnd),
                abs(geometricNormal.y));

            o.Albedo = lerp(wallColor, floorColor, floorBlend);
            o.Metallic = 0.0;
            o.Smoothness = _Smoothness;
            o.Occlusion = _Occlusion;
            o.Alpha = 1.0;
        }
        ENDCG
    }

    FallBack "Standard"
}