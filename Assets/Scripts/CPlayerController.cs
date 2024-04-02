using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(CharacterController))]
[RequireComponent(typeof(Animator))]
[RequireComponent(typeof(CPlayerInput))]
public class CPlayerController : Singleton<CPlayerController>
{
    public CCameraSettings cameraSettings;  // �ó׸ӽ� ī�޶�.
    public float maxForawrdSpeed = 8f;      // �ִ� �̵� �ӵ�.
    public float gravity = 20f;             // �߷°� (���߿����� �ϰ� �ӵ�)
    public float jumpSpeed = 10f;           // ���� ��
    public float minTurnSpeed = 400f;       // �޸��� ���� ȸ���ϴ� ��
    public float maxTurnSpeed = 1200f;      // ������ ���¿��� ȸ���ϴ� ��
    public float idleTimeout = 5f;          // Idle(����)���� ��ȯ ��� �ð�
    public bool canAttack;                  // ���� ���� ����

    public CMeleeWeapon meleeWeapon;                // ���� ����.
    public CRandomAudioPlayer footstepAudio;        // �ȱ� �����
    public CRandomAudioPlayer hurtAudio;            // �ǰ� �����
    public CRandomAudioPlayer landingPlayer;        // ���� �����
    public CRandomAudioPlayer emoteLandingPlayer;   // (����) ���� �����
    public CRandomAudioPlayer emoteDeathPlayer;     // (����) ��� �����
    public CRandomAudioPlayer emoteAttackAudio;     // (����) ���� �����
    public CRandomAudioPlayer emoteJumpAudio;       // (����) ���� �����

    // animation.
    protected Animator animator;
    protected AnimatorStateInfo currentStateInfo;       // ���� �ִϸ��̼� ����
    protected AnimatorStateInfo nextStateInfo;          // ���� �ִϸ��̼� ����
    protected bool isAnimatorTransitioning;             // �ִϸ��̼��� ��ȯ���ΰ�?

    protected AnimatorStateInfo prevCurrentStateInfo;   // (���� ������) ���� �ִϸ��̼� ����
    protected AnimatorStateInfo prevNextStateInfo;      // (���� ������) ���� �ִϸ��̼� ����
    protected bool prevAnimatorTransitioning;           // (���� ������) �ִϸ��̼� ��ȯ��?

    // user control.
    protected CharacterController controller;   // ĳ���� ��Ʈ�ѷ� ������Ʈ
    protected CPlayerInput input;               // �÷��̾� �Է� ��.
    protected bool isGrounded;                  // ���鿡 �� �ִ°�?
    protected bool prevGrounded;                // (���� ������) ���鿡 �� �ִ°�?
    protected bool isReadyToJump;               // ���� ���� �����ΰ�?
    protected bool isAttack;                    // ĳ���Ͱ� ���� ���� ���̴�.
    protected bool isCombo;                     // ĳ���Ͱ� ���� ���� �޺� ���̴�.

    // control parametor.
    protected float desiredForwardSpeed;        // ���ϴ� ���� �ӵ�
    protected float forwardSpeed;               // ���� �̵� �ӵ�
    protected float verticalSpeed;              // �ϰ� �ӵ�
    protected float idleTimer;                  // ���޻��� üũ �ð�.

    protected Material walkingSurface;          // �Ȱ� �ִ� ���� ǥ��.

    // rotation.
    protected Quaternion targetRotation;        // ĳ���Ͱ� ��ǥ�ϴ� ȸ�� ��.
    protected float angleDiff;                  // ��ǥ ȸ�� ���� ������ ���� ����.


