using Gamekit3D;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Animations;

public class CLinkedSMB<TMonoBehaviour> : CSealedSMB
    where TMonoBehaviour : MonoBehaviour
{
    protected TMonoBehaviour monoBehaviour;   // ������.
    private bool firstFrameHappened;          // ù �������� �߻��ߴ��� ����.
    private bool lastFrameHappened;           // ������ �������� �߻��ߴ��� ����.

    // SMB�� �ʱ�ȭ�ϴ� sttaic �Լ�.
    public static void Initialise(Animator animator, TMonoBehaviour monoBehaviour)
    {
        CLinkedSMB<TMonoBehaviour>[] array = animator.GetBehaviours<CLinkedSMB<TMonoBehaviour>>();
        foreach(var smb in array)
            smb.InternalInitialize(animator, monoBehaviour);
    }

    // Scene ���� MonoBehaviour���� Start �Լ��� ȣ��� �� ȣ��˴ϴ�.
    protected void InternalInitialize(Animator animator, TMonoBehaviour monoBehaviour)
    {
        this.monoBehaviour = monoBehaviour;
        OnStart(animator);
    }
    public virtual void OnStart(Animator animator) { }

    // ============================================= ENTER ==============================================================================================
    public sealed override void OnStateEnter(Animator animator, AnimatorStateInfo stateInfo, int layerIndex, AnimatorControllerPlayable controller)
    {
        firstFrameHappened = false;
        lastFrameHappened = false;

        OnSLStateEnter(animator, stateInfo, layerIndex);
        OnSLStateEnter(animator, stateInfo, layerIndex, controller);
    }    
    public virtual void OnSLStateEnter(Animator animator, AnimatorStateInfo stateInfo, int layerIndex) { }

    public virtual void OnSLStateEnter(Animator animator, AnimatorStateInfo stateInfo, int layerIndex, AnimatorControllerPlayable controller) { }

    


    // ============================================= UPDATE ==============================================================================================
    public sealed override void OnStateUpdate(Animator animator, AnimatorStateInfo stateInfo, int layerIndex, AnimatorControllerPlayable controller)
    {
        if (!animator.gameObject.activeSelf)
            return;

        // �ִϸ��̼��� ��ȯ���ε�, ���� �ִϸ��̼��� ���� �ִϸ��̼ǰ� ���ٸ�
        if (animator.IsInTransition(layerIndex) && animator.GetNextAnimatorStateInfo(layerIndex).fullPathHash == stateInfo.fullPathHash)
        {
            OnSLTransitionToStateUpdate(animator, stateInfo, layerIndex);
            OnSLTransitionToStateUpdate(animator, stateInfo, layerIndex, controller);
        }

        // �ִϸ��̼��� ��ȯ���� �ƴϰ�, ù �������� �߻��ߴٸ�
        if (!animator.IsInTransition(layerIndex) && firstFrameHappened)
        {
            OnSLStateNoTransitionUpdate(animator, stateInfo, layerIndex);
            OnSLStateNoTransitionUpdate(animator, stateInfo, layerIndex, controller);
        }

        // �ִϸ��̼��� ��ȯ���̰�, ������ �������� �߻����� �ʾҴٸ�
        if (animator.IsInTransition(layerIndex) && !lastFrameHappened && firstFrameHappened)
        {
            lastFrameHappened = true;

            OnSLStatePreExit(animator, stateInfo, layerIndex);
            OnSLStatePreExit(animator, stateInfo, layerIndex, controller);
        }

        // �ִϸ��̼��� ��ȯ���� �ƴϰ�, ù �������� �߻����� �ʾҴٸ�
        if (!animator.IsInTransition(layerIndex) && !firstFrameHappened)
        {
            firstFrameHappened = true;

            OnSLStatePostEnter(animator, stateInfo, layerIndex);
            OnSLStatePostEnter(animator, stateInfo, layerIndex, controller);
        }

        // �ִϸ��̼��� ��ȯ���̰�, ���� �ִϸ��̼��� ���� �ִϸ��̼ǰ� ���ٸ�
        if (animator.IsInTransition(layerIndex) && animator.GetCurrentAnimatorStateInfo(layerIndex).fullPathHash == stateInfo.fullPathHash)
        {
            OnSLTransitionFromStateUpdate(animator, stateInfo, layerIndex);
            OnSLTransitionFromStateUpdate(animator, stateInfo, layerIndex, controller);
        }
    }

    // ���·� ��ȯ�ϴ� ���� �� �����Ӹ��� OnSLStateEnter ���Ŀ� ȣ��˴ϴ�.
    public virtual void OnSLTransitionToStateUpdate(Animator animator, AnimatorStateInfo stateInfo, int layerIndex) { }

    public virtual void OnSLTransitionToStateUpdate(Animator animator, AnimatorStateInfo stateInfo, int layerIndex, AnimatorControllerPlayable controller) { }


    // �ִϸ��̼��� ��ȯ ���ε� ;
    public virtual void OnSLStateNoTransitionUpdate(Animator animator, AnimatorStateInfo stateInfo, int layerIndex) { }
    public virtual void OnSLStateNoTransitionUpdate(Animator animator, AnimatorStateInfo stateInfo, int layerIndex, AnimatorControllerPlayable controller) { }


    public virtual void OnSLStatePreExit(Animator animator, AnimatorStateInfo stateInfo, int layerIndex) { }
    public virtual void OnSLStatePreExit(Animator animator, AnimatorStateInfo stateInfo, int layerIndex, AnimatorControllerPlayable controller) { }


    public virtual void OnSLStatePostEnter(Animator animator, AnimatorStateInfo stateInfo, int layerIndex) { }
    public virtual void OnSLStatePostEnter(Animator animator, AnimatorStateInfo stateInfo, int layerIndex, AnimatorControllerPlayable controller) { }

    public virtual void OnSLTransitionFromStateUpdate(Animator animator, AnimatorStateInfo stateInfo, int layerIndex) { }
    public virtual void OnSLTransitionFromStateUpdate(Animator animator, AnimatorStateInfo stateInfo, int layerIndex, AnimatorControllerPlayable controller) { }




    // ============================================= EXIT ==============================================================================================
    public sealed override void OnStateExit(Animator animator, AnimatorStateInfo stateInfo, int layerIndex, AnimatorControllerPlayable controller)
    {
        lastFrameHappened = false;
        OnSLStateExit(animator, stateInfo, layerIndex);
        OnSLStateExit(animator, stateInfo, layerIndex, controller);
    }
    public virtual void OnSLStateExit(Animator animator, AnimatorStateInfo stateInfo, int layerIndex) { }
    public virtual void OnSLStateExit(Animator animator, AnimatorStateInfo stateInfo, int layerIndex, AnimatorControllerPlayable controller) { }


}


public abstract class CSealedSMB : StateMachineBehaviour
{
    // ���ʿ��� �Լ��� sealed(����)�ϰ� �ִ�.
    public sealed override void OnStateEnter(Animator animator, AnimatorStateInfo stateInfo, int layerIndex) { }

    public sealed override void OnStateUpdate(Animator animator, AnimatorStateInfo stateInfo, int layerIndex) { }

    public sealed override void OnStateExit(Animator animator, AnimatorStateInfo stateInfo, int layerIndex) { }
}