using Gamekit3D;
using UnityEditor.VersionControl;
using UnityEngine;
using Gamekit3D.Message;

// 기본적인 순서들이 다 끝난 이후에 불리는 것으로 추정됨.
[DefaultExecutionOrder(100)]
public class CChomperHehaviour : MonoBehaviour, IMessageReceiver
{
    public enum AUDIO
    {
        FRONT_STEP,
        BACK_STEP,
        HIT,
        GRUNT,
        DEATH,
        SPOTTED,
        ATTACK
    }

    public TargetDistributor.TargetFollower followerData { get; protected set; }    // 추적 대상 (세부 위치 제어)
    public CPlayerController target { get; protected set; }                         // 추적 대상.
    public CEnemyController controller { get; protected set; }                       // 적 컨트롤러.
    public Vector3 originalPosition { get; protected set; }

    [System.NonSerialized]
    public float attackDistance = 3;            // 공격 범위.

    // 기본 변수.
    public CMeleeWeapon meleeWeapon;            // 근접 무기 (이빨)
    public CPlayerScanner playerScanner;        // 전방위 스캐너.
    public float timeToLostTarget;               // Lost time.

    [Header("Audio")]
    public RandomAudioPlayer attackAudio;       // 공격 사운드.
    public RandomAudioPlayer frontStepAudio;    // 앞으로 이동 사운드.
    public RandomAudioPlayer backStepAudio;     // 뒤로 이동 사운드.
    public RandomAudioPlayer hitAudio;          // 피격 사운드.
    public RandomAudioPlayer gruntAudio;        // 피격 사운드.
    public RandomAudioPlayer deathAudio;        // 죽음 사운드.
    public RandomAudioPlayer spottedAudio;      // 발견 사운드.

    protected float timeSinceLostTarget;


    private void OnEnable()
    {
        originalPosition = transform.position;

        controller = GetComponentInChildren<CEnemyController>();
        meleeWeapon.SetOwner(gameObject);
        controller.Anim.Play(hashIdleState, 0, Random.value);

        // State machine behaviour 초기화.
        CLinkedSMB<CChomperHehaviour>.Initialise(controller.Anim, this);
    }
    private void OnDisable()
    {
        // 추적 대상이 있다면 추적 대상을 해제합니다.
        if (followerData != null)
            followerData.distributor.UnregisterFollower(followerData);
    }

    private void FixedUpdate()
    {
        Vector3 tobase = originalPosition - transform.position;
        tobase.y = 0;

        controller.Anim.SetBool(hashNearBase, tobase.sqrMagnitude < 0.1f * 0.1f);
        controller.Anim.SetBool(hashGrounded, controller.IsGrounded);
    }


    // 공겨겨 대상을 찾는다.
    public void FindTarget()
    {
        CPlayerController seeTarget = playerScanner.Detect(transform);
        if(target == null)
        {
            // 방금 플레이어를 처음 봤습니다. 플레이어 주변의 빈 곳을 선택하여 타겟팅하세요.
            if (seeTarget != null)
            {
                controller.Anim.SetTrigger(hashSpotted);
                target = seeTarget;
                TargetDistributor distributor = seeTarget.GetComponent<TargetDistributor>();
                if (distributor != null)
                    followerData = distributor.RegisterNewFollower();
            }
        }
        else
        {
            // 목표를 잃었습니다. 하지만 촘퍼는 특별한 행동을 합니다.
            // 탐지 범위를 넘어 이동하고 일정 시간 동안 플레이어를 보지 못한 경우에만 플레이어의 냄새를 잃습니다.
            // 그들이 탐지 각도를 벗어나면 그렇지 않습니다. 따라서 우리는 타겟을 제거하기 전에 이것이 사실인지 확인합니다.
            if(seeTarget == null)
            {
                // Lost 타임이 흐른 경우.
                timeSinceLostTarget += Time.deltaTime;
                if(timeSinceLostTarget >= timeToLostTarget)
                {
                    // 플레이어가 시스템상으로 감지 범위보다 더 멀리 갔을 경우에만 타겟 리셋.
                    Vector3 toTarget = target.transform.position - transform.position;      
                    if(toTarget.sqrMagnitude > playerScanner.detectionRadius * playerScanner.detectionRadius)
                    {
                        if(followerData != null)
                            followerData.distributor.UnregisterFollower(followerData);
                        target = null;
                    }
                }
            }
            else
            {
                // 새로운 타겟을 감지했을 경우.
                if(target != seeTarget)
                {
                    if (followerData != null)
                        followerData.distributor.UnregisterFollower(followerData);
                    target = seeTarget;
                    TargetDistributor distributor = seeTarget.GetComponent<TargetDistributor>();
                    if(distributor != null)
                        followerData = distributor.RegisterNewFollower();
                }

                timeSinceLostTarget = 0f;
            }
        }
    }
    // 추격 시작.
    public void StartChase()
    {
        if(followerData != null)
        {
            followerData.requireSlot = true;
            RequestTargetPosition();
        }
        controller.Anim.SetBool(hashInPursuit, true);
    }
    // 추격 중지.
    public void StopChase()
    {
        if(followerData != null)
            followerData.requireSlot = false;
        controller.Anim.SetBool(hashInPursuit, false);
    }
    public void RequestTargetPosition()
    {
        // 실제 타겟의 위치에서 내 근접 공격 범위 만큼 뒤로 빠진 위치를 계산한다.
        Vector3 fromTarget = transform.position - target.transform.position;
        fromTarget.y = 0;
        followerData.requiredPoint = target.transform.position + fromTarget.normalized * attackDistance * 0.9f;
    }

