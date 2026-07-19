using UnityEngine;
using System.Collections.Generic;

public static class MuseumPropFactory
{
    private static Transform _propRoot;
    private static Material _plantMat, _metalMat, _plasticMat, _signMat, _woodMat, _paperMat, _rubberMat, _fabricMat;
    private static bool _initialized;

    public static void Initialize(Transform root, Material[] existingMats)
    {
        _propRoot = new GameObject("_Props").transform;
        _propRoot.SetParent(root);

        var lit = Shader.Find("Universal Render Pipeline/Lit");
        _plantMat = Make(lit, "Plant", new Color(0.15f, 0.45f, 0.10f));
        _metalMat = Make(lit, "Metal", new Color(0.35f, 0.35f, 0.36f));
        _plasticMat = Make(lit, "Plastic", new Color(0.18f, 0.18f, 0.20f));
        _signMat = Make(lit, "Sign", new Color(0.9f, 0.9f, 0.85f));
        _woodMat = Make(lit, "Wood", new Color(0.48f, 0.30f, 0.15f));
        _paperMat = Make(lit, "Paper", new Color(0.95f, 0.93f, 0.88f));
        _rubberMat = Make(lit, "Rubber", new Color(0.08f, 0.08f, 0.10f));
        _fabricMat = Make(lit, "Fabric", new Color(0.6f, 0.2f, 0.2f));

        if (existingMats != null)
        {
            var list = new List<Material>(existingMats);
            list.AddRange(new[] { _plantMat, _metalMat, _plasticMat, _signMat, _woodMat, _paperMat, _rubberMat, _fabricMat });
        }
        _initialized = true;
    }

    static Material Make(Shader s, string n, Color c)
    {
        return new Material(s) { name = n, color = c };
    }

    static GameObject Cube(string name, float x, float y, float z, float sx, float sy, float sz, Material mat, Transform parent = null)
    {
        var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
        go.name = name;
        go.transform.SetParent(parent ?? _propRoot);
        go.transform.position = new Vector3(x, y + sy / 2f, z);
        go.transform.localScale = new Vector3(sx, sy, sz);
        var r = go.GetComponent<Renderer>();
        if (r != null) r.material = mat;
        Object.Destroy(go.GetComponent<BoxCollider>());
        return go;
    }

    static GameObject Cyl(string name, float x, float y, float z, float r, float h, Material mat, Transform parent = null)
    {
        var go = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        go.name = name;
        go.transform.SetParent(parent ?? _propRoot);
        go.transform.position = new Vector3(x, y + h / 2f, z);
        go.transform.localScale = new Vector3(r * 2f, h, r * 2f);
        var rend = go.GetComponent<Renderer>();
        if (rend != null) rend.material = mat;
        Object.Destroy(go.GetComponent<CapsuleCollider>());
        return go;
    }

    static GameObject Quad(string name, float x, float y, float z, float sx, float sz, Material mat, float yRot = 0f, Transform parent = null)
    {
        var go = GameObject.CreatePrimitive(PrimitiveType.Quad);
        go.name = name;
        go.transform.SetParent(parent ?? _propRoot);
        go.transform.position = new Vector3(x, y, z);
        go.transform.localScale = new Vector3(sx, sz, 1f);
        go.transform.rotation = Quaternion.Euler(0f, yRot, 0f);
        var r = go.GetComponent<Renderer>();
        if (r != null) r.material = mat;
        return go;
    }

    // --- Public prop builders ---

    public static void Bench(float x, float z, float rot = 0f)
    {
        var p = new GameObject("Bench"); p.transform.SetParent(_propRoot);
        p.transform.position = new Vector3(x, 0f, z);
        p.transform.rotation = Quaternion.Euler(0f, rot, 0f);
        Cube("Seat", 0f, 0.5f, 0f, 1.8f, 0.1f, 0.5f, _woodMat, p.transform);
        Cube("LegL", -0.7f, 0.25f, 0.4f, 0.08f, 0.5f, 0.08f, _metalMat, p.transform);
        Cube("LegR", 0.7f, 0.25f, 0.4f, 0.08f, 0.5f, 0.08f, _metalMat, p.transform);
        Cube("Back", 0f, 0.9f, -0.25f, 1.6f, 0.6f, 0.05f, _woodMat, p.transform);
    }