    // Ellen�� ��Ȯ�� �����̱� ���� ��� ��.
    const float AIRBORNE_TURN_SPEED_PROPORTION = 5.4f;      // ���� ȸ�� �ӵ� ����
    const float STICKING_GRAVITY_PROPERTION = 0.3f;         // ���鿡 ��� ���� �߷� ����
    const float GROUNDED_RAY_DISTANCE = 1f;                 // ���� üũ ������ ����
    const float JUMP_ABORT_SPEED = 10f;                     // ���� �ߴ� �ӵ�.
    const float MIN_ENEMY_DOT_COEFF = 0.2f;                 // ???
    const float INVERSE_ONE_EIGHTY = 1f / 180f;             // �� ȸ�� ��.

    const float GROUND_ACCELERATION = 20;                   // ���鿡���� ���ӵ�
    const float GROUND_DECELERATION = 25;                   // ���鿡���� ���ӵ�


    private void Awake()
    {
        input = GetComponent<CPlayerInput>();
        animator = GetComponent<Animator>();
        controller = GetComponent<CharacterController>();

        meleeWeapon.SetOwner(gameObject);
    }

    private void FixedUpdate()
    {
        CacheAnimatorState();                       // ���� �ִϸ����� ���� ĳ��(=����)
        UpdateInputBlocking();                      // Ʈ���ſ� ���� �÷��̾� �Է� ���� ����
        EquipMeleeWeapon(IsWeaponEquiped());        // ���� ��� ���� üũ

        // ���� ��� �ð� ����
        animator.SetFloat(hashStateTime, Mathf.Repeat(currentStateInfo.normalizedTime, 1f));
        animator.ResetTrigger(hashMeleeAttack);

        // ���� ��ư�� ������ ���� ������ ���¸� Ʈ���� üũ.
        if (input.IsAttack && canAttack)
            animator.SetTrigger(hashMeleeAttack);

        CalculateForawdMovement();                      // ����(=������) �ӵ� ���
        CalculateVerticalMovement();                    // ����(=�߷�, ����) �ӵ� ���

        // ĳ���� ȸ��
        SetTargetRotation();                            // �Է°��� ���� ��ǥ ȸ���� ���.
        if (IsOrientationUpdate() && IsMoveInput)       // ȸ���� �� �ִ� �������� üũ.
            UpdateOrientation();                        // ��ǥġ�� ���� ȸ��.

        PlayAudio();
        TimeoutToIdle();

        prevGrounded = isGrounded;                      // ���� ���°��� �������� ����.
    }


    // �ִϸ����� ���°� ĳ�̵� �� ȣ��Ǿ� �����̰� Ȱ��ȭ�Ǿ�� �ϴ��� ���θ� �����մϴ�.
    private bool IsWeaponEquiped()
    {
        bool equipped = currentStateInfo.shortNameHash == hashEllenCombo1 || nextStateInfo.shortNameHash == hashEllenCombo1;
        equipped |= currentStateInfo.shortNameHash == hashEllenCombo2 || nextStateInfo.shortNameHash == hashEllenCombo2;
        equipped |= currentStateInfo.shortNameHash == hashEllenCombo3 || nextStateInfo.shortNameHash == hashEllenCombo3;
        equipped |= currentStateInfo.shortNameHash == hashEllenCombo4 || nextStateInfo.shortNameHash == hashEllenCombo4;

        return equipped;
    }

    // ���⸦ ����ؾ��ϴ����� üũ�Ѵ�.
    private void EquipMeleeWeapon(bool isEquip)
    {
        meleeWeapon.gameObject.SetActive(isEquip);
        isAttack = false;
        isCombo = isEquip;

        // ���⸦ ������� ������ MeleeAttack Ʈ���Ÿ� �ʱ�ȭ�Ѵ�.
        if (!isEquip)
            animator.ResetTrigger(hashMeleeAttack);
    }

