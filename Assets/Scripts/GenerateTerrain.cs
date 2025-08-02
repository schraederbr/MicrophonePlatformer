using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class GenerateTerrain : MonoBehaviour
{
    // Visual
    public int pointsPerSecond = 25;
    public MoveBetween moveBetween;
    public Transform cursor;
    public Material lineMaterial;
    public float lineWidth = 0.3f;

    // Physics
    public bool addCollider = true;
    public float colliderEdgeRadius = 0.05f;

    // Smoothing
    public bool enableSmoothing = true;
    [Range(1, 8)]
    public int smoothSubdivisions = 3;

    // Exposed curves
    public Vector2[][] curves;

    // ----------------------------------------------------------
    private int framesPerPoint;
    private int fixedFrameCount;
    private int currentLoop = -1;
    private int curveIndex;
    private bool recording;

    private readonly List<Vector2> currentPts = new List<Vector2>();
    private readonly List<Vector2[]> curveList = new List<Vector2[]>();
    private readonly List<GameObject> curveObjs = new List<GameObject>(); // store created objects

    private GameObject curObj;
    private LineRenderer curLR;
    private EdgeCollider2D curEC;

    public InputActionReference recordCurveAction;  
    public InputActionReference sumCurvesAction;    
    public InputActionReference clearCurvesAction;

    private void OnEnable()
    {
        if (recordCurveAction != null) recordCurveAction.action.Enable();
        if (sumCurvesAction != null) sumCurvesAction.action.Enable();
        if (clearCurvesAction != null) clearCurvesAction.action.Enable();
    }

    private void OnDisable()
    {
        if (recordCurveAction != null) recordCurveAction.action.Disable();
        if (sumCurvesAction != null) sumCurvesAction.action.Disable();
        if (clearCurvesAction != null) clearCurvesAction.action.Disable();
    }

    void SumAllCurves()
    {
        if (curveList.Count < 2)
            return;                    // nothing to do

        List<float> xGrid = BuildUnifiedX();
        Vector2[] summed = new Vector2[xGrid.Count];

        for (int xi = 0; xi < xGrid.Count; xi++)
        {
            float x = xGrid[xi];
            float ySum = 0f;

            foreach (Vector2[] c in curveList)
                ySum += SampleY(c, x);

            summed[xi] = new Vector2(x, ySum);
        }

        // Replace every curve with the single summed result
        curveList.Clear();
        curveList.Add(summed);
        curves = curveList.ToArray();

        RegenerateAllVisuals();   // redraw scene
    }


    // ----------------------------------------------------------
    void Start()
    {
        float fps = 1f / Time.fixedDeltaTime;
        framesPerPoint = Mathf.Max(1, Mathf.RoundToInt(fps / pointsPerSecond));
    }

    void Update()
    {
        
        if (recordCurveAction != null &&
            recordCurveAction.action.WasPressedThisFrame())
        {
            ToggleRecording();
        }

        if (clearCurvesAction != null &&
            clearCurvesAction.action.WasPressedThisFrame())
        {
            ClearAllCurves();
        }

        if (sumCurvesAction != null &&
            sumCurvesAction.action.WasPressedThisFrame())
        {
            SumAllCurves();
        }
    }

    void FixedUpdate()
    {
        if (!recording)
            return;

        int loop = moveBetween.loopCount;

        if (loop != currentLoop)
        {
            FinaliseCurrentCurve();
            StartNewCurve(loop);
        }

        if (fixedFrameCount % framesPerPoint == 0)
            AddSample(cursor.position);

        fixedFrameCount++;
        if (loop != currentLoop)
        {
            SumAllCurves();
        }
    }

    // ----------------------------------------------------------
    private void ToggleRecording()
    {
        if (recording)
        {
            FinaliseCurrentCurve();
            recording = false;
        }
        else
        {
            StartNewCurve(moveBetween.loopCount);
            recording = true;
        }
    }

    private void StartNewCurve(int loopIndex)
    {
        currentLoop = loopIndex;
        fixedFrameCount = 0;
        currentPts.Clear();

        curObj = new GameObject("Curve_" + curveIndex);
        curveObjs.Add(curObj); // track it

        curLR = curObj.AddComponent<LineRenderer>();
        curLR.useWorldSpace = true;
        curLR.material = lineMaterial;
        curLR.widthMultiplier = lineWidth;

        Color col = Color.HSVToRGB((curveIndex * 0.25f) % 1f, 1f, 1f);
        curLR.startColor = col;
        curLR.endColor = col;

        if (addCollider)
        {
            curEC = curObj.AddComponent<EdgeCollider2D>();
            curEC.edgeRadius = colliderEdgeRadius;
        }
    }

    private void AddSample(Vector2 wp)
    {
        currentPts.Add(wp);
        UpdateVisuals();
    }

    private void UpdateVisuals()
    {
        List<Vector2> ptsOut = enableSmoothing
            ? GetSmoothed(currentPts, smoothSubdivisions)
            : currentPts;

        curLR.positionCount = ptsOut.Count;
        for (int i = 0; i < ptsOut.Count; i++)
            curLR.SetPosition(i, ptsOut[i]);

        if (addCollider)
        {
            Vector2[] local = new Vector2[ptsOut.Count];
            for (int i = 0; i < ptsOut.Count; i++)
                local[i] = curObj.transform.InverseTransformPoint(ptsOut[i]);

            curEC.points = local;
        }
    }

    private void FinaliseCurrentCurve()
    {
        if (currentPts.Count < 2)
        {
            ClearActive();
            return;
        }

        Vector2[] finished = (enableSmoothing
            ? GetSmoothed(currentPts, smoothSubdivisions)
            : currentPts).ToArray();

        curveList.Add(finished);
        curves = curveList.ToArray();

        curveIndex++;
        ClearActive();
    }

    private void ClearActive()
    {
        curObj = null;
        curLR = null;
        curEC = null;
        currentPts.Clear();
    }

    // ----------------------------------------------------------
    // Remove every generated curve object and reset lists
    private void ClearAllCurves()
    {
        recording = false;
        currentLoop = -1;
        curveIndex = 0;
        ClearActive();

        // Destroy only the objects we created
        foreach (GameObject go in curveObjs)
            if (go != null)
                Destroy(go);
        curveObjs.Clear();

        curveList.Clear();
        curves = new Vector2[0][];
    }

    // ----------------------------------------------------------
    // Catmull-Rom smoothing
    private List<Vector2> GetSmoothed(List<Vector2> src, int sub)
    {
        var outPts = new List<Vector2>();
        if (src.Count < 2) { outPts.AddRange(src); return outPts; }

        for (int i = 0; i < src.Count - 1; i++)
        {
            Vector2 p0 = i == 0 ? src[i] : src[i - 1];
            Vector2 p1 = src[i];
            Vector2 p2 = src[i + 1];
            Vector2 p3 = i + 2 < src.Count ? src[i + 2] : p2;

            outPts.Add(p1);
            for (int j = 1; j <= sub; j++)
            {
                float t = j / (float)(sub + 1);
                outPts.Add(Catmull(p0, p1, p2, p3, t));
            }
        }
        outPts.Add(src[src.Count - 1]);
        return outPts;
    }

    private static Vector2 Catmull(Vector2 p0, Vector2 p1, Vector2 p2, Vector2 p3, float t)
    {
        float t2 = t * t;
        float t3 = t2 * t;
        return 0.5f * (
            (2f * p1) +
            (-p0 + p2) * t +
            (2f * p0 - 5f * p1 + 4f * p2 - p3) * t2 +
            (-p0 + 3f * p1 - 3f * p2 + p3) * t3);
    }

    private void ClearCurveObjects()
    {
        foreach (GameObject go in curveObjs)
            if (go != null)
                Destroy(go);

        curveObjs.Clear();
    }

    //-----------------------------------------------------------------
    // 2.  Re-create visuals (LineRenderer + EdgeCollider2D)
    //    for every curve currently in curveList / curves
    //-----------------------------------------------------------------
    public void RegenerateAllVisuals()

    {
        recording = false;
        ClearActive();
        // Wipe old visuals
        ClearCurveObjects();

        // Reset index so colors reuse the same cycle
        curveIndex = 0;

        // Build a GameObject for each saved curve
        for (int i = 0; i < curveList.Count; i++)
        {
            Vector2[] src = curveList[i];
            GameObject obj = new GameObject("Curve_" + i);
            curveObjs.Add(obj);                              // track it

            LineRenderer lr = obj.AddComponent<LineRenderer>();
            lr.useWorldSpace = true;
            lr.material = lineMaterial;
            lr.widthMultiplier = lineWidth;

            Color col = Color.HSVToRGB((i * 0.25f) % 1f, 1f, 1f);
            lr.startColor = col;
            lr.endColor = col;

            lr.positionCount = src.Length;
            for (int p = 0; p < src.Length; p++)
                lr.SetPosition(p, src[p]);

            if (addCollider)
            {
                EdgeCollider2D ec = obj.AddComponent<EdgeCollider2D>();
                ec.edgeRadius = colliderEdgeRadius;

                Vector2[] local = new Vector2[src.Length];
                for (int p = 0; p < src.Length; p++)
                    local[p] = obj.transform.InverseTransformPoint(src[p]);

                ec.points = local;
            }
        }

        // Keep curveIndex in sync for the next live-recorded curve
        curveIndex = curveList.Count;
    }

    // Return y for arbitrary x on a given curve (linear segment lookup)
    float SampleY(Vector2[] curve, float x)
    {
        if (curve.Length == 0) return 0f;

        // Make sure points are ordered by x so binary search works
        if (!IsSortedByX(curve))
            System.Array.Sort(curve, (a, b) => a.x.CompareTo(b.x));

        // Off-range? clamp to nearest end
        if (x <= curve[0].x) return curve[0].y;
        if (x >= curve[curve.Length - 1].x) return curve[curve.Length - 1].y;

        // Find segment [i,i+1] that spans x (linear scan is fine for <1k pts)
        for (int i = 0; i < curve.Length - 1; i++)
        {
            if (x >= curve[i].x && x <= curve[i + 1].x)
            {
                float t = (x - curve[i].x) / (curve[i + 1].x - curve[i].x);
                return Mathf.Lerp(curve[i].y, curve[i + 1].y, t);
            }
        }
        return 0f; // should never hit
    }

    // Quick check so we only sort once
    bool IsSortedByX(Vector2[] arr)
    {
        for (int i = 1; i < arr.Length; i++)
            if (arr[i].x < arr[i - 1].x) return false;
        return true;
    }

    List<float> BuildUnifiedX()
    {
        const float EPS = 1e-4f;           // tolerance for "same x"
        var xs = new List<float>();

        foreach (Vector2[] c in curveList)
            foreach (Vector2 p in c)
                xs.Add(p.x);

        xs.Sort();

        // Unique within EPS so we do not sample duplicates
        var uniq = new List<float>();
        foreach (float v in xs)
            if (uniq.Count == 0 || Mathf.Abs(v - uniq[uniq.Count - 1]) > EPS)
                uniq.Add(v);

        return uniq;
    }
}
