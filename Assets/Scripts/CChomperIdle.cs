using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CChomperIdle : CLinkedSMB<CChomperHehaviour>
{
    // 랜덤하게 우는 시간.
    public float minimumIdleGruntTime = 2.0f;
    public float maximumIdleGruntTime = 5.0f;

    protected float remainingToNextGrunt = 0.0f;

    public override void OnStateMachineEnter(Animator animator, int stateMachinePathHash)
    {
        if(minimumIdleGruntTime > maximumIdleGruntTime)
            minimumIdleGruntTime = maximumIdleGruntTime;
        remainingToNextGrunt = Random.Range(minimumIdleGruntTime, maximumIdleGruntTime);
    }
    // 애니메이션이 전환중이지 않고 계속 Loop될 때.
    public override void OnSLStateNoTransitionUpdate(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        base.OnSLStateNoTransitionUpdate(animator, stateInfo, layerIndex);

        // 남은 울음 시간을 감소시킵니다.
        remainingToNextGrunt -= Time.deltaTime;
        if(remainingToNextGrunt <= 0f)
        {
            remainingToNextGrunt = Random.Range(minimumIdleGruntTime, maximumIdleGruntTime);
            monoBehaviour.PlayAudio(CChomperHehaviour.AUDIO.GRUNT);
        }

        monoBehaviour.FindTarget();
        if (monoBehaviour.target != null)
            monoBehaviour.StartChase();
    }


}