    // (root��� ����) �ִϸ��̼ǿ� ���� ĳ���Ͱ� �����̸� ȣ��Ǵ� �̺�Ʈ �Լ�.
    private void OnAnimatorMove()
    {
        Vector3 movement = forwardSpeed * transform.forward * Time.deltaTime;
        if (isGrounded)
        {
            Ray ray = new Ray(transform.position + Vector3.up * GROUNDED_RAY_DISTANCE * 0.5f, -Vector3.up);
            if (Physics.Raycast(ray, out RaycastHit hit, GROUNDED_RAY_DISTANCE, Physics.AllLayers, QueryTriggerInteraction.Ignore))
            {
                movement = Vector3.ProjectOnPlane(animator.deltaPosition, hit.normal);
                Renderer groundRenderer = hit.collider.GetComponentInChildren<Renderer>();
                walkingSurface = groundRenderer ? groundRenderer.sharedMaterial : null;
            }
            else
            {
                // (����ó��) ���� ��Ȥ ���鿡 ���̰� �浹���� �ʴ� ��찡 �߻��Ѵ�.
                // �̶� �⺻ ������ ó���Ѵ�.
                movement = animator.deltaPosition;
                walkingSurface = null;
            }
        }
        else
        {
            // ü���߿��� ���� �ӵ��� �����Ѵ�.
            movement = forwardSpeed * transform.forward * Time.deltaTime;
        }

        movement += verticalSpeed * Vector3.up * Time.deltaTime;    // ���� ���� �ӷ� ���� ���Ѵ�.

        // animator�� deltaPosition�� deltaRotation�� root��ǿ� ���� �̵�,ȸ�� ���� �ǹ��Ѵ�.

        controller.transform.rotation *= animator.deltaRotation;            // root ������Ʈ ȸ��
        controller.Move(movement);                                          // root ������Ʈ �̵�
        isGrounded = controller.isGrounded;                                 // ĳ���� ��Ʈ�ѷ��� ���� ���� üũ.
        if (!isGrounded)
            animator.SetFloat(hashAirBornVerticalSpeed, verticalSpeed);     // ü�� ���� �ӷ� �Ķ���� �� ����
        animator.SetBool(hashGrounded, isGrounded);                         // ���� üũ �Ķ���� �� ����.
    }

        
    // ============================ ���� ���� �Լ� ============================
    
    // �ִϸ��̼� �̺�Ʈ�� ȣ��Ǿ� Ellen�� �����̸� �ֵθ� �� ȣ��ȴ�.
    public void MeleeAttackStart(int throwing = 0)
    {
        meleeWeapon.BeginAttack(throwing != 0);
        isAttack = true;
    }

    // �ִϸ��̼� �̺�Ʈ�� ȣ��Ǿ� Ellen�� ������ �ֵθ��⸦ ���� �� ȣ��ȴ�.
    public void MeleeAttackEnd()
    {
        meleeWeapon.EndAttack();
        isAttack = false;
    }

