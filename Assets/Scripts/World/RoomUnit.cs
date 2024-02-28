using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

public enum Purpose {A, B, C, NONE, SIMPLE_ROOM, START_ROOM}; //TODO the purposes!
public class RoomUnit : MonoBehaviour
{
    // Start is called before the first frame update
    public Grid grid;
    [HideInInspector] public int width = 0;
    [HideInInspector] public int height = 0;
    [HideInInspector] public BoundsInt bounds;
    [HideInInspector] public int cellWidth = 1;
    [HideInInspector] public int cellHeight = 1;
    [HideInInspector] public int[,] roomMatrix;
    [HideInInspector] public int size = 0;

    public Purpose purpose = Purpose.SIMPLE_ROOM;
    public bool canBeRotated = false;

    public TileBase[] allDoorsTypes;


    [HideInInspector] public Vector3 unit_spawnpoint_position; 
    public TileBase spawnpoint_tile;
    public PathFinder.PFCell spawnerPosition;
    public GameObject[] whatEnemyCanBeSpawned; 

    public void SetUpRoom()
    {        
        Tilemap floor = GetTilemap(0);
        floor.CompressBounds();
        bounds = floor.cellBounds;
        width = bounds.size.x;
        height = bounds.size.y;

        allDoorsTypes = new TileBase[4];
        TileBase[] special = GetTilesBlock(3);
        for(int i = 0; i < special.Length; i++)
        {
            int x = i % width;
            int y = i / width;
            if (y == 0 && special[i] != null)
                allDoorsTypes[0] = special[i];
            if (y == (height - 1) && special[i] != null)
                allDoorsTypes[2] = special[i];
            if (x == 0 && special[i] != null)
                allDoorsTypes[1] = special[i];
            if (x == (width - 1) && special[i] != null)
                allDoorsTypes[3] = special[i];
        }

        if(spawnpoint_tile != null)
        {
            Grid grid = GameObject.Find("Grid").GetComponent<Grid>();
            float tileWidth = grid.cellSize.x;
            float tileHeight = grid.cellSize.y;

            TileBase[] floor_tiles = GetTilesBlock(0);
            for (int i = 0; i < special.Length; i++)
            {
                if (floor_tiles[i] == spawnpoint_tile)
                {
                    int x = i % width;
                    int y = i / width;
                    unit_spawnpoint_position = new Vector3(x * tileWidth + tileWidth / 2, y * tileHeight + tileHeight / 2, 0);
                    spawnerPosition = new PathFinder.PFCell(x, y);
                }                
            }
        }
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public Tilemap GetTilemap(int layer)
    {
        return grid.gameObject.transform.GetChild(layer).GetComponent<Tilemap>();
    }
    
    public TileBase [] GetTilesBlock(int layer)
    {
        Tilemap tilemap = GetTilemap(layer);
        return tilemap.GetTilesBlock(bounds);
    }

    public void FillRoomMatrix()
    {
        int cellSizeX = LevelGen.mainUnit.width;
        int cellSizeY = LevelGen.mainUnit.height;
        cellWidth = (int) Mathf.Ceil(width / (float)cellSizeX);
        cellHeight = (int) Mathf.Ceil(height / (float)cellSizeY);

        roomMatrix = new int[cellHeight, cellWidth];
        Tilemap floor = GetTilemap(0);
        for(int y = 0; y < cellHeight; y++)
        {
            for(int x = 0; x < cellWidth; x++)
            {
                TileBase [] gotTiles = GetTileBlockFromCell(x, y, 0);
                foreach(TileBase tb in gotTiles)
                {
                    if(tb != null)
                    {
                        roomMatrix[y, x] = 1;
                        this.size++;
                        break;
                    }
                }
            }
        }
    }
    
    public TileBase[] GetTileBlockFromCell(int matrix_X, int matrix_Y, int layer)
    {
        int cellSizeX = LevelGen.mainUnit.width;
        int cellSizeY = LevelGen.mainUnit.height;

        Vector3Int position = new Vector3Int(this.bounds.x + matrix_X * cellSizeX, this.bounds.y + matrix_Y * cellSizeY, 0);
        Vector3Int size = new Vector3Int(cellSizeX, cellSizeY, 1);
        BoundsInt bounds = new BoundsInt(position, size);

        return GetTilemap(layer).GetTilesBlock(bounds);
    }        

    public int[,] GetRotatedRoomMatrix(int degrees)
    {
        int parts = degrees / 90; //parts belongs to [0, 3]
        int[,] newMatrix;
        if (parts == 2) //Матрицу нужно перевернуть
            newMatrix = new int[cellHeight, cellWidth];
        else //Матрицу нужно повернуть направо или налево
            newMatrix = new int[cellWidth, cellHeight];

        if(parts == 2)
        {
            for(int y = 0; y < cellHeight; y++)
            {
                for(int x = 0; x < cellWidth; x++)
                {
                    newMatrix[cellHeight - y - 1, cellWidth - x - 1] = roomMatrix[y, x];
                }
            }
        }
        else
        {
            for(int y = 0; y < cellHeight; y++)
            {
                for(int x = 0; x < cellWidth; x++)
                {
                    newMatrix[y, x] = roomMatrix[x, cellWidth - y - 1];
                }
            }
        }

        return newMatrix;
    }
}
