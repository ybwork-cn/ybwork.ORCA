using System.Runtime.CompilerServices;
using Unity.Mathematics;

public static class RandomExtension
{
    /// <summary>Returns a uniformly random float2 value with all components in the interval [0, 1).</summary>
    /// <returns>A uniformly random float2 value in the range [0, 1).</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static float2 NextFix64Vec2(this ref Random random)
    {
        return random.NextFloat2();
    }
}
