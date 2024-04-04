using Gamekit3D;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class CPlayerScanner
{
    public float heightOffset = 0.0f;           // 눈의 높이를 설정합니다.
    public float maxHeightDifference = 1.0f;    // 최대 높이 차이를 설정합니다.
    public float detectionRadius = 10;          // 감지 반경을 설정합니다.
    
    [Range(0.0f, 360.0f)]
    public float detectionAngle = 270;          // 감지 각도를 설정합니다.
    public LayerMask viewBlockerLayerMask;      // 뷰 블로커 레이어 마스크를 설정합니다.


    public CPlayerController Detect(Transform detector)
    {
        if (CPlayerController.Instance == null)
            return null;

        Vector3 playerPos = CPlayerController.Instance.transform.position;  // 플레이어의 위치.
        Vector3 eyePos = detector.position + Vector3.up * heightOffset;     // 감시자의 눈 높이.
        Vector3 toPlayerDir = playerPos - eyePos;                           // 플레이어를 향하는 방향.
        Vector3 toPlayerTopDir = playerPos + Vector3.up * 1.5f - eyePos;    // 플레이어의 머리를 향하는 방향.

        // 플레이어가 너무 높거나 낮으면 감지하지 않는다.
        if (Mathf.Abs(toPlayerDir.y + heightOffset) > maxHeightDifference)
            return null;

        // x,z평면에서의 플레이어를 향하는 벡터
        Vector3 toPlayerFlatDir = toPlayerDir;
        toPlayerFlatDir.y = 0;

        // Vector3.sqrMagnitude : 벡터의 길이의 제곱을 반환합니다.
        // Vector3.magnitude : 벡터의 길이를 반환합니다.

        // 플레이어와의 거리가 최대 시야거리내에 있어야한다.
        if(toPlayerFlatDir.sqrMagnitude <= detectionRadius * detectionRadius)
        {
            // Vector3.Dot(Vector3 a, Vector3 b) : 두 벡터의 내적을 반환합니다.
            float angle = Vector3.Dot(toPlayerFlatDir.normalized, detector.forward);
            float cos = Mathf.Cos(detectionAngle * 0.5f * Mathf.Deg2Rad);
            if(angle > cos)
            {
                bool canSee = false;
                Debug.DrawRay(eyePos, toPlayerDir, Color.blue);         // 눈에서 플레이어까지의 방향을 그린다.
                Debug.DrawRay(eyePos, toPlayerTopDir, Color.blue);      // 눈에서 플레이어 머리까지의 방향을 그린다.

                // 플레이어를 향하는 레이를 발사해 시야 범위 내 장애물을 확인한다.
                canSee |= !Physics.Raycast(eyePos, toPlayerDir.normalized, detectionRadius, viewBlockerLayerMask, QueryTriggerInteraction.Ignore);
                canSee |= !Physics.Raycast(eyePos, toPlayerTopDir.normalized, toPlayerTopDir.magnitude, viewBlockerLayerMask, QueryTriggerInteraction.Ignore);
                if (canSee)
                    return CPlayerController.Instance;
            }
        }

        return null;
    }

#if UNITY_EDITOR
    public void DrawGismosEditor(Transform transform)
    {
        Vector3 rotatedForward = Quaternion.Euler(0f, -detectionAngle * 0.5f, 0f) * transform.forward;

        UnityEditor.Handles.color = new Color(0f, 0f, 0.7f, 0.2f);
        UnityEditor.Handles.DrawSolidArc(transform.position, Vector3.up, rotatedForward, detectionAngle, detectionRadius);

        Gizmos.color = new Color(1f, 1f, 0f, 1f);
        Gizmos.DrawWireSphere(transform.position + Vector3.up * heightOffset, 0.2f);
        
    }
#endif
}
