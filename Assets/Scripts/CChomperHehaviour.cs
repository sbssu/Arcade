using Gamekit3D;
using UnityEditor.VersionControl;
using UnityEngine;
using Gamekit3D.Message;

// �⺻���� �������� �� ���� ���Ŀ� �Ҹ��� ������ ������.
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

    public TargetDistributor.TargetFollower followerData { get; protected set; }    // ���� ��� (���� ��ġ ����)
    public CPlayerController target { get; protected set; }                         // ���� ���.
    public CEnemyController controller { get; protected set; }                       // �� ��Ʈ�ѷ�.
    public Vector3 originalPosition { get; protected set; }

    [System.NonSerialized]
    public float attackDistance = 3;            // ���� ����.

    // �⺻ ����.
    public CMeleeWeapon meleeWeapon;            // ���� ���� (�̻�)
    public CPlayerScanner playerScanner;        // ������ ��ĳ��.
    public float timeToLostTarget;               // Lost time.

    [Header("Audio")]
    public RandomAudioPlayer attackAudio;       // ���� ����.
    public RandomAudioPlayer frontStepAudio;    // ������ �̵� ����.
    public RandomAudioPlayer backStepAudio;     // �ڷ� �̵� ����.
    public RandomAudioPlayer hitAudio;          // �ǰ� ����.
    public RandomAudioPlayer gruntAudio;        // �ǰ� ����.
    public RandomAudioPlayer deathAudio;        // ���� ����.
    public RandomAudioPlayer spottedAudio;      // �߰� ����.

    protected float timeSinceLostTarget;


    private void OnEnable()
    {
        originalPosition = transform.position;

        controller = GetComponentInChildren<CEnemyController>();
        meleeWeapon.SetOwner(gameObject);
        controller.Anim.Play(hashIdleState, 0, Random.value);

        // State machine behaviour �ʱ�ȭ.
        CLinkedSMB<CChomperHehaviour>.Initialise(controller.Anim, this);
    }
    private void OnDisable()
    {
        // ���� ����� �ִٸ� ���� ����� �����մϴ�.
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


    // ���ܰ� ����� ã�´�.
    public void FindTarget()
    {
        CPlayerController seeTarget = playerScanner.Detect(transform);
        if(target == null)
        {
            // ��� �÷��̾ ó�� �ý��ϴ�. �÷��̾� �ֺ��� �� ���� �����Ͽ� Ÿ�����ϼ���.
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
            // ��ǥ�� �Ҿ����ϴ�. ������ ���۴� Ư���� �ൿ�� �մϴ�.
            // Ž�� ������ �Ѿ� �̵��ϰ� ���� �ð� ���� �÷��̾ ���� ���� ��쿡�� �÷��̾��� ������ �ҽ��ϴ�.
            // �׵��� Ž�� ������ ����� �׷��� �ʽ��ϴ�. ���� �츮�� Ÿ���� �����ϱ� ���� �̰��� ������� Ȯ���մϴ�.
            if(seeTarget == null)
            {
                // Lost Ÿ���� �帥 ���.
                timeSinceLostTarget += Time.deltaTime;
                if(timeSinceLostTarget >= timeToLostTarget)
                {
                    // �÷��̾ �ý��ۻ����� ���� �������� �� �ָ� ���� ��쿡�� Ÿ�� ����.
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
                // ���ο� Ÿ���� �������� ���.
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
    // �߰� ����.
    public void StartChase()
    {
        if(followerData != null)
        {
            followerData.requireSlot = true;
            RequestTargetPosition();
        }
        controller.Anim.SetBool(hashInPursuit, true);
    }
    // �߰� ����.
    public void StopChase()
    {
        if(followerData != null)
            followerData.requireSlot = false;
        controller.Anim.SetBool(hashInPursuit, false);
    }
    public void RequestTargetPosition()
    {
        // ���� Ÿ���� ��ġ���� �� ���� ���� ���� ��ŭ �ڷ� ���� ��ġ�� ����Ѵ�.
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
        controller.SetTarget(originalPosition);         // base ��ġ�� �̵�.
        controller.SwitchFollowNavmeshAgent(true);      // navmeshAgent�� ���󰡵��� ����.
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



    // �ִϸ����� �ؽ� �� ĳ��.
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
