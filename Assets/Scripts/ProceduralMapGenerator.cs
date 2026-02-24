using UnityEngine;
using UnityEngine.Tilemaps;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// Generatore di dungeon GRANDI con molta varietà visiva
/// Usa tutti i tile disponibili per creare mappe più interessanti
/// </summary>
public class ProceduralMapGenerator : MonoBehaviour
{
    [Header("Tilemap Layers")]
    public Tilemap groundTilemap;
    public Tilemap wallTilemap;
    public Tilemap decorTilemap;
    public Tilemap detailsTilemap; // Per piccoli dettagli
    public Tilemap walkBehindTilemap;
    public Tilemap walkInFrontTilemap;

    [Header("Floor Tiles - Varietà")]
    [Tooltip("Diverse varianti di pavimento per variare")]
    public TileBase[] floorTilesNormal;
    public TileBase[] floorTilesDark;
    public TileBase[] floorTilesLight;
    
    [Header("Wall Tiles - Varietà")]
    public TileBase[] wallTilesStone;
    public TileBase[] wallTilesBrick;
    public TileBase[] wallTilesDamaged;
    
    [Header("Decorative Tiles")]
    public TileBase[] rugTiles;
    public TileBase[] pillarTiles;
    public TileBase[] furnitureTiles;
    public TileBase[] debrisTiles;
    public TileBase[] crackTiles;

    [Header("Prefabs")]
    public GameObject[] candlePrefabs;
    public GameObject[] torchPrefabs;
    public GameObject[] hazardPrefabs;
    public GameObject doorPrefab;
    public GameObject chestPrefab;

    [Header("Map Size - GRANDE!")]
    [Range(80, 200)]
    public int mapWidth = 150;
    
    [Range(80, 200)]
    public int mapHeight = 150;

    [Header("Room Generation")]
    [Range(15, 50)]
    public int minRooms = 25;
    
    [Range(20, 60)]
    public int maxRooms = 40;
    
    [Range(8, 25)]
    public int minRoomSize = 10;
    
    [Range(15, 40)]
    public int maxRoomSize = 25;

    [Header("Corridor Settings")]
    [Range(2, 6)]
    public int corridorWidth = 3;
    
    [Range(0f, 0.3f)]
    public float corridorWindingChance = 0.15f;

    [Header("Density Settings")]
    [Range(0f, 1f)]
    [Tooltip("RIDOTTO! 0.1 = poche luci, 0.3 = molte luci")]
    public float lightDensity = 0.08f; // RIDOTTO da 0.25!
    
    [Range(0f, 1f)]
    public float decorationDensity = 0.3f;
    
    [Range(0f, 0.5f)]
    public float hazardDensity = 0.1f;
    
    [Range(0f, 0.3f)]
    public float pillarDensity = 0.15f;
    
    [Header("Enemy Spawning")]
    public EnemySpawner enemySpawner;

    [Header("Visual Variety")]
    [Range(0f, 1f)]
    public float floorVariety = 0.4f; // Quanto spesso cambia tipo di pavimento
    
    [Range(0f, 1f)]
    public float wallVariety = 0.3f;
    
    public bool useDifferentFloorsPerRoom = true;

    private int[,] map;
    private List<Room> rooms = new List<Room>();
    private List<GameObject> spawnedObjects = new List<GameObject>();
    private System.Random rng;

    [System.Serializable]
    public class Room
    {
        public int x, y, width, height;
        public RoomType type;
        public TileBase[] floorStyle; // Ogni stanza può avere uno stile diverso
        public TileBase[] wallStyle;

        public Room(int x, int y, int width, int height)
        {
            this.x = x;
            this.y = y;
            this.width = width;
            this.height = height;
            this.type = RoomType.Normal;
        }

        public Vector2Int Center() => new Vector2Int(x + width / 2, y + height / 2);

        public bool Intersects(Room other, int buffer = 3)
        {
            return !(x > other.x + other.width + buffer ||
                     x + width + buffer < other.x ||
                     y > other.y + other.height + buffer ||
                     y + height + buffer < other.y);
        }
    }

    public enum RoomType
    {
        Start,
        Normal,
        Large,
        Combat,
        Treasure,
        Library,
        Prison
    }

    void Start()
    {
        GenerateLargeDungeon();
    }

