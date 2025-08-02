using UnityEngine;

public class UncapFPS : MonoBehaviour
{
    void Awake()
    {
        QualitySettings.vSyncCount = 0;   // turn off v-sync globally
        Application.targetFrameRate = -1; // -1 = unlimited
    }
}