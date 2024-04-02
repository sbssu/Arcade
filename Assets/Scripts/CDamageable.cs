using Gamekit3D.Message;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public class CDamageable : MonoBehaviour
{
    public struct CDamageMessage
    {
        public MonoBehaviour damager;   // 데미지를 주는 객체.
        public int amount;              // 데미지 양.
        public Vector3 direction;       // 데미지를 받는 방향.
        public Vector3 damageSource;    // 데미지를 주는 위치.
        public bool throwing;           // 밀림.
        public bool stopCamera;         // 카메라를 멈추는지.
    }

    // CDamageable 클래스에 대한 싱글톤 패턴
    public static CDamageable instance;

    public int maxHitPoints;        // 최대 체력.
    public int godModeTime;         // 무적 시간.

    [Range(0f, 360f)]
    public float hitAngle = 360f;               // 피격 받을 수 있는 각도.
    [Range(0f, 360f)]
    public float hitForwardRotation = 360f;     // 피격 받을 수 있는 각도를 정할 때 사용하는 회전값.

    // 무적 상태인지 아닌지.
    public bool isGodMode { get; set; }

    // 현재 체력.
    public int currentHitPoint { get; private set; }

    // 이벤트 함수들.
    public UnityEvent OnDeath, OnReceveDamage, OnHitWhileGodMode, OnBecomeGodMode, OnResetDamage;

    public List<IMessageReceiver> onDamageMessageReceivers;

    protected float timeSinceLastHit = 0f;
    protected new Collider collider;

    private void Start()
    {
        ResetDamage();
        collider = GetComponent<Collider>();
    }

    // 데미지를 초기화하는 함수.
    public void ResetDamage()
    {
        currentHitPoint = maxHitPoints;
        isGodMode = false;
        timeSinceLastHit = 0f;
        OnResetDamage?.Invoke();
    }

    private void Update()
    {
        // 무적 상태를 해제하는 함수.
        if (isGodMode)
        {
            timeSinceLastHit += Time.deltaTime;
            if (timeSinceLastHit >= godModeTime)
            {
                timeSinceLastHit = 0f;
                isGodMode = false;
                OnBecomeGodMode.Invoke();       // 무적이 끝나는 시점 호출하는 이벤트 함수.
            }
        }
    }

    public void SwitchCollider(bool isOn)
    {
        collider.enabled = isOn;
    }

    // 데미지를 받는 함수.
    public void ApplyDamage(CDamageMessage data)
    {
        // 이미 죽었으면 데미지를 받지 않는다.
        if (currentHitPoint <= 0)
            return;

        // 무적 상태일 때 데미지를 받지 않는다.
        if (isGodMode)
        {
            OnHitWhileGodMode?.Invoke();
            return;
        }

        // 데미지를 받는 방향을 설정한다.
        Vector3 forward = transform.forward;

        // 데미지를 받는 방향을 회전시킨다.
        forward = Quaternion.AngleAxis(hitForwardRotation, transform.up) * forward;

        // 공격자를 바라보는 방향 벡터를 구하고 y축 선분을 제거한다.
        Vector3 positionToDamager = data.damageSource - transform.position;
        positionToDamager -= transform.up * Vector3.Dot(transform.up, positionToDamager);

        // 피격 각도를 벗어나면 데미지를 받지 않는다.
        if (Vector3.Angle(forward, positionToDamager) > hitAngle * 0.5f)
            return;

        isGodMode = true;
        currentHitPoint -= data.amount;
        if (currentHitPoint <= 0)
            StartCoroutine(InvokeOnDeath());     // 서로 동시에 죽였을 경우 발생되는 꼬임 현상을 피하기 위해 지연 실행
        else
            OnReceveDamage?.Invoke();

        // 데미지 메세지 수신자들에게 내가 받은 데미지 정보를 전달한다.
        var messageType = currentHitPoint <= 0 ? MessageType.DEAD : MessageType.DAMAGED;
        foreach (var receiver in onDamageMessageReceivers)
            receiver.OnReceiveMessage(messageType, this, data);
    }

    private IEnumerator InvokeOnDeath()
    {
        yield return new WaitForEndOfFrame();
        OnDeath?.Invoke();
    }

#if UNITY_EDITOR
    private void OnDrawGizmosSelected()
    {
        Vector3 forward = transform.forward;
        forward = Quaternion.AngleAxis(hitForwardRotation, transform.up) * forward;

        if (Event.current.type == EventType.Repaint)
        {
            UnityEditor.Handles.color = Color.green; // 변경된 부분: 화살표 색상을 녹색으로 설정
            UnityEditor.Handles.ArrowHandleCap(0, transform.position, Quaternion.LookRotation(forward), 1.0f,
                EventType.Repaint);
        }

        UnityEditor.Handles.color = new Color(1.0f, 0.0f, 0.0f, 0.5f);
        forward = Quaternion.AngleAxis(-hitAngle * 0.5f, transform.up) * forward;
        UnityEditor.Handles.DrawSolidArc(transform.position, transform.up, forward, hitAngle, 1.0f);
    }
#endif
}
