using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(CPlayerController))]
public class CPlayerControllerEditor : Editor
{
    SerializedProperty scriptProp;

    SerializedProperty cameraSettingsProp;       // �ó׸ӽ� ī�޶�.
    SerializedProperty maxForawrdSpeedProp;      // �ִ� �̵� �ӵ�.
    SerializedProperty gravityProp;              // �߷°� (���߿����� �ϰ� �ӵ�)
    SerializedProperty jumpSpeedProp;            // ���� ��
    SerializedProperty minTurnSpeedProp;         // �޸��� ���� ȸ���ϴ� ��
    SerializedProperty maxTurnSpeedProp;         // ������ ���¿��� ȸ���ϴ� ��
    SerializedProperty idleTimeoutProp;          // Idle(����)���� ��ȯ ��� �ð�
    SerializedProperty canAttackProp;            // ���� ���� ����
    SerializedProperty footstepAudioProp;        // �ȱ� �����
    SerializedProperty hurtAudioProp;            // �ǰ� �����
    SerializedProperty landingPlayerProp;        // ���� �����
    SerializedProperty emoteLandingPlayerProp;   // (����) ���� �����
    SerializedProperty emoteDeathPlayerProp;     // (����) ��� �����
    SerializedProperty emoteAttackAudioProp;     // (����) ���� �����
    SerializedProperty emoteJumpAudioProp;       // (����) ���� �����

    GUIContent scriptContent = new GUIContent("Script");

    private void OnEnable()
    {
        scriptProp = serializedObject.FindProperty("m_Script");

        cameraSettingsProp = serializedObject.FindProperty("cameraSettings");
        maxForawrdSpeedProp = serializedObject.FindProperty("maxForawrdSpeed");
        gravityProp = serializedObject.FindProperty("gravity");
        jumpSpeedProp = serializedObject.FindProperty("jumpSpeed");
        minTurnSpeedProp = serializedObject.FindProperty("minTurnSpeed");
        maxTurnSpeedProp = serializedObject.FindProperty("maxTurnSpeed");
        idleTimeoutProp = serializedObject.FindProperty("idleTimeout");
        canAttackProp = serializedObject.FindProperty("canAttack");

        footstepAudioProp = serializedObject.FindProperty("footstepAudio");
        hurtAudioProp = serializedObject.FindProperty("hurtAudio");
        landingPlayerProp = serializedObject.FindProperty("landingPlayer");
        emoteLandingPlayerProp = serializedObject.FindProperty("emoteLandingPlayer");
        emoteDeathPlayerProp = serializedObject.FindProperty("emoteDeathPlayer");
        emoteAttackAudioProp = serializedObject.FindProperty("emoteAttackAudio");
        emoteJumpAudioProp = serializedObject.FindProperty("emoteJumpAudio");
    }

    public override void OnInspectorGUI()
    {
        // serialzedObject : ����� �Ǵ� �ν��Ͻ� ��ü
        // Update() : �������� �ֽ�ȭ�ϴ� �Լ�
        serializedObject.Update();

        GUI.enabled = false;
        EditorGUILayout.PropertyField(scriptProp, scriptContent);
        GUI.enabled = true;

        maxForawrdSpeedProp.floatValue = EditorGUILayout.Slider("max forward speed", maxForawrdSpeedProp.floatValue, 4f, 12f);
        gravityProp.floatValue = EditorGUILayout.Slider("gravity", gravityProp.floatValue, 10f, 30f);
        jumpSpeedProp.floatValue = EditorGUILayout.Slider("jump speed", jumpSpeedProp.floatValue, 5f, 20f);

        MinMaxTurnSpeed();

        EditorGUILayout.PropertyField(idleTimeoutProp, new GUIContent("idle time out"));
        EditorGUILayout.PropertyField(canAttackProp, new GUIContent("can attack"));

        EditorGUILayout.Space();

        scriptProp.isExpanded = EditorGUILayout.Foldout(scriptProp.isExpanded, "Reference");
        if (scriptProp.isExpanded)
        {
            EditorGUI.indentLevel++;    // �鿩����
            EditorGUILayout.PropertyField(cameraSettingsProp, new GUIContent("camera settings"));
            EditorGUILayout.PropertyField(footstepAudioProp, new GUIContent("footstep Audio"));
            EditorGUILayout.PropertyField(hurtAudioProp, new GUIContent("hurt Audio"));
            EditorGUILayout.PropertyField(landingPlayerProp, new GUIContent("landing Player"));
            EditorGUILayout.PropertyField(emoteLandingPlayerProp, new GUIContent("emote Landing Player"));
            EditorGUILayout.PropertyField(emoteDeathPlayerProp, new GUIContent("emote Death Player"));
            EditorGUILayout.PropertyField(emoteAttackAudioProp, new GUIContent("emote Attack Audio"));
            EditorGUILayout.PropertyField(emoteJumpAudioProp, new GUIContent("emote Jump Audio"));
            EditorGUI.indentLevel--;
        }

        serializedObject.ApplyModifiedProperties();     // ���ݱ��� �Ͼ ��������� �����϶�.
    }

    private void MinMaxTurnSpeed()
    {
        // �⺻������ �־���� Rect(��ġ, ũ��)�� �����´�.
        Rect position = EditorGUILayout.GetControlRect(false, EditorGUIUtility.singleLineHeight);

        const float SPACING = 4f;               // ������ ����.
        const float INT_FIELD_WIDTH = 50f;      // ���� �Է� �ʵ� �ʺ�.

        position.width -= SPACING * 3f + INT_FIELD_WIDTH * 2f;

        Rect labelRect = position;

        labelRect.width *= 0.48f;

        Rect minRect = position;
        minRect.width = 50f;
        minRect.x += labelRect.width + SPACING;

        Rect sliderRect = position;
        sliderRect.width *= 0.52f;
        sliderRect.x += labelRect.width + minRect.width + SPACING * 2f;

        Rect maxRect = position;
        maxRect.width = minRect.width;
        maxRect.x += labelRect.width + minRect.width + sliderRect.width + SPACING * 3f;


        EditorGUI.LabelField(labelRect, new GUIContent("Turn Speed"));
        minTurnSpeedProp.floatValue = EditorGUI.IntField(minRect, (int)minTurnSpeedProp.floatValue);

        float minTurnSpeed = minTurnSpeedProp.floatValue;
        float maxTurnSpeed = maxTurnSpeedProp.floatValue;

        EditorGUI.MinMaxSlider(sliderRect, GUIContent.none, ref minTurnSpeed, ref maxTurnSpeed, 100f, 1500f);
        minTurnSpeedProp.floatValue = minTurnSpeed;
        maxTurnSpeedProp.floatValue = maxTurnSpeed;

        maxTurnSpeedProp.floatValue = EditorGUI.IntField(maxRect, (int)maxTurnSpeedProp.floatValue);
    }



}
