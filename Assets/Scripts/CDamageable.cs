using Gamekit3D.Message;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public class CDamageable : MonoBehaviour
{
    public struct CDamageMessage
    {
        public MonoBehaviour damager;   // �������� �ִ� ��ü.
        public int amount;              // ������ ��.
        public Vector3 direction;       // �������� �޴� ����.
        public Vector3 damageSource;    // �������� �ִ� ��ġ.
        public bool throwing;           // �и�.
        public bool stopCamera;         // ī�޶� ���ߴ���.
    }

    // CDamageable Ŭ������ ���� �̱��� ����
    public static CDamageable instance;

    public int maxHitPoints;        // �ִ� ü��.
    public int godModeTime;         // ���� �ð�.

    [Range(0f, 360f)]
    public float hitAngle = 360f;               // �ǰ� ���� �� �ִ� ����.
    [Range(0f, 360f)]
    public float hitForwardRotation = 360f;     // �ǰ� ���� �� �ִ� ������ ���� �� ����ϴ� ȸ����.

    // ���� �������� �ƴ���.
    public bool isGodMode { get; set; }

    // ���� ü��.
    public int currentHitPoint { get; private set; }

    // �̺�Ʈ �Լ���.
    public UnityEvent OnDeath, OnReceveDamage, OnHitWhileGodMode, OnBecomeGodMode, OnResetDamage;

    public List<IMessageReceiver> onDamageMessageReceivers;

    protected float timeSinceLastHit = 0f;
    protected new Collider collider;

    private void Start()
    {
        ResetDamage();
        collider = GetComponent<Collider>();
    }

    // �������� �ʱ�ȭ�ϴ� �Լ�.
    public void ResetDamage()
    {
        currentHitPoint = maxHitPoints;
        isGodMode = false;
        timeSinceLastHit = 0f;
        OnResetDamage?.Invoke();
    }

    private void Update()
    {
        // ���� ���¸� �����ϴ� �Լ�.
        if (isGodMode)
        {
            timeSinceLastHit += Time.deltaTime;
            if (timeSinceLastHit >= godModeTime)
            {
                timeSinceLastHit = 0f;
                isGodMode = false;
                OnBecomeGodMode.Invoke();       // ������ ������ ���� ȣ���ϴ� �̺�Ʈ �Լ�.
            }
        }
    }

    public void SwitchCollider(bool isOn)
    {
        collider.enabled = isOn;
    }

    // �������� �޴� �Լ�.
    public void ApplyDamage(CDamageMessage data)
    {
        // �̹� �׾����� �������� ���� �ʴ´�.
        if (currentHitPoint <= 0)
            return;

        // ���� ������ �� �������� ���� �ʴ´�.
        if (isGodMode)
        {
            OnHitWhileGodMode?.Invoke();
            return;
        }

        // �������� �޴� ������ �����Ѵ�.
        Vector3 forward = transform.forward;

        // �������� �޴� ������ ȸ����Ų��.
        forward = Quaternion.AngleAxis(hitForwardRotation, transform.up) * forward;

        // �����ڸ� �ٶ󺸴� ���� ���͸� ���ϰ� y�� ������ �����Ѵ�.
        Vector3 positionToDamager = data.damageSource - transform.position;
        positionToDamager -= transform.up * Vector3.Dot(transform.up, positionToDamager);

        // �ǰ� ������ ����� �������� ���� �ʴ´�.
        if (Vector3.Angle(forward, positionToDamager) > hitAngle * 0.5f)
            return;

        isGodMode = true;
        currentHitPoint -= data.amount;
        if (currentHitPoint <= 0)
            StartCoroutine(InvokeOnDeath());     // ���� ���ÿ� �׿��� ��� �߻��Ǵ� ���� ������ ���ϱ� ���� ���� ����
        else
            OnReceveDamage?.Invoke();

        // ������ �޼��� �����ڵ鿡�� ���� ���� ������ ������ �����Ѵ�.
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
            UnityEditor.Handles.color = Color.green; // ����� �κ�: ȭ��ǥ ������ ������� ����
            UnityEditor.Handles.ArrowHandleCap(0, transform.position, Quaternion.LookRotation(forward), 1.0f,
                EventType.Repaint);
        }

        UnityEditor.Handles.color = new Color(1.0f, 0.0f, 0.0f, 0.5f);
        forward = Quaternion.AngleAxis(-hitAngle * 0.5f, transform.up) * forward;
        UnityEditor.Handles.DrawSolidArc(transform.position, transform.up, forward, hitAngle, 1.0f);
    }
#endif
}
