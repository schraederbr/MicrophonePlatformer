using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UIElements;

public class GenerateTerrain : MonoBehaviour
{
	// ---------------------------------------------------------- //
	// Inspector references
	// ---------------------------------------------------------- //
	[Header("Scene links")]
	public MoveBetween moveBetween;
	public Transform cursor;
	public UIDocument uiDoc;              // drag your UIDocument here
	public InputActionReference recordCurveAction;
	public InputActionReference sumCurvesAction;
	public InputActionReference clearCurvesAction;
	//bool countingDown;          
	//Coroutine countdownCo;

	// Visual
	public int pointsPerSecond = 25;
	public Material lineMaterial;
	public float lineWidth = 0.3f;

	// Physics
	public bool addCollider = true;
	public float colliderEdgeRadius = 0.05f;

	// Smoothing
	public bool enableSmoothing = true;
	[Range(1, 8)]
	public int smoothSubdivisions = 3;

	// Recording control
	public bool startRecording = false;

	// Exposed curves (readonly outside)
	public Vector2[][] curves;

	// ---------------------------------------------------------- //
	// Private state
	int framesPerPoint;
	int fixedFrameCount;
	int currentLoop = -1;
	public int curveIndex;
	bool recording;

	readonly List<Vector2> currentPts = new List<Vector2>();
	readonly List<Vector2[]> curveList = new List<Vector2[]>();
	readonly List<GameObject> curveObjs = new List<GameObject>();

	GameObject curObj;
	LineRenderer curLR;
	EdgeCollider2D curEC;

	// UI Toolkit
	Label recordingInfoLbl;
	Label clearInfoLabel;
	string pressToRecordMsg = "";
	public bool IsRecording => recording;   // exposes current recording state
	public string recordKeyName;
	public string clearKeyName;
	public FreezeToggle freezeToggle;


	//IEnumerator CountdownAndStart()
	//{
	//    countingDown = true;

	//    if (recordingInfoLbl != null)
	//        recordingInfoLbl.style.display = DisplayStyle.Flex;

	//    for (int i = 3; i > 0; i--)
	//    {
	//        if (recordingInfoLbl != null) recordingInfoLbl.text = i.ToString();
	//        yield return new WaitForSeconds(1f);
	//    }

	//    if (recordingInfoLbl != null)
	//    {
	//        recordingInfoLbl.text = pressToRecordMsg;                     // blank while recording
	//        recordingInfoLbl.style.display = DisplayStyle.None;
	//    }

	//    StartNewCurve(moveBetween.loopCount);                // now begin sampling
	//    recording = true;
	//    countingDown = false;
	//}


	// ---------------------------------------------------------- //
	void OnEnable()
	{
		if (recordCurveAction != null) recordCurveAction.action.Enable();
		if (sumCurvesAction != null) sumCurvesAction.action.Enable();
		if (clearCurvesAction != null) clearCurvesAction.action.Enable();
	}

	void OnDisable()
	{
		if (recordCurveAction != null) recordCurveAction.action.Disable();
		if (sumCurvesAction != null) sumCurvesAction.action.Disable();
		if (clearCurvesAction != null) clearCurvesAction.action.Disable();
	}

	// ---------------------------------------------------------- //
	void Start()
	{
		var b = clearCurvesAction.action.bindings[0];
		clearKeyName = InputControlPath.ToHumanReadableString(
			b.effectivePath,
			InputControlPath.HumanReadableStringOptions.OmitDevice);
		// Sample-rate setup
		float fps = 1f / Time.fixedDeltaTime;
		framesPerPoint = Mathf.Max(1, Mathf.RoundToInt(fps / pointsPerSecond));

		// UI label setup
		if (uiDoc != null)
		{
			recordingInfoLbl = uiDoc.rootVisualElement.Q<Label>("RecordingInfo");
			clearInfoLabel = uiDoc.rootVisualElement.Q<Label>("ClearInfo");


			if (recordCurveAction != null && recordCurveAction.action.bindings.Count > 0)
			{
				b = recordCurveAction.action.bindings[0];
				recordKeyName = InputControlPath.ToHumanReadableString(
					b.effectivePath,
					InputControlPath.HumanReadableStringOptions.OmitDevice);
			}
			pressToRecordMsg = "Press " + recordKeyName + " to record line";
			recordingInfoLbl.text = pressToRecordMsg;

			clearInfoLabel.text = "Press " + clearKeyName + " to clear the last line";
			clearInfoLabel.style.display = DisplayStyle.None;

		}

		// Optional auto-record
		if (startRecording)
		{
			int loopIdx = (moveBetween != null) ? moveBetween.loopCount : 0;
			StartNewCurve(loopIdx);
			recording = true;
			if (recordingInfoLbl != null)
			{
				recordingInfoLbl.style.display = DisplayStyle.None;
			}
		}
	}