    public static void PlantPot(float x, float z, float scale = 1f)
    {
        var p = new GameObject("Plant"); p.transform.SetParent(_propRoot);
        p.transform.position = new Vector3(x, 0f, z);
        p.transform.localScale = Vector3.one * scale;
        Cyl("Pot", 0f, 0f, 0f, 0.3f, 0.4f, _plasticMat, p.transform);
        Cyl("Foliage", 0f, 0.55f, 0f, 0.35f, 0.5f, _plantMat, p.transform);
        Cyl("FoliageTop", 0f, 0.9f, 0f, 0.2f, 0.2f, _plantMat, p.transform);
    }

    public static void InfoBoard(float x, float z, float yRot = 0f)
    {
        var p = new GameObject("InfoBoard"); p.transform.SetParent(_propRoot);
        p.transform.position = new Vector3(x, 0f, z);
        p.transform.rotation = Quaternion.Euler(0f, yRot, 0f);
        Cube("Post", 0f, 1.2f, 0f, 0.08f, 2.4f, 0.08f, _metalMat, p.transform);
        Quad("Panel", 0f, 2f, 0.05f, 1.2f, 0.9f, _paperMat, 0f, p.transform);
    }

    public static void Painting(float x, float y, float z, float yRot = 0f, Color? frameColor = null)
    {
        var p = new GameObject("Painting"); p.transform.SetParent(_propRoot);
        p.transform.position = new Vector3(x, y, z);
        p.transform.rotation = Quaternion.Euler(0f, yRot, 0f);
        var frm = frameColor ?? new Color(0.55f, 0.35f, 0.15f);
        var frmMat = new Material(Shader.Find("Universal Render Pipeline/Lit")) { name = "Frame", color = frm };
        Quad("Canvas", 0f, 0f, 0.04f, 1.2f, 0.9f, _paperMat, 0f, p.transform);
        Cube("FrameT", 0f, 0.48f, 0f, 1.3f, 0.06f, 0.08f, frmMat, p.transform);
        Cube("FrameB", 0f, -0.48f, 0f, 1.3f, 0.06f, 0.08f, frmMat, p.transform);
        Cube("FrameL", -0.64f, 0f, 0f, 0.06f, 1.0f, 0.08f, frmMat, p.transform);
        Cube("FrameR", 0.64f, 0f, 0f, 0.06f, 1.0f, 0.08f, frmMat, p.transform);
    }

    public static void DisplayCase(float x, float z, float yRot = 0f, bool hasArtifact = true, Material artMat = null)
    {
        var p = new GameObject("DisplayCase"); p.transform.SetParent(_propRoot);
        p.transform.position = new Vector3(x, 0f, z);
        p.transform.rotation = Quaternion.Euler(0f, yRot, 0f);
        Cube("Base", 0f, 0.5f, 0f, 1.4f, 1f, 1.4f, accentMat);
        Cube("GlassTop", 0f, 1.1f, 0f, 1.2f, 0.05f, 1.2f, glassMat);
        Cube("GlassW", 0f, 0.8f, -0.62f, 1.2f, 0.6f, 0.05f, glassMat);
        Cube("GlassE", 0f, 0.8f, 0.62f, 1.2f, 0.6f, 0.05f, glassMat);
        Cube("GlassN", -0.62f, 0.8f, 0f, 0.05f, 0.6f, 1.2f, glassMat);
        Cube("GlassS", 0.62f, 0.8f, 0f, 0.05f, 0.6f, 1.2f, glassMat);
        if (hasArtifact)
        {
            var aMat = artMat ?? new Material(Shader.Find("Universal Render Pipeline/Lit")) { color = new Color(0.9f, 0.7f, 0.2f) };
            Cyl("Artifact", 0f, 0.3f, 0f, 0.12f, 0.25f, aMat, p.transform);
        }
    }

    public static void BrokenDisplayCase(float x, float z, float yRot = 0f)
    {
        var p = new GameObject("BrokenCase"); p.transform.SetParent(_propRoot);
        p.transform.position = new Vector3(x, 0f, z);
        p.transform.rotation = Quaternion.Euler(0f, yRot, 0f);
        Cube("Base", 0f, 0.5f, 0f, 1.4f, 1f, 1.4f, accentMat);
        // Broken glass — scattered shards
        var shardMat = new Material(Shader.Find("Universal Render Pipeline/Lit")) { color = new Color(0.6f, 0.7f, 0.8f, 0.5f) };
        Cube("Shard1", 0.3f, 0.3f, 0.4f, 0.15f, 0.02f, 0.1f, shardMat, p.transform);
        Cube("Shard2", -0.2f, 0.2f, -0.3f, 0.1f, 0.02f, 0.15f, shardMat, p.transform);
        Cube("Shard3", 0.1f, 0.15f, 0.1f, 0.2f, 0.02f, 0.08f, shardMat, p.transform);
        // Empty base — no artifact
        Cube("Pedestal", 0f, 0.3f, 0f, 0.3f, 0.05f, 0.3f, _metalMat, p.transform);
    }