    public void WalkBackToBase()
    {
        if(followerData != null)
            followerData.distributor.UnregisterFollower(followerData);
        target = null;
        StopChase();
        controller.SetTarget(originalPosition);         // base 위치로 이동.
        controller.SwitchFollowNavmeshAgent(true);      // navmeshAgent를 따라가도록 설정.
    }
    public void TriggerAttack()
    {
        controller.Anim.SetTrigger(hashAttack);
    }

    public void AttackBegin()
    {
        meleeWeapon.BeginAttack(false);
    }

    public void AttackEnd()
    {
        meleeWeapon.EndAttack();
    }



    public void PlayStep(int frontFoot)
    {
        Debug.Log($"PlayStep:{frontFoot}");
        if (frontFoot == 1)
            PlayAudio(AUDIO.FRONT_STEP);
        else
            PlayAudio(AUDIO.BACK_STEP);
    }
    public void Grunt()
    {
        PlayAudio(AUDIO.GRUNT);
    }
    public void Spotted()
    {
        PlayAudio(AUDIO.SPOTTED);
    }

    public void PlayAudio(AUDIO audio)
    {
        RandomAudioPlayer audioPlayer = audio switch
        {
            AUDIO.DEATH => deathAudio,
            AUDIO.FRONT_STEP => frontStepAudio,
            AUDIO.BACK_STEP => backStepAudio,
            AUDIO.HIT => hitAudio,
            AUDIO.GRUNT => gruntAudio,
            AUDIO.SPOTTED => spottedAudio,
            AUDIO.ATTACK => attackAudio,
            _ => null
        };

        if (audioPlayer != null)
            audioPlayer.PlayRandomClip();
    }

    public void OnReceiveMessage(MessageType type, object sender, object msg)
    {
       
    }

#if UNITY_EDITOR
    private void OnDrawGizmosSelected()
    {
        playerScanner.DrawGismosEditor(transform);
    }
#endif



    // 애니메이터 해쉬 값 캐싱.
    public static readonly int hashInPursuit = Animator.StringToHash("InPursuit");
    public static readonly int hashAttack = Animator.StringToHash("Attack");
    public static readonly int hashHit = Animator.StringToHash("Hit");
    public static readonly int hashVerticalDot = Animator.StringToHash("VerticalHitDot");
    public static readonly int hashHorizontalDot = Animator.StringToHash("HorizontalHitDot");
    public static readonly int hashThrown = Animator.StringToHash("Thrown");
    public static readonly int hashGrounded = Animator.StringToHash("Grounded");
    public static readonly int hashVerticalVelocity = Animator.StringToHash("VerticalVelocity");
    public static readonly int hashSpotted = Animator.StringToHash("Spotted");
    public static readonly int hashNearBase = Animator.StringToHash("NearBase");
    public static readonly int hashIdleState = Animator.StringToHash("ChomperIdle");
}
