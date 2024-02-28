using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

public class PathFinder
{
    private static BoundsInt bounds;

    private static bool[,] move_matrix;

    public static void Init()
    {
        bounds = LevelGen.transformer.GetTilemap(0).cellBounds;

        move_matrix = new bool[bounds.size.y, bounds.size.x];

        TileBase[] walls = LevelGen.transformer.GetTilemap(2).GetTilesBlock(bounds);
        TileBase[] floor = LevelGen.transformer.GetTilemap(0).GetTilesBlock(bounds);
        for(int i = 0; i < walls.Length; i++)
        {
            int x = i % bounds.size.x;
            int y = i / bounds.size.x;

            if (walls[i] == null && floor[i] != null)
                try
                {
                    move_matrix[y, x] = true;
                }catch(IndexOutOfRangeException e)
                {
                    Debug.Log("Interrupted!" + x + " " + y);
                }
        }
    }

    public static Stack<PFCell> GetWay(PFCell currentCell, PFCell targetCell, int acceptableDepth)
    {
        if (Mathf.Abs(targetCell.x - currentCell.x) > acceptableDepth || Mathf.Abs(targetCell.y - currentCell.y) > acceptableDepth || acceptableDepth == 0)
            return null;

        int mark = 1;

        int currentX = acceptableDepth, currentY = acceptableDepth;
        int targetX = currentX + targetCell.x - currentCell.x, targetY = currentY + targetCell.y - currentCell.y;

        int[,] marked = new int[acceptableDepth * 2 + 1, acceptableDepth * 2 + 1];

        marked[currentY, currentX] = mark;

        int MAX_MARK_VALUE = acceptableDepth * acceptableDepth + 100;

        while (marked[targetY, targetX] == 0 && mark < MAX_MARK_VALUE)
        {
            for (int y = 0; y < marked.GetLength(0); y++)
            {
                for (int x = 0; x < marked.GetLength(1); x++)
                {
                    if (marked[y, x] == mark)
                    {
                        int realX = currentCell.x + x - acceptableDepth;
                        int realY = currentCell.y + y - acceptableDepth;

                        if (x + 1 < marked.GetLength(1) && marked[y, x + 1] == 0 && move_matrix[realY, realX + 1])
                            marked[y, x + 1] = (mark + 1);
                        if (x - 1 >= 0 && marked[y, x - 1] == 0 && move_matrix[realY, realX - 1])
                            marked[y, x - 1] = (mark + 1);

                        if (y + 1 < marked.GetLength(0) && marked[y + 1, x] == 0 && move_matrix[realY + 1, realX])
                            marked[y + 1, x] = (mark + 1);
                        if (y - 1 >= 0 && marked[y - 1, x] == 0 && move_matrix[realY - 1, realX])
                            marked[y - 1, x] = (mark + 1);
                    }
                }
            }
            mark++;
        }

        if (marked[targetY, targetX] == 0)
            return null;

        int tx = targetX, ty = targetY;
        Stack<PFCell> way = new Stack<PFCell>();
        way.Push(targetCell);

        while (marked[ty, tx] != 1)
        {
            int realX = currentCell.x + tx - acceptableDepth;
            int realY = currentCell.y + ty - acceptableDepth;
            if (tx + 1 < marked.GetLength(1) && marked[ty, tx + 1] == (marked[ty, tx] - 1))
            {
                tx++;
                way.Push(new PFCell(realX + 1, realY));
            }
            else if (tx - 1 >= 0 && marked[ty, tx - 1] == (marked[ty, tx] - 1))
            {
                tx--;
                way.Push(new PFCell(realX - 1, realY));
            }

            else if (ty + 1 < marked.GetLength(0) && marked[ty + 1, tx] == (marked[ty, tx] - 1))
            {
                ty++;
                way.Push(new PFCell(realX, realY + 1));
            }
            else if (ty - 1 >= 0 && marked[ty - 1, tx] == (marked[ty, tx] - 1))
            {
                ty--;
                way.Push(new PFCell(realX, realY - 1));
            }
        }
        way.Pop();

        return way;
    }

    public class PFCell
    {
        public int x, y;
        public PFCell(int x, int y)
        {
            this.x = x;
            this.y = y;
        }
    }
}
