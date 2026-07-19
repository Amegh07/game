using UnityEngine;
using System.Collections.Generic;

public class MuseumLevelBuilder : MonoBehaviour
{
    const float WALL_H = 4f;
    const float WALL_T = 0.25f;

    [Header("Build on Start")]
    public bool buildOnStart = false;

    public Material floorMat { get; private set; }
    public Material wallMat { get; private set; }
    public Material ceilingMat { get; private set; }
    public Material accentMat { get; private set; }
    public Material glassMat { get; private set; }
    public Material securityMat { get; private set; }
    public Material vaultMat { get; private set; }

    private Transform geoRoot;
    private Transform gameRoot;
    private Transform missionRoot;
    private List<Material> mats = new();
    private LockdownVisualController lockdown;
    private Transform envRoot;
    private bool hasBuilt;

    public Transform GameplayRoot => gameRoot;
    public Transform MissionRoot => missionRoot;

    void Start() { if (buildOnStart) BuildLevel(); }

    public void BuildLevel()
    {
        if (hasBuilt) return;
        hasBuilt = true;
        geoRoot = new GameObject("_Geometry").transform;
        gameRoot = new GameObject("_Gameplay").transform;
        missionRoot = new GameObject("_Mission").transform;
        envRoot = new GameObject("_Environment").transform;

        CreateMaterials();
        MuseumPropFactory.Initialize(envRoot, mats.ToArray());
        MuseumLightingSystem.Initialize(envRoot);
        NavigationGuide.Initialize(envRoot);
        StorytellingProps.Initialize(envRoot);

        lockdown = LockdownVisualController.EnsureInstance();

        BuildEntrance(0f, 6f, 16f, 12f);
        BuildLobby(0f, -4f, 16f, 14f);
        BuildExhibitionHall(0f, -18f, 22f, 16f);
        BuildSecurityOffice(-9f, -14f, 8f, 8f);
        BuildServerRoom(9f, -14f, 8f, 8f);
        BuildVaultCorridor(0f, -34f, 14f, 6f);
        BuildMainVault(0f, -44f, 10f, 10f);
        BuildEscapeRoute(0f, -54f, 6f, 10f);
        BuildOuterWalls(-11f, 11f, 10f, -60f);

        BuildNavigationAids();
        BuildAudioHooks();

        SetStaticFlags(geoRoot);

        Debug.Log("[MuseumLevelBuilder] Level complete.");
    }

    private void SetStaticFlags(Transform root)
    {
        foreach (Transform child in root)
        {
            child.gameObject.isStatic = true;
        }
    }

    void CreateMaterials()
    {
        floorMat = MakeMat("Floor", new Color(0.15f, 0.15f, 0.16f));
        wallMat = MakeMat("Wall", new Color(0.82f, 0.80f, 0.76f));
        ceilingMat = MakeMat("Ceiling", new Color(0.92f, 0.92f, 0.93f));
        accentMat = MakeMat("Accent", new Color(0.45f, 0.30f, 0.20f));
        glassMat = MakeMat("Glass", new Color(0.7f, 0.8f, 0.9f, 0.35f));
        securityMat = MakeMat("Security", new Color(0.12f, 0.12f, 0.15f));
        vaultMat = MakeMat("Vault", new Color(0.25f, 0.22f, 0.20f));
    }

    Material MakeMat(string n, Color c)
    {
        var m = new Material(Shader.Find("Universal Render Pipeline/Lit")) { name = n, color = c };
        mats.Add(m); return m;
    }

    // --- Primitive builders ---

    GameObject Cube(string name, float x, float y, float z, float sx, float sy, float sz, Material mat)
    {
        var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
        go.name = name; go.transform.SetParent(geoRoot);
        go.transform.position = new Vector3(x, y + sy / 2f, z);
        go.transform.localScale = new Vector3(sx, sy, sz);
        var r = go.GetComponent<Renderer>(); if (r != null) r.material = mat;
        return go;
    }

