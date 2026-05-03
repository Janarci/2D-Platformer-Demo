using UnityEditor;

[CustomEditor(typeof(GameplayEventTimelineClip))]
public sealed class GameplayEventTimelineClipEditor : Editor
{
    private SerializedProperty eventType;
    private SerializedProperty targetRole;
    private SerializedProperty fireInEditorPreview;
    private SerializedProperty damageAmount;
    private SerializedProperty allowFriendlyFire;
    private SerializedProperty audioClip;
    private SerializedProperty audioVolume;
    private SerializedProperty vfxPrefab;
    private SerializedProperty vfxSpawnPoint;
    private SerializedProperty vfxOffset;
    private SerializedProperty vfxParentMode;
    private SerializedProperty finishedState;

    private void OnEnable()
    {
        eventType = serializedObject.FindProperty("eventType");
        targetRole = serializedObject.FindProperty("targetRole");
        fireInEditorPreview = serializedObject.FindProperty("fireInEditorPreview");
        damageAmount = serializedObject.FindProperty("damageAmount");
        allowFriendlyFire = serializedObject.FindProperty("allowFriendlyFire");
        audioClip = serializedObject.FindProperty("audioClip");
        audioVolume = serializedObject.FindProperty("audioVolume");
        vfxPrefab = serializedObject.FindProperty("vfxPrefab");
        vfxSpawnPoint = serializedObject.FindProperty("vfxSpawnPoint");
        vfxOffset = serializedObject.FindProperty("vfxOffset");
        vfxParentMode = serializedObject.FindProperty("vfxParentMode");
        finishedState = serializedObject.FindProperty("finishedState");
    }

    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        EditorGUILayout.PropertyField(eventType);
        EditorGUILayout.PropertyField(targetRole);
        EditorGUILayout.PropertyField(fireInEditorPreview);
        EditorGUILayout.Space();

        switch ((GameplayEventTimelineType)eventType.enumValueIndex)
        {
            case GameplayEventTimelineType.Damage:
                DrawDamageFields();
                break;

            case GameplayEventTimelineType.PlaySfx:
                DrawPlaySfxFields();
                break;

            case GameplayEventTimelineType.SpawnVfx:
                DrawSpawnVfxFields();
                break;

            case GameplayEventTimelineType.AttackTimelineFinished:
                DrawAttackTimelineFinishedFields();
                break;

            case GameplayEventTimelineType.EnemyDestroyed:
            case GameplayEventTimelineType.EnemyRevive:
            case GameplayEventTimelineType.EnemyRevived:
                EditorGUILayout.HelpBox("Uses the selected target role to resolve the enemy GameObject and HealthComponent from the bound actor GameObject.", MessageType.Info);
                break;
        }

        serializedObject.ApplyModifiedProperties();
    }

    private void DrawDamageFields()
    {
        EditorGUILayout.PropertyField(damageAmount);
        EditorGUILayout.PropertyField(allowFriendlyFire);
    }

    private void DrawPlaySfxFields()
    {
        EditorGUILayout.PropertyField(audioClip);
        EditorGUILayout.PropertyField(audioVolume);
    }

    private void DrawSpawnVfxFields()
    {
        EditorGUILayout.PropertyField(vfxPrefab);
        EditorGUILayout.PropertyField(vfxSpawnPoint);
        EditorGUILayout.PropertyField(vfxOffset);
        EditorGUILayout.PropertyField(vfxParentMode);
    }

    private void DrawAttackTimelineFinishedFields()
    {
        EditorGUILayout.PropertyField(finishedState);
    }
}
