using System;
using UnityEngine;

[CreateAssetMenu(fileName = "MapConfig", menuName = "SlitherRoyale/MapConfig")]
public class MapConfig : ScriptableObject
{
    [Header("Identity")]
    public string mapName; // used by BiomeAmbientVFX to pick particle palette

    [Header("Arena")]
    public float arenaRadius = 800f;
    public Color bgColor = new Color(0.043f, 0.055f, 0.078f);
    public Color gridColor = new Color(0.15f, 0.15f, 0.2f);
    public Color wallColor = new Color(0.42f, 0.31f, 1f);

    [Header("Pellets")]
    public int pelletCount = 80;

    [Header("Neon Grid")]
    public bool hasSpeedPads;
    public bool hasLaserFences;

    [Header("Coral Reef")]
    public bool hasCurrents;
    public bool hasJellyfish;

    [Header("Magma Core")]
    public bool hasShrinkZone;
    public bool hasLavaPools;
    public float shrinkInterval = 10f;
    public float shrinkDamage = 20f;

    [Header("Candy Kingdom")]
    public bool hasSyrupZones;
    public bool hasGiantPellets;

    [Header("Space Station")]
    public bool hasLowGravity;
    public bool hasAirlockZones;

    [Header("Haunted Forest")]
    public bool hasDarknessEvents;
    public bool hasWisps;

    [Header("Giant Pellets")]
    public float giantPelletValue = 10f;
    public int giantPelletCount = 5;

    public static MapConfig NeonGrid()
    {
        var c = CreateInstance<MapConfig>();
        c.mapName = "neon grid";
        c.name = "Neon Grid";
        c.arenaRadius = 800f;
        c.bgColor = new Color(0.043f, 0.055f, 0.078f);
        c.gridColor = new Color(0.15f, 0.15f, 0.6f);
        c.wallColor = new Color(0.42f, 0.31f, 1f);
        c.pelletCount = 80;
        c.hasSpeedPads = true;
        c.hasLaserFences = true;
        return c;
    }

    public static MapConfig CoralReef()
    {
        var c = CreateInstance<MapConfig>();
        c.mapName = "coral reef";
        c.name = "Coral Reef";
        c.arenaRadius = 850f;
        c.bgColor = new Color(0.0f, 0.1f, 0.2f);
        c.gridColor = new Color(0.0f, 0.3f, 0.4f);
        c.wallColor = new Color(0.25f, 0.88f, 0.77f);
        c.pelletCount = 90;
        c.hasCurrents = true;
        c.hasJellyfish = true;
        return c;
    }

    public static MapConfig MagmaCore()
    {
        var c = CreateInstance<MapConfig>();
        c.mapName = "magma core";
        c.name = "Magma Core";
        c.arenaRadius = 900f;
        c.bgColor = new Color(0.1f, 0.02f, 0.0f);
        c.gridColor = new Color(0.3f, 0.1f, 0.0f);
        c.wallColor = new Color(1f, 0.42f, 0.36f);
        c.pelletCount = 70;
        c.hasShrinkZone = true;
        c.hasLavaPools = true;
        c.shrinkInterval = 10f;
        c.shrinkDamage = 20f;
        return c;
    }

    public static MapConfig CandyKingdom()
    {
        var c = CreateInstance<MapConfig>();
        c.mapName = "candy kingdom";
        c.name = "Candy Kingdom";
        c.arenaRadius = 750f;
        c.bgColor = new Color(0.15f, 0.06f, 0.18f);
        c.gridColor = new Color(0.35f, 0.15f, 0.35f);
        c.wallColor = new Color(1f, 0.5f, 0.7f);
        c.pelletCount = 90;
        c.hasSyrupZones = true;
        c.hasGiantPellets = true;
        c.giantPelletValue = 10f;
        c.giantPelletCount = 5;
        return c;
    }

    public static MapConfig SpaceStation()
    {
        var c = CreateInstance<MapConfig>();
        c.mapName = "space station";
        c.name = "Space Station";
        c.arenaRadius = 850f;
        c.bgColor = new Color(0.01f, 0.01f, 0.04f);
        c.gridColor = new Color(0.1f, 0.1f, 0.25f);
        c.wallColor = new Color(0.6f, 0.6f, 0.8f);
        c.pelletCount = 75;
        c.hasLowGravity = true;
        c.hasAirlockZones = true;
        return c;
    }

    public static MapConfig HauntedForest()
    {
        var c = CreateInstance<MapConfig>();
        c.mapName = "haunted forest";
        c.name = "Haunted Forest";
        c.arenaRadius = 800f;
        c.bgColor = new Color(0.02f, 0.0f, 0.03f);
        c.gridColor = new Color(0.08f, 0.05f, 0.1f);
        c.wallColor = new Color(0.5f, 0.4f, 0.1f);
        c.pelletCount = 85;
        c.hasDarknessEvents = true;
        c.hasWisps = true;
        return c;
    }
}