    GameObject Floor(float x, float z, float w, float d, Material m = null) =>
        Cube("Floor", x, 0.05f, z, w, 0.1f, d, m ?? floorMat);

    GameObject Ceiling(float x, float z, float w, float d) =>
        Cube("Ceiling", x, WALL_H, z, w, 0.1f, d, ceilingMat);

    GameObject Wall(float x, float z, float w, float d, Material m = null) =>
        Cube("Wall", x, WALL_H / 2f, z, w, WALL_H, d, m ?? wallMat);

    void Pillar(float x, float z, float size = 0.4f) =>
        Cube("Pillar", x, WALL_H / 2f, z, size, WALL_H, size, wallMat);

    void Room(float cx, float cz, float w, float d, Material fl = null, Material wl = null)
    {
        Floor(cx, cz, w, d, fl);
        Ceiling(cx, cz, w, d);
        float hw = w / 2f, hd = d / 2f;
        Wall(cx, cz + hd, w, WALL_T, wl);
        Wall(cx, cz - hd, w, WALL_T, wl);
        Wall(cx - hw, cz, WALL_T, d, wl);
        Wall(cx + hw, cz, WALL_T, d, wl);
    }

    void DoorwayInWall(float cx, float cz, float w, bool northSouth, float doorOffset = 0f, float doorW = 2f, Material m = null)
    {
        float hw = w / 2f, hdw = doorW / 2f;
        if (northSouth)
        {
            float l = cx - hw, r = cx + hw, dL = doorOffset - hdw, dR = doorOffset + hdw;
            if (l < dL) Wall((l + dL) / 2f, cz, dL - l, WALL_T, m);
            if (dR < r) Wall((dR + r) / 2f, cz, r - dR, WALL_T, m);
        }
        else
        {
            float t = cz + hw, b = cz - hw, dT = doorOffset + hdw, dB = doorOffset - hdw;
            if (b < dB) Wall(cx, (b + dB) / 2f, WALL_T, dB - b, m);
            if (dT < t) Wall(cx, (dT + t) / 2f, WALL_T, t - dT, m);
        }
    }

    // --- Zones ---

    void BuildEntrance(float cx, float cz, float w, float d)
    {
        float hw = w / 2f, hd = d / 2f;
        Floor(cx, cz, w, d);
        Wall(cx, cz + hd, w, WALL_T);
        Wall(cx - hw, cz, WALL_T, d);
        Wall(cx + hw, cz, WALL_T, d);
        float openW = 4f;
        DoorwayInWall(cx, cz - hd, w, true, 0f, openW);

        // Reception desk
        Cube("ReceptionDesk", cx - 3f, 1.2f, cz + 2f, 2.5f, 0.8f, 1.5f, accentMat);
        MuseumPropFactory.ComputerSetup(cx - 3f, cz + 2.5f, 0f);
        MuseumPropFactory.Chair(cx - 1.8f, cz + 2.5f, 0f);
        MuseumPropFactory.TrashCan(cx + 3f, cz + 3f);
        MuseumPropFactory.PlantPot(cx + hw - 1f, cz, 0.8f);
        MuseumPropFactory.PlantPot(cx - hw + 1f, cz, 0.8f);
        MuseumPropFactory.InfoBoard(cx + 2f, cz + 4f, 180f);

        // Map right at entrance
        MuseumPropFactory.VisitorMap(cx, cz + hd - 1f, 180f);

        // Pillars
        Pillar(cx - hw + 1f, cz - hd + 0.5f);
        Pillar(cx + hw - 1f, cz - hd + 0.5f);

        // Lighting — warm
        MuseumLightingSystem.LightRoom(cx, cz, w, d, WALL_H, MuseumLightingSystem.RoomMood.Warm);

        // Audio
        AudioHookPlaceholder.Create("EntranceAmbient", AudioHookPlaceholder.AudioZoneType.AmbientMuseum,
            new Vector3(cx, 1f, cz), 10f, 0.6f, envRoot);
    }

