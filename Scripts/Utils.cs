using System;
using Godot;

public static class Utils
{
    public static float MapNumber(float num, float oldMin, float oldMax, float newMin, float newMax)
    {
        return newMin + (num-oldMin)*(newMax-newMin)/(oldMax-oldMin);
    }

    public static float ConvergeValue(float value, float target, float increment)
    {
        // Move value towards target in steps of size increment.
        // If increment is negative can also be used to do the opposite

        float difference = value - target;
        if (Mathf.Abs(difference) < increment) return target;
        else return value + -Mathf.Sign(difference) * increment;
    }

    public static float Slerp(float start, float end, float weight)
    {
        // Don't know if it's actually a slerp, moves the thing like a segment of a sine wave
        
        if (end == start) return end;

        var normalized = Mathf.Sin(weight * Mathf.Pi - (Mathf.Pi / 2)) / 2 + 0.5f;
        return normalized * (end - start) + start;
    }

    public static Vector3 Slerp(Vector3 start, Vector3 end, float weight)
    {
        // Because godot slerp is plain broken

        return new Vector3(
            Slerp(start.x, end.x, weight),
            Slerp(start.y, end.y, weight),
            Slerp(start.z, end.z, weight)
        );
    }
}