    [ContextMenu("Generate Large Dungeon")]
    public void GenerateLargeDungeon()
    {
        Debug.Log("🏰 Generazione dungeon GRANDE iniziata...");
        
        ClearAll();
        
        rng = new System.Random(Random.Range(0, 999999));
        
        InitializeMap();
        GenerateRooms();
        ConnectAllRooms();
        PlaceAllTiles();
        PopulateRooms();
        AddAtmosphere();
        
        Debug.Log($"✅ Dungeon completato: {rooms.Count} stanze, {mapWidth}x{mapHeight} tiles");
    }

    void ClearAll()
    {
        if (groundTilemap) groundTilemap.ClearAllTiles();
        if (wallTilemap) wallTilemap.ClearAllTiles();
        if (decorTilemap) decorTilemap.ClearAllTiles();
        if (detailsTilemap) detailsTilemap.ClearAllTiles();
        if (walkBehindTilemap) walkBehindTilemap.ClearAllTiles();
        if (walkInFrontTilemap) walkInFrontTilemap.ClearAllTiles();

        foreach (var obj in spawnedObjects)
            if (obj) Destroy(obj);
        
        spawnedObjects.Clear();
        rooms.Clear();
    }

    void InitializeMap()
    {
        map = new int[mapWidth, mapHeight];
    }

    void GenerateRooms()
    {
        int targetRooms = rng.Next(minRooms, maxRooms);
        
        // Prima stanza: spawn del player (medio-grande)
        int startSize = rng.Next(15, 20);
        Room startRoom = new Room(
            mapWidth / 2 - startSize / 2,
            mapHeight / 2 - startSize / 2,
            startSize,
            startSize
        );
        startRoom.type = RoomType.Start;
        AssignRoomStyle(startRoom);
        CarveRoom(startRoom);
        rooms.Add(startRoom);

        int attempts = 0;
        int maxAttempts = targetRooms * 10;

        while (rooms.Count < targetRooms && attempts < maxAttempts)
        {
            attempts++;
            
            int width = rng.Next(minRoomSize, maxRoomSize);
            int height = rng.Next(minRoomSize, maxRoomSize);
            int x = rng.Next(5, mapWidth - width - 5);
            int y = rng.Next(5, mapHeight - height - 5);

            Room newRoom = new Room(x, y, width, height);
            
            // Determina tipo di stanza
            DetermineRoomType(newRoom);
            
            // Alcune stanze speciali sono più grandi
            if (newRoom.type == RoomType.Large || newRoom.type == RoomType.Combat)
            {
                newRoom.width = Mathf.Max(newRoom.width, maxRoomSize - 5);
                newRoom.height = Mathf.Max(newRoom.height, maxRoomSize - 5);
            }

            bool valid = true;
            foreach (Room room in rooms)
            {
                if (newRoom.Intersects(room))
                {
                    valid = false;
                    break;
                }
            }

            if (valid)
            {
                AssignRoomStyle(newRoom);
                CarveRoom(newRoom);
                rooms.Add(newRoom);
            }
        }

        Debug.Log($"📦 Generate {rooms.Count} stanze");
    }

    void DetermineRoomType(Room room)
    {
        float roll = (float)rng.NextDouble();
        
        if (roll < 0.15f)
            room.type = RoomType.Large;
        else if (roll < 0.3f)
            room.type = RoomType.Combat;
        else if (roll < 0.4f)
            room.type = RoomType.Treasure;
        else if (roll < 0.5f)
            room.type = RoomType.Library;
        else if (roll < 0.55f)
            room.type = RoomType.Prison;
        else
            room.type = RoomType.Normal;
    }

    void AssignRoomStyle(Room room)
    {
        if (useDifferentFloorsPerRoom)
        {
            float styleRoll = (float)rng.NextDouble();
            
            if (styleRoll < 0.4f && floorTilesNormal.Length > 0)
                room.floorStyle = floorTilesNormal;
            else if (styleRoll < 0.7f && floorTilesDark.Length > 0)
                room.floorStyle = floorTilesDark;
            else if (floorTilesLight.Length > 0)
                room.floorStyle = floorTilesLight;
            else
                room.floorStyle = floorTilesNormal;
        }
        else
        {
            room.floorStyle = floorTilesNormal;
        }

        // Assegna stile muri
        float wallRoll = (float)rng.NextDouble();
        
        if (wallRoll < 0.5f && wallTilesStone.Length > 0)
            room.wallStyle = wallTilesStone;
        else if (wallRoll < 0.8f && wallTilesBrick.Length > 0)
            room.wallStyle = wallTilesBrick;
        else if (wallTilesDamaged.Length > 0)
            room.wallStyle = wallTilesDamaged;
        else
            room.wallStyle = wallTilesStone;
    }

