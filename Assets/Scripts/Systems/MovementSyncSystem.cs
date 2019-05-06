using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;
using static Unity.Mathematics.math;

public class MovementSyncSystem : ComponentSystem
{

    EntityQuery query;

    protected override void OnCreate()
    {
        query = GetEntityQuery(
            ComponentType.ReadOnly<Transform>(),
            ComponentType.ReadWrite<Translation>(),
            ComponentType.ReadOnly<PlayerTag>());
    }
    protected override void OnUpdate()
    {
        Entities.With(query).ForEach(
            (Entity entity, Transform transform, ref Translation translation) => {
                translation.Value = transform.position;
            });
    }
}