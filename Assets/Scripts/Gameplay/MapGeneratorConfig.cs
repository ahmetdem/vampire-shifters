using UnityEngine;
using UnityEngine.Tilemaps;

/// <summary>
/// Configuration for procedural map generation.
/// Create via: Right-click in Project > Create > Map > Map Generator Config
/// </summary>
[CreateAssetMenu(fileName = "MapGeneratorConfig", menuName = "Map/Map Generator Config")]
public class MapGeneratorConfig : ScriptableObject
{
    [Header("Map Size")]
    [Tooltip("Width of the map in tiles")]
    public int mapWidth = 100;
    
    [Tooltip("Height of the map in tiles")]
    public int mapHeight = 100;

    [Header("Noise Settings")]
    [Tooltip("Scale of Perlin noise - lower = larger patches, higher = smaller patches")]
    [Range(0.01f, 0.5f)]
    public float noiseScale = 0.1f;
    
    [Tooltip("Threshold for grass vs dirt. Higher = more grass")]
    [Range(0f, 1f)]
    public float grassThreshold = 0.5f;

    [Header("Ground Tiles")]
    [Tooltip("Main fill tile for grassy areas (usually the center grass tile)")]
    public TileBase grassFillTile;
    
    [Tooltip("Main fill tile for dirt/path areas")]
    public TileBase dirtFillTile;
    
    [Tooltip("All grass edge tiles (corners and sides) for auto-tiling")]
    public TileBase[] grassEdgeTiles;
    
    [Tooltip("Ground variation tiles for visual variety")]
    public TileBase[] groundVariationTiles;

    [Header("Decoration Settings")]
    [Tooltip("Small decorations (bushes, mushrooms, small rocks)")]
    public TileBase[] smallDecorations;
    
    [Tooltip("Chance to spawn small decoration per tile (0-1)")]
    [Range(0f, 0.2f)]
    public float smallDecorationChance = 0.05f;
    
    [Tooltip("Medium decorations (stumps, logs, bones)")]
    public TileBase[] mediumDecorations;
    
    [Tooltip("Chance to spawn medium decoration per tile (0-1)")]
    [Range(0f, 0.1f)]
    public float mediumDecorationChance = 0.02f;
    
    [Tooltip("Large decorations (trees, big rocks) - these will block movement")]
    public TileBase[] largeDecorations;
    
    [Tooltip("Chance to spawn large decoration per tile (0-1)")]
    [Range(0f, 0.05f)]
    public float largeDecorationChance = 0.01f;

    [Header("Spawn Area")]
    [Tooltip("Radius around center to keep clear of large decorations (for player spawn)")]
    public int spawnSafeRadius = 5;
}
