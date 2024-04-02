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
            //  = �����Ϳ����� ���Ǹ� ����ĳ��Ʈ�� ���Ǵ� ���� ��θ� ǥ���ϴ� �� ���˴ϴ�.
            previousPositions = new List<Vector3>();
#endif
        }
    }

    public int damage = 1;                          // �⺻ ������.
    public ParticleSystem hitParticlePrefab;        // Ÿ�� ��ƼŬ ������.
    public LayerMask targetLayers;                  // Ÿ�� ������ ���̾�.

    public AttackPoint[] attackPoints;              // ���� ����.
    public CTimeEffect[] timeEttects;               // �ִϸ��̼� ����Ʈ.

    [Header("Audio")]
    public CRandomAudioPlayer hitAudio;              // Ÿ�� ����.
    public CRandomAudioPlayer attackAudio;           // ���� ����.

    // ================================== ��� ���� ==================================


    protected GameObject owner;                     // ������.
    protected Vector3[] previousAttackPoints;       // attackPoints�� ���� ��ġ.
    protected Vector3 direction;                    // ����.

    protected bool isThrowingHit = false;           // ��ô ���� ����.
    protected bool inAttack = false;                // ���� �� ����.    

    private const int MAX_PARTICLE_COUNT = 10;                                              // ��ƼŬ ����.
    protected int currentParticle = 0;                                                      // ���� ��ƼŬ.
    protected ParticleSystem[] particlesPool = new ParticleSystem[MAX_PARTICLE_COUNT];      // ��ƼŬ Ǯ.

    protected static RaycastHit[] raycastHitCache = new RaycastHit[32];                     // ����ĳ��Ʈ ��Ʈ ĳ��.
    protected static Collider[] colliderCache = new Collider[32];                           // �ݶ��̴� ĳ��.

    public bool ThrowingHit
    {
        get { return isThrowingHit; }
        set { isThrowingHit = value; }
    }

    // ================================== ��� �Լ� ==================================

    private void Awake()
    {
        // ��ƼŬ Ǯ �ʱ�ȭ.
        if (hitParticlePrefab != null)
        {
            for (int i = 0; i < MAX_PARTICLE_COUNT; i++)
            {
                particlesPool[i] = Instantiate(hitParticlePrefab);  // ��ƼŬ ����.
                particlesPool[i].Stop();                            // ��ƼŬ ����.
            }
        }
    }

    public void SetOwner(GameObject owner)
    {
        this.owner = owner;
    }

    // ���� ���� �Լ� (isThrowing : �˹� ���� ����)
    public void BeginAttack(bool isThrowing = false)
    {
        attackAudio?.PlayRandomClip();
        isThrowingHit = isThrowing;
        inAttack = true;

        previousAttackPoints = new Vector3[attackPoints.Length];
        for(int i = 0; i<previousAttackPoints.Length; i++)
        {
            // TransformDirection : ���� ��ǥ�踦 ���� ��ǥ��� ��ȯ.
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

            // ��ü ĳ��Ʈ�� ���� ���� ���ʹ� ����� �������� �ʽ��ϴ�. ������ �ݶ��̴��� ���������� ������ "��ü"�� ��ġ����.
            // ���� �츮�� �� "������" ��ü ĳ��Ʈ�� ��ġ�� ��� ���� ���� ���̶�� ���� Ȯ���ϰ� �ϱ� ���� �ſ� ���� �̼��� ���� ĳ��Ʈ�� �����մϴ�.
            if (vector.magnitude < 0.001f)
                vector = Vector3.forward * 0.0001f;

            Ray ray = new Ray(worldPos, vector.normalized);

            // SphereCastNonAlloc : ��ü ĳ��Ʈ�� �����ϰ� �浹�� ��� �ݶ��̴��� �迭�� ����.
            // raycastHitCache : �浹�� ��� �ݶ��̴��� ������ �迭.
            // vector.magnitude : ��ü ĳ��Ʈ�� �Ÿ� (������ ��Į�� ��)
            // targetLayers : Ÿ�� ������ ���̾�.
            // QueryTriggerInteraction.Ignore : Ʈ���� �ݶ��̴��� �����ϰ� �浹�� ����.
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
        // ���� ������ ���������� ������ �������� �ʽ��ϴ� (�츮�� �ڽſ��� "�ݹ�"���� �ʽ��ϴ�).
        if (damageable.gameObject == owner)
            return true;

        // �ǰ� ����� Renderer�� �˻��� Material�� �ش��ϴ� ���� ����� ���.
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

        // ���� ����Ʈ �߻�
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
                // Anti-aliased : ��輱�� �ε巴�� ���̵��� �ϴ� ���.
                UnityEditor.Handles.DrawAAPolyLine(10, point.previousPositions.ToArray());
            }
        }
    }
#endif
}