	// ---------------------------------------------------------- //
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
			ClearLastCurve();
		}

		if (sumCurvesAction != null &&
			sumCurvesAction.action.WasPressedThisFrame())
		{
			SumAllCurves();
		}
	}

	// ---------------------------------------------------------- //
	void FixedUpdate()
	{
		if (!recording)
			return;

		// Stop when MoveBetween finishes a leg
		int loop = moveBetween.loopCount;
		if (loop != currentLoop)
		{
			FinaliseCurrentCurve();
			recording = false;
			if (recordingInfoLbl != null)
			{
				recordingInfoLbl.style.display = DisplayStyle.Flex;
				recordingInfoLbl.text = "Press " + recordKeyName + " to record another line ";
			}
			clearInfoLabel.style.display = DisplayStyle.Flex;
			return; // no auto-restart
		}

		// Normal sampling
		if (fixedFrameCount % framesPerPoint == 0)
			AddSample(cursor.position);

		fixedFrameCount++;
	}

	// ---------------------------------------------------------- //
	void ToggleRecording()
	{
		// --- Stop an active recording ---
		if (recording)
		{
			FinaliseCurrentCurve();
			recording = false;
			if (recordingInfoLbl != null)
			{
				recordingInfoLbl.text = "Press " + recordKeyName + " to record another line";
				recordingInfoLbl.style.display = DisplayStyle.Flex;
				clearInfoLabel.style.display = DisplayStyle.Flex;
				freezeToggle.Freeze();
			}
				
			return;
		}
		else
		{
			 
			recordingInfoLbl.text = "Press " + recordKeyName + " to stop recording";
			//recordingInfoLbl.style.display = DisplayStyle.None;
			clearInfoLabel.style.display = DisplayStyle.Flex;
		}



		freezeToggle.Freeze();
		StartNewCurve(moveBetween.loopCount);                // now begin sampling
		recording = true;
		//countingDown = false;
		//// --- Ignore if a countdown is already running ---
		//if (countingDown) return;

		//// --- Start the 3-second countdown ---
		//countdownCo = StartCoroutine(CountdownAndStart());
	}


	// ---------------------------------------------------------- //
	// Curve creation / visual update helpers
	// ---------------------------------------------------------- //
	void StartNewCurve(int loopIndex)
	{
		currentLoop = loopIndex;
		fixedFrameCount = 0;
		currentPts.Clear();

		curObj = new GameObject("Curve_" + curveIndex);
		curveObjs.Add(curObj);

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

	void AddSample(Vector2 wp)
	{
		if (wp.y < 0f) wp.y = 0f;
		currentPts.Add(wp);
		UpdateVisuals();
	}

	void UpdateVisuals()
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

	void FinaliseCurrentCurve()
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

		//if (curveList.Count >= 2)
		//    SumAllCurves();
	}

	void ClearActive()
	{
		curObj = null;
		curLR = null;
		curEC = null;
		currentPts.Clear();
	}

	// ---------------------------------------------------------- //
	// Curve utilities
	// ---------------------------------------------------------- //
	void SumAllCurves()
	{
		if (curveList.Count < 2) return;

		List<float> xGrid = BuildUnifiedX();
		Vector2[] summed = new Vector2[xGrid.Count];

		for (int xi = 0; xi < xGrid.Count; xi++)
		{
			float x = xGrid[xi];
			float ySum = 0f;
			foreach (Vector2[] c in curveList) ySum += SampleY(c, x);
			summed[xi] = new Vector2(x, ySum);
		}

		curveList.Clear();
		curveList.Add(summed);
		curves = curveList.ToArray();
		RegenerateAllVisuals();
	}

	void ClearAllCurves()
	{
		recording = false;
		currentLoop = -1;
		curveIndex = 0;
		ClearActive();

		foreach (GameObject go in curveObjs)
			if (go != null) Destroy(go);
		curveObjs.Clear();

		curveList.Clear();
		curves = new Vector2[0][];
		if (recordingInfoLbl != null) recordingInfoLbl.text = pressToRecordMsg;
	}

	List<Vector2> GetSmoothed(List<Vector2> src, int sub)
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

	static Vector2 Catmull(Vector2 p0, Vector2 p1, Vector2 p2, Vector2 p3, float t)
	{
		float t2 = t * t;
		float t3 = t2 * t;
		return 0.5f * (
			(2f * p1) +
			(-p0 + p2) * t +
			(2f * p0 - 5f * p1 + 4f * p2 - p3) * t2 +
			(-p0 + 3f * p1 - 3f * p2 + p3) * t3);
	}

	float SampleY(Vector2[] curve, float x)
	{
		if (curve.Length == 0) return 0f;

		if (!IsSortedByX(curve))
			System.Array.Sort(curve, (a, b) => a.x.CompareTo(b.x));

		if (x <= curve[0].x) return curve[0].y;
		if (x >= curve[curve.Length - 1].x) return curve[curve.Length - 1].y;

		for (int i = 0; i < curve.Length - 1; i++)
		{
			if (x >= curve[i].x && x <= curve[i + 1].x)
			{
				float t = (x - curve[i].x) / (curve[i + 1].x - curve[i].x);
				return Mathf.Lerp(curve[i].y, curve[i + 1].y, t);
			}
		}
		return 0f;
	}

	bool IsSortedByX(Vector2[] arr)
	{
		for (int i = 1; i < arr.Length; i++)
			if (arr[i].x < arr[i - 1].x) return false;
		return true;
	}

	List<float> BuildUnifiedX()
	{
		const float EPS = 1e-4f;
		var xs = new List<float>();

		foreach (Vector2[] c in curveList)
			foreach (Vector2 p in c) xs.Add(p.x);

		xs.Sort();

		var uniq = new List<float>();
		foreach (float v in xs)
			if (uniq.Count == 0 || Mathf.Abs(v - uniq[uniq.Count - 1]) > EPS)
				uniq.Add(v);
		return uniq;
	}



	// ------------------------------------------------------------------ //
	// Deletes the active curve if one is being drawn; otherwise deletes
	// the last saved curve.  Also tells the mover to return to pointA.
	// ------------------------------------------------------------------ //
	void ClearLastCurve()
	{
		bool clearedSomething = false; // tracks whether we actually removed a curve

		// —— 1. Cancel an in-progress recording ————————————————
		if (recording)
		{
			if (curObj != null) Destroy(curObj);
			if (curveObjs.Count > 0) curveObjs.RemoveAt(curveObjs.Count - 1);
			ClearActive();
			recording = false;
			curveIndex = curveList.Count;
			clearedSomething = true;

			if (recordingInfoLbl != null)
			{
				recordingInfoLbl.text = pressToRecordMsg;
				recordingInfoLbl.style.display = DisplayStyle.Flex;
			}
			if (clearInfoLabel != null)
				clearInfoLabel.style.display = curveList.Count > 0
					? DisplayStyle.Flex
					: DisplayStyle.None;
		}
		// —— 2. Or delete the last finished curve ———————————————
		else if (curveList.Count > 0)
		{
			int last = curveList.Count - 1;

			if (last < curveObjs.Count && curveObjs[last] != null)
				Destroy(curveObjs[last]);
			if (last < curveObjs.Count) curveObjs.RemoveAt(last);

			curveList.RemoveAt(last);
			curves = curveList.ToArray();
			curveIndex = curveList.Count;
			clearedSomething = true;

			if (recordingInfoLbl != null && curveIndex == 0)
				recordingInfoLbl.text = pressToRecordMsg;
			if (clearInfoLabel != null)
				clearInfoLabel.style.display = curveIndex > 0
					? DisplayStyle.Flex
					: DisplayStyle.None;
		}

		// —— 3. After *any* successful clear, jump back to pointA ——–
		if (clearedSomething && moveBetween != null)
			moveBetween.SnapToStart();
	}




	void ClearCurveObjects()
	{
		foreach (GameObject go in curveObjs)
			if (go != null) Destroy(go);
		curveObjs.Clear();
	}

	public void RegenerateAllVisuals()
	{
		recording = false;
		ClearActive();
		ClearCurveObjects();

		curveIndex = 0;

		for (int i = 0; i < curveList.Count; i++)
		{
			Vector2[] src = curveList[i];
			GameObject obj = new GameObject("Curve_" + i);
			curveObjs.Add(obj);

			LineRenderer lr = obj.AddComponent<LineRenderer>();
			lr.useWorldSpace = true;
			lr.material = lineMaterial;
			lr.widthMultiplier = lineWidth;

			Color col = Color.HSVToRGB((i * 0.25f) % 1f, 1f, 1f);
			lr.startColor = col;
			lr.endColor = col;

			lr.positionCount = src.Length;
			for (int p = 0; p < src.Length; p++) lr.SetPosition(p, src[p]);

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
		curveIndex = curveList.Count;
	}
}
