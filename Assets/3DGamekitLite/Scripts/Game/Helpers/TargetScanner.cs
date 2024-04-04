using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Gamekit3D
{
    // 이 클래스는 매개변수에 따라 플레이어를 간단히 스캔하고 찾는 데 사용됩니다. 적 캐릭터의 동작에 사용됩니다.
    [System.Serializable]
    public class TargetScanner
    {
        public float heightOffset = 0.0f;           // 눈의 높이를 설정합니다.
        public float detectionRadius = 10;          // 감지 반경을 설정합니다.
        [Range(0.0f, 360.0f)]
        public float detectionAngle = 270;          // 감지 각도를 설정합니다.
        public float maxHeightDifference = 1.0f;    // 최대 높이 차이를 설정합니다.
        public LayerMask viewBlockerLayerMask;      // 뷰 블로커 레이어 마스크를 설정합니다.

        /// <summary>
        /// 해당 스캐너 매개변수에 따라 플레이어가 보이는지 확인합니다.
        /// </summary>
        /// <param name="detector">감지를 실행할 위치의 Transform</param>
        /// <param name="useHeightDifference">높이 차이를 maxHeightDifference 값과 비교할지 무시할지 여부</param>
        /// <returns>플레이어 컨트롤러가 보이면 해당 컨트롤러를 반환하고, 그렇지 않으면 null을 반환합니다.</returns>
        public PlayerController Detect(Transform detector, bool useHeightDifference = true)
        {
            // 플레이어가 없거나 스포닝 중이라면 검색하지 않는다.
            if (PlayerController.instance == null || PlayerController.instance.respawning)
                return null;

            Vector3 eyePos = detector.position + Vector3.up * heightOffset;         // 눈의 위치
            Vector3 toPlayer = PlayerController.instance.transform.position - eyePos;   // 플레이어를 향하는 벡터
            Vector3 toPlayerTop = PlayerController.instance.transform.position + Vector3.up * 1.5f - eyePos;    //  플레이어의 머리를 향하는 벡터

            if (useHeightDifference && Mathf.Abs(toPlayer.y + heightOffset) > maxHeightDifference)
            {
                // 대상이 너무 높거나 너무 낮으면 도달을 시도할 필요가 없으므로, 추적을 포기합니다.
                return null;
            }

            // x,z평면에서의 플레이어를 향하는 벡터
            Vector3 toPlayerFlat = toPlayer;
            toPlayerFlat.y = 0;

            if (toPlayerFlat.sqrMagnitude <= detectionRadius * detectionRadius)
            {
                if (Vector3.Dot(toPlayerFlat.normalized, detector.forward) >
                    Mathf.Cos(detectionAngle * 0.5f * Mathf.Deg2Rad))
                {

                    bool canSee = false;

                    Debug.DrawRay(eyePos, toPlayer, Color.blue);
                    Debug.DrawRay(eyePos, toPlayerTop, Color.blue);

                    canSee |= !Physics.Raycast(eyePos, toPlayer.normalized, detectionRadius,
                        viewBlockerLayerMask, QueryTriggerInteraction.Ignore);

                    canSee |= !Physics.Raycast(eyePos, toPlayerTop.normalized, toPlayerTop.magnitude,
                        viewBlockerLayerMask, QueryTriggerInteraction.Ignore);

                    if (canSee)
                        return PlayerController.instance;
                }
            }

            return null;
        }


#if UNITY_EDITOR

        public void EditorGizmo(Transform transform)
        {
            Color c = new Color(0, 0, 0.7f, 0.4f);

            UnityEditor.Handles.color = c;
            Vector3 rotatedForward = Quaternion.Euler(0, -detectionAngle * 0.5f, 0) * transform.forward;
            UnityEditor.Handles.DrawSolidArc(transform.position, Vector3.up, rotatedForward, detectionAngle, detectionRadius);

            Gizmos.color = new Color(1.0f, 1.0f, 0.0f, 1.0f);
            Gizmos.DrawWireSphere(transform.position + Vector3.up * heightOffset, 0.2f);
        }

#endif
    }

}