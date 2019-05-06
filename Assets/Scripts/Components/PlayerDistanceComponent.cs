using System;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;

[Serializable]
public struct PlayerDistance : IComponentData
{
    public float Value;
}