    void CarveRoom(Room room)
    {
        for (int x = room.x; x < room.x + room.width; x++)
        {
            for (int y = room.y; y < room.y + room.height; y++)
            {
                if (IsInBounds(x, y))
                {
                    map[x, y] = 1;
                }
            }
        }
    }

    void ConnectAllRooms()
    {
        // MST per connessioni base
        List<Room> connected = new List<Room> { rooms[0] };
        List<Room> unconnected = new List<Room>(rooms);
        unconnected.RemoveAt(0);

        while (unconnected.Count > 0)
        {
            Room bestRoom = null;
            Room bestTarget = null;
            float shortestDist = float.MaxValue;

            foreach (Room room in unconnected)
            {
                foreach (Room target in connected)
                {
                    float dist = Vector2Int.Distance(room.Center(), target.Center());
                    if (dist < shortestDist)
                    {
                        shortestDist = dist;
                        bestRoom = room;
                        bestTarget = target;
                    }
                }
            }

            if (bestRoom != null)
            {
                CreateCorridor(bestRoom.Center(), bestTarget.Center());
                connected.Add(bestRoom);
                unconnected.Remove(bestRoom);
            }
        }

        // Aggiungi loop extra per varietà
        int extraConnections = rooms.Count / 3;
        for (int i = 0; i < extraConnections; i++)
        {
            Room a = rooms[rng.Next(rooms.Count)];
            Room b = rooms[rng.Next(rooms.Count)];
            
            if (a != b && Vector2Int.Distance(a.Center(), b.Center()) < 40)
            {
                CreateCorridor(a.Center(), b.Center());
            }
        }
    }

    void CreateCorridor(Vector2Int start, Vector2Int end)
    {
        Vector2Int current = start;
        bool horizontal = rng.Next(2) == 0;

        while (current != end)
        {
            // Scava il corridoio
            for (int dx = -corridorWidth / 2; dx <= corridorWidth / 2; dx++)
            {
                for (int dy = -corridorWidth / 2; dy <= corridorWidth / 2; dy++)
                {
                    int x = current.x + dx;
                    int y = current.y + dy;
                    
                    if (IsInBounds(x, y))
                        map[x, y] = 1;
                }
            }

            // Avanza
            if (horizontal)
            {
                if (current.x != end.x)
                {
                    current.x += (current.x < end.x) ? 1 : -1;
                    
                    // Cambia direzione casualmente
                    if (rng.NextDouble() < corridorWindingChance)
                        horizontal = false;
                }
                else
                {
                    horizontal = false;
                }
            }
            else
            {
                if (current.y != end.y)
                {
                    current.y += (current.y < end.y) ? 1 : -1;
                    
                    if (rng.NextDouble() < corridorWindingChance)
                        horizontal = true;
                }
                else
                {
                    horizontal = true;
                }
            }
        }
    }

    void PlaceAllTiles()
    {
        for (int x = 0; x < mapWidth; x++)
        {
            for (int y = 0; y < mapHeight; y++)
            {
                Vector3Int pos = new Vector3Int(x, y, 0);

                if (map[x, y] == 1)
                {
                    // Trova la stanza a cui appartiene questo tile
                    Room room = GetRoomAt(x, y);
                    
                    TileBase floorTile;
                    if (room != null && room.floorStyle.Length > 0)
                    {
                        floorTile = room.floorStyle[rng.Next(room.floorStyle.Length)];
                    }
                    else if (floorTilesNormal.Length > 0)
                    {
                        floorTile = floorTilesNormal[rng.Next(floorTilesNormal.Length)];
                    }
                    else
                    {
                        continue;
                    }

                    if (groundTilemap)
                        groundTilemap.SetTile(pos, floorTile);
                    
                    // Aggiungi crepe casuali
                    if (crackTiles != null && crackTiles.Length > 0 && rng.NextDouble() < 0.05f)
                    {
                        if (detailsTilemap)
                            detailsTilemap.SetTile(pos, crackTiles[rng.Next(crackTiles.Length)]);
                    }
                }
                else if (IsWallTile(x, y))
                {
                    // Trova la stanza più vicina per lo stile
                    Room nearestRoom = GetNearestRoom(x, y);
                    
                    TileBase wallTile;
                    if (nearestRoom != null && nearestRoom.wallStyle.Length > 0)
                    {
                        wallTile = nearestRoom.wallStyle[rng.Next(nearestRoom.wallStyle.Length)];
                    }
                    else if (wallTilesStone.Length > 0)
                    {
                        wallTile = wallTilesStone[rng.Next(wallTilesStone.Length)];
                    }
                    else
                    {
                        continue;
                    }

                    if (wallTilemap)
                        wallTilemap.SetTile(pos, wallTile);
                }
            }
        }
    }