    void BuildLobby(float cx, float cz, float w, float d)
    {
        float hw = w / 2f, hd = d / 2f;
        Room(cx, cz, w, d);
        DoorwayInWall(cx, cz + hd, w, true, 0f, 4f);
        DoorwayInWall(cx, cz - hd, w, true, 0f, 3f);

        // Gift shop counter (east)
        Cube("GiftCounter", cx + hw - 2f, 1f, cz + 1f, 2.5f, 0.6f, 1.5f, accentMat);
        MuseumPropFactory.Signage("GIFT SHOP", cx + hw - 2f, 2.5f, cz + 1f, 270f, new Color(0.3f, 0.2f, 0.1f));

        // Benches
        MuseumPropFactory.Bench(cx, cz + 3f, 0f);
        MuseumPropFactory.Bench(cx, cz - 3f, 0f);

        // Plants
        MuseumPropFactory.PlantPot(cx - 5f, cz + 2f, 1.2f);
        MuseumPropFactory.PlantPot(cx + 5f, cz - 2f, 1f);

        // Info board / statue
        MuseumPropFactory.Statue(cx, cz, 0f);
        MuseumPropFactory.Plaque(cx, cz + 0.8f, 0f);

        // Central pedestal (replaced statue base)
        var cyl = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        cyl.name = "PedestalBase"; cyl.transform.SetParent(geoRoot);
        cyl.transform.position = new Vector3(cx, 0.5f, cz);
        cyl.transform.localScale = new Vector3(0.8f, 0.5f, 0.8f);
        var cr = cyl.GetComponent<Renderer>(); if (cr != null) cr.material = accentMat;

        // Pillars
        Pillar(cx - 4f, cz - 3f); Pillar(cx + 4f, cz - 3f);
        Pillar(cx - 4f, cz + 3f); Pillar(cx + 4f, cz + 3f);

        // Storytelling — notice board
        StorytellingProps.NoticeBoard(cx - hw + 0.5f, cz + 4f, 90f);
        StorytellingProps.PinnedNote(cx + 2f, 1.8f, cz - hd + 0.3f, 0f);

        // Lighting — warm
        MuseumLightingSystem.LightRoom(cx, cz, w, d, WALL_H, MuseumLightingSystem.RoomMood.Warm);

        // Audio
        AudioHookPlaceholder.Create("LobbyAmbient", AudioHookPlaceholder.AudioZoneType.AmbientMuseum,
            new Vector3(cx, 1f, cz), 8f, 0.5f, envRoot);
        AudioHookPlaceholder.Create("LobbyFootstep", AudioHookPlaceholder.AudioZoneType.FootstepCarpet,
            new Vector3(cx, 0f, cz), 7f, 0.4f, envRoot);

        // Emergency light
        lockdown.RegisterEmergencyLight(MuseumLightingSystem.SpawnEmergencyLight(cx - hw + 1f, WALL_H - 0.3f, cz + hd - 1f));
        lockdown.RegisterEmergencyLight(MuseumLightingSystem.SpawnEmergencyLight(cx + hw - 1f, WALL_H - 0.3f, cz - hd + 1f));
    }

