using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CChomperIdle : CLinkedSMB<CChomperHehaviour>
{
    // �����ϰ� ��� �ð�.
    public float minimumIdleGruntTime = 2.0f;
    public float maximumIdleGruntTime = 5.0f;

    protected float remainingToNextGrunt = 0.0f;

    public override void OnStateMachineEnter(Animator animator, int stateMachinePathHash)
    {
        if(minimumIdleGruntTime > maximumIdleGruntTime)
            minimumIdleGruntTime = maximumIdleGruntTime;
        remainingToNextGrunt = Random.Range(minimumIdleGruntTime, maximumIdleGruntTime);
    }
    // �ִϸ��̼��� ��ȯ������ �ʰ� ��� Loop�� ��.
    public override void OnSLStateNoTransitionUpdate(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        base.OnSLStateNoTransitionUpdate(animator, stateInfo, layerIndex);

        // ���� ���� �ð��� ���ҽ�ŵ�ϴ�.
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