    private void CacheAnimatorState()
    {
        prevCurrentStateInfo = currentStateInfo;
        prevNextStateInfo = nextStateInfo;
        prevAnimatorTransitioning = isAnimatorTransitioning;

        // ����, ���� �ִϸ��̼� ���� �׸��� ��ȯ������ üũ�Ѵ�.
        currentStateInfo = animator.GetCurrentAnimatorStateInfo(0);
        nextStateInfo = animator.GetNextAnimatorStateInfo(0);
        isAnimatorTransitioning = animator.IsInTransition(0);           // �ִϸ��̼��� ��ȯ���ΰ�?
    }
    private void UpdateInputBlocking()
    {
        // ���� Ȥ�� ���� �ִϸ��̼��� hash�� block input�� ��� �Է��� �����Ѵ�.
        bool isBlocked = currentStateInfo.tagHash == hashBlockInput && !isAnimatorTransitioning;
        isBlocked |= nextStateInfo.tagHash == hashBlockInput;
        input.isLockControl = isBlocked;
    }
    private void CalculateForawdMovement()
    {
        Vector2 moveInput = input.MoveInput;
        if (moveInput.sqrMagnitude > 1f)        // �ش� ������ ũ�Ⱑ 1���� ũ�ٸ�...
            moveInput.Normalize();              // ũ�Ⱑ 1�� ���� ���ͷ� ����ȭ.

        // �÷��̾ �����ؾ��ϴ� �ӵ�.
        desiredForwardSpeed = moveInput.magnitude * maxForawrdSpeed;

        // ���� �Է°��� ���� ��,���ӷ��� ����Ѵ�.
        float acclerataion = IsMoveInput ? GROUND_ACCELERATION : GROUND_DECELERATION;

        // ���� �ӵ��� (�������) �����ؾ��ϴ� �ӵ����� ��,�����Ѵ�.
        forwardSpeed = Mathf.MoveTowards(forwardSpeed, desiredForwardSpeed, acclerataion);
        animator.SetFloat(hashForwardSpeed, forwardSpeed);
    }
    private void CalculateVerticalMovement()
    {
        if (!input.IsJump && isGrounded)
            isReadyToJump = true;
        
        
        if(isGrounded)
        {
            // Ellen�� ���鿡 "���"���� ����Ǵ� ���� �߷� ��.
            verticalSpeed = -gravity * STICKING_GRAVITY_PROPERTION;
            if(input.IsJump && isReadyToJump && !isCombo)
            {
                // ĳ���Ͱ� ����Ű�� ������ ��. (�޺� �߿��� �� �� ����)
                verticalSpeed = jumpSpeed;
                isGrounded = false;
                isReadyToJump = false;
            }
        }
        else
        {
            // ĳ���Ͱ� �������� �ö󰡴� ���̸� ���� ��ư�� ������ ���� ���� ���
            //  => ����ڰ� ���� ���� �������� �Ѵ�.
            if(!input.IsJump && verticalSpeed > 0f)
            {
                // �ö󰡴� �ӵ��� �߷� +@�� ���� ������ ���ߵ��� �Ѵ�.
                verticalSpeed -= JUMP_ABORT_SPEED * Time.deltaTime;
            }

            // ���� �ӷ��� 0�� ���� �ٻ��� ��� 0���� �����Ѵ�.
            if (Mathf.Approximately(verticalSpeed, 0))
                verticalSpeed = 0f;

            // ĳ���Ͱ� ü�����̸� �߷� ���� �����Ѵ�.
            verticalSpeed -= gravity * Time.deltaTime;
        }
    }
    private void SetTargetRotation()
    {
        Vector2 moveInput = input.MoveInput;
        Vector3 localMovementDirection = new Vector3(moveInput.x, 0f, moveInput.y).normalized;

        // �÷��̾��� ������ ���ϴ� ����.
        Vector3 forward = Quaternion.Euler(0f, cameraSettings.cam.m_XAxis.Value, 0f) * Vector3.forward;
        forward.y = 0;
        forward.Normalize();

        Quaternion targetRotation;
        if (Mathf.Approximately(Vector3.Dot(localMovementDirection, Vector3.forward), -1.0f))
        {
            targetRotation = Quaternion.LookRotation(-forward);
        }
        else
        {
            Quaternion cameraToInputOffset = Quaternion.FromToRotation(Vector3.forward, localMovementDirection);
            targetRotation = Quaternion.LookRotation(cameraToInputOffset * forward);
        }

        // ĳ���Ͱ� �������� ��ǥ�ϴ� ����.
        Vector3 resultingForward = targetRotation * Vector3.forward;

        // ĳ������ ���� ȸ�� ���� �÷��̾��� ��ǥ ȸ�� ���� ���̸� ����Ѵ�.
        float angleCurrent = Mathf.Atan2(transform.forward.x, transform.forward.z) * Mathf.Rad2Deg;
        float targetAngle = Mathf.Atan2(resultingForward.x, resultingForward.z) * Mathf.Rad2Deg;
     
        angleDiff = Mathf.DeltaAngle(angleCurrent, targetAngle);
        this.targetRotation = targetRotation;
    }

