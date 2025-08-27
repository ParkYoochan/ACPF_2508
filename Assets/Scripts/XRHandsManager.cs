using UnityEngine;
using UnityEngine.XR.Hands;
using UnityEngine.XR.Management;

public class XRHandsManager : MonoBehaviour
{
    XRHandSubsystem handSubsystem;

    void OnEnable()
    {
        // XR Loader에서 XRHandSubsystem 찾기
        var loader = XRGeneralSettings.Instance.Manager.activeLoader;
        if (loader != null)
        {
            handSubsystem = loader.GetLoadedSubsystem<XRHandSubsystem>();
            if (handSubsystem != null)
            {
                Debug.Log("XR Hand Subsystem initialized");
                handSubsystem.Start();
            }
        }
    }

    void OnDisable()
    {
        if (handSubsystem != null)
            handSubsystem.Stop();
    }
}
