using UnityEngine;

public class TerrainGenerator : MonoBehaviour
{
    public GameObject[] rocks;
    private Terrain terrain;
    public int depth = 20;

    public int width = 500;
    public int height = 500;

    public int scale = 10;

    float offSetX;
    float offSetY;

    void Awake()
    {
        terrain = GetComponent<Terrain>();
    }

    void Start()
    {
        offSetX = Random.Range(0f, 9999f);
        offSetY = Random.Range(0f, 9999f);
        Terrain terrain = GetComponent<Terrain>();
        terrain.terrainData = GenerateTerrain(terrain.terrainData);
        PlaceRocks();
    }

    TerrainData GenerateTerrain (TerrainData terrainData)
    {
        terrainData.heightmapResolution = width + 1;

        terrainData.size = new Vector3(width, depth, height);
        terrainData.SetHeights(0,0, GenerateHeights());

        return terrainData;
    }

    float [,] GenerateHeights()
    {
        float [,] heights = new float[width,height];
        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                heights[y,x] = CalculateHeight(x,y);
            }
        }
        return heights;
    }

    float CalculateHeight(int x, int y)
    {
        float xCoord = (float)x/width * scale + offSetX;
        float yCoord = (float)y/height * scale + offSetY;

        return Mathf.PerlinNoise(xCoord,yCoord);

    }

    public void PlaceRocks()
    {
        foreach(GameObject rock in rocks)
        {
            Vector3 pos = rock.transform.position;
            pos.y = terrain.SampleHeight(pos) + terrain.transform.position.y - 1f;
            rock.transform.position = pos;
        }
    }
}