using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(CPlayerController))]
public class CPlayerControllerEditor : Editor
{
    SerializedProperty scriptProp;

    SerializedProperty cameraSettingsProp;       // 시네머신 카메라.
    SerializedProperty maxForawrdSpeedProp;      // 최대 이동 속도.
    SerializedProperty gravityProp;              // 중력값 (공중에서의 하강 속도)
    SerializedProperty jumpSpeedProp;            // 점프 힘
    SerializedProperty minTurnSpeedProp;         // 달리는 도중 회전하는 힘
    SerializedProperty maxTurnSpeedProp;         // 정지한 상태에서 회전하는 힘
    SerializedProperty idleTimeoutProp;          // Idle(유휴)상태 전환 대기 시간
    SerializedProperty canAttackProp;            // 공격 가능 여부
    SerializedProperty footstepAudioProp;        // 걷기 오디오
    SerializedProperty hurtAudioProp;            // 피격 오디오
    SerializedProperty landingPlayerProp;        // 착지 오디오
    SerializedProperty emoteLandingPlayerProp;   // (음성) 착지 오디오
    SerializedProperty emoteDeathPlayerProp;     // (음성) 사망 오디오
    SerializedProperty emoteAttackAudioProp;     // (음성) 공격 오디오
    SerializedProperty emoteJumpAudioProp;       // (음성) 점프 오디오

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
        // serialzedObject : 대상이 되는 인스턴스 객체
        // Update() : 변경점을 최신화하는 함수
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
            EditorGUI.indentLevel++;    // 들여쓰기
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

        serializedObject.ApplyModifiedProperties();     // 지금까지 일어난 변경사항을 적용하라.
    }

    private void MinMaxTurnSpeed()
    {
        // 기본값으로 있어야할 Rect(위치, 크기)를 가져온다.
        Rect position = EditorGUILayout.GetControlRect(false, EditorGUIUtility.singleLineHeight);

        const float SPACING = 4f;               // 컨텐츠 간격.
        const float INT_FIELD_WIDTH = 50f;      // 숫자 입력 필드 너비.

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
