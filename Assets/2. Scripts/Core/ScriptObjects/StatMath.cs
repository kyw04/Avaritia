public static class StatMath
{
    public static T Add<T>(T a, T b)
    {
        if (typeof(T) == typeof(float)) return (T)(object)((float)(object)a + (float)(object)b);
        if (typeof(T) == typeof(int)) return (T)(object)((int)(object)a + (int)(object)b);
        return a;
    }
}
