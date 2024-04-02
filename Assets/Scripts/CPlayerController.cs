using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(CharacterController))]
[RequireComponent(typeof(Animator))]
[RequireComponent(typeof(CPlayerInput))]
public class CPlayerController : Singleton<CPlayerController>
{
    public CCameraSettings cameraSettings;  // 시네머신 카메라.
    public float maxForawrdSpeed = 8f;      // 최대 이동 속도.
    public float gravity = 20f;             // 중력값 (공중에서의 하강 속도)
    public float jumpSpeed = 10f;           // 점프 힘
    public float minTurnSpeed = 400f;       // 달리는 도중 회전하는 힘
    public float maxTurnSpeed = 1200f;      // 정지한 상태에서 회전하는 힘
    public float idleTimeout = 5f;          // Idle(유휴)상태 전환 대기 시간
    public bool canAttack;                  // 공격 가능 여부

    public CMeleeWeapon meleeWeapon;                // 근접 무기.
    public CRandomAudioPlayer footstepAudio;        // 걷기 오디오
    public CRandomAudioPlayer hurtAudio;            // 피격 오디오
    public CRandomAudioPlayer landingPlayer;        // 착지 오디오
    public CRandomAudioPlayer emoteLandingPlayer;   // (음성) 착지 오디오
    public CRandomAudioPlayer emoteDeathPlayer;     // (음성) 사망 오디오
    public CRandomAudioPlayer emoteAttackAudio;     // (음성) 공격 오디오
    public CRandomAudioPlayer emoteJumpAudio;       // (음성) 점프 오디오

    // animation.
    protected Animator animator;
    protected AnimatorStateInfo currentStateInfo;       // 현재 애니메이션 정보
    protected AnimatorStateInfo nextStateInfo;          // 다음 애니메이션 정보
    protected bool isAnimatorTransitioning;             // 애니메이션이 전환중인가?

    protected AnimatorStateInfo prevCurrentStateInfo;   // (이전 프레임) 현재 애니메이션 정보
    protected AnimatorStateInfo prevNextStateInfo;      // (이전 프레임) 다음 애니메이션 정보
    protected bool prevAnimatorTransitioning;           // (이전 프레임) 애니메이션 전환중?

    // user control.
    protected CharacterController controller;   // 캐릭터 컨트롤러 컴포넌트
    protected CPlayerInput input;               // 플레이어 입력 값.
    protected bool isGrounded;                  // 지면에 서 있는가?
    protected bool prevGrounded;                // (이전 프레임) 지면에 서 있는가?
    protected bool isReadyToJump;               // 점프 가능 상태인가?
    protected bool isAttack;                    // 캐릭터가 근접 공격 중이다.
    protected bool isCombo;                     // 캐릭터가 근접 공격 콤보 중이다.

    // control parametor.
    protected float desiredForwardSpeed;        // 원하는 최종 속도
    protected float forwardSpeed;               // 현재 이동 속도
    protected float verticalSpeed;              // 하강 속도
    protected float idleTimer;                  // 유휴상태 체크 시간.

    protected Material walkingSurface;          // 걷고 있는 땅의 표면.

    // rotation.
    protected Quaternion targetRotation;        // 캐릭터가 목표하는 회전 값.
    protected float angleDiff;                  // 목표 회전 값과 현재의 차이 각도.


    // Ellen이 정확히 움직이기 위한 상수 값.
    const float AIRBORNE_TURN_SPEED_PROPORTION = 5.4f;      // 공중 회전 속도 비율
    const float STICKING_GRAVITY_PROPERTION = 0.3f;         // 지면에 닿기 위한 중력 비율
    const float GROUNDED_RAY_DISTANCE = 1f;                 // 지면 체크 레이의 길이
    const float JUMP_ABORT_SPEED = 10f;                     // 점프 중단 속도.
    const float MIN_ENEMY_DOT_COEFF = 0.2f;                 // ???
    const float INVERSE_ONE_EIGHTY = 1f / 180f;             // 역 회전 값.

    const float GROUND_ACCELERATION = 20;                   // 지면에서의 가속도
    const float GROUND_DECELERATION = 25;                   // 지면에서의 감속도


    private void Awake()
    {
        input = GetComponent<CPlayerInput>();
        animator = GetComponent<Animator>();
        controller = GetComponent<CharacterController>();

        meleeWeapon.SetOwner(gameObject);
    }