    void BuildExhibitionHall(float cx, float cz, float w, float d)
    {
        float hw = w / 2f, hd = d / 2f;
        Room(cx, cz, w, d);
        DoorwayInWall(cx, cz + hd, w, true, 0f, 3f);
        DoorwayInWall(cx, cz - hd, w, true, 0f, 2.5f);

        // Pillars
        for (int i = -1; i <= 1; i += 2)
            for (int j = -1; j <= 1; j += 2)
                Pillar(cx + i * 5f, cz + j * 5f);

        // Display cases — mix of full and empty, one broken
        MuseumPropFactory.DisplayCase(cx - 6f, cz + 5f, 0f, true, new Material(Shader.Find("Universal Render Pipeline/Lit")) { color = new Color(0.9f, 0.6f, 0.1f) });
        MuseumPropFactory.DisplayCase(cx + 6f, cz + 5f, 0f, true, new Material(Shader.Find("Universal Render Pipeline/Lit")) { color = new Color(0.7f, 0.2f, 0.5f) });
        MuseumPropFactory.DisplayCase(cx - 6f, cz, 0f, true, new Material(Shader.Find("Universal Render Pipeline/Lit")) { color = new Color(0.3f, 0.6f, 0.8f) });
        MuseumPropFactory.DisplayCase(cx + 6f, cz, 0f, false);
        MuseumPropFactory.DisplayCase(cx - 6f, cz - 5f, 0f, true, new Material(Shader.Find("Universal Render Pipeline/Lit")) { color = new Color(0.9f, 0.8f, 0.2f) });
        MuseumPropFactory.BrokenDisplayCase(cx + 6f, cz - 5f, 0f);

        // Plaques by each case
        MuseumPropFactory.Plaque(cx - 6f, cz + 5.8f, 0f);
        MuseumPropFactory.Plaque(cx + 6f, cz + 5.8f, 0f);
        MuseumPropFactory.Plaque(cx - 6f, cz + 0.8f, 0f);
        MuseumPropFactory.Plaque(cx + 6f, cz + 0.8f, 0f);
        MuseumPropFactory.Plaque(cx - 6f, cz - 4.2f, 0f);

        // Paintings on walls
        MuseumPropFactory.Painting(cx, 2f, cz + hd - 0.15f, 180f);
        MuseumPropFactory.Painting(cx + 3f, 2f, cz + hd - 0.15f, 180f);
        MuseumPropFactory.Painting(cx - 3f, 2f, cz + hd - 0.15f, 180f);

        // Barriers near restricted area
        MuseumPropFactory.Barrier(cx + 2f, cz - hd + 2f, 0f);
        MuseumPropFactory.Barrier(cx - 2f, cz - hd + 2f, 0f);

        // Seating
        MuseumPropFactory.Bench(cx, cz + 2f, 0f);

        // Signs
        MuseumPropFactory.Signage("GALLERY 1", cx - hw + 0.5f, 2.5f, cz, 90f, new Color(0.2f, 0.15f, 0.1f));
        NavigationGuide.RestrictedMark(cx, cz - hd + 0.5f, 0f);

        // Storytelling
        StorytellingProps.PinnedNote(cx - 4f, 2f, cz + hd - 0.2f, 0f);
        StorytellingProps.PinnedNote(cx + 2f, 1.8f, cz + hd - 0.2f, 0f);

        // Lighting — warm
        MuseumLightingSystem.LightRoom(cx, cz, w, d, WALL_H, MuseumLightingSystem.RoomMood.Warm);

        // Audio
        AudioHookPlaceholder.Create("ExhibitionFootstep", AudioHookPlaceholder.AudioZoneType.FootstepTile,
            new Vector3(cx, 0f, cz), 8f, 0.4f, envRoot);
        AudioHookPlaceholder.Create("HallEcho", AudioHookPlaceholder.AudioZoneType.EchoingHall,
            new Vector3(cx, 2f, cz), 12f, 0.3f, envRoot);

        // Emergency lights
        lockdown.RegisterEmergencyLight(MuseumLightingSystem.SpawnEmergencyLight(cx - hw + 1f, WALL_H - 0.3f, cz + hd - 1f));
        lockdown.RegisterEmergencyLight(MuseumLightingSystem.SpawnEmergencyLight(cx + hw - 1f, WALL_H - 0.3f, cz - hd + 1f));

        // Door to security office (west)
        DoorwayInWall(cx - hw, cz, d, false, cz + 3f, 2f);
        // Door to server room (east)
        DoorwayInWall(cx + hw, cz, d, false, cz + 3f, 2f);
    }

