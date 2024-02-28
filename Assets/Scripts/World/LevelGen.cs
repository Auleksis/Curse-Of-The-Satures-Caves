using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;
using UnityEditor.Tilemaps;

public class LevelGen : MonoBehaviour
{    
    public static int LvlWidth = 10;
    public int LvlWidthI = 10;
    public static int LvlHeight = 10;
    public int LvlHeightI = 10;

    public static PairPurposeInt[] purposesCount;
    public PairPurposeInt[] purposesCountI;
    public static RoomUnit mainUnit;
    public RoomUnit mainUnitI;
    public static RoomUnit[] allRoomUnits;
    public RoomUnit[] allRoomUnitsI;
    public static HallUnit hallUnit;
    public HallUnit hallUnitI;

    public Grid levelGrid;

    public static float occupancy = 0.7f;    
    [Range(0.1f, 0.7f)] public float occupancyI = 0.7f;

    public static Vector3 player_spawn;
    public static PathFinder.PFCell player_PF_position;
    public static List<Spawner> all_spawners;

    public static TableTransformer transformer;

    public class Cell
    {
        public bool[] walls;
        public int x, y;
        public bool visited;
        public int inOrder;

        public Cell(int x, int y)
        {
            this.x = x;
            this.y = y;
            walls = new bool[] { true, true, true, true };
            visited = false;
            inOrder = 0;
        }
    }

    public class CellTable
    {
        public Cell[,] cells;
        public int width, height;

        public CellTable(int width, int height)
        {
            this.width = width;
            this.height = height;
            cells = new Cell[height, width];
        }

