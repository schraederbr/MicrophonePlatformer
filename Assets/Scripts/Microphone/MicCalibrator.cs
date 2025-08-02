using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SocialPlatforms.Impl;
using UnityEngine.UIElements;

public class MicCalibration : MonoBehaviour
{
    public MicAnalysisReceiver mic;  // Assign in Inspector

    [Header("Calibrated Ranges")]
    public float minLoudness = 10f;
    public float maxLoudness = 100f;
    public float minFrequency = 100f;
    public float maxFrequency = 1000f;
    public static float loudness = 0f;
    public static float frequency = 0f;
    public UIDocument uiDocument;
    public Label calLabel;
    public bool calibrationComplete = false;
    public InputActionReference jumpAction;

    private void OnEnable()
    {
        if (jumpAction != null) jumpAction.action.Enable();
    }

    private void OnDisable()
    {
        if (jumpAction != null) jumpAction.action.Disable();
    }

    const string KEY_MIN_LOUDNESS = "cal_minLoudness";
    const string KEY_MAX_LOUDNESS = "cal_maxLoudness";
    const string KEY_MIN_FREQ = "cal_minFreq";
    const string KEY_MAX_FREQ = "cal_maxFreq";

    void Awake()
    {
        // Get the root VisualElement from this UIDocument
        var root = uiDocument.rootVisualElement;

        // Find the Label whose name (or UXML id) is "FPSCounter"
        calLabel = root.Q<Label>("Calibration");

        if (calLabel == null)
            Debug.LogWarning("Label 'Score' not found in UIDocument.");
    }

    void Update()
    {
        if (calibrationComplete)
        {
            loudness = GetNormalizedLoudness();
            frequency = GetNormalizedFrequency();
            //Debug.Log($"Loudness: {loudness:F1} | Frequency: {frequency:F1}");
        }
    }

    void Start()
    {
        if (LoadCalibrationFromPrefs())
        {
            Debug.Log("Calibration loaded from PlayerPrefs");
            calibrationComplete = true;
        }
        else
        {
            StartCoroutine(RunCalibration());
        }
    }

    public IEnumerator RunCalibration()
    {
        // Make sure the Jump action is enabled
        if (!jumpAction.action.enabled) jumpAction.action.Enable();
        calLabel.style.display = DisplayStyle.Flex;

        // Fetch a human-readable key / button name from the first binding
        string jumpKey = jumpAction.action.GetBindingDisplayString();

        // 1) Prompt the user and wait for Jump
        calLabel.text = $"Press {jumpKey} to start calibration";
        yield return new WaitUntil(() => jumpAction.action.triggered);


        // 3) Quiet sample
        calLabel.text = "Be as quiet as possible…";
        yield return new WaitForSeconds(1f);
        yield return SampleLoudness(3f, quiet: true);

        // 4) Loud sample
        calLabel.text = "Be loud!";
        yield return SampleLoudness(3f, quiet: false);

        //// 5) Low pitch
        //calLabel.text = "Sing your lowest pitch…";
        //yield return SampleFrequency(0.1f, low: true);

        //// 6) High pitch
        //calLabel.text = "Sing your highest pitch…";
        //yield return SampleFrequency(0.1f, low: false);

        // 7) Finish up
        SaveCalibrationToPrefs();
        calibrationComplete = true;
        calLabel.text = "Calibration complete!";
        yield return new WaitForSeconds(2f);

        calLabel.style.display = DisplayStyle.None;
    }

    private IEnumerator SampleLoudness(float duration, bool quiet)
    {
        float t = 0f;
        float val = quiet ? float.MaxValue : float.MinValue;

        while (t < duration)
        {
            float loud = mic.loudness;
            if (quiet) val = Mathf.Min(val, loud);
            else val = Mathf.Max(val, loud);
            t += Time.deltaTime;
            yield return null;
        }

        if (quiet) minLoudness = val;
        else maxLoudness = val;
    }

    //private IEnumerator SampleFrequency(float duration, bool low)
    //{
    //    float t = 0f;
    //    float val = low ? float.MaxValue : float.MinValue;

    //    while (t < duration)
    //    {
    //        float freq = mic.frequency;
    //        if (low) val = Mathf.Min(val, freq);
    //        else val = Mathf.Max(val, freq);
    //        t += Time.deltaTime;
    //        yield return null;
    //    }

    //    if (low) minFrequency = val;
    //    else maxFrequency = val;
    //}

    public float GetNormalizedLoudness()
    {
        return Mathf.InverseLerp(minLoudness, maxLoudness, mic.loudness);
    }

    public float GetNormalizedFrequency()
    {
        return Mathf.InverseLerp(minFrequency, maxFrequency, mic.frequency);
    }

    private void SaveCalibrationToPrefs()
    {
        PlayerPrefs.SetFloat(KEY_MIN_LOUDNESS, minLoudness);
        PlayerPrefs.SetFloat(KEY_MAX_LOUDNESS, maxLoudness);
        PlayerPrefs.SetFloat(KEY_MIN_FREQ, minFrequency);
        PlayerPrefs.SetFloat(KEY_MAX_FREQ, maxFrequency);
        PlayerPrefs.Save();
    }

    private bool LoadCalibrationFromPrefs()
    {
        if (PlayerPrefs.HasKey(KEY_MIN_LOUDNESS) &&
            PlayerPrefs.HasKey(KEY_MAX_LOUDNESS) &&
            PlayerPrefs.HasKey(KEY_MIN_FREQ) &&
            PlayerPrefs.HasKey(KEY_MAX_FREQ))
        {
            minLoudness = PlayerPrefs.GetFloat(KEY_MIN_LOUDNESS);
            maxLoudness = PlayerPrefs.GetFloat(KEY_MAX_LOUDNESS);
            minFrequency = PlayerPrefs.GetFloat(KEY_MIN_FREQ);
            maxFrequency = PlayerPrefs.GetFloat(KEY_MAX_FREQ);
            return true;
        }

        return false;
    }

    public void ClearCalibration()
    {
        PlayerPrefs.DeleteKey(KEY_MIN_LOUDNESS);
        PlayerPrefs.DeleteKey(KEY_MAX_LOUDNESS);
        PlayerPrefs.DeleteKey(KEY_MIN_FREQ);
        PlayerPrefs.DeleteKey(KEY_MAX_FREQ);
        PlayerPrefs.Save();
    }
}
