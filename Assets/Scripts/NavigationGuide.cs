using UnityEngine;

public static class NavigationGuide
{
    static Transform _navRoot;
    static Material _floorMarkMat, _arrowMat, _signBgMat;

    public static void Initialize(Transform root)
    {
        _navRoot = new GameObject("_Navigation").transform;
        _navRoot.SetParent(root);

        var lit = Shader.Find("Universal Render Pipeline/Lit");
        _floorMarkMat = new Material(lit) { name = "FloorMark", color = new Color(0.9f, 0.9f, 0.2f, 0.4f) };
        _arrowMat = new Material(lit) { name = "Arrow", color = new Color(1f, 1f, 1f, 0.6f) };
        _signBgMat = new Material(lit) { name = "SignBg", color = new Color(0.05f, 0.05f, 0.2f) };
    }

    public static void FloorArrow(float x, float z, float rot = 0f, float length = 1.5f)
    {
        var p = new GameObject("FloorArrow"); p.transform.SetParent(_navRoot);
        p.transform.position = new Vector3(x, 0.06f, z);
        p.transform.rotation = Quaternion.Euler(90f, rot, 0f);
        var q = GameObject.CreatePrimitive(PrimitiveType.Quad);
        q.name = "ArrowHead"; q.transform.SetParent(p.transform);
        q.transform.localPosition = new Vector3(0f, 0f, length * 0.4f);
        q.transform.localScale = new Vector3(0.4f, 0.3f, 1f);
        var r = q.GetComponent<Renderer>(); if (r != null) r.material = _arrowMat;
        Object.Destroy(q.GetComponent<MeshCollider>());

        var shaft = GameObject.CreatePrimitive(PrimitiveType.Quad);
        shaft.name = "ArrowShaft"; shaft.transform.SetParent(p.transform);
        shaft.transform.localPosition = new Vector3(0f, 0f, -length * 0.15f);
        shaft.transform.localScale = new Vector3(0.08f, length * 0.7f, 1f);
        var sr = shaft.GetComponent<Renderer>(); if (sr != null) sr.material = _arrowMat;
        Object.Destroy(shaft.GetComponent<MeshCollider>());
    }

    public static void FloorLine(float x1, float z1, float x2, float z2)
    {
        var p = new GameObject("FloorLine"); p.transform.SetParent(_navRoot);
        float mx = (x1 + x2) / 2f, mz = (z1 + z2) / 2f;
        float dx = x2 - x1, dz = z2 - z1;
        float len = Mathf.Sqrt(dx * dx + dz * dz);
        float angle = Mathf.Atan2(dx, dz) * Mathf.Rad2Deg;
        p.transform.position = new Vector3(mx, 0.06f, mz);
        p.transform.rotation = Quaternion.Euler(90f, -angle, 0f);
        var q = GameObject.CreatePrimitive(PrimitiveType.Quad);
        q.name = "Line"; q.transform.SetParent(p.transform);
        q.transform.localPosition = Vector3.zero;
        q.transform.localScale = new Vector3(0.04f, len, 1f);
        var r = q.GetComponent<Renderer>(); if (r != null) r.material = _floorMarkMat;
        Object.Destroy(q.GetComponent<MeshCollider>());
    }

    public static void WallSign(string label, float x, float y, float z, float yRot = 0f, Color? bg = null)
    {
        var p = new GameObject($"WallSign_{label}"); p.transform.SetParent(_navRoot);
        p.transform.position = new Vector3(x, y, z);
        p.transform.rotation = Quaternion.Euler(0f, yRot, 0f);
        var bgc = bg ?? new Color(0.05f, 0.05f, 0.2f);
        var bgMat = new Material(Shader.Find("Universal Render Pipeline/Lit")) { color = bgc };
        var bgQ = GameObject.CreatePrimitive(PrimitiveType.Quad);
        bgQ.name = "BG"; bgQ.transform.SetParent(p.transform);
        bgQ.transform.localPosition = new Vector3(0f, 0f, 0.01f);
        bgQ.transform.localScale = new Vector3(0.6f, 0.2f, 1f);
        var br = bgQ.GetComponent<Renderer>(); if (br != null) br.material = bgMat;
        Object.Destroy(bgQ.GetComponent<MeshCollider>());

        var txtMat = new Material(Shader.Find("Universal Render Pipeline/Lit")) { color = Color.white };
        var txt = GameObject.CreatePrimitive(PrimitiveType.Quad);
        txt.name = "Text"; txt.transform.SetParent(p.transform);
        txt.transform.localPosition = new Vector3(0f, 0f, 0.03f);
        txt.transform.localScale = new Vector3(0.5f, 0.12f, 1f);
        var tr = txt.GetComponent<Renderer>(); if (tr != null) tr.material = txtMat;
        Object.Destroy(txt.GetComponent<MeshCollider>());
    }

    public static void RestrictedMark(float x, float z, float rot = 0f)
    {
        var p = new GameObject("RestrictedMark"); p.transform.SetParent(_navRoot);
        p.transform.position = new Vector3(x, 0.06f, z);
        p.transform.rotation = Quaternion.Euler(90f, rot, 0f);
        var red = new Material(Shader.Find("Universal Render Pipeline/Lit")) { color = new Color(0.8f, 0.1f, 0.1f, 0.4f) };
        var q = GameObject.CreatePrimitive(PrimitiveType.Quad);
        q.name = "Mark"; q.transform.SetParent(p.transform);
        q.transform.localPosition = Vector3.zero;
        q.transform.localScale = new Vector3(0.6f, 0.6f, 1f);
        var r = q.GetComponent<Renderer>(); if (r != null) r.material = red;
        Object.Destroy(q.GetComponent<MeshCollider>());
    }

    public static void ExitSign(float x, float y, float z, float yRot = 0f)
    {
        var p = new GameObject("ExitSign"); p.transform.SetParent(_navRoot);
        p.transform.position = new Vector3(x, y, z);
        p.transform.rotation = Quaternion.Euler(0f, yRot, 0f);
        var green = new Material(Shader.Find("Universal Render Pipeline/Lit")) { color = new Color(0f, 0.6f, 0.1f) };
        var q = GameObject.CreatePrimitive(PrimitiveType.Quad);
        q.name = "Sign"; q.transform.SetParent(p.transform);
        q.transform.localPosition = new Vector3(0f, 0f, 0.02f);
        q.transform.localScale = new Vector3(0.5f, 0.25f, 1f);
        var r = q.GetComponent<Renderer>(); if (r != null) r.material = green;
        Object.Destroy(q.GetComponent<MeshCollider>());
        var bg = GameObject.CreatePrimitive(PrimitiveType.Quad);
        bg.name = "BG"; bg.transform.SetParent(p.transform);
        bg.transform.localPosition = new Vector3(0f, 0f, 0f);
        bg.transform.localScale = new Vector3(0.6f, 0.35f, 1f);
        var br = bg.GetComponent<Renderer>(); if (br != null) br.material = new Material(Shader.Find("Universal Render Pipeline/Lit")) { color = Color.white };
        Object.Destroy(bg.GetComponent<MeshCollider>());
    }
}