    // �÷��̾��� �Է¿� ���� ȸ���� �� �� �ִ��� ��Ȳ�� �Ǵ��Ѵ�.
    private bool IsOrientationUpdate()
    {
        int currentHash = currentStateInfo.shortNameHash;
        int nextHash = nextStateInfo.shortNameHash;

        bool updateLocomotion = !isAnimatorTransitioning && currentHash == hashLocomotion || nextHash == hashLocomotion;
        bool updateAirborn = !isAnimatorTransitioning && currentHash == hashAirborn || nextHash == hashAirborn;
        bool updateLanding = !isAnimatorTransitioning && currentHash == hashLanding || nextHash == hashLanding;

        return updateLocomotion || updateAirborn || updateLanding || isCombo && !isAttack;
    }

    // �÷��̾ �ùٸ� �ִϸ����� ������ ��� target rotation�� ���� ȸ���Ѵ�.
    private void UpdateOrientation()
    {
        animator.SetFloat(hashAngleDeltaRad, angleDiff * Mathf.Deg2Rad);    // ��ǥġ���� ���� ȸ������ ����.

        Vector3 localInput = new Vector3(input.MoveInput.x, 0f, input.MoveInput.y);

        // �޸��� �ӵ� ��� ���鿡���� ȸ�� �ӵ�.
        // ü�� �� ȸ���ϴ� �ӵ� (���鿡������ �� ������ ����)
        float groundedSpeed = Mathf.Lerp(maxTurnSpeed, minTurnSpeed, forwardSpeed / desiredForwardSpeed);   
        float airbornSpeed = Vector3.Angle(transform.forward, localInput) * INVERSE_ONE_EIGHTY * AIRBORNE_TURN_SPEED_PROPORTION * groundedSpeed;
        float actualTurnSpeed = isGrounded ? groundedSpeed : airbornSpeed;

        // ���� �� ȸ�������� ��ǥ ȸ�������� ������� ���� �ӵ���ŭ ȸ���Ѵ�.
        targetRotation = Quaternion.RotateTowards(transform.rotation, targetRotation, actualTurnSpeed * Time.deltaTime);
        transform.rotation = targetRotation;
    }

    private void PlayAudio()
    {
        // �߼Ҹ� ����� ���.
        float footfallCurve = animator.GetFloat(hashFootFall);
        if (footfallCurve > 0.01f && !footstepAudio.isPlaying && footstepAudio.canPlay)
        {
            footstepAudio.isPlaying = true;
            footstepAudio.canPlay = false;
            footstepAudio.PlayRandomClip(walkingSurface, forwardSpeed < 4 ? 0 : 1);
        }
        else if(footstepAudio.isPlaying)
        {
            footstepAudio.isPlaying = false;
        }
        else if(footfallCurve < 0.01f && !footstepAudio.canPlay)
        {
            footstepAudio.canPlay = true;
        }

        // ���� ����� ���.
        if(isGrounded && !prevGrounded)
        {
            landingPlayer.PlayRandomClip(walkingSurface, forwardSpeed < 4 ? 0 : 1);
            emoteLandingPlayer.PlayRandomClip();
        }
        
        // ���� ����� ���.
        if(!isGrounded && prevGrounded && verticalSpeed > 0f)
            emoteJumpAudio.PlayRandomClip();

        // �ǰ� ����� ���.
        if (currentStateInfo.shortNameHash == hashHurt && prevCurrentStateInfo.shortNameHash != hashHurt)
            hurtAudio.PlayRandomClip();

        // ��� ����� ���.
        if (currentStateInfo.shortNameHash == hashDeath && prevCurrentStateInfo.shortNameHash != hashDeath)
            emoteDeathPlayer.PlayRandomClip();

        // �޺� ���� ���� ����� ���.
        if(currentStateInfo.shortNameHash == hashEllenCombo1 && prevCurrentStateInfo.shortNameHash != hashEllenCombo1 ||
            currentStateInfo.shortNameHash == hashEllenCombo2 && prevCurrentStateInfo.shortNameHash != hashEllenCombo2 ||
            currentStateInfo.shortNameHash == hashEllenCombo3 && prevCurrentStateInfo.shortNameHash != hashEllenCombo3 ||
            currentStateInfo.shortNameHash == hashEllenCombo4 && prevCurrentStateInfo.shortNameHash != hashEllenCombo4)
        {
            emoteAttackAudio.PlayRandomClip();
        }
    }
    private void TimeoutToIdle()
    {
        // ��� �Է��� �����Ǿ��°�?
        bool inputDetected = IsMoveInput || input.IsAttack || input.IsJump;

        // �÷��̾ ���鿡�� ������ ���� ��� Ÿ�̸Ӹ� ���� ���� �ð��� ����ϸ� Ʈ���� ON.
        if(isGrounded && !inputDetected)
        {
            idleTimer += Time.deltaTime;
            if(idleTimer >= idleTimeout)
            {
                idleTimer = 0f;
                animator.SetTrigger(hashTimeoutToIdle);
            }
        }
        else
        {
            // ĳ���Ͱ� �����̸� ����.
            idleTimer = 0f;
            animator.ResetTrigger(hashTimeoutToIdle);
        }

        // ���� �Է°��� �ִ����� ���� ���� �Ķ���ͷ� ����.
        animator.SetBool(hashInputDetected, inputDetected);
    }


   


