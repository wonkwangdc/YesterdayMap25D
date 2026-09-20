Shader "YesterdayMap/Environment/PlayerHomeSurface"
{
    Properties
    {
        _WoodTex ("Main Wood Floor", 2D) = "white" {}
        _UtilityTex ("Storage Floor", 2D) = "white" {}
        _BathroomTex ("Bathroom Tile", 2D) = "white" {}
        _WallpaperTex ("Interior Wallpaper", 2D) = "white" {}
        _WallTopTex ("Dark Wall Top", 2D) = "white" {}
        _TrimTex ("Lower Wall Trim", 2D) = "white" {}

        _WoodTint ("Wood Tint", Color) = (0.72, 0.58, 0.46, 1)
        _UtilityTint ("Storage Tint", Color) = (0.70, 0.70, 0.68, 1)
        _BathroomTint ("Bathroom Tint", Color) = (0.82, 0.82, 0.80, 1)
        _WallpaperTint ("Wallpaper Tint", Color) = (0.76, 0.69, 0.58, 1)
        _ExteriorBrickTint ("Exterior Brick Tint", Color) = (0.70, 0.62, 0.49, 1)
        _WallTopTint ("Wall Top Tint", Color) = (0.52, 0.52, 0.52, 1)
        _TrimTint ("Lower Wall Trim Tint", Color) = (0.80, 0.65, 0.52, 1)

        _HouseSize ("House World Size XZ", Vector) = (45, 34.26, 0, 0)
        _BathroomRect ("Bathroom Screen Rect", Vector) = (0.40, 0.46, 0.60, 0.79)
        _StorageRect ("Storage Screen Rect", Vector) = (0.61, 0.04, 0.97, 0.42)
        _RoomEdge ("Room Boundary Feather", Range(0.001, 0.08)) = 0.012
        _ExteriorEdge ("Exterior Edge Start", Range(0.5, 1)) = 0.84
        _TopHeight ("Wall Top World Height", Range(1, 10)) = 5.35
        _TrimBottom ("Lower Trim Bottom Height", Range(0, 2)) = 0.52
        _TrimTop ("Lower Trim Top Height", Range(0, 2)) = 1.10

        _WoodTiling ("Wood Tiles Per Meter", Range(0.02, 2)) = 0.18
        _UtilityTiling ("Storage Tiles Per Meter", Range(0.02, 2)) = 0.24
        _BathroomTiling ("Bathroom Tiles Per Meter", Range(0.02, 2)) = 0.23
        _WallpaperTiling ("Wallpaper Tiles Per Meter", Range(0.02, 2)) = 0.26
        _WallTopTiling ("Wall Top Tiles Per Meter", Range(0.02, 2)) = 0.25
        _TrimTiling ("Lower Trim Tiles Per Meter", Range(0.02, 2)) = 0.15
        _BrickScale ("Exterior Brick Scale", Range(0.2, 4)) = 1.20
        _Smoothness ("Smoothness", Range(0, 1)) = 0.14
        _TrimSmoothness ("Lower Trim Smoothness", Range(0, 1)) = 0.08
        _Occlusion ("Occlusion", Range(0, 1)) = 0.94
    }

    SubShader
    {
        Tags { "RenderType"="Opaque" "Queue"="Geometry" }
        LOD 300
        Cull Off

        CGPROGRAM
        #pragma surface surf Standard fullforwardshadows addshadow
        #pragma target 3.0

        sampler2D _WoodTex;
        sampler2D _UtilityTex;
        sampler2D _BathroomTex;
        sampler2D _WallpaperTex;
        sampler2D _WallTopTex;
        sampler2D _TrimTex;

        fixed4 _WoodTint;
        fixed4 _UtilityTint;
        fixed4 _BathroomTint;
        fixed4 _WallpaperTint;
        fixed4 _ExteriorBrickTint;
        fixed4 _WallTopTint;
        fixed4 _TrimTint;

        float4 _HouseSize;
        float4 _BathroomRect;
        float4 _StorageRect;
        half _RoomEdge;
        half _ExteriorEdge;
        half _TopHeight;
        half _TrimBottom;
        half _TrimTop;

        half _WoodTiling;
        half _UtilityTiling;
        half _BathroomTiling;
        half _WallpaperTiling;
        half _WallTopTiling;
        half _TrimTiling;
        half _BrickScale;
        half _Smoothness;
        half _TrimSmoothness;
        half _Occlusion;

        struct Input
        {
            float3 worldPos;
            float3 worldNormal;
        };

        float RectangleMask(float2 p, float4 rect, float feather)
        {
            float left = smoothstep(rect.x, rect.x + feather, p.x);
            float bottom = smoothstep(rect.y, rect.y + feather, p.y);
            float right = 1.0 - smoothstep(rect.z - feather, rect.z, p.x);
            float top = 1.0 - smoothstep(rect.w - feather, rect.w, p.y);
            return saturate(left * bottom * right * top);
        }

        fixed3 TriplanarWall(sampler2D source, float3 p, float3 n, half tiling)
        {
            float weightX = abs(n.x);
            float weightZ = abs(n.z);
            float sum = max(weightX + weightZ, 0.0001);
            weightX /= sum;
            fixed3 alongX = tex2D(source, float2(p.z, p.y) * tiling).rgb;
            fixed3 alongZ = tex2D(source, float2(p.x, p.y) * tiling).rgb;
            return lerp(alongZ, alongX, weightX);
        }

        fixed3 TriplanarTrim(sampler2D source, float3 p, float3 n, half tiling)
        {
            float weightX = abs(n.x);
            float weightZ = abs(n.z);
            float sum = max(weightX + weightZ, 0.0001);
            weightX /= sum;
            float horizontal = lerp(p.x, p.z, weightX);

            // Rotate the plank texture so its grain follows the wall horizontally.
            return tex2D(source, float2(p.y, horizontal) * tiling).rgb;
        }

        fixed3 ProceduralBrick(float3 p, float3 n)
        {
            float weightX = abs(n.x);
            float weightZ = abs(n.z);
            float sum = max(weightX + weightZ, 0.0001);
            weightX /= sum;
            float horizontal = lerp(p.x, p.z, weightX);

            float2 brickUv = float2(horizontal * 0.72, p.y * 1.35) * _BrickScale;
            float row = floor(brickUv.y);
            brickUv.x += fmod(abs(row), 2.0) * 0.5;
            float2 cell = frac(brickUv);
            float mortarX = smoothstep(0.025, 0.075, min(cell.x, 1.0 - cell.x));
            float mortarY = smoothstep(0.035, 0.090, min(cell.y, 1.0 - cell.y));
            float brickMask = mortarX * mortarY;

            float variation = frac(sin(dot(floor(brickUv), float2(12.9898, 78.233))) * 43758.5453);
            fixed3 brick = _ExteriorBrickTint.rgb * lerp(0.82, 1.08, variation);
            fixed3 mortar = fixed3(0.50, 0.47, 0.41);
            return lerp(mortar, brick, brickMask);
        }

        void surf(Input IN, inout SurfaceOutputStandard o)
        {
            float3 n = normalize(IN.worldNormal);
            float3 objectOrigin = mul(unity_ObjectToWorld, float4(0, 0, 0, 1)).xyz;
            float3 relative = IN.worldPos - objectOrigin;

            // Normalized top-view room coordinates used by the Inspector rectangles.
            float2 screenUv = float2(
                0.5 + relative.x / max(_HouseSize.x, 0.001),
                0.5 + relative.z / max(_HouseSize.y, 0.001));

            float floorFacing = smoothstep(0.58, 0.86, n.y);
            float actualFloor = floorFacing * (1.0 - smoothstep(0.65, 1.05, relative.y));
            float wallCap = floorFacing * smoothstep(_TopHeight - 0.20, _TopHeight + 0.10, relative.y);
            float elevatedHorizontal = saturate(floorFacing - actualFloor - wallCap);

            float bathroomMask = RectangleMask(screenUv, _BathroomRect, _RoomEdge);
            float storageMask = RectangleMask(screenUv, _StorageRect, _RoomEdge);
            storageMask *= 1.0 - bathroomMask;

            fixed3 wood = tex2D(_WoodTex, IN.worldPos.xz * _WoodTiling).rgb * _WoodTint.rgb;
            fixed3 utility = tex2D(_UtilityTex, IN.worldPos.xz * _UtilityTiling).rgb * _UtilityTint.rgb;
            fixed3 bathroom = tex2D(_BathroomTex, IN.worldPos.xz * _BathroomTiling).rgb * _BathroomTint.rgb;
            fixed3 wallTop = tex2D(_WallTopTex, IN.worldPos.xz * _WallTopTiling).rgb * _WallTopTint.rgb;

            fixed3 floorColor = lerp(wood, utility, storageMask);
            floorColor = lerp(floorColor, bathroom, bathroomMask);

            fixed3 wallpaper = TriplanarWall(_WallpaperTex, relative, n, _WallpaperTiling) * _WallpaperTint.rgb;
            fixed3 brick = ProceduralBrick(relative, n);

            float2 edgeRatio = abs(relative.xz) / max(_HouseSize.xy * 0.5, float2(0.001, 0.001));
            float nearOuterEdge = smoothstep(_ExteriorEdge, min(_ExteriorEdge + 0.10, 0.995), max(edgeRatio.x, edgeRatio.y));
            float2 outward = normalize(relative.xz + float2(0.0001, 0.0001));
            float outwardFacing = smoothstep(0.08, 0.50, dot(normalize(n.xz + float2(0.0001, 0.0001)), outward));
            float exteriorMask = nearOuterEdge * outwardFacing;
            fixed3 verticalColor = lerp(wallpaper, brick, exteriorMask);

            // Cover the lower-wall face, its sloped bevels, and its top lip.
            // The perfectly horizontal main floor is intentionally excluded.
            float trimBand = smoothstep(_TrimBottom - 0.025, _TrimBottom + 0.025, relative.y)
                * (1.0 - smoothstep(_TrimTop - 0.035, _TrimTop + 0.035, relative.y));
            float bevelOrVertical = 1.0 - smoothstep(0.94, 0.995, n.y);
            float trimSurface = saturate(bevelOrVertical + elevatedHorizontal);
            float trimMask = trimBand * trimSurface * (1.0 - exteriorMask);
            fixed3 trimColor = TriplanarTrim(_TrimTex, relative, n, _TrimTiling) * _TrimTint.rgb;

            // The supplied model is a single mesh. Horizontal furniture surfaces are kept dark
            // so they do not accidentally receive a floor or wallpaper pattern.
            fixed3 elevatedColor = wallTop * 0.72;
            fixed3 color = verticalColor;
            color = lerp(color, elevatedColor, elevatedHorizontal);
            color = lerp(color, wallTop, wallCap);
            color = lerp(color, floorColor, actualFloor);
            color = lerp(color, trimColor, trimMask);

            o.Albedo = color;
            o.Metallic = 0.0;
            o.Smoothness = lerp(_Smoothness, _TrimSmoothness, trimMask);
            o.Occlusion = _Occlusion;
            o.Alpha = 1.0;
        }
        ENDCG
    }

    FallBack "Standard"
}