    void BuildSecurityOffice(float cx, float cz, float w, float d)
    {
        float hw = w / 2f, hd = d / 2f;
        Room(cx, cz, w, d, floorMat, securityMat);
        DoorwayInWall(cx + hw, cz, d, false, cz + 1f, 2f);

        // Desk setup
        Cube("SecurityDesk", cx, 0.9f, cz - 1f, 2f, 0.6f, 1.2f, securityMat);
        MuseumPropFactory.ComputerSetup(cx + 0.3f, cz - 1.2f, 0f);
        MuseumPropFactory.OfficeChair(cx - 0.5f, cz - 0.5f, 180f);
        MuseumPropFactory.MonitorWall(cx, 2f, cz + hd - 0.15f, 180f);

        // Storytelling
        StorytellingProps.CoffeeCup(cx + 0.3f, 0.93f, cz - 0.5f);
        StorytellingProps.OpenNotebook(cx - 0.5f, 0.93f, cz - 0.8f, 45f);
        StorytellingProps.ScatteredPaper(cx + 0.8f, 0.93f, cz - 0.3f, 30f);
        StorytellingProps.Clipboard(cx, 0.93f, cz + 0.5f, 0f);
        StorytellingProps.KeyHookBoard(cx - hw + 0.3f, 1.5f, cz, 90f);

        // Signs
        NavigationGuide.WallSign("SECURITY OFFICE", cx, 3f, cz + hd - 0.15f, 0f, new Color(0.1f, 0.05f, 0.15f));
        NavigationGuide.WallSign("AUTHORIZED PERSONNEL ONLY", cx - hw + 0.15f, 2.5f, cz, 90f, new Color(0.3f, 0.05f, 0.05f));

        // Lighting — cool fluorescent
        MuseumLightingSystem.LightRoom(cx, cz, w, d, WALL_H, MuseumLightingSystem.RoomMood.Office);

        // Audio
        AudioHookPlaceholder.Create("SecurityHVAC", AudioHookPlaceholder.AudioZoneType.HVAC,
            new Vector3(cx, 2f, cz), 5f, 0.4f, envRoot);
        AudioHookPlaceholder.Create("SecurityFootstep", AudioHookPlaceholder.AudioZoneType.FootstepTile,
            new Vector3(cx, 0f, cz), 4f, 0.3f, envRoot);

        // Emergency light
        lockdown.RegisterEmergencyLight(MuseumLightingSystem.SpawnEmergencyLight(cx, WALL_H - 0.3f, cz));
    }

    void BuildServerRoom(float cx, float cz, float w, float d)
    {
        float hw = w / 2f, hd = d / 2f;
        Room(cx, cz, w, d, floorMat, securityMat);
        DoorwayInWall(cx - hw, cz, d, false, cz + 1f, 2f);

        // Racks
        for (int i = -1; i <= 1; i++)
            MuseumPropFactory.ServerRack(cx + i * 1.5f, cz + 1f, 0f);

        // Storage
        MuseumPropFactory.Shelf(cx, cz - 2f, 0f);
        MuseumPropFactory.Crate(cx - 1.5f, cz - 2.5f, new Color(0.4f, 0.4f, 0.4f));
        MuseumPropFactory.Crate(cx + 1.5f, cz - 2.5f, new Color(0.3f, 0.3f, 0.3f));

        // Storytelling
        StorytellingProps.MaintenanceLog(cx, 0.93f, cz - 1f, 0f);
        StorytellingProps.ScatteredPaper(cx + 1f, 0.93f, cz - 1.5f, 15f);

        // Signs
        NavigationGuide.WallSign("SERVER ROOM", cx, 3f, cz + hd - 0.15f, 0f, new Color(0.05f, 0.1f, 0.2f));
        NavigationGuide.WallSign("RESTRICTED", cx - hw + 0.15f, 2.5f, cz, 90f, new Color(0.3f, 0.05f, 0.05f));

        // Lighting — cool
        MuseumLightingSystem.LightRoom(cx, cz, w, d, WALL_H, MuseumLightingSystem.RoomMood.Cool);

        // Audio
        AudioHookPlaceholder.Create("ServerHum", AudioHookPlaceholder.AudioZoneType.ServerHum,
            new Vector3(cx, 1.5f, cz), 6f, 0.7f, envRoot);

        // Emergency light
        lockdown.RegisterEmergencyLight(MuseumLightingSystem.SpawnEmergencyLight(cx, WALL_H - 0.3f, cz));
    }

