Shader "YesterdayMap/Environment/HouseMaskedDirectional"
{
    Properties
    {
        _LegacyTex ("Original Atlas Mask", 2D) = "white" {}
        _WallTex ("Wall Plaster", 2D) = "white" {}
        _FloorTex ("Floor Wood", 2D) = "white" {}
        _BathroomTex ("Bathroom Cold Tile", 2D) = "white" {}
        _WallTint ("Wall Tint", Color) = (0.78, 0.78, 0.76, 1)
        _FloorTint ("Floor Tint", Color) = (0.76, 0.72, 0.68, 1)
        _BathroomTint ("Bathroom Tile Tint", Color) = (0.72, 0.76, 0.78, 1)
        _WindowTint ("Window Glass Tint", Color) = (0.12, 0.24, 0.30, 1)
        _LegacyTint ("Preserved Trim Tint", Color) = (0.82, 0.82, 0.82, 1)
        _WallTiling ("Wall Tiles Per Meter", Range(0.05, 2)) = 0.34
        _FloorTiling ("Floor Tiles Per Meter", Range(0.05, 2)) = 0.25
        _BathroomTiling ("Bathroom Tiles Per Meter", Range(0.05, 2)) = 0.22
        _WallMaskMin ("Wall Visibility Start", Range(0, 1)) = 0.012
        _WallMaskMax ("Wall Visibility Full", Range(0, 1)) = 0.050
        _FloorBlendStart ("Floor Blend Start", Range(0, 1)) = 0.55
        _FloorBlendEnd ("Floor Blend End", Range(0, 1)) = 0.82
        _BathroomBoundsX ("Bathroom Local X Min Max", Vector) = (160, 760, 0, 0)
        _BathroomBoundsZ ("Bathroom Rotated Y Min Max", Vector) = (-580, 25, 0, 0)
        _BathroomEdge ("Bathroom Boundary Feather", Range(0.1, 50)) = 8
        _Smoothness ("Smoothness", Range(0, 1)) = 0.17
        _Occlusion ("Occlusion", Range(0, 1)) = 0.95
    }

    SubShader
    {
        Tags { "RenderType" = "Opaque" "Queue" = "Geometry" }
        LOD 300

        CGPROGRAM
        #pragma surface surf Standard fullforwardshadows addshadow
        #pragma target 3.0

        sampler2D _LegacyTex;
        sampler2D _WallTex;
        sampler2D _FloorTex;
        sampler2D _BathroomTex;
        float4 _LegacyTex_TexelSize;

        fixed4 _WallTint;
        fixed4 _FloorTint;
        fixed4 _BathroomTint;
        fixed4 _WindowTint;
        fixed4 _LegacyTint;
        half _WallTiling;
        half _FloorTiling;
        half _BathroomTiling;
        half _WallMaskMin;
        half _WallMaskMax;
        half _FloorBlendStart;
        half _FloorBlendEnd;
        float4 _BathroomBoundsX;
        float4 _BathroomBoundsZ;
        half _BathroomEdge;
        half _Smoothness;
        half _Occlusion;

        struct Input
        {
            float2 uv_LegacyTex;
            float3 worldPos;
            float3 worldNormal;
        };

        float RectMask(float2 uv, float4 rect)
        {
            const float feather = 0.0015;
            float insideX = smoothstep(rect.x, rect.x + feather, uv.x)
                * (1.0 - smoothstep(rect.z - feather, rect.z, uv.x));
            float insideY = smoothstep(rect.y, rect.y + feather, uv.y)
                * (1.0 - smoothstep(rect.w - feather, rect.w, uv.y));
            return saturate(insideX * insideY);
        }

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
            float2 bathroomUv = float2(IN.worldPos.x, -signY * IN.worldPos.z) * _BathroomTiling;

            fixed3 wallColorX = tex2D(_WallTex, wallUvX).rgb;
            fixed3 wallColorZ = tex2D(_WallTex, wallUvZ).rgb;
            fixed3 wallColor = lerp(wallColorZ, wallColorX, wallWeightX) * _WallTint.rgb;
            fixed3 floorColor = tex2D(_FloorTex, floorUv).rgb * _FloorTint.rgb;
            fixed3 bathroomColor = tex2D(_BathroomTex, bathroomUv).rgb * _BathroomTint.rgb;

            fixed3 legacySample = tex2D(_LegacyTex, IN.uv_LegacyTex).rgb;
            fixed3 legacyColor = legacySample * _LegacyTint.rgb;

            float2 atlasDx = float2(_LegacyTex_TexelSize.x * 12.0, 0.0);
            float2 atlasDy = float2(0.0, _LegacyTex_TexelSize.y * 12.0);
            fixed3 regionSample = legacySample;
            regionSample += tex2D(_LegacyTex, IN.uv_LegacyTex + atlasDx).rgb;
            regionSample += tex2D(_LegacyTex, IN.uv_LegacyTex - atlasDx).rgb;
            regionSample += tex2D(_LegacyTex, IN.uv_LegacyTex + atlasDy).rgb;
            regionSample += tex2D(_LegacyTex, IN.uv_LegacyTex - atlasDy).rgb;
            regionSample *= 0.2;

            float legacyLuma = dot(regionSample, fixed3(0.299, 0.587, 0.114));
            float legacyMax = max(regionSample.r, max(regionSample.g, regionSample.b));
            float legacyMin = min(regionSample.r, min(regionSample.g, regionSample.b));
            float legacyChroma = legacyMax - legacyMin;

            float neutralSurface = 1.0 - smoothstep(0.08, 0.28, legacyChroma);
            float visibleSurface = smoothstep(
                _WallMaskMin,
                max(_WallMaskMin + 0.001, _WallMaskMax),
                legacyLuma);

            // Explicit UV regions cover every window island in the original 2048 atlas.
            // Color checks inside those regions isolate neutral glass while preserving warm wood frames.
            float windowRegion = 0.0;
            windowRegion = max(windowRegion, RectMask(IN.uv_LegacyTex, float4(0.000000, 0.914551, 0.048828, 0.975586)));
            windowRegion = max(windowRegion, RectMask(IN.uv_LegacyTex, float4(0.136719, 0.577637, 0.205078, 0.650879)));
            windowRegion = max(windowRegion, RectMask(IN.uv_LegacyTex, float4(0.654297, 0.653320, 0.722656, 0.731445)));
            windowRegion = max(windowRegion, RectMask(IN.uv_LegacyTex, float4(0.664063, 0.592285, 0.727539, 0.658203)));
            windowRegion = max(windowRegion, RectMask(IN.uv_LegacyTex, float4(0.000000, 0.394531, 0.031738, 0.472656)));
            windowRegion = max(windowRegion, RectMask(IN.uv_LegacyTex, float4(0.039063, 0.392090, 0.112305, 0.472656)));
            windowRegion = max(windowRegion, RectMask(IN.uv_LegacyTex, float4(0.336914, 0.316406, 0.419922, 0.404297)));
            windowRegion = max(windowRegion, RectMask(IN.uv_LegacyTex, float4(0.317383, 0.169922, 0.368652, 0.262695)));
            windowRegion = max(windowRegion, RectMask(IN.uv_LegacyTex, float4(0.378418, 0.169922, 0.458984, 0.267578)));
            windowRegion = max(windowRegion, RectMask(IN.uv_LegacyTex, float4(0.317383, 0.091797, 0.371094, 0.179688)));
            windowRegion = max(windowRegion, RectMask(IN.uv_LegacyTex, float4(0.393066, 0.000000, 0.449219, 0.052734)));
            windowRegion = max(windowRegion, RectMask(IN.uv_LegacyTex, float4(0.751953, 0.345703, 0.791016, 0.389648)));

            float centerLuma = dot(legacySample, fixed3(0.299, 0.587, 0.114));
            float centerMax = max(legacySample.r, max(legacySample.g, legacySample.b));
            float centerMin = min(legacySample.r, min(legacySample.g, legacySample.b));
            float centerChroma = centerMax - centerMin;
            float neutralGlass = 1.0 - smoothstep(0.075, 0.230, centerChroma);
            float visibleGlass = smoothstep(0.065, 0.140, centerLuma);
            float atlasGlass = windowRegion * neutralGlass * visibleGlass;

            // Keep a color-based fallback for tiny glass fragments outside the main islands.
            float coolGlass = smoothstep(0.004, 0.045, legacySample.b - legacySample.r);
            float brightGlass = smoothstep(0.100, 0.220, centerLuma);
            float windowMask = saturate(max(atlasGlass, coolGlass * brightGlass));

            float plasterMask = saturate(neutralSurface * visibleSurface * (1.0 - windowMask));

            float floorBlend = smoothstep(
                _FloorBlendStart,
                max(_FloorBlendStart + 0.001, _FloorBlendEnd),
                abs(geometricNormal.y));

            float3 localPos = mul(unity_WorldToObject, float4(IN.worldPos, 1.0)).xyz;
            float edge = max(_BathroomEdge, 0.001);
            float bathroomX = smoothstep(
                _BathroomBoundsX.x,
                _BathroomBoundsX.x + edge,
                localPos.x)
                * (1.0 - smoothstep(
                    _BathroomBoundsX.y - edge,
                    _BathroomBoundsX.y,
                    localPos.x));
            float bathroomZ = smoothstep(
                _BathroomBoundsZ.x,
                _BathroomBoundsZ.x + edge,
                localPos.y)
                * (1.0 - smoothstep(
                    _BathroomBoundsZ.y - edge,
                    _BathroomBoundsZ.y,
                    localPos.y));
            float bathroomMask = saturate(bathroomX * bathroomZ);

            fixed3 verticalColor = lerp(legacyColor, wallColor, plasterMask);
            fixed3 windowColor = lerp(legacyColor, _WindowTint.rgb, 0.78);
            verticalColor = lerp(verticalColor, windowColor, windowMask);
            fixed3 selectedFloorColor = lerp(floorColor, bathroomColor, bathroomMask);
            o.Albedo = lerp(verticalColor, selectedFloorColor, floorBlend);
            o.Metallic = 0.0;
            o.Emission = _WindowTint.rgb * windowMask * 0.06;
            o.Smoothness = lerp(_Smoothness, 0.55, windowMask * (1.0 - floorBlend));
            o.Occlusion = _Occlusion;
            o.Alpha = 1.0;
        }
        ENDCG
    }

    FallBack "Standard"
}