    void PopulateRooms()
    {
        Vector3 playerSpawnPos = GetPlayerSpawnPosition();
        
        foreach (Room room in rooms)
        {
            PopulateRoom(room);
            
            // Spawna nemici
            if (enemySpawner != null)
            {
                enemySpawner.SpawnEnemiesInRoom(room, playerSpawnPos, transform);
            }
        }
        
        if (enemySpawner != null)
        {
            Debug.Log($"👾 Spawnati {enemySpawner.GetEnemyCount()} nemici");
        }
    }

    void PopulateRoom(Room room)
    {
        switch (room.type)
        {
            case RoomType.Start:
                PlaceLights(room, lightDensity * 0.4f); // Ancora meno luci
                break;
                
            case RoomType.Large:
                PlacePillars(room);
                PlaceLights(room, lightDensity * 0.8f); // Meno luci
                PlaceDecorations(room, decorationDensity * 0.5f);
                break;
                
            case RoomType.Combat:
                PlaceLights(room, lightDensity * 1.0f); // Normale
                PlaceHazards(room, hazardDensity * 2f);
                break;
                
            case RoomType.Treasure:
                PlaceLights(room, lightDensity * 1.5f); // Più luminose
                PlaceChests(room);
                break;
                
            case RoomType.Library:
                PlaceLights(room, lightDensity * 0.6f);
                PlaceFurniture(room);
                break;
                
            case RoomType.Prison:
                PlaceLights(room, lightDensity * 0.2f); // Molto buio
                PlaceDecorations(room, decorationDensity * 0.8f);
                break;
                
            default:
                PlaceLights(room, lightDensity * 0.6f); // Ridotto
                PlaceDecorations(room, decorationDensity);
                if (rng.NextDouble() < 0.3f)
                    PlaceHazards(room, hazardDensity);
                break;
        }
    }

    void PlaceLights(Room room, float density)
    {
        if ((candlePrefabs == null || candlePrefabs.Length == 0) && 
            (torchPrefabs == null || torchPrefabs.Length == 0))
            return;

        int count = Mathf.RoundToInt(room.width * room.height * density);

        for (int i = 0; i < count; i++)
        {
            Vector3 pos = GetRandomPosInRoom(room);
            
            bool useCandle = rng.NextDouble() > 0.4f;
            GameObject[] prefabs = useCandle ? candlePrefabs : torchPrefabs;
            
            if (prefabs != null && prefabs.Length > 0)
            {
                GameObject light = Instantiate(prefabs[rng.Next(prefabs.Length)], pos, Quaternion.identity, transform);
                spawnedObjects.Add(light);
            }
        }
    }

    void PlacePillars(Room room)
    {
        if (pillarTiles == null || pillarTiles.Length == 0 || decorTilemap == null)
            return;

        int count = Mathf.RoundToInt(room.width * room.height * pillarDensity);

        for (int i = 0; i < count; i++)
        {
            int x = rng.Next(room.x + 3, room.x + room.width - 3);
            int y = rng.Next(room.y + 3, room.y + room.height - 3);
            
            Vector3Int pos = new Vector3Int(x, y, 0);
            decorTilemap.SetTile(pos, pillarTiles[rng.Next(pillarTiles.Length)]);
        }
    }

    void PlaceDecorations(Room room, float density)
    {
        if (debrisTiles == null || debrisTiles.Length == 0 || decorTilemap == null)
            return;

        int count = Mathf.RoundToInt(room.width * room.height * density);

        for (int i = 0; i < count; i++)
        {
            int x = rng.Next(room.x + 2, room.x + room.width - 2);
            int y = rng.Next(room.y + 2, room.y + room.height - 2);
            
            Vector3Int pos = new Vector3Int(x, y, 0);
            decorTilemap.SetTile(pos, debrisTiles[rng.Next(debrisTiles.Length)]);
        }
    }

