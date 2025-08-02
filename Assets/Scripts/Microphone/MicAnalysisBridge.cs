using UnityEngine;
using System.Runtime.InteropServices;

public class MicAnalysisBridge : MonoBehaviour
{
#if UNITY_WEBGL && !UNITY_EDITOR
    [DllImport("__Internal")]
    private static extern void StartMicAnalysis();
#endif

    void Start()
    {
#if UNITY_WEBGL && !UNITY_EDITOR
        StartMicAnalysis();
#endif
    }
}
