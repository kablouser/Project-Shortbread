using UnityEngine;

public static class BiasedRandom
{
    /// <param name="bias">0 is uniform distribution, 1 is normal distribution, 2 returns 0.5 everytime</param>
    /// <returns>random number between min and max</returns>
    public static float Range(float minInclusive, float maxInclusive, float bias = 1.0f)
    {
        bias = Mathf.Clamp(bias, 0f, 2f);
        float a = UnityEngine.Random.value;
        float b = UnityEngine.Random.value;
        float c = UnityEngine.Random.value;
        float d = UnityEngine.Random.value;

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

        return normalDist;
    }


}
