using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GameSettings : MonoBehaviour
{
    public GameObject PlayerPrefab;

    public GameObject WireframeBoxPrefab;

    public GameObject BlockDirtType;

    public readonly Vector3 PlayerSpawnPosition = new Vector3(0, 10, 0);
}