    private void FixedUpdate()
    {
        CacheAnimatorState();                       // 이전 애니메이터 상태 캐싱(=저장)
        UpdateInputBlocking();                      // 트리거에 따른 플레이어 입력 상태 막기
        EquipMeleeWeapon(IsWeaponEquiped());        // 무기 장비 여부 체크

        // 유휴 대기 시간 갱신
        animator.SetFloat(hashStateTime, Mathf.Repeat(currentStateInfo.normalizedTime, 1f));
        animator.ResetTrigger(hashMeleeAttack);

        // 공격 버튼을 눌렀고 공격 가능한 상태면 트리거 체크.
        if (input.IsAttack && canAttack)
            animator.SetTrigger(hashMeleeAttack);

        CalculateForawdMovement();                      // 전진(=움직임) 속도 계산
        CalculateVerticalMovement();                    // 수직(=중력, 점프) 속도 계산

        // 캐릭터 회전
        SetTargetRotation();                            // 입력값에 따른 목표 회전값 계산.
        if (IsOrientationUpdate() && IsMoveInput)       // 회전할 수 있는 상태인지 체크.
            UpdateOrientation();                        // 목표치를 향해 회전.

        PlayAudio();
        TimeoutToIdle();

        prevGrounded = isGrounded;                      // 현재 상태값을 이전으로 저장.
    }


    // 애니메이터 상태가 캐싱된 후 호출되어 지팡이가 활성화되어야 하는지 여부를 결정합니다.
    private bool IsWeaponEquiped()
    {
        bool equipped = currentStateInfo.shortNameHash == hashEllenCombo1 || nextStateInfo.shortNameHash == hashEllenCombo1;
        equipped |= currentStateInfo.shortNameHash == hashEllenCombo2 || nextStateInfo.shortNameHash == hashEllenCombo2;
        equipped |= currentStateInfo.shortNameHash == hashEllenCombo3 || nextStateInfo.shortNameHash == hashEllenCombo3;
        equipped |= currentStateInfo.shortNameHash == hashEllenCombo4 || nextStateInfo.shortNameHash == hashEllenCombo4;

        return equipped;
    }

    // 무기를 장비해야하는지를 체크한다.
    private void EquipMeleeWeapon(bool isEquip)
    {
        meleeWeapon.gameObject.SetActive(isEquip);
        isAttack = false;
        isCombo = isEquip;

        // 무기를 장비하지 않으면 MeleeAttack 트리거를 초기화한다.
        if (!isEquip)
            animator.ResetTrigger(hashMeleeAttack);
    }

