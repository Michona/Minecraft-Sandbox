using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;

public class DistanceToPlayerSystem : JobComponentSystem
{
    private float3 playerPosition;

    NativeQueue<float3> queue;


    protected override void OnCreate()
    {
        queue = new NativeQueue<float3>(Allocator.Persistent);
    }

    protected override void OnDestroy()
    {
        queue.Dispose();
        base.OnDestroy();
    }

    [BurstCompile]
    [RequireComponentTag(typeof(PlayerTag))]
    struct GetPlayerPositionJob : IJobForEach<Translation>
    {
        [WriteOnly]
        public NativeQueue<float3>.Concurrent Queue;

        public void Execute([ReadOnly] ref Translation translation)
        {
            Queue.Enqueue(translation.Value);
        }
    }

    [BurstCompile]
    [RequireComponentTag(typeof(BlockTag))]
    struct UpdateDistanceOnBlocksJob : IJobForEach<Translation, PlayerDistance>
    {
        [ReadOnly]
        public float3 PlayerPosition;

        public void Execute([ReadOnly] ref Translation translation, [WriteOnly] ref PlayerDistance distance)
        {
            float3 playerOffset = new float3(PlayerPosition.x, PlayerPosition.y, PlayerPosition.z);
            distance.Value = math.distance(translation.Value, playerOffset);
        }
    }

    protected override JobHandle OnUpdate(JobHandle inputDependencies)
    {

        var positionJob = new GetPlayerPositionJob {
            Queue = queue.ToConcurrent()
        }.Schedule(this, inputDependencies);

        positionJob.Complete();

        if (queue.TryDequeue(out playerPosition)) {

            var distanceJob = new UpdateDistanceOnBlocksJob {
                PlayerPosition = playerPosition
            }.Schedule(this, inputDependencies);

            return distanceJob;
        }
        queue.Dispose();

        return positionJob;
    }
}