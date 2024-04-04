using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class CEnemyController : MonoBehaviour
{
    public bool interpolateTurning = false;         // 회전 보간 여부.
    public bool applyAnimationRotation = false;     // 애니메이션 회전 여부.

    const float GROUNDED_RAY_DISTANCE = 0.8f;       // 땅에 붙어있는지 체크할 레이 길이.

    public Animator Anim => anim;

    // 맴버 변수(=필드)
    protected Animator anim;                        // 애니메이터.
    protected Rigidbody rigidbody;                  // 리지드바디.
    protected NavMeshAgent navMeshAgent;            // NavMeshAgent.
    protected bool followNavmeshAgent;              // NavMeshAgent를 따라가는지 여부.

    protected bool underExternalForce;              // 외부 힘에 의해 움직이는지 여부.
    protected bool externalForceAddGravity = true;  // 외부 힘에 중력을 적용할지 여부.
    protected Vector3 externalForce;                // 외부 힘.
    protected bool grounded;                        // 땅에 붙어있는지 여부.

    private void OnEnable()
    {
        // 컴포넌트 캐싱.
        anim = GetComponent<Animator>();
        navMeshAgent = GetComponent<NavMeshAgent>();

        // 애니메이터 설정.
        anim.updateMode = AnimatorUpdateMode.AnimatePhysics;

        // NavMeshAgent 설정.
        navMeshAgent.updatePosition = false;        // NavMeshAgent의 위치를 업데이트하지 않음.

        // 리지드바디 설정.
        rigidbody = GetComponentInChildren<Rigidbody>();
        if (rigidbody == null)
            rigidbody = gameObject.AddComponent<Rigidbody>();

        rigidbody.isKinematic = true;                                   // 물리적인 영향을 받지 않음.
        rigidbody.useGravity = false;                                   // 중력 사용 안함.
        rigidbody.interpolation = RigidbodyInterpolation.Interpolate;   // 보간 모드 설정.

        // NavMeshAgent를 따라가는지 여부.
        followNavmeshAgent = true;      
    }

    private void FixedUpdate()
    {
        // 플레이어의 조작이 멈추면 애니메이션 속도를 0으로 한다. (=freeze)
        anim.speed = CPlayerInput.Instance != null && CPlayerInput.Instance.isLockControl ? 1.0f : 0.0f;   // 애니메이션 속도 설정.
    }

    // 땅에 붙어있는지 체크하는 함수.
    private void CheckGrounded()
    {
        Ray ray = new Ray(transform.position + Vector3.up * 0.1f, Vector3.down);   // 레이 생성.
        grounded = Physics.Raycast(ray, out RaycastHit hit, GROUNDED_RAY_DISTANCE, Physics.AllLayers, QueryTriggerInteraction.Ignore);   // 레이캐스트로 땅에 붙어있는지 체크.
    }
    
    // 외부 힘에 의해 움직이는 함수.
    private void ForceMovement()
    {
        if(externalForceAddGravity)
            externalForce += Physics.gravity * Time.deltaTime;   // 중력을 외부 힘에 더한다.

        Vector3 movement = externalForce * Time.deltaTime;      // 외부 힘을 시간에 따라 계산한 움직임.

        // weepTest: 움직임이 충돌하는지 체크하는 함수.
        if (!rigidbody.SweepTest(movement.normalized, out RaycastHit hit, movement.sqrMagnitude))   // 움직임이 충돌하는지 체크.
            rigidbody.MovePosition(rigidbody.position + movement);                                  // 움직임 적용.

        // navmeshAgent.Warp : NavMeshAgent의 위치를 설정하는 함수.
        navMeshAgent.Warp(rigidbody.position);      // NavMeshAgent의 위치를 리지드바디의 위치로 설정.
    }

    // root모션이 있는 애니메이션을 사용할 때 호출되는 함수.
    private void OnAnimatorMove()
    {
        // 외부 힘에 의해 움직이는 중이다.
        if (underExternalForce)
            return;

        // NavMeshAgent를 따라가는 중이다.
        if(followNavmeshAgent)
        {
            navMeshAgent.speed = (anim.deltaPosition / Time.deltaTime).magnitude;   // NavMeshAgent의 속도 설정.
            transform.position = navMeshAgent.nextPosition;                         // 플레이어의 위치를 NavMeshAgent의 다음 위치로 설정.
        }
        else
        {
            // Rigidbody의 충돌 테스트를 통해 이동할 수 있는지 체크한 뒤 이동.
            if(!rigidbody.SweepTest(anim.deltaPosition.normalized, out RaycastHit hit, anim.deltaPosition.sqrMagnitude))
                rigidbody.MovePosition(rigidbody.position + anim.deltaPosition);

            if(applyAnimationRotation)
                transform.forward = anim.deltaRotation * transform.forward;   // 애니메이션의 회전을 적용.
        }
    }

    public void SwitchFollowNavmeshAgent(bool follow)
    {
        if (!follow && navMeshAgent.enabled)
            navMeshAgent.ResetPath();               // NavMeshAgent의 경로 초기화.
        else if(follow && !navMeshAgent.enabled)
            navMeshAgent.Warp(transform.position);  // NavMeshAgent의 위치를 플레이어의 위치로 설정.

        followNavmeshAgent = follow;                // NavMeshAgent를 따라가는지 여부 설정.
        navMeshAgent.enabled = follow;              // NavMeshAgent 활성화 여부 설정.
    }
    public void AddForce(Vector3 force, bool useGravity = true)
    {
        // 외부의 힘이 가해지면 네브메시의 경로를 초기화한다(=멈춘다)
        if(navMeshAgent.enabled)
            navMeshAgent.ResetPath();

        externalForce = force;                       // 외부 힘 설정.
        navMeshAgent.enabled = false;                // NavMeshAgent 비활성화.
        underExternalForce = true;                   // 외부 힘에 의해 움직이는 중.
        externalForceAddGravity = useGravity;        // 중력 적용 여부 설정.
    }
    public void ClearForce()
    {
        underExternalForce = false;                  // 외부 힘에 의해 움직이는 중이 아님.
        navMeshAgent.enabled = true;                 // NavMeshAgent 활성화.       
    }

    public void SetForward(Vector3 forward)
    {
        // 타겟을 바라보는 회전 값.
        Quaternion targetRotation = Quaternion.LookRotation(forward);

        // 회전 보간 (= 바로 회전하는 것이 아니라 천천히 회전)
        if (interpolateTurning)
        {
            // 타겟 방향으로 천천히 회전하기 위한 다음 회전 값.
            targetRotation = Quaternion.RotateTowards(transform.rotation,
                targetRotation, navMeshAgent.angularSpeed * Time.deltaTime);
        }
        transform.rotation = targetRotation;   // 회전 적용.
    }
    public bool SetTarget(Vector3 position)
    {
        return navMeshAgent.SetDestination(position);   // NavMeshAgent의 목적지 설정.
    }
}