    // (root모션 한정) 애니메이션에 의해 캐릭터가 움직이면 호출되는 이벤트 함수.
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
                // (예외처리) 정말 간혹 지면에 레이가 충돌하지 않는 경우가 발생한다.
                // 이때 기본 값으로 처리한다.
                movement = animator.deltaPosition;
                walkingSurface = null;
            }
        }
        else
        {
            // 체공중에는 현재 속도를 유지한다.
            movement = forwardSpeed * transform.forward * Time.deltaTime;
        }

        movement += verticalSpeed * Vector3.up * Time.deltaTime;    // 현재 수직 속력 값을 더한다.

        // animator의 deltaPosition과 deltaRotation은 root모션에 의한 이동,회전 값을 의미한다.

        controller.transform.rotation *= animator.deltaRotation;            // root 오브제트 회전
        controller.Move(movement);                                          // root 오브제트 이동
        isGrounded = controller.isGrounded;                                 // 캐릭터 컨트롤러에 의한 지면 체크.
        if (!isGrounded)
            animator.SetFloat(hashAirBornVerticalSpeed, verticalSpeed);     // 체공 수직 속력 파라미터 값 대입
        animator.SetBool(hashGrounded, isGrounded);                         // 지면 체크 파라미터 값 대입.
    }

        
    // ============================ 공격 관련 함수 ============================
    
    // 애니메이션 이벤트로 호출되어 Ellen이 지팡이를 휘두를 때 호출된다.
    public void MeleeAttackStart(int throwing = 0)
    {
        meleeWeapon.BeginAttack(throwing != 0);
        isAttack = true;
    }

    // 애니메이션 이벤트로 호출되어 Ellen이 지팡이 휘두르기를 끝낼 때 호출된다.
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

        // 현재, 다음 애니메이션 상태 그리고 전환중임을 체크한다.
        currentStateInfo = animator.GetCurrentAnimatorStateInfo(0);
        nextStateInfo = animator.GetNextAnimatorStateInfo(0);
        isAnimatorTransitioning = animator.IsInTransition(0);           // 애니메이션이 전환중인가?
    }
    private void UpdateInputBlocking()
    {
        // 현재 혹은 다음 애니메이션의 hash가 block input일 경우 입력을 제한한다.
        bool isBlocked = currentStateInfo.tagHash == hashBlockInput && !isAnimatorTransitioning;
        isBlocked |= nextStateInfo.tagHash == hashBlockInput;
        input.isLockControl = isBlocked;
    }
    private void CalculateForawdMovement()
    {
        Vector2 moveInput = input.MoveInput;
        if (moveInput.sqrMagnitude > 1f)        // 해당 벡터의 크기가 1보다 크다면...
            moveInput.Normalize();              // 크기가 1인 단위 벡터로 정규화.

        // 플레이어가 도달해야하는 속도.
        desiredForwardSpeed = moveInput.magnitude * maxForawrdSpeed;

        // 현재 입력값에 따라 감,가속력을 계산한다.
        float acclerataion = IsMoveInput ? GROUND_ACCELERATION : GROUND_DECELERATION;

        // 현재 속도가 (등속으로) 도달해야하는 속도까지 감,가속한다.
        forwardSpeed = Mathf.MoveTowards(forwardSpeed, desiredForwardSpeed, acclerataion);
        animator.SetFloat(hashForwardSpeed, forwardSpeed);
    }
    private void CalculateVerticalMovement()
    {
        if (!input.IsJump && isGrounded)
            isReadyToJump = true;
        
        
        if(isGrounded)
        {
            // Ellen이 지면에 "닿기"위해 적용되는 작은 중력 값.
            verticalSpeed = -gravity * STICKING_GRAVITY_PROPERTION;
            if(input.IsJump && isReadyToJump && !isCombo)
            {
                // 캐릭터가 점프키를 눌렀을 때. (콤보 중에는 뛸 수 없다)
                verticalSpeed = jumpSpeed;
                isGrounded = false;
                isReadyToJump = false;
            }
        }
        else
        {
            // 캐릭터가 공중으로 올라가는 중이며 점프 버튼을 누르고 있지 않을 경우
            //  => 사용자가 높게 뛰지 않으려고 한다.
            if(!input.IsJump && verticalSpeed > 0f)
            {
                // 올라가는 속도를 중력 +@로 더해 빠르게 멈추도록 한다.
                verticalSpeed -= JUMP_ABORT_SPEED * Time.deltaTime;
            }

            // 현재 속력이 0에 거의 근사할 경우 0으로 변경한다.
            if (Mathf.Approximately(verticalSpeed, 0))
                verticalSpeed = 0f;

            // 캐릭터가 체공중이면 중력 값을 적용한다.
            verticalSpeed -= gravity * Time.deltaTime;
        }
    }
    private void SetTargetRotation()
    {
        Vector2 moveInput = input.MoveInput;
        Vector3 localMovementDirection = new Vector3(moveInput.x, 0f, moveInput.y).normalized;

        // 플레이어의 정면을 향하는 벡터.
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

        // 캐릭터가 정면으로 목표하는 방향.
        Vector3 resultingForward = targetRotation * Vector3.forward;

        // 캐릭터의 현재 회전 값과 플레이어의 목표 회전 값의 차이를 계산한다.
        float angleCurrent = Mathf.Atan2(transform.forward.x, transform.forward.z) * Mathf.Rad2Deg;
        float targetAngle = Mathf.Atan2(resultingForward.x, resultingForward.z) * Mathf.Rad2Deg;
     
        angleDiff = Mathf.DeltaAngle(angleCurrent, targetAngle);
        this.targetRotation = targetRotation;
    }

    // 플레이어의 입력에 따른 회전을 할 수 있는지 상황을 판단한다.
    private bool IsOrientationUpdate()
    {
        int currentHash = currentStateInfo.shortNameHash;
        int nextHash = nextStateInfo.shortNameHash;

        bool updateLocomotion = !isAnimatorTransitioning && currentHash == hashLocomotion || nextHash == hashLocomotion;
        bool updateAirborn = !isAnimatorTransitioning && currentHash == hashAirborn || nextHash == hashAirborn;
        bool updateLanding = !isAnimatorTransitioning && currentHash == hashLanding || nextHash == hashLanding;

        return updateLocomotion || updateAirborn || updateLanding || isCombo && !isAttack;
    }

    // 플레이어가 올바른 애니메이터 상태인 경우 target rotation을 향해 회전한다.
    private void UpdateOrientation()
    {
        animator.SetFloat(hashAngleDeltaRad, angleDiff * Mathf.Deg2Rad);    // 목표치까지 남은 회전량을 전달.

        Vector3 localInput = new Vector3(input.MoveInput.x, 0f, input.MoveInput.y);

        // 달리는 속도 대비 지면에서의 회전 속도.
        // 체공 중 회전하는 속도 (지면에서보다 더 빠르게 돈다)
        float groundedSpeed = Mathf.Lerp(maxTurnSpeed, minTurnSpeed, forwardSpeed / desiredForwardSpeed);   
        float airbornSpeed = Vector3.Angle(transform.forward, localInput) * INVERSE_ONE_EIGHTY * AIRBORNE_TURN_SPEED_PROPORTION * groundedSpeed;
        float actualTurnSpeed = isGrounded ? groundedSpeed : airbornSpeed;

        // 현재 내 회전값에서 목표 회전값까지 등속으로 실제 속도만큼 회전한다.
        targetRotation = Quaternion.RotateTowards(transform.rotation, targetRotation, actualTurnSpeed * Time.deltaTime);
        transform.rotation = targetRotation;
    }

    private void PlayAudio()
    {
        // 발소리 오디오 재생.
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

        // 착지 오디오 재생.
        if(isGrounded && !prevGrounded)
        {
            landingPlayer.PlayRandomClip(walkingSurface, forwardSpeed < 4 ? 0 : 1);
            emoteLandingPlayer.PlayRandomClip();
        }
        
        // 점프 오디오 재생.
        if(!isGrounded && prevGrounded && verticalSpeed > 0f)
            emoteJumpAudio.PlayRandomClip();

        // 피격 오디오 재생.
        if (currentStateInfo.shortNameHash == hashHurt && prevCurrentStateInfo.shortNameHash != hashHurt)
            hurtAudio.PlayRandomClip();

        // 사망 오디오 재생.
        if (currentStateInfo.shortNameHash == hashDeath && prevCurrentStateInfo.shortNameHash != hashDeath)
            emoteDeathPlayer.PlayRandomClip();

        // 콤보 어택 공격 오디오 재생.
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
        // 어떠한 입력이 감지되었는가?
        bool inputDetected = IsMoveInput || input.IsAttack || input.IsJump;

        // 플레이어가 지면에서 가만히 있을 경우 타이머를 돌려 일정 시간이 경과하면 트리거 ON.
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
            // 캐릭터가 움직이면 리셋.
            idleTimer = 0f;
            animator.ResetTrigger(hashTimeoutToIdle);
        }

        // 현재 입력값이 있는지에 대한 값을 파라미터로 전달.
        animator.SetBool(hashInputDetected, inputDetected);
    }


   


    // 애니메이터 파라미터.
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

    // 애니메이터 클립
    readonly int hashLocomotion = Animator.StringToHash("Locomotion");
    readonly int hashAirborn = Animator.StringToHash("Airborne");
    readonly int hashLanding = Animator.StringToHash("Landing");
    readonly int hashEllenCombo1 = Animator.StringToHash("EllenCombo1");
    readonly int hashEllenCombo2 = Animator.StringToHash("EllenCombo2");
    readonly int hashEllenCombo3 = Animator.StringToHash("EllenCombo3");
    readonly int hashEllenCombo4 = Animator.StringToHash("EllenCombo4");
    readonly int hashEllenDeath = Animator.StringToHash("EllenDeath");

    // 테그
    readonly int hashBlockInput = Animator.StringToHash("BlockInput");

    // Mathf.Approximately : 근사치 비교
    // => 물리적인 연산에 의해 실수 형태의 값이 발생하는 작은 오차를 감안해서 비교한다.
    protected bool IsMoveInput => !Mathf.Approximately(input.MoveInput.sqrMagnitude, 0f);
}
