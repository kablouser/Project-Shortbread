using System;
using System.Collections.Generic;
using UnityEngine;

public static class VisualiseDistribution
{
    public static void WithAnimationCurve(Func<float> function, AnimationCurve animationCurve, float min = 0f, float max = 1f, int distributionSegments = 1000, int sampleSize = 1000000)
    {
        List<int> bins = GetBins(function, min, max, distributionSegments, sampleSize);

        animationCurve.ClearKeys();
        for (int i = 0; i < distributionSegments; i++)
        {
            animationCurve.AddKey(
                (i + 0.5f) / (float)distributionSegments * (max - min) + min,
                bins[i] / (float)sampleSize * distributionSegments);
        }
    }

    public static void WithLineRenderer(Func<float> function, LineRenderer lineRenderer, float min = 0f, float max = 1f, int distributionSegments = 1000, int sampleSize = 1000000)
    {
        List<int> bins = GetBins(function, min, max, distributionSegments, sampleSize);

        Vector3[] positions = new Vector3[distributionSegments];
        lineRenderer.positionCount = distributionSegments;
        for (int i = 0; i < distributionSegments; i++)
        {
            positions[i] = new Vector3(
                (i + 0.5f) / (float)distributionSegments * (max - min) + min,
                bins[i] / (float)sampleSize * distributionSegments);
        }
        lineRenderer.SetPositions(positions);
    }

    // don't use this directly
    public static List<int> GetBins(Func<float> function, float min, float max, int distributionSegments, int sampleSize)
    {
        List<int> bins = new(distributionSegments);
        for (int i = 0; i < distributionSegments; i++)
        {
            bins.Add(0);
        }

        for (int i = 0; i < sampleSize; i++)
        {
            float value = function();
            float valueNormal = Mathf.Clamp01((value - min) / (max - min));
            int binI = Mathf.FloorToInt(valueNormal * distributionSegments);
            if (binI == distributionSegments)
                binI = distributionSegments - 1;
            bins[binI]++;
        }

        return bins;
    }
}
