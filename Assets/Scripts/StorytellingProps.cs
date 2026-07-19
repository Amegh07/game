using UnityEngine;

public static class StorytellingProps
{
    static Transform _storyRoot;
    static Material _paperMat, _stickyMat, _coffeeMat;

    public static void Initialize(Transform root)
    {
        _storyRoot = new GameObject("_Storytelling").transform;
        _storyRoot.SetParent(root);

        var lit = Shader.Find("Universal Render Pipeline/Lit");
        _paperMat = new Material(lit) { name = "StoryPaper", color = new Color(0.95f, 0.92f, 0.85f) };
        _stickyMat = new Material(lit) { name = "StickyNote", color = new Color(1f, 0.95f, 0.5f) };
        _coffeeMat = new Material(lit) { name = "Coffee", color = new Color(0.3f, 0.15f, 0.05f) };
    }

    // --- Notice Board / pinned papers ---

    public static void NoticeBoard(float x, float z, float yRot = 0f)
    {
        var p = new GameObject("NoticeBoard"); p.transform.SetParent(_storyRoot);
        p.transform.position = new Vector3(x, 1.5f, z);
        p.transform.rotation = Quaternion.Euler(0f, yRot, 0f);
        var cork = new Material(Shader.Find("Universal Render Pipeline/Lit")) { color = new Color(0.5f, 0.35f, 0.2f) };
        var bg = GameObject.CreatePrimitive(PrimitiveType.Quad);
        bg.name = "Cork"; bg.transform.SetParent(p.transform);
        bg.transform.localPosition = Vector3.zero;
        bg.transform.localScale = new Vector3(1.2f, 0.9f, 1f);
        var br = bg.GetComponent<Renderer>(); if (br != null) br.material = cork;
        Object.Destroy(bg.GetComponent<MeshCollider>());

        // Pinned papers
        Vector3[] positions = {
            new(-0.3f, 0.2f, 0.02f), new(0.25f, 0.15f, 0.02f), new(-0.1f, -0.1f, 0.02f), new(0.3f, -0.15f, 0.02f)
        };
        foreach (var pos in positions)
        {
            var pp = GameObject.CreatePrimitive(PrimitiveType.Quad);
            pp.name = "Paper"; pp.transform.SetParent(p.transform);
            pp.transform.localPosition = pos;
            pp.transform.localScale = new Vector3(0.25f, 0.18f, 1f);
            var pr = pp.GetComponent<Renderer>(); if (pr != null) pr.material = _paperMat;
            Object.Destroy(pp.GetComponent<MeshCollider>());
        }
    }

    // --- Pinned note on wall ---

    public static void PinnedNote(float x, float y, float z, float yRot = 0f)
    {
        var p = new GameObject("PinnedNote"); p.transform.SetParent(_storyRoot);
        p.transform.position = new Vector3(x, y, z);
        p.transform.rotation = Quaternion.Euler(0f, yRot, 0f);
        var q = GameObject.CreatePrimitive(PrimitiveType.Quad);
        q.name = "Note"; q.transform.SetParent(p.transform);
        q.transform.localPosition = new Vector3(0f, 0f, 0.02f);
        q.transform.localScale = new Vector3(0.2f, 0.15f, 1f);
        var r = q.GetComponent<Renderer>(); if (r != null) r.material = _stickyMat;
        Object.Destroy(q.GetComponent<MeshCollider>());
    }

    // --- Coffee cup on surface ---

    public static void CoffeeCup(float x, float y, float z, float rot = 0f)
    {
        var p = new GameObject("CoffeeCup"); p.transform.SetParent(_storyRoot);
        p.transform.position = new Vector3(x, y, z);
        p.transform.rotation = Quaternion.Euler(0f, rot, 0f);
        var cup = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        cup.name = "Cup"; cup.transform.SetParent(p.transform);
        cup.transform.localPosition = Vector3.zero;
        cup.transform.localScale = new Vector3(0.04f, 0.06f, 0.04f);
        var cr = cup.GetComponent<Renderer>(); if (cr != null) cr.material = _coffeeMat;
        Object.Destroy(cup.GetComponent<CapsuleCollider>());
    }

    // --- Open notebook ---

    public static void OpenNotebook(float x, float y, float z, float rot = 0f)
    {
        var p = new GameObject("Notebook"); p.transform.SetParent(_storyRoot);
        p.transform.position = new Vector3(x, y, z);
        p.transform.rotation = Quaternion.Euler(0f, rot, 0f);
        var coverMat = new Material(Shader.Find("Universal Render Pipeline/Lit")) { color = new Color(0.15f, 0.15f, 0.15f) };
        var left = GameObject.CreatePrimitive(PrimitiveType.Quad);
        left.name = "LeftPage"; left.transform.SetParent(p.transform);
        left.transform.localPosition = new Vector3(-0.08f, 0f, 0f);
        left.transform.localRotation = Quaternion.Euler(0f, 10f, 0f);
        left.transform.localScale = new Vector3(0.15f, 0.2f, 1f);
        var lr = left.GetComponent<Renderer>(); if (lr != null) lr.material = _paperMat;
        Object.Destroy(left.GetComponent<MeshCollider>());

        var right = GameObject.CreatePrimitive(PrimitiveType.Quad);
        right.name = "RightPage"; right.transform.SetParent(p.transform);
        right.transform.localPosition = new Vector3(0.08f, 0f, 0f);
        right.transform.localRotation = Quaternion.Euler(0f, -10f, 0f);
        right.transform.localScale = new Vector3(0.15f, 0.2f, 1f);
        var rr = right.GetComponent<Renderer>(); if (rr != null) rr.material = _paperMat;
        Object.Destroy(right.GetComponent<MeshCollider>());
    }

