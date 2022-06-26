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
}