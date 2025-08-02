using System.Collections;
using UnityEngine;

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

    public bool calibrationComplete = false;

    const string KEY_MIN_LOUDNESS = "cal_minLoudness";
    const string KEY_MAX_LOUDNESS = "cal_maxLoudness";
    const string KEY_MIN_FREQ = "cal_minFreq";
    const string KEY_MAX_FREQ = "cal_maxFreq";

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
        yield return new WaitForSeconds(5f); // Allow mic to initialize

        Debug.Log("Be as quiet as possible...");
        yield return SampleLoudness(3f, quiet: true);

        Debug.Log("Now be loud!");
        yield return SampleLoudness(3f, quiet: false);

        Debug.Log("Sing your lowest pitch...");
        yield return SampleFrequency(3f, low: true);

        Debug.Log("Sing your highest pitch...");
        yield return SampleFrequency(3f, low: false);

        SaveCalibrationToPrefs();
        calibrationComplete = true;
        Debug.Log("Calibration complete and saved!");
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

    private IEnumerator SampleFrequency(float duration, bool low)
    {
        float t = 0f;
        float val = low ? float.MaxValue : float.MinValue;

        while (t < duration)
        {
            float freq = mic.frequency;
            if (low) val = Mathf.Min(val, freq);
            else val = Mathf.Max(val, freq);
            t += Time.deltaTime;
            yield return null;
        }

        if (low) minFrequency = val;
        else maxFrequency = val;
    }

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
