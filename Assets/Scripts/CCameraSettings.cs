using Cinemachine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CCameraSettings : MonoBehaviour
{
    public Transform follow;                        // ���󰡴� ���.
    public Transform lookAt;                        // �ٶ� ���.
    public CinemachineFreeLook cam;                 // �ó׸ӽ� ī�޶�.
    public bool invertX;                            // ���� ����.
    public bool invertY;                            // ���� ����.
    public bool allowRuntimeCameraSettingsChanges;  // ��Ÿ�� �� ���� ���� ���.

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
