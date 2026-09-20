using UnityEngine;
using UnityEngine.UI;

namespace YesterdayMap.UI
{
    /// <summary>
    /// Draws only a circular annulus, so warning pulses can never become a
    /// rectangular Image highlight.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class CircularStatusWarningGraphic : MaskableGraphic
    {
        private const int SegmentCount = 96;
        private const float RingThicknessRatio = 0.075f;
        private const float MinimumRingThickness = 3.5f;
        private const float EdgeFeather = 1.15f;

        protected override void Awake()
        {
            base.Awake();
            raycastTarget = false;
        }

        protected override void OnPopulateMesh(VertexHelper vertexHelper)
        {
            vertexHelper.Clear();

            Rect rect = rectTransform.rect;
            float outerRadius =
                Mathf.Max(0f, Mathf.Min(rect.width, rect.height) * 0.5f);
            if (outerRadius <= 0f)
            {
                return;
            }

            float thickness = Mathf.Max(
                MinimumRingThickness,
                outerRadius * RingThicknessRatio);
            float innerRadius = Mathf.Max(0f, outerRadius - thickness);
            float feather = Mathf.Min(
                EdgeFeather,
                Mathf.Max(0.25f, thickness * 0.3f));
            float[] radii =
            {
                outerRadius,
                Mathf.Max(innerRadius, outerRadius - feather),
                Mathf.Min(outerRadius, innerRadius + feather),
                innerRadius
            };

            Color32 solidColor = color;
            Color32 transparentColor = solidColor;
            transparentColor.a = 0;
            Color32[] colors =
            {
                transparentColor,
                solidColor,
                solidColor,
                transparentColor
            };

            Vector2 center = rect.center;
            UIVertex vertex = UIVertex.simpleVert;
            for (int segment = 0; segment <= SegmentCount; segment++)
            {
                float angle = segment / (float)SegmentCount *
                              Mathf.PI * 2f;
                Vector2 direction = new(
                    Mathf.Cos(angle),
                    Mathf.Sin(angle));

                for (int band = 0; band < radii.Length; band++)
                {
                    vertex.position =
                        center + direction * radii[band];
                    vertex.color = colors[band];
                    vertex.uv0 = new Vector2(
                        band / (float)(radii.Length - 1),
                        segment / (float)SegmentCount);
                    vertexHelper.AddVert(vertex);
                }
            }

            const int bandsPerSegment = 4;
            for (int segment = 0; segment < SegmentCount; segment++)
            {
                int current = segment * bandsPerSegment;
                int next = (segment + 1) * bandsPerSegment;
                for (int band = 0; band < bandsPerSegment - 1; band++)
                {
                    vertexHelper.AddTriangle(
                        current + band,
                        next + band,
                        next + band + 1);
                    vertexHelper.AddTriangle(
                        current + band,
                        next + band + 1,
                        current + band + 1);
                }
            }
        }
    }
}
