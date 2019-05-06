using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Physics;
using Unity.Transforms;
using UnityEngine;

// JobComponentSystems can run on worker threads.
// However, creating and removing Entities can only be done on the main thread to prevent race conditions.
// The system uses an EntityCommandBuffer to defer tasks that can't be done inside the Job.
[UpdateInGroup(typeof(SimulationSystemGroup))]
public class BlockSpawnerSystem : JobComponentSystem
{
    BeginInitializationEntityCommandBufferSystem m_EntityCommandBufferSystem;

    protected override void OnCreate()
    {
        m_EntityCommandBufferSystem = World.GetOrCreateSystem<BeginInitializationEntityCommandBufferSystem>();
    }

    public struct SpawnJob : IJobForEachWithEntity<BlockSpawner, LocalToWorld>
    {
        public EntityCommandBuffer.Concurrent CommandBuffer;

        public void Execute(Entity entity, int index,
            [ReadOnly] ref BlockSpawner spawner,
            [ReadOnly] ref LocalToWorld location)
        {

            for (int x = 0; x < spawner.CountX; x++) {
                for (int y = 0; y < spawner.CountY; y++) {

                    var noiseHeight = noise.snoise(new float2(x, y) * 0.08F) * 4 + 3;

                    if (noiseHeight < 0) {
                        noiseHeight = 0;
                    }

                    if (noiseHeight >= 0) {
                        for (int z = 0; z <= (int)noiseHeight; z++) {

                            var dirtInst = CommandBuffer.Instantiate(index, spawner.DirtPrefab);

                            var dirtEntityPos = math.transform(location.Value, new float3(x, z, y));
                            CommandBuffer.SetComponent(index, dirtInst, new Translation { Value = dirtEntityPos });
                            CommandBuffer.AddComponent(index, dirtInst, new BlockTag());
                            CommandBuffer.AddComponent(index, dirtInst, new ColliderData { HasColliderBox = false });
                            CommandBuffer.AddComponent(index, dirtInst, new PlayerDistance { Value = 500.0f });
                        }
                    }


                    var grassInst = CommandBuffer.Instantiate(index, spawner.GrassPrefab);
                    var grassEntityPos = math.transform(location.Value, new float3(x, (int)noiseHeight + 1, y));
                    CommandBuffer.SetComponent(index, grassInst, new Translation { Value = grassEntityPos });
                    CommandBuffer.AddComponent(index, grassInst, new BlockTag());
                    CommandBuffer.AddComponent(index, grassInst, new ColliderData { HasColliderBox = false });
                    CommandBuffer.AddComponent(index, grassInst, new PlayerDistance { Value = 500.0f });
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