    public static void Plaque(float x, float z, float yRot = 0f)
    {
        var p = new GameObject("Plaque"); p.transform.SetParent(_propRoot);
        p.transform.position = new Vector3(x, 0.15f, z);
        p.transform.rotation = Quaternion.Euler(0f, yRot, 0f);
        Quad("Text", 0f, 0f, 0.02f, 0.5f, 0.15f, _metalMat, 0f, p.transform);
    }

    // --- Furniture ---

    public static void Table(float x, float z, float w = 1.2f, float d = 0.8f, float rot = 0f)
    {
        var p = new GameObject("Table"); p.transform.SetParent(_propRoot);
        p.transform.position = new Vector3(x, 0f, z);
        p.transform.rotation = Quaternion.Euler(0f, rot, 0f);
        Cube("Top", 0f, 0.75f, 0f, w, 0.05f, d, _woodMat, p.transform);
        Cube("LegL", -w / 2f + 0.1f, 0.375f, -d / 2f + 0.1f, 0.06f, 0.75f, 0.06f, _metalMat, p.transform);
        Cube("LegR", w / 2f - 0.1f, 0.375f, -d / 2f + 0.1f, 0.06f, 0.75f, 0.06f, _metalMat, p.transform);
        Cube("LegLB", -w / 2f + 0.1f, 0.375f, d / 2f - 0.1f, 0.06f, 0.75f, 0.06f, _metalMat, p.transform);
        Cube("LegRB", w / 2f - 0.1f, 0.375f, d / 2f - 0.1f, 0.06f, 0.75f, 0.06f, _metalMat, p.transform);
    }

    public static void Chair(float x, float z, float rot = 0f)
    {
        var p = new GameObject("Chair"); p.transform.SetParent(_propRoot);
        p.transform.position = new Vector3(x, 0f, z);
        p.transform.rotation = Quaternion.Euler(0f, rot, 0f);
        Cube("Seat", 0f, 0.5f, 0f, 0.5f, 0.05f, 0.5f, _plasticMat, p.transform);
        Cube("Leg1", -0.2f, 0.25f, -0.2f, 0.04f, 0.5f, 0.04f, _metalMat, p.transform);
        Cube("Leg2", 0.2f, 0.25f, -0.2f, 0.04f, 0.5f, 0.04f, _metalMat, p.transform);
        Cube("Leg3", -0.2f, 0.25f, 0.2f, 0.04f, 0.5f, 0.04f, _metalMat, p.transform);
        Cube("Leg4", 0.2f, 0.25f, 0.2f, 0.04f, 0.5f, 0.04f, _metalMat, p.transform);
        Cube("Back", 0f, 0.85f, -0.25f, 0.4f, 0.5f, 0.03f, _plasticMat, p.transform);
    }

    public static void Barrier(float x, float z, float rot = 0f)
    {
        var p = new GameObject("Barrier"); p.transform.SetParent(_propRoot);
        p.transform.position = new Vector3(x, 0f, z);
        p.transform.rotation = Quaternion.Euler(0f, rot, 0f);
        Cube("PostL", -0.5f, 0.5f, 0f, 0.06f, 1f, 0.06f, _metalMat, p.transform);
        Cube("PostR", 0.5f, 0.5f, 0f, 0.06f, 1f, 0.06f, _metalMat, p.transform);
        Cube("Bar", 0f, 0.9f, 0f, 1.1f, 0.05f, 0.05f, _rubberMat, p.transform);
        Cube("MidBar", 0f, 0.5f, 0f, 1.0f, 0.03f, 0.03f, _rubberMat, p.transform);
    }

    public static void TrashCan(float x, float z)
    {
        var p = new GameObject("TrashCan"); p.transform.SetParent(_propRoot);
        p.transform.position = new Vector3(x, 0f, z);
        Cyl("Body", 0f, 0f, 0f, 0.18f, 0.4f, _metalMat, p.transform);
        Cube("Lid", 0f, 0.45f, 0f, 0.32f, 0.04f, 0.32f, _metalMat, p.transform);
    }

    // --- Security / Office ---