    void BuildVaultCorridor(float cx, float cz, float w, float d)
    {
        float hw = w / 2f, hd = d / 2f;
        Floor(cx, cz, w, d);
        Ceiling(cx, cz, w, d);
        Wall(cx, cz + hd, w, WALL_T, securityMat);
        Wall(cx, cz - hd, w, WALL_T, vaultMat);
        Wall(cx - hw, cz, WALL_T, d, securityMat);
        Wall(cx + hw, cz, WALL_T, d, vaultMat);

        DoorwayInWall(cx, cz + hd, w, true, 0f, 2.5f);
        DoorwayInWall(cx, cz - hd, w, true, 0f, 2f);

        // Lights along corridor
        for (int i = -1; i <= 1; i += 2)
            Cube("Light", cx + i * 3f, WALL_H - 0.2f, cz, 1f, 0.1f, 0.3f, ceilingMat);

        // Restricted signs
        NavigationGuide.RestrictedMark(cx, cz + hd - 0.5f, 0f);
        NavigationGuide.RestrictedMark(cx, cz - hd + 0.5f, 0f);
        NavigationGuide.WallSign("RESTRICTED AREA", cx - hw + 0.15f, 2.5f, cz, 90f, new Color(0.3f, 0f, 0f));
        NavigationGuide.WallSign("RESTRICTED AREA", cx + hw - 0.15f, 2.5f, cz, -90f, new Color(0.3f, 0f, 0f));

        // Floor line leading to vault
        NavigationGuide.FloorArrow(cx, cz + 2f, 180f, 1.5f);
        NavigationGuide.FloorArrow(cx, cz - 1f, 180f, 1.5f);

        // Lighting — cold
        MuseumLightingSystem.LightRoom(cx, cz, w, d, WALL_H, MuseumLightingSystem.RoomMood.Cold);

        // Audio
        AudioHookPlaceholder.Create("VaultCorridorFootstep", AudioHookPlaceholder.AudioZoneType.FootstepMetal,
            new Vector3(cx, 0f, cz), 5f, 0.5f, envRoot);
        AudioHookPlaceholder.Create("VaultCorridorHVAC", AudioHookPlaceholder.AudioZoneType.HVAC,
            new Vector3(cx, 2f, cz), 4f, 0.3f, envRoot);

        // Emergency lights
        lockdown.RegisterEmergencyLight(MuseumLightingSystem.SpawnEmergencyLight(cx - hw + 1f, WALL_H - 0.3f, cz));
        lockdown.RegisterEmergencyLight(MuseumLightingSystem.SpawnEmergencyLight(cx + hw - 1f, WALL_H - 0.3f, cz));
    }

