using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Tile
{
    public enum ObstacleType
    {
        None,
        Tree,
        Building
    }
    public Building building;
    public bool occupied;
    public ObstacleType obstacleType;

    public void SetObstacleType(ObstacleType obstacleType)
    {
        occupied = obstacleType != ObstacleType.None;
        this.obstacleType = obstacleType;
    }

    public void SetObstacleType(ObstacleType obstacleType, Building building)
    {
        occupied = obstacleType != ObstacleType.None;
        this.obstacleType = obstacleType;
        this.building = building;
    }

    public void Clear()
    {
        occupied = false;
        obstacleType = ObstacleType.None;
        building = null;
    }
}
