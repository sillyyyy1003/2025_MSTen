using UnityEngine;
using UnityEditor;
using GameData;

[CustomEditor(typeof(PieceDataSO), true)]
public class PieceDataSOEditor : Editor
{
    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        // 基本パラメータ
        EditorGUILayout.PropertyField(serializedObject.FindProperty("pieceName"));
        EditorGUILayout.PropertyField(serializedObject.FindProperty("maxHP"));
        EditorGUILayout.PropertyField(serializedObject.FindProperty("populationCost"));
        EditorGUILayout.PropertyField(serializedObject.FindProperty("resourceCost"));

        EditorGUILayout.Space();

        // 行動力パラメータ

        EditorGUILayout.PropertyField(serializedObject.FindProperty("maxAP"));
        EditorGUILayout.PropertyField(serializedObject.FindProperty("aPRecoveryRate"));
        EditorGUILayout.PropertyField(serializedObject.FindProperty("moveAPCost"));

        EditorGUILayout.Space();

        // 戦闘パラメータ

        SerializedProperty canAttackProp = serializedObject.FindProperty("canAttack");
        EditorGUILayout.PropertyField(canAttackProp);

        // canAttack が true の場合のみ表示
        if (canAttackProp.boolValue)
        {
            EditorGUI.indentLevel++; // インデント追加
            EditorGUILayout.PropertyField(serializedObject.FindProperty("attackPower"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("attackRange"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("attackCooldown"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("attackAPCost"));
            EditorGUI.indentLevel--;
        }

        EditorGUILayout.Space();

        // ビジュアル

        EditorGUILayout.PropertyField(serializedObject.FindProperty("pieceSprite"));
        EditorGUILayout.PropertyField(serializedObject.FindProperty("pieceMesh"));
        EditorGUILayout.PropertyField(serializedObject.FindProperty("pieceMaterial"));
        EditorGUILayout.PropertyField(serializedObject.FindProperty("piecePrefab"));
        EditorGUILayout.PropertyField(serializedObject.FindProperty("animatorController"));

        EditorGUILayout.Space();

        serializedObject.ApplyModifiedProperties();
    }
}

[CustomEditor(typeof(FarmerDataSO), true)]
public class FarmerDataSOEditor : PieceDataSOEditor
{
    public override void OnInspectorGUI()
    {
        base.OnInspectorGUI();

        serializedObject.Update();

        //建築能力

        EditorGUILayout.PropertyField(serializedObject.FindProperty("Buildings"));
        EditorGUILayout.PropertyField(serializedObject.FindProperty("buildingSpeedModifier"));

        EditorGUILayout.Space();

        // 資源生産効率

        EditorGUILayout.PropertyField(serializedObject.FindProperty("productEfficiency"));

        EditorGUILayout.Space();

        // スキルレベル設定

        EditorGUILayout.PropertyField(serializedObject.FindProperty("maxSkillLevel"));
        EditorGUILayout.PropertyField(serializedObject.FindProperty("skillProductionBonus"));

        serializedObject.ApplyModifiedProperties();

    }
}

// 軍隊専用のEditor
[CustomEditor(typeof(MilitaryDataSO))]
public class MilitaryDataSOEditor : PieceDataSOEditor
{
    public override void OnInspectorGUI()
    {
        base.OnInspectorGUI();

        serializedObject.Update();

        EditorGUILayout.Space();


        EditorGUILayout.PropertyField(serializedObject.FindProperty("armorValue"));
        EditorGUILayout.PropertyField(serializedObject.FindProperty("criticalChance"));
        EditorGUILayout.PropertyField(serializedObject.FindProperty("damageType"));
        EditorGUILayout.PropertyField(serializedObject.FindProperty("availableSkills"));

        serializedObject.ApplyModifiedProperties();
    }
}