    void BuildMainVault(float cx, float cz, float w, float d)
    {
        float hw = w / 2f, hd = d / 2f;
        Room(cx, cz, w, d, vaultMat, vaultMat);
        DoorwayInWall(cx, cz + hd, w, true, 0f, 2f);

        // Reinforced walls
        Wall(cx - hw + WALL_T, cz, WALL_T, d, vaultMat);
        Wall(cx + hw - WALL_T, cz, WALL_T, d, vaultMat);

        // Pedestal
        var cyl = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        cyl.name = "VaultPedestal"; cyl.transform.SetParent(geoRoot);
        cyl.transform.position = new Vector3(cx, 0.8f, cz);
        cyl.transform.localScale = new Vector3(0.8f, 0.8f, 0.8f);
        var cr = cyl.GetComponent<Renderer>(); if (cr != null) cr.material = new Material(Shader.Find("Universal Render Pipeline/Lit")) { color = Color.black };

        // Additional display pedestals (empty)
        MuseumPropFactory.DisplayCase(cx - 3f, cz - 2f, 0f, false);
        MuseumPropFactory.DisplayCase(cx + 3f, cz - 2f, 0f, false);

        // Storage
        MuseumPropFactory.Crate(cx - 3f, cz + 2f, new Color(0.2f, 0.2f, 0.2f));
        MuseumPropFactory.Crate(cx + 3f, cz + 2f, new Color(0.25f, 0.22f, 0.2f));

        // Signs
        NavigationGuide.WallSign("VAULT", cx, 3f, cz + hd - 0.15f, 0f, new Color(0.1f, 0.05f, 0.15f));
        NavigationGuide.WallSign("MAXIMUM SECURITY", cx - hw + 0.15f, 2.5f, cz, 90f, new Color(0.3f, 0f, 0f));

        // Floor markings
        NavigationGuide.RestrictedMark(cx, cz + hd - 0.5f, 0f);

        // Lighting — cold white
        MuseumLightingSystem.LightRoom(cx, cz, w, d, WALL_H, MuseumLightingSystem.RoomMood.Vault);

        // Audio
        AudioHookPlaceholder.Create("VaultAmbient", AudioHookPlaceholder.AudioZoneType.AmbientMuseum,
            new Vector3(cx, 1f, cz), 5f, 0.2f, envRoot);
        AudioHookPlaceholder.Create("VaultFootstep", AudioHookPlaceholder.AudioZoneType.FootstepMetal,
            new Vector3(cx, 0f, cz), 5f, 0.5f, envRoot);

        // Emergency lights
        lockdown.RegisterEmergencyLight(MuseumLightingSystem.SpawnEmergencyLight(cx - hw + 1f, WALL_H - 0.3f, cz));
        lockdown.RegisterEmergencyLight(MuseumLightingSystem.SpawnEmergencyLight(cx + hw - 1f, WALL_H - 0.3f, cz));
    }

    void BuildEscapeRoute(float cx, float cz, float w, float d)
    {
        float hw = w / 2f, hd = d / 2f;
        Floor(cx, cz, w, d);
        Ceiling(cx, cz, w, d);
        Wall(cx - hw, cz, WALL_T, d);
        Wall(cx + hw, cz, WALL_T, d);
        DoorwayInWall(cx, cz + hd, w, true, 0f, 2f);

        // Exit signage
        NavigationGuide.ExitSign(cx, 2.5f, cz - hd + 0.5f, 0f);
        NavigationGuide.ExitSign(cx, 2.5f, cz + hd - 0.5f, 180f);

        // Floor arrows pointing south (to exit)
        NavigationGuide.FloorArrow(cx, cz + 2f, 180f, 2f);
        NavigationGuide.FloorArrow(cx, cz - 1f, 180f, 2f);
        NavigationGuide.FloorArrow(cx, cz - 4f, 180f, 2f);

        // Signs
        NavigationGuide.WallSign("EMERGENCY EXIT", cx, 2.5f, cz - hd + 0.15f, 0f, new Color(0f, 0.3f, 0f));

        // Wet floor prop
        MuseumPropFactory.WetFloorSign(cx + 1.5f, cz + 1f);

        // Barrier at exit
        MuseumPropFactory.Barrier(cx, cz - hd + 1f, 0f);

        // Lighting — cool
        MuseumLightingSystem.LightRoom(cx, cz, w, d, WALL_H, MuseumLightingSystem.RoomMood.Cold);

        // Audio
        AudioHookPlaceholder.Create("EscapeFootstep", AudioHookPlaceholder.AudioZoneType.FootstepMetal,
            new Vector3(cx, 0f, cz), 6f, 0.4f, envRoot);

        // Emergency lights
        lockdown.RegisterEmergencyLight(MuseumLightingSystem.SpawnEmergencyLight(cx, WALL_H - 0.3f, cz));
    }

