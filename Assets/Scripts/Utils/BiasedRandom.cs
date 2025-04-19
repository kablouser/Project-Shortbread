using UnityEngine;

public static class BiasedRandom
{
    /// <param name="bias">0 is uniform distribution, 1 is normal distribution, 2 returns midpoint everytime</param>
    /// <returns>random number between min and max</returns>
    public static float Range(float minInclusive, float maxInclusive, float bias = 1.0f)
    {
        if (maxInclusive <= minInclusive)
            return minInclusive;
        if (bias <= 0f)
            return Random.Range(minInclusive, maxInclusive);
        if (2f <= bias)
            return (minInclusive + maxInclusive) / 2f;

        bias = Mathf.Clamp(bias, 0f, 2f);
        float a = Random.value;
        float b = Random.value;
        float c = Random.value;
        float d = Random.value;

        float normalDist;
        if (bias <= 1.0f)
        {
            normalDist = (a + b * bias + c * bias + d * bias) / (1.0f + bias * 3.0f);
        }
        else
        {
            // [1 -> 0]
            float mixRandom = 2.0f - bias;
            // [0 -> 1]
            float mixAverage = bias - 1.0f;
            normalDist = (a + b + c + d) * 0.25f * mixRandom + 0.5f * mixAverage;
        }

        return normalDist * (maxInclusive - minInclusive) + minInclusive;
    }

    /// <param name="bias">0 is uniform distribution, 1 is normal distribution, 2 returns midpoint everytime</param>
    /// <returns>random number x: min <= x < max</returns>
    public static int Range(int minInclusive, int maxExclusive, float bias = 1.0f)
    {
        if (maxExclusive <= minInclusive + 1)
            return minInclusive;

        int value = (int)Range((float)minInclusive, (float)maxExclusive, bias);
        if (maxExclusive <= value)
            return minInclusive;

        return value;
    }
}