        public void Initialize()
        {
            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    cells[y, x] = new Cell(x, y);
                }
            }
        }

        public void CreateLabyrinth()
        {
            Stack<Cell> stack = new Stack<Cell>();
            Cell currentCell = cells[0, 0];
            currentCell.visited = true;
            currentCell.inOrder = 1;
            stack.Push(currentCell);

            while (stack.Count > 0)
            {
                Cell last = currentCell;
                currentCell = GetRandomNotVisitedNeighbour(currentCell.x, currentCell.y);
                if (currentCell != null)
                {
                    MoveToCell(last, currentCell);
                    stack.Push(currentCell);
                }
                else
                {
                    currentCell = stack.Pop();
                }
                if (stack.Count > width * height)
                    break;
            }

            string s = "";
            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    s += cells[y, x].inOrder + " ";
                }
                s += "\n";
            }

            Debug.Log(s);
        }

        public Cell MoveToCell(Cell cell1, Cell cell2)
        {
            cell2.visited = true;
            cell2.inOrder = (cell1.inOrder + 1);
            int dx = cell2.x - cell1.x;
            int dy = cell2.y - cell1.y;

            if (dy < 0)
            {
                cell1.walls[0] = false;
                cell2.walls[2] = false;
            }
            else if (dy > 0)
            {
                cell1.walls[2] = false;
                cell2.walls[0] = false;
            }
            else if (dx > 0)
            {
                cell1.walls[1] = false;
                cell2.walls[3] = false;
            }
            else if (dx < 0)
            {
                cell1.walls[3] = false;
                cell2.walls[1] = false;
            }

            return cell2;
        }

        public Cell[] GetNeighbours(int x, int y)
        {
            Cell[] neighbours = { null, null, null, null };

            if (y - 1 >= 0)
                neighbours[0] = cells[y - 1, x];
            if (x + 1 < width)
                neighbours[1] = cells[y, x + 1];
            if (y + 1 < height)
                neighbours[2] = cells[y + 1, x];
            if (x - 1 >= 0)
                neighbours[3] = cells[y, x - 1];

            return neighbours;
        }

        public Cell GetRandomNotVisitedNeighbour(int x, int y)
        {
            Cell rndCell = null;
            List<Cell> cellsNVE = new List<Cell>(GetNeighbours(x, y));

            for (int i = 0; i < cellsNVE.Count; i++)
            {
                if (cellsNVE[i] == null || cellsNVE[i].visited)
                {
                    cellsNVE.RemoveAt(i);
                    i--;
                }
            }

            if (cellsNVE.Count > 0)
                rndCell = cellsNVE[Random.Range(0, cellsNVE.Count)];

            return rndCell;
        }

        public List<Cell> GetNotNullNeighbours(int x, int y)
        {
            List<Cell> notNullCells = new List<Cell>(GetNeighbours(x, y));
            for (int i = 0; i < notNullCells.Count; i++)
            {
                if (notNullCells[i] == null)
                {
                    notNullCells.RemoveAt(i);
                    i--;
                }
            }
            return notNullCells;
        }

        public Cell GetRandomNeighbour(int x, int y)
        {
            List<Cell> notNullNeigbours = GetNotNullNeighbours(x, y);
            return notNullNeigbours[Random.Range(0, notNullNeigbours.Count)];
        }
    }

    public class LevelCell
    {
        public Cell cell;
        public Purpose purpose;
        public bool visited;
        public bool isStartOfRoom;
        public RoomUnit copyRoomUnit;

        public LevelCell(Cell cell, Purpose purpose)
        {
            this.cell = cell;
            this.purpose = purpose;
            this.visited = false;
            this.isStartOfRoom = false;
            this.copyRoomUnit = null;
        }
    }

    public class LevelTable
    {
        public LevelCell[,] levelCells;
        public CellTable table;
        
        public LevelCell spawnPoint;

        public static int MAX_ATTEMPTS_TO_SPAWN_ROOM = 50;

        public LevelTable(CellTable table)
        {
            this.table = table;            
            levelCells = new LevelCell[table.height, table.width];
            spawnPoint = null;
        }        

        public void CreateLevelTable()
        {
            for (int y = 0; y < table.height; y++)
            {
                for (int x = 0; x < table.width; x++)
                {
                    levelCells[y, x] = new LevelCell(table.cells[y, x], Purpose.NONE);
                }
            }

            //place rooms by purpose
            foreach(PairPurposeInt p in LevelGen.purposesCount)
            {
                foreach(RoomUnit ru in LevelGen.allRoomUnits)
                {
                    if (p.purpose == ru.purpose)
                        p.rooms.Add(ru);
                }
            }

            //define chunkSize and cellSizes of rooms
            int chunkSize = 0; //chunk consists of cells

            foreach(RoomUnit ru in LevelGen.allRoomUnits)
            {
                ru.SetUpRoom();
                ru.FillRoomMatrix(); //define roomMatrix
                chunkSize = Mathf.Max(chunkSize, ru.cellWidth, ru.cellHeight);
            }

            int widthByChunk = table.width / chunkSize;
            int heightByChunk = table.height / chunkSize;

            int targetRoomsCount = (int)Mathf.Ceil(LevelGen.occupancy * levelCells.Length);
            int spawned = 0;
            int attempts = 0;
            List<LevelCell> roomsStarts = new List<LevelCell>();
            //cy = ChunkY, cx = ChunkX

            for (int cy = 0; cy < heightByChunk; cy++)
            {
                for(int cx = 0; cx < widthByChunk; cx++)
                {
                    PairPurposeInt p = null;
                    do
                    {
                        p = LevelGen.purposesCount[Random.Range(0, LevelGen.purposesCount.Length)];
                        attempts++;
                    } while (p.count == 0 && attempts < MAX_ATTEMPTS_TO_SPAWN_ROOM);
                    if (attempts < MAX_ATTEMPTS_TO_SPAWN_ROOM)
                    {
                        RoomUnit ru = p.rooms[Random.Range(0, p.rooms.Count)];
                        int cellX = chunkSize * cx;
                        int cellY = chunkSize * cy;
                        PasteRoomMatrix(ru, cellX, cellY);
                        roomsStarts.Add(levelCells[cellY, cellX]);
                        spawned += ru.size;
                        p.count--;
                        attempts = 0;
                    }
                }
            }

            //filling with SIMPLE_ROOM to achieve the target number of rooms
            int chanceToSpawnRoom = 35;
            while (spawned < targetRoomsCount)
            {
                if (attempts == MAX_ATTEMPTS_TO_SPAWN_ROOM)
                {
                    targetRoomsCount--;
                    attempts = 0;
                    continue;
                }
                for (int y = 0; y < table.height; y++)
                {
                    for (int x = 0; x < table.width; x++)
                    {
                        if (Random.Range(0, 100) < chanceToSpawnRoom && levelCells[y, x].purpose == Purpose.NONE && !levelCells[y, x].isStartOfRoom)
                        {
                            LevelCell startCell = levelCells[y, x];
                            startCell.isStartOfRoom = true;
                            startCell.copyRoomUnit = mainUnit;
                            roomsStarts.Add(startCell);
                            spawned++;
                            attempts = 0;
                        }
                    }
                }
                attempts++;                               
            }                     
        }
        
        
        public void PasteRoomMatrix(RoomUnit room, int cellX, int cellY)
        {
            levelCells[cellY, cellX].isStartOfRoom = true;
            levelCells[cellY, cellX].copyRoomUnit = room;
            for(int y = 0; y < room.cellHeight; y++)
            {
                for(int x = 0; x < room.cellWidth; x++)
                {
                    if (room.roomMatrix[y, x] == 1)
                        levelCells[cellY + y, cellX + x].purpose = room.purpose;
                }
            }
        }

        public LevelCell[] GetNeighboursArray(LevelCell lvlCell)
        {
            Cell[] cellNeighbours = table.GetNeighbours(lvlCell.cell.x, lvlCell.cell.y);
            LevelCell[] available = new LevelCell[4];

            int i = -1;
            foreach (Cell c in cellNeighbours)
            {
                i++;
                if (c != null)
                {
                    int dx = lvlCell.cell.x - c.x;
                    int dy = lvlCell.cell.y - c.y;
                    if (dx > 0 && lvlCell.cell.walls[3] || dx < 0 && lvlCell.cell.walls[1] || dy > 0 && lvlCell.cell.walls[0] || dy < 0 && lvlCell.cell.walls[2])
                        continue;
                    available[i] = levelCells[c.y, c.x];
                }
            }
            return available;
        }

        public List<LevelCell> GetNeighbours(LevelCell lvlCell)
        {
            Cell[] cellNeighbours = table.GetNeighbours(lvlCell.cell.x, lvlCell.cell.y);
            List<LevelCell> neighbours = new List<LevelCell>();

            foreach (Cell c in cellNeighbours)
            {
                if (c != null)
                {
                    int dx = lvlCell.cell.x - c.x;
                    int dy = lvlCell.cell.y - c.y;
                    if (dx > 0 && lvlCell.cell.walls[3] || dx < 0 && lvlCell.cell.walls[1] || dy > 0 && lvlCell.cell.walls[0] || dy < 0 && lvlCell.cell.walls[2])
                        continue;
                    neighbours.Add(levelCells[c.y, c.x]);
                }
            }
            return neighbours;
        }

        public bool[] GetOpenSides(LevelCell lvlCell)
        {
            Cell[] cellNeighbours = table.GetNeighbours(lvlCell.cell.x, lvlCell.cell.y);
            bool[] openSides = { false, false, false, false };

            for(int i = 0; i < cellNeighbours.Length; i++)
            {
                Cell c = cellNeighbours[i];
                if (c != null)
                {
                    int dx = lvlCell.cell.x - c.x;
                    int dy = lvlCell.cell.y - c.y;
                    if (dx > 0 && lvlCell.cell.walls[3] || dx < 0 && lvlCell.cell.walls[1] || dy > 0 && lvlCell.cell.walls[0] || dy < 0 && lvlCell.cell.walls[2])
                        continue;
                    openSides[i] = true;
                }
            }
            
            return openSides;
        }

        public List<LevelCell> GetNotVisitedNeighbours(LevelCell lvlCell)
        {
            List<LevelCell> neighbours = GetNeighbours(lvlCell);
            for (int i = 0; i < neighbours.Count; i++)
            {
                if (neighbours[i].visited)
                {
                    neighbours.RemoveAt(i);
                    i--;
                }
            }
            return neighbours;
        }

        public bool IsPossibleToSpawnRoom(LevelCell lvlCell, List<LevelCell> spawnedRoomCellsList, RoomUnit room)
        {
            foreach (LevelCell c in spawnedRoomCellsList)
            {
                if (Mathf.Abs(c.cell.x - lvlCell.cell.x) <= room.cellWidth || Mathf.Abs(c.cell.y - lvlCell.cell.y) < room.cellHeight)
                    return false;
            }
            return true;
        }
    }

    public class TableTransformer
    {
        public Grid grid;               

        public TableTransformer(Grid grid)
        {
            this.grid = grid;            
        }

        public Tilemap GetTilemap(int layer)
        {
            return grid.gameObject.transform.GetChild(layer).GetComponent<Tilemap>();
        }

        public void PlaceRoom(RoomUnit room, BoundsInt bounds, LevelTable lvlTable, LevelCell startCell)
        {
            int sx = startCell.cell.x;
            int sy = startCell.cell.y;

            //Только первый слой легко поставить
            Tilemap tilemap = GetTilemap(0);
            TileBase[] ground = room.GetTilesBlock(0);
            tilemap.SetTilesBlock(bounds, ground);

            //С другими слоями приходится сложнее из-за дверей
            for (int y = 0; y < room.cellHeight; y++)
            {
                for(int x = 0; x < room.cellWidth; x++)
                {
                    if (room.roomMatrix[y, x] == 1)
                    {
                        int ty = sy + y;
                        int tx = sx + x;

                        int cellSizeX = mainUnit.width;
                        int cellSizeY = mainUnit.height;

                        Vector3Int position = new Vector3Int(tx * cellSizeX, ty * cellSizeY, 0);
                        Vector3Int size = new Vector3Int(cellSizeX, cellSizeY, 1);
                        BoundsInt tBounds = new BoundsInt(position, size);

                        TileBase[] special = room.GetTileBlockFromCell(x, y, 3);
                        TileBase[] wall = room.GetTileBlockFromCell(x, y, 2);
                        TileBase[] wall_sides = room.GetTileBlockFromCell(x, y, 1);

                        LevelCell lvlCell = lvlTable.levelCells[ty, tx];
                        bool[] openSides = lvlTable.GetOpenSides(lvlCell);
                        if (!openSides[0] && special[cellSizeX / 2] != null)
                        {
                            special[cellSizeX / 2] = null;
                            wall[cellSizeX / 2] = hallUnit.wallTile;
                        }
                        if (!openSides[1] && special[cellSizeX * (cellSizeY / 2 + 1) - 1] != null)
                        {
                            int place = cellSizeX * (cellSizeY / 2 + 1) - 1;
                            special[place] = null;
                            wall[place] = hallUnit.wallTile;         
                        }
                        if (!openSides[2] && special[cellSizeX * cellSizeY - cellSizeX / 2 - 1] != null)
                        {
                            int place = cellSizeX * cellSizeY - cellSizeX / 2 - 1;
                            special[place] = null;
                            wall_sides[cellSizeX * (cellSizeY - 1) - cellSizeX / 2 - 1] = hallUnit.wall_sideTile;
                            wall[place] = hallUnit.wallTile;
                        }
                        if (!openSides[3] && special[cellSizeX * (cellSizeY / 2)] != null)
                        {
                            int place = cellSizeX * (cellSizeY / 2);
                            special[place] = null;
                            wall[place] = hallUnit.wallTile;
                        }

                        GetTilemap(3).SetTilesBlock(tBounds, special);
                        GetTilemap(2).SetTilesBlock(tBounds, wall);
                        GetTilemap(1).SetTilesBlock(tBounds, wall_sides);
                    }
                }
            }            
        }

        public void PlaceHall(BoundsInt bounds, LevelTable lvlTable, LevelCell lvlCell)
        {
            bool[] openSides = lvlTable.GetOpenSides(lvlCell);
            int cellSizeX = bounds.size.x;
            int cellSizeY = bounds.size.y;
            int length = bounds.size.x * bounds.size.y;
            TileBase[] floor = new TileBase[length];
            TileBase[] wall_sides = new TileBase[length];
            TileBase[] walls = new TileBase[length];
            TileBase[] special = new TileBase[length];

            for(int y = 0; y < cellSizeY; y++)
            {
                for(int x = 0; x < cellSizeX; x++)
                {
                    if(y >= cellSizeY / 2 - 1 && y <= cellSizeY / 2 + 1 && x >= cellSizeX / 2 - 1 && x <= cellSizeX / 2 + 1)
                    {
                        floor[y * cellSizeX + x] = hallUnit.floorTile;
                        if (y == cellSizeY / 2 + 1 && !openSides[2])
                            wall_sides[y * cellSizeX + x] = hallUnit.wall_sideTile;
                    }
                    if ((y == cellSizeY / 2 - 2 && !openSides[0] || y == cellSizeY / 2 + 2 && !openSides[2]) && x >= cellSizeX / 2 - 2 && x <= cellSizeX / 2 + 2)
                        walls[y * cellSizeX + x] = hallUnit.wallTile;
                    if((x == cellSizeX / 2 - 2 && !openSides[3] || x == cellSizeX / 2 + 2 && !openSides[1]) && y >= cellSizeY / 2 - 2 && y <= cellSizeY / 2 + 2)
                        walls[y * cellSizeX + x] = hallUnit.wallTile;

                    if (x < cellSizeX / 2 - 1 && openSides[3] || x > cellSizeX / 2 + 1 && openSides[1])
                    {
                        if (y < cellSizeY / 2 + 1 && y >= cellSizeY / 2)
                        {
                            floor[y * cellSizeX + x] = hallUnit.floorTile;
                            floor[(y + 1) * cellSizeX + x] = hallUnit.floorTile;
                            wall_sides[(y + 1) * cellSizeX + x] = hallUnit.wall_sideTile;
                            walls[(y + 2) * cellSizeX + x] = hallUnit.wallTile;
                        }
                        else if(y < cellSizeY / 2 && y >= cellSizeY / 2 - 1)
                        {
                            floor[y * cellSizeX + x] = hallUnit.floorTile;
                            walls[(y - 1) * cellSizeX + x] = hallUnit.wallTile;
                        }
                    }
                    if(y < cellSizeY / 2 - 1 && openSides[0] || y > cellSizeY / 2 + 1 && openSides[2])
                    {
                        if(x >= cellSizeX / 2 - 1 && x <= cellSizeX / 2 + 1)
                        {
                            floor[y * cellSizeX + x] = hallUnit.floorTile;
                            if (y == cellSizeY - 1 && (x == cellSizeX / 2 - 1|| x == cellSizeX / 2 + 1) && lvlCell.cell.y + 1 < lvlTable.levelCells.Length && lvlTable.levelCells[lvlCell.cell.y + 1, lvlCell.cell.x].purpose != Purpose.NONE)
                                wall_sides[y * cellSizeX + x] = hallUnit.wall_sideTile;
                        }
                        else if (x == cellSizeX / 2 - 2 || x == cellSizeX / 2 + 2)
                            walls[y * cellSizeX + x] = hallUnit.wallTile;                        
                    }
                }
            }

            GetTilemap(0).SetTilesBlock(bounds, floor);
            GetTilemap(1).SetTilesBlock(bounds, wall_sides);
            GetTilemap(2).SetTilesBlock(bounds, walls);
            GetTilemap(3).SetTilesBlock(bounds, special);
        }        

        public void Transform(LevelTable lvlTable)
        {
            BoundsInt commonBounds = LevelGen.mainUnit.bounds;

            for (int y = 0; y < lvlTable.table.height; y++)
            {
                for (int x = 0; x < lvlTable.table.width; x++)
                {
                    LevelCell lvlCell = lvlTable.levelCells[y, x];

                    Vector3Int position = new Vector3Int(commonBounds.size.x * x, commonBounds.size.y * y, 0);
                    BoundsInt currentBounds;

                    if (lvlCell.isStartOfRoom)
                    {
                        currentBounds = new BoundsInt(position, lvlCell.copyRoomUnit.bounds.size);
                        PlaceRoom(lvlCell.copyRoomUnit, currentBounds, lvlTable, lvlCell);

                        if (lvlCell.purpose != Purpose.START_ROOM)
                        {
                            Vector3 pos = new Vector3(position.x * grid.cellSize.x, position.y * grid.cellSize.y, 0);
                            Vector3 spawn_pos = pos + lvlCell.copyRoomUnit.unit_spawnpoint_position;

                            PathFinder.PFCell pfCell = lvlCell.copyRoomUnit.spawnerPosition;
                            PathFinder.PFCell spawn_cell = new PathFinder.PFCell(pfCell.x + position.x, pfCell.y + position.y);
                            all_spawners.Add(new Spawner(spawn_cell, spawn_pos, lvlCell.copyRoomUnit.whatEnemyCanBeSpawned));
                        }
                    }
                    /*else if (lvlCell.purpose == Purpose.SIMPLE_ROOM)
                    {
                        currentBounds = new BoundsInt(position, commonBounds.size);
                        PlaceRoom(lvlCell.copyRoomUnit, currentBounds, lvlTable, lvlCell);
                        Debug.Log(lvlCell.purpose + " " + x + " " + y);
                    }*/
                    else if (lvlCell.purpose == Purpose.NONE)
                    {                        
                        currentBounds = new BoundsInt(position, commonBounds.size);
                        PlaceHall(currentBounds, lvlTable, lvlCell);
                    }
                    
                    if(lvlCell.purpose == Purpose.START_ROOM)
                    {
                        Vector3 pos = new Vector3(position.x * grid.cellSize.x, position.y * grid.cellSize.y, 0);
                        player_spawn = pos + lvlCell.copyRoomUnit.unit_spawnpoint_position;

                        PathFinder.PFCell pfCell = lvlCell.copyRoomUnit.spawnerPosition;

                        player_PF_position = new PathFinder.PFCell(pfCell.x + position.x, pfCell.y + position.y);
                    }
                }
            }
            GetTilemap(0).CompressBounds();
        }
    }

    private void Awake()
    {
        
    }

    public void SetUpLevel()
    {
        LvlWidth = LvlWidthI;
        LvlHeight = LvlHeightI;
        mainUnit = mainUnitI;
        purposesCount = purposesCountI;
        allRoomUnits = allRoomUnitsI;
        occupancy = occupancyI;
        hallUnit = hallUnitI;        

        if (all_spawners == null)
            all_spawners = new List<Spawner>();
        else
            all_spawners.Clear();

        CellTable table = new CellTable(LvlWidth, LvlHeight);
        table.Initialize();
        table.CreateLabyrinth();

        LevelTable levelTable = new LevelTable(table);
        levelTable.CreateLevelTable();

        transformer = new TableTransformer(levelGrid);
        transformer.Transform(levelTable);
    }
}