    public static void ServerRack(float x, float z, float rot = 0f)
    {
        var p = new GameObject("ServerRack"); p.transform.SetParent(_propRoot);
        p.transform.position = new Vector3(x, 0f, z);
        p.transform.rotation = Quaternion.Euler(0f, rot, 0f);
        Cube("Cabinet", 0f, 1.5f, 0f, 0.8f, 3f, 0.8f, securityMat, p.transform);
        for (int i = -1; i <= 1; i++)
        {
            var led = new Color(0f, 0.6f, 0.1f);
            var lm = new Material(Shader.Find("Universal Render Pipeline/Lit")) { color = led };
            Cube($"LED_{i}", 0.35f, 1.2f + i * 0.5f, 0.35f, 0.03f, 0.02f, 0.02f, lm, p.transform);
        }
    }

    public static void OfficeChair(float x, float z, float rot = 0f)
    {
        var p = new GameObject("OfficeChair"); p.transform.SetParent(_propRoot);
        p.transform.position = new Vector3(x, 0f, z);
        p.transform.rotation = Quaternion.Euler(0f, rot, 0f);
        Cube("Seat", 0f, 0.5f, 0f, 0.6f, 0.06f, 0.6f, _rubberMat, p.transform);
        Cyl("Base", 0f, 0.15f, 0f, 0.25f, 0.3f, _plasticMat, p.transform);
        Cyl("Post", 0f, 0.4f, 0f, 0.04f, 0.5f, _metalMat, p.transform);
        Cube("Back", 0f, 0.85f, -0.3f, 0.5f, 0.4f, 0.04f, _rubberMat, p.transform);
    }

    public static void ComputerSetup(float x, float z, float rot = 0f)
    {
        var p = new GameObject("Computer"); p.transform.SetParent(_propRoot);
        p.transform.position = new Vector3(x, 0f, z);
        p.transform.rotation = Quaternion.Euler(0f, rot, 0f);
        Cube("MonitorBase", 0f, 0.05f, 0f, 0.3f, 0.05f, 0.15f, _plasticMat, p.transform);
        Cube("MonitorStand", 0f, 0.25f, 0f, 0.05f, 0.4f, 0.05f, _metalMat, p.transform);
        var screenMat = new Material(Shader.Find("Universal Render Pipeline/Lit")) { color = new Color(0.1f, 0.2f, 0.4f) };
        Quad("Screen", 0f, 0.55f, 0.08f, 0.5f, 0.35f, screenMat, 0f, p.transform);
        Cube("ScreenFrame", 0f, 0.55f, 0f, 0.55f, 0.4f, 0.04f, _plasticMat, p.transform);
        Cube("Keyboard", 0f, 0.05f, 0.2f, 0.3f, 0.03f, 0.1f, _plasticMat, p.transform);
    }

    // --- Storage / Utility ---

    public static void Crate(float x, float z, Color? c = null)
    {
        var col = c ?? new Color(0.5f, 0.35f, 0.15f);
        var cm = new Material(Shader.Find("Universal Render Pipeline/Lit")) { color = col };
        var p = new GameObject("Crate"); p.transform.SetParent(_propRoot);
        p.transform.position = new Vector3(x, 0.25f, z);
        Cube("Body", 0f, 0f, 0f, 0.5f, 0.5f, 0.5f, cm, p.transform);
        Cube("Strap1", 0f, 0f, 0.2f, 0.52f, 0.02f, 0.04f, _rubberMat, p.transform);
        Cube("Strap2", 0f, 0f, -0.2f, 0.52f, 0.02f, 0.04f, _rubberMat, p.transform);
    }

    public static void Shelf(float x, float z, float rot = 0f)
    {
        var p = new GameObject("Shelf"); p.transform.SetParent(_propRoot);
        p.transform.position = new Vector3(x, 0f, z);
        p.transform.rotation = Quaternion.Euler(0f, rot, 0f);
        Cube("PostL", -0.5f, 1f, 0f, 0.06f, 2f, 0.5f, _metalMat, p.transform);
        Cube("PostR", 0.5f, 1f, 0f, 0.06f, 2f, 0.5f, _metalMat, p.transform);
        for (int i = 0; i < 4; i++)
        {
            float sy = 0.3f + i * 0.5f;
            Cube($"Shelf{i}", 0f, sy, 0f, 1f, 0.03f, 0.45f, _metalMat, p.transform);
        }
    }

