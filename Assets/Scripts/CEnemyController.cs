using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class CEnemyController : MonoBehaviour
{
    public bool interpolateTurning = false;         // ȸ�� ���� ����.
    public bool applyAnimationRotation = false;     // �ִϸ��̼� ȸ�� ����.

    const float GROUNDED_RAY_DISTANCE = 0.8f;       // ���� �پ��ִ��� üũ�� ���� ����.

    public Animator Anim => anim;

    // �ɹ� ����(=�ʵ�)
    protected Animator anim;                        // �ִϸ�����.
    protected Rigidbody rigidbody;                  // ������ٵ�.
    protected NavMeshAgent navMeshAgent;            // NavMeshAgent.
    protected bool followNavmeshAgent;              // NavMeshAgent�� ���󰡴��� ����.

    protected bool underExternalForce;              // �ܺ� ���� ���� �����̴��� ����.
    protected bool externalForceAddGravity = true;  // �ܺ� ���� �߷��� �������� ����.
    protected Vector3 externalForce;                // �ܺ� ��.
    protected bool grounded;                        // ���� �پ��ִ��� ����.

    private void OnEnable()
    {
        // ������Ʈ ĳ��.
        anim = GetComponent<Animator>();
        navMeshAgent = GetComponent<NavMeshAgent>();

        // �ִϸ����� ����.
        anim.updateMode = AnimatorUpdateMode.AnimatePhysics;

        // NavMeshAgent ����.
        navMeshAgent.updatePosition = false;        // NavMeshAgent�� ��ġ�� ������Ʈ���� ����.

        // ������ٵ� ����.
        rigidbody = GetComponentInChildren<Rigidbody>();
        if (rigidbody == null)
            rigidbody = gameObject.AddComponent<Rigidbody>();

        rigidbody.isKinematic = true;                                   // �������� ������ ���� ����.
        rigidbody.useGravity = false;                                   // �߷� ��� ����.
        rigidbody.interpolation = RigidbodyInterpolation.Interpolate;   // ���� ��� ����.

        // NavMeshAgent�� ���󰡴��� ����.
        followNavmeshAgent = true;      
    }

    private void FixedUpdate()
    {
        // �÷��̾��� ������ ���߸� �ִϸ��̼� �ӵ��� 0���� �Ѵ�. (=freeze)
        anim.speed = CPlayerInput.Instance != null && CPlayerInput.Instance.isLockControl ? 1.0f : 0.0f;   // �ִϸ��̼� �ӵ� ����.
    }

    // ���� �پ��ִ��� üũ�ϴ� �Լ�.
    private void CheckGrounded()
    {
        Ray ray = new Ray(transform.position + Vector3.up * 0.1f, Vector3.down);   // ���� ����.
        grounded = Physics.Raycast(ray, out RaycastHit hit, GROUNDED_RAY_DISTANCE, Physics.AllLayers, QueryTriggerInteraction.Ignore);   // ����ĳ��Ʈ�� ���� �پ��ִ��� üũ.
    }
    
    // �ܺ� ���� ���� �����̴� �Լ�.
    private void ForceMovement()
    {
        if(externalForceAddGravity)
            externalForce += Physics.gravity * Time.deltaTime;   // �߷��� �ܺ� ���� ���Ѵ�.

        Vector3 movement = externalForce * Time.deltaTime;      // �ܺ� ���� �ð��� ���� ����� ������.

        // weepTest: �������� �浹�ϴ��� üũ�ϴ� �Լ�.
        if (!rigidbody.SweepTest(movement.normalized, out RaycastHit hit, movement.sqrMagnitude))   // �������� �浹�ϴ��� üũ.
            rigidbody.MovePosition(rigidbody.position + movement);                                  // ������ ����.

        // navmeshAgent.Warp : NavMeshAgent�� ��ġ�� �����ϴ� �Լ�.
        navMeshAgent.Warp(rigidbody.position);      // NavMeshAgent�� ��ġ�� ������ٵ��� ��ġ�� ����.
    }

    // root����� �ִ� �ִϸ��̼��� ����� �� ȣ��Ǵ� �Լ�.
    private void OnAnimatorMove()
    {
        // �ܺ� ���� ���� �����̴� ���̴�.
        if (underExternalForce)
            return;

        // NavMeshAgent�� ���󰡴� ���̴�.
        if(followNavmeshAgent)
        {
            navMeshAgent.speed = (anim.deltaPosition / Time.deltaTime).magnitude;   // NavMeshAgent�� �ӵ� ����.
            transform.position = navMeshAgent.nextPosition;                         // �÷��̾��� ��ġ�� NavMeshAgent�� ���� ��ġ�� ����.
        }
        else
        {
            // Rigidbody�� �浹 �׽�Ʈ�� ���� �̵��� �� �ִ��� üũ�� �� �̵�.
            if(!rigidbody.SweepTest(anim.deltaPosition.normalized, out RaycastHit hit, anim.deltaPosition.sqrMagnitude))
                rigidbody.MovePosition(rigidbody.position + anim.deltaPosition);

            if(applyAnimationRotation)
                transform.forward = anim.deltaRotation * transform.forward;   // �ִϸ��̼��� ȸ���� ����.
        }
    }

    public void SwitchFollowNavmeshAgent(bool follow)
    {
        if (!follow && navMeshAgent.enabled)
            navMeshAgent.ResetPath();               // NavMeshAgent�� ��� �ʱ�ȭ.
        else if(follow && !navMeshAgent.enabled)
            navMeshAgent.Warp(transform.position);  // NavMeshAgent�� ��ġ�� �÷��̾��� ��ġ�� ����.

        followNavmeshAgent = follow;                // NavMeshAgent�� ���󰡴��� ���� ����.
        navMeshAgent.enabled = follow;              // NavMeshAgent Ȱ��ȭ ���� ����.
    }
    public void AddForce(Vector3 force, bool useGravity = true)
    {
        // �ܺ��� ���� �������� �׺�޽��� ��θ� �ʱ�ȭ�Ѵ�(=�����)
        if(navMeshAgent.enabled)
            navMeshAgent.ResetPath();

        externalForce = force;                       // �ܺ� �� ����.
        navMeshAgent.enabled = false;                // NavMeshAgent ��Ȱ��ȭ.
        underExternalForce = true;                   // �ܺ� ���� ���� �����̴� ��.
        externalForceAddGravity = useGravity;        // �߷� ���� ���� ����.
    }
    public void ClearForce()
    {
        underExternalForce = false;                  // �ܺ� ���� ���� �����̴� ���� �ƴ�.
        navMeshAgent.enabled = true;                 // NavMeshAgent Ȱ��ȭ.       
    }

    public void SetForward(Vector3 forward)
    {
        // Ÿ���� �ٶ󺸴� ȸ�� ��.
        Quaternion targetRotation = Quaternion.LookRotation(forward);

        // ȸ�� ���� (= �ٷ� ȸ���ϴ� ���� �ƴ϶� õõ�� ȸ��)
        if (interpolateTurning)
        {
            // Ÿ�� �������� õõ�� ȸ���ϱ� ���� ���� ȸ�� ��.
            targetRotation = Quaternion.RotateTowards(transform.rotation,
                targetRotation, navMeshAgent.angularSpeed * Time.deltaTime);
        }
        transform.rotation = targetRotation;   // ȸ�� ����.
    }
    public bool SetTarget(Vector3 position)
    {
        return navMeshAgent.SetDestination(position);   // NavMeshAgent�� ������ ����.
    }
}
