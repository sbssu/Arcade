using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CPlayerInput : Singleton<CPlayerInput>
{
    public bool isLockControl;

    private Vector2 moveInput;   // ������ �Է� ��
    private Vector2 cameraInput; // ȸ�� �Է� ��
    private bool isJump;         // ���� �Է� ��
    private bool isAttack;       // ���� �Է� ��
    private bool isPause;        // �Ͻ� ���� �Է� ��

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