    public static void MopBucket(float x, float z)
    {
        var p = new GameObject("MopBucket"); p.transform.SetParent(_propRoot);
        p.transform.position = new Vector3(x, 0f, z);
        Cyl("Bucket", 0f, 0f, 0f, 0.15f, 0.3f, _plasticMat, p.transform);
        Cyl("Mop", 0.1f, 0.3f, 0f, 0.02f, 0.8f, _metalMat, p.transform);
        Cube("MopHead", 0.1f, 0.75f, 0f, 0.08f, 0.1f, 0.04f, _fabricMat, p.transform);
    }

    public static void FireExtinguisher(float x, float z)
    {
        var p = new GameObject("FireExtinguisher"); p.transform.SetParent(_propRoot);
        p.transform.position = new Vector3(x, 0f, z);
        var red = new Material(Shader.Find("Universal Render Pipeline/Lit")) { color = Color.red };
        Cyl("Body", 0f, 0f, 0f, 0.1f, 0.45f, red, p.transform);
        Cyl("Neck", 0f, 0.3f, 0f, 0.04f, 0.1f, _metalMat, p.transform);
    }

    public static void WetFloorSign(float x, float z)
    {
        var p = new GameObject("WetFloorSign"); p.transform.SetParent(_propRoot);
        p.transform.position = new Vector3(x, 0f, z);
        var yellow = new Material(Shader.Find("Universal Render Pipeline/Lit")) { color = new Color(1f, 0.8f, 0f) };
        Cyl("Base", 0f, 0f, 0f, 0.15f, 0.05f, _plasticMat, p.transform);
        Cyl("Post", 0f, 0.35f, 0f, 0.03f, 0.7f, _metalMat, p.transform);
        Quad("Sign", 0f, 0.75f, 0.05f, 0.4f, 0.3f, yellow, 0f, p.transform);
    }

    // --- Exhibits ---

    public static void Statue(float x, float z, float rot = 0f)
    {
        var p = new GameObject("Statue"); p.transform.SetParent(_propRoot);
        p.transform.position = new Vector3(x, 0f, z);
        p.transform.rotation = Quaternion.Euler(0f, rot, 0f);
        var stone = new Material(Shader.Find("Universal Render Pipeline/Lit")) { color = new Color(0.7f, 0.68f, 0.65f) };
        Cube("Base", 0f, 0.15f, 0f, 0.6f, 0.3f, 0.6f, stone, p.transform);
        Cyl("Body", 0f, 0.8f, 0f, 0.2f, 0.8f, stone, p.transform);
        Cyl("Head", 0f, 1.3f, 0f, 0.15f, 0.2f, stone, p.transform);
    }

    public static void VisitorMap(float x, float z, float yRot = 0f)
    {
        var p = new GameObject("VisitorMap"); p.transform.SetParent(_propRoot);
        p.transform.position = new Vector3(x, 0f, z);
        p.transform.rotation = Quaternion.Euler(0f, yRot, 0f);
        Cube("Post", 0f, 1f, 0f, 0.06f, 2f, 0.06f, _metalMat, p.transform);
        Quad("Map", 0f, 1.6f, 0.05f, 1f, 0.8f, _paperMat, 0f, p.transform);
        Quad("Frame", 0f, 1.6f, 0f, 1.1f, 0.9f, _metalMat, 0f, p.transform);
    }

    // --- Props for security / vault ---

    public static void MonitorWall(float x, float y, float z, float yRot = 0f)
    {
        var p = new GameObject("MonitorWall"); p.transform.SetParent(_propRoot);
        p.transform.position = new Vector3(x, y, z);
        p.transform.rotation = Quaternion.Euler(0f, yRot, 0f);
        var scr = new Material(Shader.Find("Universal Render Pipeline/Lit")) { color = new Color(0.05f, 0.1f, 0.2f) };
        for (int i = -1; i <= 1; i++)
        {
            Quad($"Screen{i}", i * 0.4f, 0f, 0.05f, 0.3f, 0.2f, scr, 0f, p.transform);
            Cube($"Bezel{i}", i * 0.4f, 0f, 0f, 0.35f, 0.25f, 0.05f, _plasticMat, p.transform);
        }
    }

    public static void EmergencyLight(float x, float y, float z)
    {
        var p = new GameObject("EmergencyLight"); p.transform.SetParent(_propRoot);
        p.transform.position = new Vector3(x, y, z);
        var red = new Material(Shader.Find("Universal Render Pipeline/Lit")) { color = Color.red };
        Cube("Housing", 0f, 0f, 0f, 0.3f, 0.1f, 0.15f, _plasticMat, p.transform);
        Cube("Lens", 0f, -0.06f, 0f, 0.25f, 0.02f, 0.1f, red, p.transform);
    }

