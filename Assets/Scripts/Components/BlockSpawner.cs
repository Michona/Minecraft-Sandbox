using Unity.Entities;
using Unity.Mathematics;

public struct BlockSpawner : IComponentData
{
    public int FieldSize;
    public float2 MiddlePosition;

    public Entity DirtPrefab;
    public Entity GrassPrefab;
    public Entity SnowPrefab;
}
