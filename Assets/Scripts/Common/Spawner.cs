using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Spawner
{
    public PathFinder.PFCell pfCell;
    public Vector3 spawn_position;
    public GameObject[] availableEnemies;

    public Spawner(PathFinder.PFCell pfCell, Vector3 spawn_position, GameObject[] availableEnemies)
    {
        this.pfCell = pfCell;
        this.spawn_position = spawn_position;
        this.availableEnemies = availableEnemies;
    }
}