    void BuildOuterWalls(float xMin, float xMax, float zMax, float zMin)
    {
        float w = xMax - xMin, d = zMax - zMin;
        Wall((xMin + xMax) / 2f, zMax, w, WALL_T);
        Wall(xMin, (zMin + zMax) / 2f, WALL_T, d);
        Wall(xMax, (zMin + zMax) / 2f, WALL_T, d);
        DoorwayInWall((xMin + xMax) / 2f, zMin, w, true, 0f, 6f);
    }

    void BuildNavigationAids()
    {
        // Main route: Entrance → Lobby → Exhibition → Vault → Escape
        NavigationGuide.FloorLine(-1.5f, 8f, -1.5f, 3f);
        NavigationGuide.FloorLine(-1.5f, 3f, -1.5f, -11f);
        NavigationGuide.FloorLine(0f, -11f, 0f, -26f);
        NavigationGuide.FloorLine(0f, -26f, 0f, -37f);
        NavigationGuide.FloorLine(0f, -39f, 0f, -54f);

        // Arrow at each transition
        NavigationGuide.FloorArrow(-1.5f, 3f, 180f);
        NavigationGuide.FloorArrow(0f, -11f, 180f);
        NavigationGuide.FloorArrow(0f, -26f, 180f);
        NavigationGuide.FloorArrow(0f, -39f, 180f);

        // Wall signs at key junctions
        NavigationGuide.WallSign("→ EXHIBITION HALL", 0f, 1.5f, -10.5f, 0f, new Color(0.15f, 0.1f, 0.05f));
        NavigationGuide.WallSign("→ SECURITY OFFICE", -8f, 1.5f, -18.5f, 90f, new Color(0.1f, 0.05f, 0.15f));
        NavigationGuide.WallSign("→ VAULT CORRIDOR", 0f, 1.5f, -25.5f, 0f, new Color(0.1f, 0.05f, 0.1f));
        NavigationGuide.WallSign("→ EXIT →", 0f, 1.5f, -37.5f, 0f, new Color(0f, 0.3f, 0f));
    }

    void BuildAudioHooks()
    {
        // Camera motor sounds near camera positions
        AudioHookPlaceholder.Create("CameraLobbyMotor", AudioHookPlaceholder.AudioZoneType.CameraMotor,
            new Vector3(6f, 2f, 1f), 2f, 0.2f, envRoot);
        AudioHookPlaceholder.Create("CameraEastMotor", AudioHookPlaceholder.AudioZoneType.CameraMotor,
            new Vector3(8f, 2f, -16f), 2f, 0.2f, envRoot);
        AudioHookPlaceholder.Create("CameraWestMotor", AudioHookPlaceholder.AudioZoneType.CameraMotor,
            new Vector3(-8f, 2f, -20f), 2f, 0.2f, envRoot);
        AudioHookPlaceholder.Create("CameraVaultMotor", AudioHookPlaceholder.AudioZoneType.CameraMotor,
            new Vector3(5f, 2f, -34f), 2f, 0.2f, envRoot);
        AudioHookPlaceholder.Create("CameraVault2Motor", AudioHookPlaceholder.AudioZoneType.CameraMotor,
            new Vector3(-5f, 2f, -35f), 2f, 0.2f, envRoot);

        // Door creak zones near doors
        AudioHookPlaceholder.Create("EntranceDoorCreak", AudioHookPlaceholder.AudioZoneType.DoorCreak,
            new Vector3(0f, 1f, 0f), 2f, 0.3f, envRoot);
        AudioHookPlaceholder.Create("VaultDoorCreak", AudioHookPlaceholder.AudioZoneType.DoorCreak,
            new Vector3(0f, 1f, -38f), 2f, 0.3f, envRoot);

        // Alarm speaker at exit
        AudioHookPlaceholder.Create("AlarmSpeaker", AudioHookPlaceholder.AudioZoneType.AlarmSpeaker,
            new Vector3(0f, 3f, -54f), 8f, 0.8f, envRoot);
    }

    void OnDestroy()
    {
        foreach (var m in mats) if (m != null) Destroy(m);
    }
}
