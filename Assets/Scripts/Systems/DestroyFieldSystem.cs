using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Transforms;

/**
 * Runs on block entites that are not created by the player. 
 * If they are far away from the player it destroys them.
 * 
 * Uses BeginInitializationEntityCommandBufferSystem so the "actual" deletion 
 * is in the InitializationSystemGroup.
 * */
[UpdateInGroup(typeof(SimulationSystemGroup))]
[UpdateAfter(typeof(BlocksSpawnerSystem))]
[UpdateAfter(typeof(RelativePositionSystem))]
public class DestroyFieldSystem : JobComponentSystem
{
    EntityCommandBufferSystem m_EntityCommandBufferSystem;

    protected override void OnCreate()
    {
        m_EntityCommandBufferSystem = World.GetOrCreateSystem<BeginInitializationEntityCommandBufferSystem>();
    }

    [BurstCompile]
    [RequireComponentTag(typeof(BlockTag))]
    [ExcludeComponent(typeof(PlayerCreatedTag))]
    struct DestroyFieldSystemJob : IJobForEachWithEntity<Translation, PlayerPosition>
    {
        public EntityCommandBuffer.Concurrent CommandBuffer;

        [ReadOnly]
        public int FieldSize;

        public void Execute(Entity entity, int index, [ReadOnly] ref Translation translation, [ReadOnly] ref PlayerPosition playerPosition)
        {
            if (math.distance(translation.Value.z, playerPosition.Value.z) > FieldSize || math.distance(translation.Value.x, playerPosition.Value.x) > FieldSize) {
                CommandBuffer.DestroyEntity(index, entity);
            }
        }
    }

    protected override JobHandle OnUpdate(JobHandle inputDependencies)
    {
        var job = new DestroyFieldSystemJob {
            CommandBuffer = m_EntityCommandBufferSystem.CreateCommandBuffer().ToConcurrent(),
            FieldSize = GameInstance.Settings.FieldSize
        }.Schedule(this, inputDependencies);

        m_EntityCommandBufferSystem.AddJobHandleForProducer(job);

        return job;
    }
}