    // --- Signs ---

    public static void Signage(string text, float x, float y, float z, float yRot = 0f, Color? bg = null)
    {
        var p = new GameObject($"Sign_{text}"); p.transform.SetParent(_propRoot);
        p.transform.position = new Vector3(x, y, z);
        p.transform.rotation = Quaternion.Euler(0f, yRot, 0f);
        var bgCol = bg ?? new Color(0.1f, 0.1f, 0.3f);
        var bgMat = new Material(Shader.Find("Universal Render Pipeline/Lit")) { color = bgCol };
        Quad("Background", 0f, 0f, 0.01f, 0.8f, 0.25f, bgMat, 0f, p.transform);
        var txtMat = new Material(Shader.Find("Universal Render Pipeline/Lit")) { color = Color.white };
        Quad("Text", 0f, 0f, 0.03f, 0.6f, 0.12f, txtMat, 0f, p.transform);
    }

    // --- Storytelling props ---

    public static void CoffeeCup(float x, float z, float rot = 0f)
    {
        var p = new GameObject("CoffeeCup"); p.transform.SetParent(_propRoot);
        p.transform.position = new Vector3(x, 0.78f, z);
        p.transform.rotation = Quaternion.Euler(0f, rot, 0f);
        Cyl("Cup", 0f, 0f, 0f, 0.03f, 0.06f, _plasticMat, p.transform);
    }

    public static void Notebook(float x, float z, float rot = 0f)
    {
        var p = new GameObject("Notebook"); p.transform.SetParent(_propRoot);
        p.transform.position = new Vector3(x, 0.78f, z);
        p.transform.rotation = Quaternion.Euler(0f, rot, 0f);
        Cube("Pages", 0f, 0f, 0f, 0.2f, 0.01f, 0.15f, _paperMat, p.transform);
        Cube("Cover", 0f, 0.005f, 0f, 0.22f, 0.005f, 0.17f, _rubberMat, p.transform);
    }

    public static void PaperSheet(float x, float z, float rot = 0f)
    {
        var p = new GameObject("Paper"); p.transform.SetParent(_propRoot);
        p.transform.position = new Vector3(x, 0.78f, z);
        p.transform.rotation = Quaternion.Euler(rot, 0f, 0f);
        Quad("Sheet", 0f, 0f, 0f, 0.25f, 0.15f, _paperMat, 0f, p.transform);
    }

    public static void CeilingLight(float x, float y, float z, Color color, float range = 4f)
    {
        var p = new GameObject("CeilingLight"); p.transform.SetParent(_propRoot);
        p.transform.position = new Vector3(x, y, z);
        Cube("Fixture", 0f, 0f, 0f, 0.5f, 0.05f, 0.3f, _metalMat, p.transform);
        Cube("Diffuser", 0f, -0.04f, 0f, 0.4f, 0.02f, 0.2f, _paperMat, p.transform);
        var lightGo = new GameObject("Light");
        lightGo.transform.SetParent(p.transform);
        lightGo.transform.localPosition = Vector3.zero;
        var l = lightGo.AddComponent<Light>();
        l.type = LightType.Point;
        l.color = color;
        l.range = range;
        l.intensity = 0.6f;
        l.shadows = LightShadows.Soft;
    }

    // Materials exposed for other systems
    public static Material AccentMat => accentMat;
    public static Material MetalMat => _metalMat;
    public static Material PlasticMat => _plasticMat;
    public static Material SignMat => _signMat;
    public static Material WoodMat => _woodMat;
    public static Material PaperMat => _paperMat;

    static Material accentMat
    {
        get
        {
            if (_accentMat == null)
                _accentMat = new Material(Shader.Find("Universal Render Pipeline/Lit")) { color = new Color(0.45f, 0.30f, 0.20f) };
            return _accentMat;
        }
    }
    static Material _accentMat;

    static Material glassMat
    {
        get
        {
            if (_glassMat == null)
                _glassMat = new Material(Shader.Find("Universal Render Pipeline/Lit")) { color = new Color(0.7f, 0.8f, 0.9f, 0.35f) };
            return _glassMat;
        }
    }
    static Material _glassMat;

    static Material securityMat
    {
        get
        {
            if (_securityMat == null)
                _securityMat = new Material(Shader.Find("Universal Render Pipeline/Lit")) { color = new Color(0.12f, 0.12f, 0.15f) };
            return _securityMat;
        }
    }
    static Material _securityMat;
}
