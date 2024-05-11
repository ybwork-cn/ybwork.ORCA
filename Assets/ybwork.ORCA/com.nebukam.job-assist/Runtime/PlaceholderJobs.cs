using Unity.Burst;
using Unity.Jobs;

namespace Nebukam.JobAssist
{
    [BurstCompile]
    public struct DisabledProcessor : IJob { public readonly void Execute() { } }

    [BurstCompile]
    public struct Unemployed : IJob { public readonly void Execute() { } }

    [BurstCompile]
    public struct UnemployedParallel : IJobParallelFor { public readonly void Execute(int index) { } }

    [BurstCompile]
    public struct EmptyCompound : IJob { public readonly void Execute() { } }
}
