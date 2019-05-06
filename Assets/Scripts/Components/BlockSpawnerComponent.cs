using Unity.Entities;

public struct BlockSpawner : IComponentData
{
    public int CountX;
    public int CountY;
    public Entity DirtPrefab;
    public Entity GrassPrefab;
}