    // �ִϸ����� �Ķ����.
    readonly int hashAirBornVerticalSpeed = Animator.StringToHash("AirbornVerticalSpeed");
    readonly int hashForwardSpeed = Animator.StringToHash("ForwardSpeed");
    readonly int hashAngleDeltaRad = Animator.StringToHash("AngleDeltaRad");
    readonly int hashTimeoutToIdle = Animator.StringToHash("TimeoutToIdle");
    readonly int hashGrounded = Animator.StringToHash("Grounded");
    readonly int hashInputDetected = Animator.StringToHash("InputDetected");
    readonly int hashMeleeAttack = Animator.StringToHash("MeleeAttack");
    readonly int hashHurt = Animator.StringToHash("Hurt");
    readonly int hashDeath = Animator.StringToHash("Death");
    readonly int hashRespawn = Animator.StringToHash("Respawn");
    readonly int hashHurtFromX = Animator.StringToHash("HurtFromX");
    readonly int hashHurtFromY = Animator.StringToHash("HurtFromY");
    readonly int hashStateTime = Animator.StringToHash("StateTime");
    readonly int hashFootFall = Animator.StringToHash("FootFall");

    // �ִϸ����� Ŭ��
    readonly int hashLocomotion = Animator.StringToHash("Locomotion");
    readonly int hashAirborn = Animator.StringToHash("Airborne");
    readonly int hashLanding = Animator.StringToHash("Landing");
    readonly int hashEllenCombo1 = Animator.StringToHash("EllenCombo1");
    readonly int hashEllenCombo2 = Animator.StringToHash("EllenCombo2");
    readonly int hashEllenCombo3 = Animator.StringToHash("EllenCombo3");
    readonly int hashEllenCombo4 = Animator.StringToHash("EllenCombo4");
    readonly int hashEllenDeath = Animator.StringToHash("EllenDeath");

    // �ױ�
    readonly int hashBlockInput = Animator.StringToHash("BlockInput");

    // Mathf.Approximately : �ٻ�ġ ��
    // => �������� ���꿡 ���� �Ǽ� ������ ���� �߻��ϴ� ���� ������ �����ؼ� ���Ѵ�.
    protected bool IsMoveInput => !Mathf.Approximately(input.MoveInput.sqrMagnitude, 0f);
}
