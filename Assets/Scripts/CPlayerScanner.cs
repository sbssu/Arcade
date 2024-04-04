using Gamekit3D;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class CPlayerScanner
{
    public float heightOffset = 0.0f;           // ���� ���̸� �����մϴ�.
    public float maxHeightDifference = 1.0f;    // �ִ� ���� ���̸� �����մϴ�.
    public float detectionRadius = 10;          // ���� �ݰ��� �����մϴ�.
    
    [Range(0.0f, 360.0f)]
    public float detectionAngle = 270;          // ���� ������ �����մϴ�.
    public LayerMask viewBlockerLayerMask;      // �� ���Ŀ ���̾� ����ũ�� �����մϴ�.


    public CPlayerController Detect(Transform detector)
    {
        if (CPlayerController.Instance == null)
            return null;

        Vector3 playerPos = CPlayerController.Instance.transform.position;  // �÷��̾��� ��ġ.
        Vector3 eyePos = detector.position + Vector3.up * heightOffset;     // �������� �� ����.
        Vector3 toPlayerDir = playerPos - eyePos;                           // �÷��̾ ���ϴ� ����.
        Vector3 toPlayerTopDir = playerPos + Vector3.up * 1.5f - eyePos;    // �÷��̾��� �Ӹ��� ���ϴ� ����.

        // �÷��̾ �ʹ� ���ų� ������ �������� �ʴ´�.
        if (Mathf.Abs(toPlayerDir.y + heightOffset) > maxHeightDifference)
            return null;

        // x,z��鿡���� �÷��̾ ���ϴ� ����
        Vector3 toPlayerFlatDir = toPlayerDir;
        toPlayerFlatDir.y = 0;

        // Vector3.sqrMagnitude : ������ ������ ������ ��ȯ�մϴ�.
        // Vector3.magnitude : ������ ���̸� ��ȯ�մϴ�.

        // �÷��̾���� �Ÿ��� �ִ� �þ߰Ÿ����� �־���Ѵ�.
        if(toPlayerFlatDir.sqrMagnitude <= detectionRadius * detectionRadius)
        {
            // Vector3.Dot(Vector3 a, Vector3 b) : �� ������ ������ ��ȯ�մϴ�.
            float angle = Vector3.Dot(toPlayerFlatDir.normalized, detector.forward);
            float cos = Mathf.Cos(detectionAngle * 0.5f * Mathf.Deg2Rad);
            if(angle > cos)
            {
                bool canSee = false;
                Debug.DrawRay(eyePos, toPlayerDir, Color.blue);         // ������ �÷��̾������ ������ �׸���.
                Debug.DrawRay(eyePos, toPlayerTopDir, Color.blue);      // ������ �÷��̾� �Ӹ������� ������ �׸���.

                // �÷��̾ ���ϴ� ���̸� �߻��� �þ� ���� �� ��ֹ��� Ȯ���Ѵ�.
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
