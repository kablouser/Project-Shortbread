using UnityEngine;

public static class NormalRandom
{
    /// <param name="distribution">[0, 2] 0 is uniform distribution, 1 is normal distribution, 2 returns midpoint everytime</param>
    /// <param name="bias">[0, 1] skew the distribution towards the min or max</param>
    /// <returns>random number between min and max</returns>
    public static float Range(float minInclusive, float maxInclusive, float distribution = 1.0f, float bias = 0.5f)
    {
        if (maxInclusive <= minInclusive)
            return minInclusive;
        if (distribution <= 0f)
            return Random.Range(minInclusive, maxInclusive);
        if (2f <= distribution)
            return (minInclusive + maxInclusive) / 2f;
        if (bias <= 0f)
            return minInclusive;
        if (1f <= bias)
            return maxInclusive;

        distribution = Mathf.Clamp(distribution, 0f, 2f);
        bias = Mathf.Clamp01(bias);
        float a = Random.value;
        float b = Random.value;
        float c = Random.value;
        float d = Random.value;

        float normalDist;
        if (distribution <= 1.0f)
        {
            normalDist = (a + b * distribution + c * distribution + d * distribution) / (1.0f + distribution * 3.0f);
        }
        else
        {
            // [1 -> 0]
            float mixRandom = 2.0f - distribution;
            // [0 -> 1]
            float mixAverage = distribution - 1.0f;
            normalDist = (a + b + c + d) * 0.25f * mixRandom + 0.5f * mixAverage;
        }
        if (bias != 0.5f)
        {
            if (bias < 0.5f)
            {
                normalDist = 1f - normalDist;
                normalDist = Mathf.Pow(normalDist, bias * 2f);
                normalDist = 1f - normalDist;
            }
            else
                normalDist = Mathf.Pow(normalDist, 2f - bias * 2f);
        }
        

        return normalDist * (maxInclusive - minInclusive) + minInclusive;
    }

    /// <param name="distribution">[0, 2] 0 is uniform distribution, 1 is normal distribution, 2 returns midpoint everytime</param>
    /// <param name="bias">[0, 1] skew the distribution towards the min or max</param>
    /// <returns>random number between min and max</returns>
    public static int Range(int minInclusive, int maxExclusive, float distribution = 1.0f, float bias = 0.5f)
    {
        if (maxExclusive <= minInclusive + 1)
            return minInclusive;

        int value = (int)Range((float)minInclusive, (float)maxExclusive, distribution, bias);
        if (maxExclusive <= value)
            return maxExclusive - 1;

        return value;
    }
}
