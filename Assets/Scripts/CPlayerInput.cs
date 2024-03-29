using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CPlayerInput : Singleton<CPlayerInput>
{
    public bool isLockControl;

    private Vector2 moveInput;   // 움직임 입력 값
    private Vector2 cameraInput; // 회전 입력 값
    private bool isJump;         // 점프 입력 값
    private bool isAttack;       // 공격 입력 값
    private bool isPause;        // 일시 정지 입력 값

    public Vector2 MoveInput
    {
        get
        {
            if (isLockControl)
                return Vector2.zero;
            return moveInput;
        }
    }
    public Vector2 CameraInput
    {
        get
        {
            if (isLockControl)
                return Vector2.zero;
            return cameraInput;
        }
    }
    public bool IsJump => isJump && !isLockControl;
    public bool IsAttack => isAttack && !isLockControl;
    public bool IsPause => isPause;


    private void Update()
    {
        moveInput.Set(Input.GetAxis("Horizontal"), Input.GetAxis("Vertical"));
        cameraInput.Set(Input.GetAxis("Mouse X"), Input.GetAxis("Mouse Y"));
        isJump = Input.GetButton("Jump");
        isAttack = Input.GetButtonDown("Fire1");
        isPause = Input.GetButtonDown("Pause");
    }

}
