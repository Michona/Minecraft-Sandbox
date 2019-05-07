using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Transforms;

/**
 * Spawns block entities using noise generation.
 * Is processing BlockSpawner component for spawn location and entity "prefabs".
 * Uses BeginInitializationEntityCommandBufferSystem to schedule the actual spawning  
 * in the InitializationSystemGroup.
 * Its the first system that runs its update (from the custom systems) in SimulationSystemGroup.
 */
[UpdateInGroup(typeof(SimulationSystemGroup))]
public class BlocksSpawnerSystem : JobComponentSystem
{
    BeginInitializationEntityCommandBufferSystem m_EntityCommandBufferSystem;

    protected override void OnCreate()
    {
        m_EntityCommandBufferSystem = World.GetOrCreateSystem<BeginInitializationEntityCommandBufferSystem>();
    }

    public struct SpawnJob : IJobForEachWithEntity<BlockSpawner, LocalToWorld>
    {
        public EntityCommandBuffer.Concurrent CommandBuffer;

        public void Execute(Entity entity, int index, [ReadOnly] ref BlockSpawner spawner, [ReadOnly] ref LocalToWorld location)
        {
            int halfLength = spawner.FieldSize / 2;
            int startX = (int)(spawner.MiddlePosition.x - halfLength);
            int startY = (int)(spawner.MiddlePosition.y - halfLength);

            for (int x = startX; x < startX + spawner.FieldSize; x++) {
                for (int y = startY; y < startY + spawner.FieldSize; y++) {

                    var noiseHeight = noise.snoise(new float2(x, y) * 0.08F) * 4 + 3;
                    if (noiseHeight < 0) {
                        noiseHeight = 0;
                    }

                    for (int z = 0; z <= (int)noiseHeight; z++) {

                        var dirtInstance = CommandBuffer.Instantiate(index, spawner.DirtPrefab);

                        var dirtEntityPos = math.transform(location.Value, new float3(x, z, y));
                        CommandBuffer.SetComponent(index, dirtInstance, new Translation { Value = dirtEntityPos });
                        CommandBuffer.AddComponent(index, dirtInstance, new BlockTag());
                        CommandBuffer.AddComponent(index, dirtInstance, new ColliderData { HasColliderBox = false });
                        CommandBuffer.AddComponent(index, dirtInstance, new PlayerPosition { Value = new float3(0, 0, 0) });
                    }


                    Entity topLayerEntity = noiseHeight > 5 ? spawner.SnowPrefab : spawner.GrassPrefab;

                    var topLayerInstance = CommandBuffer.Instantiate(index, topLayerEntity);
                    var topLayerPos = math.transform(location.Value, new float3(x, (int)noiseHeight + 1, y));
                    CommandBuffer.SetComponent(index, topLayerInstance, new Translation { Value = topLayerPos });
                    CommandBuffer.AddComponent(index, topLayerInstance, new BlockTag());
                    CommandBuffer.AddComponent(index, topLayerInstance, new ColliderData { HasColliderBox = false });
                    CommandBuffer.AddComponent(index, topLayerInstance, new PlayerPosition { Value = new float3(0, 0, 0) });
                }
            }

            CommandBuffer.DestroyEntity(index, entity);
        }
    }

    protected override JobHandle OnUpdate(JobHandle inputDeps)
    {
        var job = new SpawnJob {
            CommandBuffer = m_EntityCommandBufferSystem.CreateCommandBuffer().ToConcurrent()
        }.Schedule(this, inputDeps);

        m_EntityCommandBufferSystem.AddJobHandleForProducer(job);
        return job;
    }
}