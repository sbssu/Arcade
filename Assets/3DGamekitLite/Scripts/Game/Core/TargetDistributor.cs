using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Gamekit3D
{
    // 이 클래스는 "crowding"을 위해 적들이 플레이어(또는 다른 대상)에게 다양한 방향에서 접근하도록
    // 타겟 주위에 호를 분배하는 기능을 제공합니다.

    [DefaultExecutionOrder(-1)]
    public class TargetDistributor : MonoBehaviour
    {
        // Use as a mean to communicate between this target and the followers        
        public class TargetFollower
        {
            public bool requireSlot;        // 타겟이 시스템에게 위치를 제공해야 할 때 이 값을 true로 설정해야 합니다
            public int assignedSlot;        // 할당된 슬롯. (없으면 -1)
            public Vector3 requiredPoint;   // 타겟이 원하는 위치.

            public TargetDistributor distributor;

            public TargetFollower(TargetDistributor owner)
            {
                distributor = owner;
                requiredPoint = Vector3.zero;
                requireSlot = false;
                assignedSlot = -1;
            }
        }

        public int arcsCount;                   // 호의 개수 (최대 등분 수)
        protected Vector3[] m_WorldDirection;   // 개수에 따른 월드 방향.

        protected bool[] m_FreeArcs;            // index번째 호가 사용 가능한지 여부.
        protected float arcDegree;              // 호의 각도.

        protected List<TargetFollower> m_Followers;     // 나를 공격하러 오는 객체.

        public void OnEnable()
        {
            m_WorldDirection = new Vector3[arcsCount];
            m_FreeArcs = new bool[arcsCount];
            m_Followers = new List<TargetFollower>();

            arcDegree = 360.0f / arcsCount;
            Quaternion rotation = Quaternion.Euler(0, -arcDegree, 0);
            Vector3 currentDirection = Vector3.forward;
            for (int i = 0; i < arcsCount; ++i)
            {
                m_FreeArcs[i] = true;
                m_WorldDirection[i] = currentDirection;
                currentDirection = rotation * currentDirection;
            }
        }

        public TargetFollower RegisterNewFollower()
        {
            TargetFollower follower = new TargetFollower(this);
            m_Followers.Add(follower);
            return follower;
        }

        public void UnregisterFollower(TargetFollower follower)
        {
            if (follower.assignedSlot != -1)
            {
                m_FreeArcs[follower.assignedSlot] = true;
            }

            m_Followers.Remove(follower);
        }

        // at the end of the frame, we distribute target position to all follower that asked for one.
        // 프레임의 끝에서, 우리는 타겟 위치를 요청한 모든 팔로워에게 분배합니다.
        private void LateUpdate()
        {
            for (int i = 0; i < m_Followers.Count; ++i)
            {
                TargetFollower follower = m_Followers[i];

                //we free whatever arc this follower may already have. 
                //If it still need it, it will be picked again next lines.
                //if it changed position the new one will be picked.
                if (follower.assignedSlot != -1)
                {
                    m_FreeArcs[follower.assignedSlot] = true;
                }

                if (follower.requireSlot)
                {
                    follower.assignedSlot = GetFreeArcIndex(follower);
                }
            }
        }

        public Vector3 GetDirection(int index)
        {
            return m_WorldDirection[index];
        }

        public int GetFreeArcIndex(TargetFollower follower)
        {
            bool found = false;

            Vector3 wanted = follower.requiredPoint - transform.position;
            Vector3 rayCastPosition = transform.position + Vector3.up * 0.4f;

            wanted.y = 0;
            float wantedDistance = wanted.magnitude;

            wanted.Normalize();

            float angle = Vector3.SignedAngle(wanted, Vector3.forward, Vector3.up);
            if (angle < 0)
                angle = 360 + angle;

            int wantedIndex = Mathf.RoundToInt(angle / arcDegree);
            if (wantedIndex >= m_WorldDirection.Length)
                wantedIndex -= m_WorldDirection.Length;

            int choosenIndex = wantedIndex;

            RaycastHit hit;
            if (!Physics.Raycast(rayCastPosition, GetDirection(choosenIndex), out hit, wantedDistance))
                found = m_FreeArcs[choosenIndex];

            if (!found)
            {//we are going to test left right with increasing offset
                int offset = 1;
                int halfCount = arcsCount / 2;
                while (offset <= halfCount)
                {
                    int leftIndex = wantedIndex - offset;
                    int rightIndex = wantedIndex + offset;

                    if (leftIndex < 0) leftIndex += arcsCount;
                    if (rightIndex >= arcsCount) rightIndex -= arcsCount;

                    if (!Physics.Raycast(rayCastPosition, GetDirection(leftIndex), wantedDistance) &&
                        m_FreeArcs[leftIndex])
                    {
                        choosenIndex = leftIndex;
                        found = true;
                        break;
                    }

                    if (!Physics.Raycast(rayCastPosition, GetDirection(rightIndex), wantedDistance) &&
                        m_FreeArcs[rightIndex])
                    {
                        choosenIndex = rightIndex;
                        found = true;
                        break;
                    }

                    offset += 1;
                }
            }

            if (!found)
            {//we couldn't find a free direction, return -1 to tell the caller there is no free space
                return -1;
            }

            m_FreeArcs[choosenIndex] = false;
            return choosenIndex;
        }

        public void FreeIndex(int index)
        {
            m_FreeArcs[index] = true;
        }
    }

}