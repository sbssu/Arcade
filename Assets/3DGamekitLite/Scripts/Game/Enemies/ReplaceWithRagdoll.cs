using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Gamekit3D
{
    public class ReplaceWithRagdoll : MonoBehaviour
    {
        public GameObject ragdollPrefab;

        public void Replace()
        {
            GameObject ragdollInstance = Instantiate(ragdollPrefab, transform.position, transform.rotation);

            // 복제된 계층 구조의 객체의 위치/회전을 복사할 때 매번 "정정"을 시도하는 랙돌이 변형된/오작동된 인스턴스를 만들게 됩니다.
            // 따라서 비활성화해야 합니다. 
            ragdollInstance.SetActive(false);

            EnemyController baseController = GetComponent<EnemyController>();
            RigidbodyDelayedForce t = ragdollInstance.AddComponent<RigidbodyDelayedForce>();
            t.forceToAdd = baseController.externalForce;

            Transform ragdollCurrent = ragdollInstance.transform;
            Transform current = transform;
            bool first = true;

            while (current != null && ragdollCurrent != null)
            {
                if (first || ragdollCurrent.name == current.name)
                {
                    //we only match part of the hierarchy that are named the same, except for the very first, as the 2 objects will have different name (but must have the same skeleton)
                    ragdollCurrent.rotation = current.rotation;
                    ragdollCurrent.position = current.position;
                    first = false;
                }

                if (current.childCount > 0)
                {
                    // Get first child.
                    current = current.GetChild(0);
                    ragdollCurrent = ragdollCurrent.GetChild(0);
                }
                else
                {
                    while (current != null)
                    {
                        if (current.parent == null || ragdollCurrent.parent == null)
                        {
                            // No more transforms to find.
                            current = null;
                            ragdollCurrent = null;
                        }
                        else if (current.GetSiblingIndex() == current.parent.childCount - 1 ||
                                 current.GetSiblingIndex() + 1 >= ragdollCurrent.parent.childCount)
                        {
                            // Need to go up one level.
                            current = current.parent;
                            ragdollCurrent = ragdollCurrent.parent;
                        }
                        else
                        {
                            // Found next sibling for next iteration.
                            current = current.parent.GetChild(current.GetSiblingIndex() + 1);
                            ragdollCurrent = ragdollCurrent.parent.GetChild(ragdollCurrent.GetSiblingIndex() + 1);
                            break;
                        }
                    }
                }
            }


            ragdollInstance.SetActive(true);
            Destroy(gameObject);
        }
    }
}