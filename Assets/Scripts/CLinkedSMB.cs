using Gamekit3D;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Animations;

public class CLinkedSMB<TMonoBehaviour> : CSealedSMB
    where TMonoBehaviour : MonoBehaviour
{
    protected TMonoBehaviour monoBehaviour;   // 제어자.
    private bool firstFrameHappened;          // 첫 프레임이 발생했는지 여부.
    private bool lastFrameHappened;           // 마지막 프레임이 발생했는지 여부.

    // SMB를 초기화하는 sttaic 함수.
    public static void Initialise(Animator animator, TMonoBehaviour monoBehaviour)
    {
        CLinkedSMB<TMonoBehaviour>[] array = animator.GetBehaviours<CLinkedSMB<TMonoBehaviour>>();
        foreach(var smb in array)
            smb.InternalInitialize(animator, monoBehaviour);
    }

    // Scene 내의 MonoBehaviour에서 Start 함수가 호출될 때 호출됩니다.
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

        // 애니메이션이 전환중인데, 현재 애니메이션이 다음 애니메이션과 같다면
        if (animator.IsInTransition(layerIndex) && animator.GetNextAnimatorStateInfo(layerIndex).fullPathHash == stateInfo.fullPathHash)
        {
            OnSLTransitionToStateUpdate(animator, stateInfo, layerIndex);
            OnSLTransitionToStateUpdate(animator, stateInfo, layerIndex, controller);
        }

        // 애니메이션이 전환중이 아니고, 첫 프레임이 발생했다면
        if (!animator.IsInTransition(layerIndex) && firstFrameHappened)
        {
            OnSLStateNoTransitionUpdate(animator, stateInfo, layerIndex);
            OnSLStateNoTransitionUpdate(animator, stateInfo, layerIndex, controller);
        }

        // 애니메이션이 전환중이고, 마지막 프레임이 발생하지 않았다면
        if (animator.IsInTransition(layerIndex) && !lastFrameHappened && firstFrameHappened)
        {
            lastFrameHappened = true;

            OnSLStatePreExit(animator, stateInfo, layerIndex);
            OnSLStatePreExit(animator, stateInfo, layerIndex, controller);
        }

        // 애니메이션이 전환중이 아니고, 첫 프레임이 발생하지 않았다면
        if (!animator.IsInTransition(layerIndex) && !firstFrameHappened)
        {
            firstFrameHappened = true;

            OnSLStatePostEnter(animator, stateInfo, layerIndex);
            OnSLStatePostEnter(animator, stateInfo, layerIndex, controller);
        }

        // 애니메이션이 전환중이고, 현재 애니메이션이 이전 애니메이션과 같다면
        if (animator.IsInTransition(layerIndex) && animator.GetCurrentAnimatorStateInfo(layerIndex).fullPathHash == stateInfo.fullPathHash)
        {
            OnSLTransitionFromStateUpdate(animator, stateInfo, layerIndex);
            OnSLTransitionFromStateUpdate(animator, stateInfo, layerIndex, controller);
        }
    }

    // 상태로 전환하는 동안 매 프레임마다 OnSLStateEnter 이후에 호출됩니다.
    public virtual void OnSLTransitionToStateUpdate(Animator animator, AnimatorStateInfo stateInfo, int layerIndex) { }

    public virtual void OnSLTransitionToStateUpdate(Animator animator, AnimatorStateInfo stateInfo, int layerIndex, AnimatorControllerPlayable controller) { }


    // 애니메이션이 전환 중인데 ;
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
    // 불필요한 함수를 sealed(봉인)하고 있다.
    public sealed override void OnStateEnter(Animator animator, AnimatorStateInfo stateInfo, int layerIndex) { }

    public sealed override void OnStateUpdate(Animator animator, AnimatorStateInfo stateInfo, int layerIndex) { }

    public sealed override void OnStateExit(Animator animator, AnimatorStateInfo stateInfo, int layerIndex) { }
}