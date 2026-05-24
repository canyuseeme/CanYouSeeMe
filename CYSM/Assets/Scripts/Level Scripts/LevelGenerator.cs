using UnityEngine;
using System.Collections.Generic;

public class LevelGenerator : MonoBehaviour
{
    public Texture2D map;
    public GameObject wallPrefab;
    public float tileSize = 1f;

    private bool[,] visited;

    void Start()
    {
        GenerateLevelGreedy();
    }

    void GenerateLevelGreedy()
    {
        int width = map.width;
        int height = map.height;
        visited = new bool[width, height];

        float offsetX = width / 2f;
        float offsetY = height / 2f;

        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                // If it's a wall and we haven't processed it yet
                if (IsWall(x, y) && !visited[x, y])
                {
                    // 1. Determine how wide this rectangle can be
                    int rectWidth = 0;
                    while (x + rectWidth < width && IsWall(x + rectWidth, y) && !visited[x + rectWidth, y])
                    {
                        rectWidth++;
                    }

                    // 2. Determine how high this rectangle can be
                    int rectHeight = 1;
                    while (y + rectHeight < height)
                    {
                        bool rowIsValid = true;
                        for (int k = 0; k < rectWidth; k++)
                        {
                            if (!IsWall(x + k, y + rectHeight) || visited[x + k, y + rectHeight])
                            {
                                rowIsValid = false;
                                break;
                            }
                        }

                        if (rowIsValid) rectHeight++;
                        else break;
                    }

                    // 3. Mark all pixels in this rectangle as visited
                    for (int row = y; row < y + rectHeight; row++)
                    {
                        for (int col = x; col < x + rectWidth; col++)
                        {
                            visited[col, row] = true;
                        }
                    }

                    // 4. Calculate Center Position
                    // Offset by 0.5 because Unity positions are at the center of the unit
                    float posX = (x + (rectWidth / 2f) - offsetX) * tileSize;
                    float posY = (y + (rectHeight / 2f) - offsetY) * tileSize;

                    SpawnWall(new Vector2(posX, posY), rectWidth, rectHeight);
                }
            }
        }
    }

    bool IsWall(int x, int y)
    {
        return map.GetPixel(x, y).r > 0.9f;
    }

    void SpawnWall(Vector2 pos, int w, int h)
    {
        GameObject wall = Instantiate(wallPrefab, pos, Quaternion.identity, transform);
        wall.transform.localScale = new Vector3(w * tileSize, h * tileSize, 1f);
    }
}