    void PlaceFurniture(Room room)
    {
        if (furnitureTiles == null || furnitureTiles.Length == 0 || decorTilemap == null)
            return;

        int count = rng.Next(3, 8);

        for (int i = 0; i < count; i++)
        {
            int x = rng.Next(room.x + 2, room.x + room.width - 2);
            int y = rng.Next(room.y + 2, room.y + room.height - 2);
            
            Vector3Int pos = new Vector3Int(x, y, 0);
            decorTilemap.SetTile(pos, furnitureTiles[rng.Next(furnitureTiles.Length)]);
        }
    }

    void PlaceHazards(Room room, float density)
    {
        if (hazardPrefabs == null || hazardPrefabs.Length == 0)
            return;

        int count = Mathf.RoundToInt(room.width * room.height * density);

        for (int i = 0; i < count; i++)
        {
            Vector3 pos = GetRandomPosInRoom(room);
            GameObject hazard = Instantiate(hazardPrefabs[rng.Next(hazardPrefabs.Length)], pos, Quaternion.identity, transform);
            spawnedObjects.Add(hazard);
        }
    }

    void PlaceChests(Room room)
    {
        if (chestPrefab == null)
            return;

        Vector3 centerPos = groundTilemap.CellToWorld(new Vector3Int(
            room.x + room.width / 2,
            room.y + room.height / 2,
            0
        )) + new Vector3(0.5f, 0.5f, 0);

        GameObject chest = Instantiate(chestPrefab, centerPos, Quaternion.identity, transform);
        spawnedObjects.Add(chest);
    }

    void AddAtmosphere()
    {
        // Aggiungi tappeti in alcune stanze
        if (rugTiles != null && rugTiles.Length > 0 && decorTilemap != null)
        {
            foreach (Room room in rooms)
            {
                if (rng.NextDouble() < 0.3f) // 30% delle stanze
                {
                    int rugX = room.x + room.width / 2 - 2;
                    int rugY = room.y + room.height / 2 - 2;
                    
                    for (int dx = 0; dx < 4; dx++)
                    {
                        for (int dy = 0; dy < 4; dy++)
                        {
                            Vector3Int pos = new Vector3Int(rugX + dx, rugY + dy, 0);
                            if (rng.NextDouble() < 0.8f)
                                decorTilemap.SetTile(pos, rugTiles[rng.Next(rugTiles.Length)]);
                        }
                    }
                }
            }
        }
    }

    // Helper methods
    Room GetRoomAt(int x, int y)
    {
        foreach (Room room in rooms)
        {
            if (x >= room.x && x < room.x + room.width &&
                y >= room.y && y < room.y + room.height)
            {
                return room;
            }
        }
        return null;
    }

    Room GetNearestRoom(int x, int y)
    {
        Room nearest = null;
        float minDist = float.MaxValue;

        foreach (Room room in rooms)
        {
            float dist = Vector2.Distance(new Vector2(x, y), room.Center());
            if (dist < minDist)
            {
                minDist = dist;
                nearest = room;
            }
        }

        return nearest;
    }

    bool IsWallTile(int x, int y)
    {
        if (!IsInBounds(x, y) || map[x, y] == 1)
            return false;

        for (int dx = -1; dx <= 1; dx++)
        {
            for (int dy = -1; dy <= 1; dy++)
            {
                int nx = x + dx;
                int ny = y + dy;
                
                if (IsInBounds(nx, ny) && map[nx, ny] == 1)
                    return true;
            }
        }

        return false;
    }

    bool IsInBounds(int x, int y)
    {
        return x >= 0 && x < mapWidth && y >= 0 && y < mapHeight;
    }

    Vector3 GetRandomPosInRoom(Room room)
    {
        int x = rng.Next(room.x + 2, room.x + room.width - 2);
        int y = rng.Next(room.y + 2, room.y + room.height - 2);
        
        Vector3 worldPos = groundTilemap.CellToWorld(new Vector3Int(x, y, 0));
        return worldPos + new Vector3(0.5f, 0.5f, 0);
    }

    public Vector3 GetPlayerSpawnPosition()
    {
        if (rooms.Count > 0)
        {
            Room startRoom = rooms[0];
            return GetRandomPosInRoom(startRoom);
        }
        return Vector3.zero;
    }
}