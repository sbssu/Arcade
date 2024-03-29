using Cinemachine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CCameraSettings : MonoBehaviour
{
    public Transform follow;                        // 따라가는 대상.
    public Transform lookAt;                        // 바라볼 대상.
    public CinemachineFreeLook cam;                 // 시네머신 카메라.
    public bool invertX;                            // 수평 반전.
    public bool invertY;                            // 수직 반전.
    public bool allowRuntimeCameraSettingsChanges;  // 런타임 중 세팅 변경 허용.

    private void Awake()
    {
        UpdateCemeraSettings();
    }
    private void Update()
    {
        if (allowRuntimeCameraSettingsChanges)
            UpdateCemeraSettings();
    }

    private void UpdateCemeraSettings()
    {
        cam.Follow = follow;
        cam.LookAt = lookAt;
        cam.m_XAxis.m_InvertInput = invertX;
        cam.m_YAxis.m_InvertInput = invertY;
        cam.Priority = 1;
    }
}
