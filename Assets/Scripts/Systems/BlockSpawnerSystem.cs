using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Transforms;

// JobComponentSystems can run on worker threads.
// However, creating and removing Entities can only be done on the main thread to prevent race conditions.
// The system uses an EntityCommandBuffer to defer tasks that can't be done inside the Job.
[UpdateInGroup(typeof(SimulationSystemGroup))]
public class BlockSpawnerSystem : JobComponentSystem
{
    // BeginInitializationEntityCommandBufferSystem is used to create a command buffer which will then be played back
    // when that barrier system executes.
    // Though the instantiation command is recorded in the SpawnJob, it's not actually processed (or "played back")
    // until the corresponding EntityCommandBufferSystem is updated. To ensure that the transform system has a chance
    // to run on the newly-spawned entities before they're rendered for the first time, the HelloSpawnerSystem
    // will use the BeginSimulationEntityCommandBufferSystem to play back its commands. This introduces a one-frame lag
    // between recording the commands and instantiating the entities, but in practice this is usually not noticeable.
    BeginInitializationEntityCommandBufferSystem m_EntityCommandBufferSystem;

    protected override void OnCreate()
    {
        // Cache the BeginInitializationEntityCommandBufferSystem in a field, so we don't have to create it every frame
        m_EntityCommandBufferSystem = World.GetOrCreateSystem<BeginInitializationEntityCommandBufferSystem>();
    }

    struct SpawnJob : IJobForEachWithEntity<BlockSpawner, LocalToWorld>
    {
        public EntityCommandBuffer CommandBuffer;

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

                            var inst = CommandBuffer.Instantiate(spawner.Prefab);

                            var pos = math.transform(location.Value,
                                new float3(x, z, y));
                            CommandBuffer.SetComponent(inst, new Translation { Value = pos });
                        }
                    }
                }
            }

            CommandBuffer.DestroyEntity(entity);
        }
    }

    protected override JobHandle OnUpdate(JobHandle inputDeps)
    {
        //Instead of performing structural changes directly, a Job can add a command to an EntityCommandBuffer to perform such changes on the main thread after the Job has finished.
        //Command buffers allow you to perform any, potentially costly, calculations on a worker thread, while queuing up the actual insertions and deletions for later.

        // Schedule the job that will add Instantiate commands to the EntityCommandBuffer.
        var job = new SpawnJob {
            CommandBuffer = m_EntityCommandBufferSystem.CreateCommandBuffer()
        }.ScheduleSingle(this, inputDeps);


        // SpawnJob runs in parallel with no sync point until the barrier system executes.
        // When the barrier system executes we want to complete the SpawnJob and then play back the commands (Creating the entities and placing them).
        // We need to tell the barrier system which job it needs to complete before it can play back the commands.
        m_EntityCommandBufferSystem.AddJobHandleForProducer(job);

        return job;
    }
}