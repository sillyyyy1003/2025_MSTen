using UnityEngine;
using UnityEditor;
using GameData;

[CustomEditor(typeof(PieceDataSO), true)]
public class PieceDataSOEditor : Editor
{
    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        EditorGUILayout.PropertyField(serializedObject.FindProperty("originalPID"));
        EditorGUILayout.PropertyField(serializedObject.FindProperty("pieceName"));
        EditorGUILayout.PropertyField(serializedObject.FindProperty("populationCost"));
        EditorGUILayout.PropertyField(serializedObject.FindProperty("resourceCost"));
        EditorGUILayout.PropertyField(serializedObject.FindProperty("initialLevel"));

        EditorGUILayout.Space();

        EditorGUILayout.PropertyField(serializedObject.FindProperty("aPRecoveryRate"));
        EditorGUILayout.PropertyField(serializedObject.FindProperty("moveAPCost"));
        EditorGUILayout.PropertyField(serializedObject.FindProperty("moveSpeed"));

        EditorGUILayout.Space();

        SerializedProperty canAttackProp = serializedObject.FindProperty("canAttack");
        EditorGUILayout.PropertyField(canAttackProp);

        SerializedProperty attackPowerByLevel = serializedObject.FindProperty("attackPowerByLevel");

        if (canAttackProp.boolValue)
        {
            EditorGUI.indentLevel++;
            EditorGUILayout.PropertyField(serializedObject.FindProperty("attackPower"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("attackRange"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("attackCooldown"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("attackAPCost"));
            EditorGUI.indentLevel--;
        }

        EditorGUILayout.Space();

        // アップグレード設定
        EditorGUILayout.LabelField("アップグレード設定", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox("レベル0:初期、1:升級1、2:升級2、3:升級3", MessageType.Info);

        SerializedProperty maxHPByLevel = serializedObject.FindProperty("maxHPByLevel");
        SerializedProperty maxAPByLevel = serializedObject.FindProperty("maxAPByLevel");

        if (maxHPByLevel != null)
        {
            EditorGUILayout.PropertyField(maxHPByLevel, true);
        }
        if (maxAPByLevel != null)
        {
            EditorGUILayout.PropertyField(maxAPByLevel, true);
        }
        if (attackPowerByLevel != null && canAttackProp.boolValue)
        {
            EditorGUILayout.PropertyField(attackPowerByLevel, true);
        }

        EditorGUILayout.Space();

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

        EditorGUILayout.Space();
        EditorGUILayout.PropertyField(serializedObject.FindProperty("Buildings"));
        EditorGUILayout.PropertyField(serializedObject.FindProperty("buildingSpeedModifier"));

        EditorGUILayout.Space();
        EditorGUILayout.PropertyField(serializedObject.FindProperty("productEfficiency"));

        EditorGUILayout.Space();
        EditorGUILayout.HelpBox("レベル0:初期、1:升級1、2:升級2", MessageType.Info);
        EditorGUILayout.PropertyField(serializedObject.FindProperty("maxHpByLevel"), new GUIContent("最大HP"), true);
        EditorGUILayout.PropertyField(serializedObject.FindProperty("maxApByLevel"), new GUIContent("最大AP"), true);
        EditorGUILayout.PropertyField(serializedObject.FindProperty("maxSacrificeLevel"), new GUIContent("生贄スキルレベル"), true);

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

        EditorGUILayout.Space();
        EditorGUILayout.PropertyField(serializedObject.FindProperty("availableSkills"));

        EditorGUILayout.Space();
        EditorGUILayout.HelpBox("レベル0:初期、1:升級1、2:升級2、3:升級3", MessageType.Info);
        EditorGUILayout.PropertyField(serializedObject.FindProperty("hasAntiConversionSkill"), new GUIContent("魅惑耐性"), true);

        serializedObject.ApplyModifiedProperties();
    }
}

// 宣教師専用のEditor
[CustomEditor(typeof(MissionaryDataSO))]
public class MissionaryDataSOEditor : PieceDataSOEditor
{
    public override void OnInspectorGUI()
    {
        base.OnInspectorGUI();

        serializedObject.Update();

        EditorGUILayout.Space();
        EditorGUILayout.PropertyField(serializedObject.FindProperty("occupyAPCost"));

        EditorGUILayout.Space();
        EditorGUILayout.PropertyField(serializedObject.FindProperty("baseConversionAttackChance"));
        EditorGUILayout.PropertyField(serializedObject.FindProperty("conversionTurnDuration"));

        EditorGUILayout.Space();
        EditorGUILayout.PropertyField(serializedObject.FindProperty("baseConversionDefenseChance"));

        EditorGUILayout.Space();
        EditorGUILayout.HelpBox("レベル0:初期、1:升級1、2:升級2、3:升級3", MessageType.Info);

        EditorGUILayout.PropertyField(serializedObject.FindProperty("occupyEmptySuccessRateByLevel"), new GUIContent("自領地占領成功率"), true);
        EditorGUILayout.PropertyField(serializedObject.FindProperty("occupyEnemySuccessRateByLevel"), new GUIContent("敵領地占領成功率"), true);
        EditorGUILayout.PropertyField(serializedObject.FindProperty("convertMissionaryChanceByLevel"), new GUIContent("宣教師魅惑成功率"), true);
        EditorGUILayout.PropertyField(serializedObject.FindProperty("convertFarmerChanceByLevel"), new GUIContent("信徒魅惑成功率"), true);
        EditorGUILayout.PropertyField(serializedObject.FindProperty("convertMilitaryChanceByLevel"), new GUIContent("十字軍魅惑成功率"), true);
        EditorGUILayout.PropertyField(serializedObject.FindProperty("hasAntiConversionSkill"), new GUIContent("魅惑耐性"), true);

        serializedObject.ApplyModifiedProperties();
    }
}

// 教皇専用のEditor
[CustomEditor(typeof(PopeDataSO))]
public class PopeDataSOEditor : PieceDataSOEditor
{
    public override void OnInspectorGUI()
    {
        base.OnInspectorGUI();

        serializedObject.Update();

        EditorGUILayout.Space();
        EditorGUILayout.PropertyField(serializedObject.FindProperty("swapCooldown"));

        EditorGUILayout.Space();
        EditorGUILayout.PropertyField(serializedObject.FindProperty("hpBuff"));
        EditorGUILayout.PropertyField(serializedObject.FindProperty("atkBuff"));
        EditorGUILayout.PropertyField(serializedObject.FindProperty("convertBuff"));

        serializedObject.ApplyModifiedProperties();
    }
}

