using System;
using System.Collections.Generic;
using UnityEngine;

public static class VisualiseDistribution
{
    public static void WithAnimationCurveFloat(Func<float> function, AnimationCurve animationCurve, float min = 0f, float max = 1f, int distributionSegments = 1000, int sampleSize = 1000000)
    {
        List<int> bins = GetBinsFloat(function, min, max, distributionSegments, sampleSize);

        animationCurve.ClearKeys();
        for (int i = 0; i < distributionSegments; i++)
        {
            Vector2 p = GraphBinFloat(i, bins[i], min, max, distributionSegments, sampleSize);
            animationCurve.AddKey(p.x, p.y);
        }
    }

    public static void WithAnimationCurveInt(Func<int> function, AnimationCurve animationCurve, int min, int maxExclusive, int sampleSize = 1000000)
    {
        List<int> bins = GetBinsInt(function, min, maxExclusive, sampleSize);

        animationCurve.ClearKeys();
        for (int i = 0; i < bins.Count; i++)
        {
            Vector2 p = GraphBinInt(i, bins[i], min, maxExclusive, sampleSize);
            animationCurve.AddKey(p.x, p.y);
        }
    }

    public static void WithLineRendererFloat(Func<float> function, LineRenderer lineRenderer, float min = 0f, float max = 1f, int distributionSegments = 1000, int sampleSize = 1000000)
    {
        List<int> bins = GetBinsFloat(function, min, max, distributionSegments, sampleSize);

        Vector3[] positions = new Vector3[distributionSegments];
        lineRenderer.positionCount = distributionSegments;
        for (int i = 0; i < distributionSegments; i++)
        {
            Vector2 p = GraphBinFloat(i, bins[i], min, max, distributionSegments, sampleSize);
            positions[i] = p;
        }
        lineRenderer.SetPositions(positions);
    }

    public static void WithLineRendererInt(Func<int> function, LineRenderer lineRenderer, int min, int maxExclusive, int sampleSize = 1000000)
    {
        List<int> bins = GetBinsInt(function, min, maxExclusive, sampleSize);

        Vector3[] positions = new Vector3[bins.Count];
        lineRenderer.positionCount = bins.Count;
        for (int i = 0; i < bins.Count; i++)
        {
            Vector2 p = GraphBinInt(i, bins[i], min, maxExclusive, sampleSize);
            positions[i] = p;
        }
        lineRenderer.SetPositions(positions);
    }

    // don't use this directly
    public static List<int> GetBinsFloat(Func<float> function, float min, float max, int distributionSegments, int sampleSize)
    {
        List<int> bins = new(distributionSegments);
        for (int i = 0; i < distributionSegments; i++)
        {
            bins.Add(0);
        }

        for (int i = 0; i < sampleSize; i++)
        {
            float value = function();
            float valueNormal = (value - min) / Mathf.Max(1, max - min);
            if (valueNormal < 0f || 1f < valueNormal)
            {
                valueNormal = Mathf.Clamp01(valueNormal);
                Debug.Assert(false, "no entry please");
            }
            int binI = Mathf.FloorToInt(valueNormal * distributionSegments);
            if (binI == distributionSegments)
                binI = distributionSegments - 1;
            bins[binI]++;
        }

        return bins;
    }

    // don't use this directly
    public static List<int> GetBinsInt(Func<int> function, int min, int maxExclusive, int sampleSize)
    {
        return GetBinsFloat(() => function(), min, maxExclusive - 1f, maxExclusive - min, sampleSize);
    }

    // don't use this directly
    public static Vector2 GraphBinFloat(int i, int binValue, float min, float max, int distributionSegments, int sampleSize)
    {
        return new(
            (i + 0.5f) / distributionSegments * (max - min) + min,
            binValue / (float)sampleSize * distributionSegments);
    }

    // don't use this directly
    public static Vector2 GraphBinInt(int i, int binValue, int min, int maxExclusive, int sampleSize)
    {
        return new(
            (float)i + min,
            binValue / (float)sampleSize * (maxExclusive - min));
    }
}
