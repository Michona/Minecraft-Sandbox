using Unity.Entities;
using Unity.Transforms;
using UnityEngine;

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