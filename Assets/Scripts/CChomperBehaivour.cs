using Gamekit3D;
using UnityEngine;


public class CChomperHegaviour : MonoBehaviour
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

    public TargetDistributor.TargetFollower followerData { get; protected set; }
    public CPlayerController target { get; protected set; }
    public CEnemyController contoller { get; protected set; }
    public Vector3 originalPosition { get; protected set; }

    [System.NonSerialized]
    public float attackDistance = 3;

    // �⺻ ����.
    public CMeleeWeapon meleeWeapon;
    public CPlayerScanner playerScanner;
    public float timeToStopChase;

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

        contoller = GetComponentInChildren<CEnemyController>();
        meleeWeapon.SetOwner(gameObject);
        contoller.Anim.Play(hashIdleState, 0, Random.value);
    }
    private void OnDisable()
    {
        
    }

    private void PlayStep(int frontFoot)
    {
        if (frontFoot == 1)
            PlayAudio(AUDIO.FRONT_STEP);
        else
            PlayAudio(AUDIO.BACK_STEP);
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


#if UNITY_EDITOR
    private void OnDrawGizmosSelected()
    {
        playerScanner.DrawGismosEditor(transform);
    }
#endif
}
