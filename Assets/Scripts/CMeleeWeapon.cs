using Gamekit3D;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CMeleeWeapon : MonoBehaviour
{
    [System.Serializable]
    public class AttackPoint
    {
        public float radius;
        public Vector3 offset;
        public Transform attackRoot;

#if UNITY_EDITOR
        [NonSerialized] public List<Vector3> previousPositions;
#endif

        public AttackPoint()
        {
            radius = 0f;
            offset = Vector3.zero;
            attackRoot = null;
#if UNITY_EDITOR
            // editor only as it's only used in editor to display the path of the attack that is used by the raycast
            //  = 에디터에서만 사용되며 레이캐스트에 사용되는 공격 경로를 표시하는 데 사용됩니다.
            previousPositions = new List<Vector3>();
#endif
        }
    }

    public int damage = 1;                          // 기본 데미지.
    public ParticleSystem hitParticlePrefab;        // 타격 파티클 프리팹.
    public LayerMask targetLayers;                  // 타격 가능한 레이어.

    public AttackPoint[] attackPoints;              // 공격 지점.
    public CTimeEffect[] timeEttects;               // 애니메이션 이펙트.

    [Header("Audio")]
    public CRandomAudioPlayer hitAudio;              // 타격 사운드.
    public CRandomAudioPlayer attackAudio;           // 공격 사운드.

    // ================================== 멤버 변수 ==================================


    protected GameObject owner;                     // 소유자.
    protected Vector3[] previousAttackPoints;       // attackPoints의 이전 위치.
    protected Vector3 direction;                    // 방향.

    protected bool isThrowingHit = false;           // 투척 공격 여부.
    protected bool inAttack = false;                // 공격 중 여부.    

    private const int MAX_PARTICLE_COUNT = 10;                                              // 파티클 개수.
    protected int currentParticle = 0;                                                      // 현재 파티클.
    protected ParticleSystem[] particlesPool = new ParticleSystem[MAX_PARTICLE_COUNT];      // 파티클 풀.

    protected static RaycastHit[] raycastHitCache = new RaycastHit[32];                     // 레이캐스트 히트 캐시.
    protected static Collider[] colliderCache = new Collider[32];                           // 콜라이더 캐시.

    public bool ThrowingHit
    {
        get { return isThrowingHit; }
        set { isThrowingHit = value; }
    }

    // ================================== 멤버 함수 ==================================

    private void Awake()
    {
        // 파티클 풀 초기화.
        if (hitParticlePrefab != null)
        {
            for (int i = 0; i < MAX_PARTICLE_COUNT; i++)
            {
                particlesPool[i] = Instantiate(hitParticlePrefab);  // 파티클 생성.
                particlesPool[i].Stop();                            // 파티클 정지.
            }
        }
    }

    public void SetOwner(GameObject owner)
    {
        this.owner = owner;
    }

    // 공격 시작 함수 (isThrowing : 넉백 공격 여부)
    public void BeginAttack(bool isThrowing = false)
    {
        attackAudio?.PlayRandomClip();
        isThrowingHit = isThrowing;
        inAttack = true;

        previousAttackPoints = new Vector3[attackPoints.Length];
        for(int i = 0; i<previousAttackPoints.Length; i++)
        {
            // TransformDirection : 로컬 좌표계를 월드 좌표계로 변환.
            Vector3 worldPos = attackPoints[i].attackRoot.position +
            attackPoints[i].attackRoot.TransformDirection(attackPoints[i].offset);
            previousAttackPoints[i] = worldPos;

#if UNITY_EDITOR
            attackPoints[i].previousPositions.Add(previousAttackPoints[i]);
#endif
        }
    }
    public void EndAttack()
    {
        inAttack = false;

#if UNITY_EDITOR
        for (int i = 0; i < attackPoints.Length; i++)
            attackPoints[i].previousPositions.Clear();
#endif
    }

    private void FixedUpdate()
    {
        if (!inAttack)
            return;

        for(int i = 0; i < attackPoints.Length; i++)
        {
            AttackPoint point = attackPoints[i];
            Vector3 worldPos = point.attackRoot.position + point.attackRoot.TransformDirection(point.offset);
            Vector3 vector = worldPos - previousAttackPoints[i];

            // A zero vector for the sphere cast don't yield any result, even if a collider overlap the "sphere" created by radius. 
            // so we set a very tiny microscopic forward cast to be sure it will catch anything overlaping that "stationary" sphere cast

            // 구체 캐스트에 대한 제로 벡터는 결과를 생성하지 않습니다. 심지어 콜라이더가 반지름으로 생성된 "구체"와 겹치더라도.
            // 따라서 우리는 그 "정지된" 구체 캐스트를 겹치는 모든 것을 잡을 것이라는 것을 확실하게 하기 위해 매우 작은 미세한 전방 캐스트를 설정합니다.
            if (vector.magnitude < 0.001f)
                vector = Vector3.forward * 0.0001f;

            Ray ray = new Ray(worldPos, vector.normalized);

            // SphereCastNonAlloc : 구체 캐스트를 수행하고 충돌한 모든 콜라이더를 배열에 저장.
            // raycastHitCache : 충돌한 모든 콜라이더를 저장할 배열.
            // vector.magnitude : 구체 캐스트의 거리 (벡터의 스칼라 값)
            // targetLayers : 타격 가능한 레이어.
            // QueryTriggerInteraction.Ignore : 트리거 콜라이더를 무시하고 충돌을 검출.
            int contacts = Physics.SphereCastNonAlloc(ray, point.radius, raycastHitCache, vector.magnitude, targetLayers, QueryTriggerInteraction.Ignore);
            foreach (var hit in raycastHitCache)
            {
                Collider collider = hit.collider;
                if (collider != null)
                    CheckDamage(collider, point);
            }
        }
    }
    private bool CheckDamage(Collider collider, AttackPoint point)
    {
        CDamageable damageable = collider.GetComponent<CDamageable>();
        if (damageable == null)
            return false;

        // ignore self harm, but do not end the attack (we don't "bounce" off ourselves)
        // 셀프 공격을 무시하지만 공격을 종료하지 않습니다 (우리는 자신에게 "반발"하지 않습니다).
        if (damageable.gameObject == owner)
            return true;

        // 피격 대상의 Renderer를 검색해 Material에 해당하는 공격 오디오 재생.
        if (hitAudio != null)
        {
            Renderer renderer = collider.GetComponent<Renderer>();
            if (!renderer)
                renderer = collider.GetComponentInChildren<Renderer>();

            if (renderer)
                hitAudio.PlayRandomClip(renderer.sharedMaterial);
            else
                hitAudio.PlayRandomClip();
        }

        CDamageable.CDamageMessage message;
        message.amount = damage;
        message.damager = this;
        message.direction = direction.normalized;
        message.damageSource = owner.transform.position;
        message.throwing = isThrowingHit;
        message.stopCamera = false;

        damageable.ApplyDamage(message);

        // 공격 이펙트 발생
        if(!hitParticlePrefab)
        {
            particlesPool[currentParticle].transform.position = point.attackRoot.transform.position;
            particlesPool[currentParticle].time = 0;
            particlesPool[currentParticle].Play();
            currentParticle = (int)Mathf.Repeat(currentParticle + 1, MAX_PARTICLE_COUNT);
        }
        return true;
    }
       

#if UNITY_EDITOR
    private void OnDrawGizmosSelected()
    {
        for(int i = 0; i<attackPoints.Length; i++)
        {
            AttackPoint point = attackPoints[i];
            if(!point.attackRoot)
            {
                Vector3 worldPos = point.attackRoot.TransformVector(point.offset);
                Gizmos.color = new Color(1f, 1f, 1f, 0.4f);
                Gizmos.DrawSphere(point.attackRoot.position + worldPos, point.radius);
            }
            if(point.previousPositions.Count > 1)
            {
                // DrawAAPolyLine : Anti-aliased line drawing function that takes an array of points.
                // 
                // Anti-aliased : 경계선이 부드럽게 보이도록 하는 기법.
                UnityEditor.Handles.DrawAAPolyLine(10, point.previousPositions.ToArray());
            }
        }
    }
#endif
}