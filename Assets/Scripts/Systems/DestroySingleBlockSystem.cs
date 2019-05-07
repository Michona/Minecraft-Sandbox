using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Transforms;

/**
 * Gets the position of the block entity that needs to be destroyed
 * and destroys entity at that position.
 * The entity with DestroyBlockTag is created by the player.
 * It runs the jobs only if the player tries to remove a block.
 * */
[UpdateInGroup(typeof(SimulationSystemGroup))]
[UpdateAfter(typeof(BlocksSpawnerSystem))]
[UpdateAfter(typeof(RelativePositionSystem))]
public class DestroySingleBlockSystem : JobComponentSystem
{

    EntityCommandBufferSystem m_EntityCommandBufferSystem;
    private EntityQuery m_DestroyBlockGroup;
    private NativeArray<float3> destroyedBlockPositions;

    protected override void OnCreate()
    {
        m_DestroyBlockGroup = GetEntityQuery(new EntityQueryDesc {
            All = new [] { ComponentType.ReadOnly<DestroyBlockTag>(), ComponentType.ReadOnly<Translation>() }
        });
        destroyedBlockPositions = new NativeArray<float3>(1, Allocator.Persistent);

        m_EntityCommandBufferSystem = World.GetOrCreateSystem<BeginSimulationEntityCommandBufferSystem>();
    }

    protected override void OnStopRunning()
    {
        destroyedBlockPositions.Dispose();
        base.OnStopRunning();
    }

    [BurstCompile]
    struct GetDestroyBlockPositionJob : IJobForEachWithEntity<Translation>
    {
        [WriteOnly]
        public NativeArray<float3> TargetPosition;
        public EntityCommandBuffer.Concurrent CommandBuffer;

        public void Execute(Entity entity, int index, [ReadOnly] ref Translation translation)
        {
            TargetPosition[index] = translation.Value;
            CommandBuffer.DestroyEntity(index, entity);
        }
    }

    [BurstCompile]
    [RequireComponentTag(typeof(BlockTag))]
    struct DestroySingleBlockSystemJob : IJobForEachWithEntity<Translation>
    {
        [ReadOnly]
        public float3 TargetPosition;
        public EntityCommandBuffer.Concurrent CommandBuffer;

        public void Execute(Entity entity, int index, [ReadOnly] ref Translation translation)
        {
            if (math.distance(translation.Value, TargetPosition) < 1) {
                CommandBuffer.DestroyEntity(index, entity);
            }
        }
    }

    protected override JobHandle OnUpdate(JobHandle inputDependencies)
    {
        var getDestroyBlockPositionJob = new GetDestroyBlockPositionJob {
            TargetPosition = destroyedBlockPositions,
            CommandBuffer = m_EntityCommandBufferSystem.CreateCommandBuffer().ToConcurrent()
        }.Schedule(m_DestroyBlockGroup, inputDependencies);

        getDestroyBlockPositionJob.Complete();
        if (m_DestroyBlockGroup.CalculateLength() > 0) {

            var job = new DestroySingleBlockSystemJob {
                TargetPosition = destroyedBlockPositions[0],
                CommandBuffer = m_EntityCommandBufferSystem.CreateCommandBuffer().ToConcurrent()
            }.Schedule(this, getDestroyBlockPositionJob);

            return job;
        }

        return getDestroyBlockPositionJob;
    }
}