    // --- Scattered paper sheet ---

    public static void ScatteredPaper(float x, float y, float z, float rot = 0f)
    {
        var p = new GameObject("ScatteredPaper"); p.transform.SetParent(_storyRoot);
        p.transform.position = new Vector3(x, y, z);
        p.transform.rotation = Quaternion.Euler(Random.Range(-5f, 5f), rot, Random.Range(-5f, 5f));
        var q = GameObject.CreatePrimitive(PrimitiveType.Quad);
        q.name = "Sheet"; q.transform.SetParent(p.transform);
        q.transform.localPosition = Vector3.zero;
        q.transform.localScale = new Vector3(0.2f, 0.14f, 1f);
        var r = q.GetComponent<Renderer>(); if (r != null) r.material = _paperMat;
        Object.Destroy(q.GetComponent<MeshCollider>());
    }

    // --- Clipboard ---

    public static void Clipboard(float x, float y, float z, float rot = 0f)
    {
        var p = new GameObject("Clipboard"); p.transform.SetParent(_storyRoot);
        p.transform.position = new Vector3(x, y, z);
        p.transform.rotation = Quaternion.Euler(0f, rot, 0f);
        var clipMat = new Material(Shader.Find("Universal Render Pipeline/Lit")) { color = new Color(0.6f, 0.4f, 0.2f) };
        var board = GameObject.CreatePrimitive(PrimitiveType.Quad);
        board.name = "Board"; board.transform.SetParent(p.transform);
        board.transform.localPosition = Vector3.zero;
        board.transform.localScale = new Vector3(0.18f, 0.24f, 1f);
        var br = board.GetComponent<Renderer>(); if (br != null) br.material = clipMat;
        Object.Destroy(board.GetComponent<MeshCollider>());

        var paper = GameObject.CreatePrimitive(PrimitiveType.Quad);
        paper.name = "Paper"; paper.transform.SetParent(p.transform);
        paper.transform.localPosition = new Vector3(0f, 0.01f, 0.02f);
        paper.transform.localScale = new Vector3(0.15f, 0.2f, 1f);
        var pr = paper.GetComponent<Renderer>(); if (pr != null) pr.material = _paperMat;
        Object.Destroy(paper.GetComponent<MeshCollider>());
    }

    // --- Key hook board ---

    public static void KeyHookBoard(float x, float y, float z, float yRot = 0f)
    {
        var p = new GameObject("KeyHookBoard"); p.transform.SetParent(_storyRoot);
        p.transform.position = new Vector3(x, y, z);
        p.transform.rotation = Quaternion.Euler(0f, yRot, 0f);
        var boardMat = new Material(Shader.Find("Universal Render Pipeline/Lit")) { color = new Color(0.4f, 0.3f, 0.2f) };
        var bg = GameObject.CreatePrimitive(PrimitiveType.Quad);
        bg.name = "Board"; bg.transform.SetParent(p.transform);
        bg.transform.localPosition = Vector3.zero;
        bg.transform.localScale = new Vector3(0.4f, 0.3f, 1f);
        var br = bg.GetComponent<Renderer>(); if (br != null) br.material = boardMat;
        Object.Destroy(bg.GetComponent<MeshCollider>());

        var metalMat = new Material(Shader.Find("Universal Render Pipeline/Lit")) { color = new Color(0.6f, 0.6f, 0.6f) };
        for (int i = -1; i <= 1; i++)
        {
            var hook = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            hook.name = $"Hook{i}"; hook.transform.SetParent(p.transform);
            hook.transform.localPosition = new Vector3(i * 0.1f, 0.06f, 0.03f);
            hook.transform.localScale = new Vector3(0.02f, 0.04f, 0.02f);
            var hr = hook.GetComponent<Renderer>(); if (hr != null) hr.material = metalMat;
            Object.Destroy(hook.GetComponent<CapsuleCollider>());
        }
    }

    // --- Maintenance log ---

    public static void MaintenanceLog(float x, float y, float z, float rot = 0f)
    {
        var p = new GameObject("MaintenanceLog"); p.transform.SetParent(_storyRoot);
        p.transform.position = new Vector3(x, y, z);
        p.transform.rotation = Quaternion.Euler(0f, rot, 0f);
        var q = GameObject.CreatePrimitive(PrimitiveType.Quad);
        q.name = "Page"; q.transform.SetParent(p.transform);
        q.transform.localPosition = Vector3.zero;
        q.transform.localScale = new Vector3(0.22f, 0.28f, 1f);
        var r = q.GetComponent<Renderer>(); if (r != null) r.material = _paperMat;
        Object.Destroy(q.GetComponent<MeshCollider>());

        var cover = GameObject.CreatePrimitive(PrimitiveType.Quad);
        cover.name = "Cover"; cover.transform.SetParent(p.transform);
        cover.transform.localPosition = new Vector3(0f, 0f, -0.01f);
        cover.transform.localScale = new Vector3(0.24f, 0.3f, 1f);
        var cr = cover.GetComponent<Renderer>(); if (cr != null) cr.material = new Material(Shader.Find("Universal Render Pipeline/Lit")) { color = new Color(0.1f, 0.3f, 0.5f) };
        Object.Destroy(cover.GetComponent<MeshCollider>());
    }
}
