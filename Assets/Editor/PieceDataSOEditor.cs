using UnityEngine;
using UnityEditor;
using GameData;

[CustomEditor(typeof(PieceDataSO), true)]
public class PieceDataSOEditor : Editor
{
    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        EditorGUILayout.PropertyField(serializedObject.FindProperty("piecePrefabResourcePath"));
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
        SerializedProperty upgradeCostByLevel = serializedObject.FindProperty("upgradeCostByLevel");

        if (maxHPByLevel != null)
        {
            EditorGUILayout.PropertyField(maxHPByLevel, true);
            SerializedProperty hpUpgradeCost = serializedObject.FindProperty("hpUpgradeCost");
            if (hpUpgradeCost != null)
            {
                EditorGUI.indentLevel++;
                EditorGUILayout.PropertyField(hpUpgradeCost, new GUIContent("血量アップグレード花費"), true);
                EditorGUI.indentLevel--;
            }
        }
        if (maxAPByLevel != null)
        {
            EditorGUILayout.PropertyField(maxAPByLevel, true);
            SerializedProperty apUpgradeCost = serializedObject.FindProperty("apUpgradeCost");
            if (apUpgradeCost != null)
            {
                EditorGUI.indentLevel++;
                EditorGUILayout.PropertyField(apUpgradeCost, new GUIContent("行動力アップグレード花費"), true);
                EditorGUI.indentLevel--;
            }
        }
        if (attackPowerByLevel != null && canAttackProp.boolValue)
        {
            EditorGUILayout.PropertyField(attackPowerByLevel, true);
        }

        EditorGUILayout.Space();

        // Prefabのみを使用（外見はPrefabに直接設定）
        EditorGUILayout.PropertyField(serializedObject.FindProperty("piecePrefab"));

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
        EditorGUILayout.PropertyField(serializedObject.FindProperty("buildingSpeedModifier"));

        EditorGUILayout.Space();
        EditorGUILayout.PropertyField(serializedObject.FindProperty("productEfficiency"));

        EditorGUILayout.PropertyField(serializedObject.FindProperty("devotionAPCost"));

        EditorGUILayout.Space();
        EditorGUILayout.HelpBox("レベル0:初期、1:升級1、2:升級2", MessageType.Info);
        EditorGUILayout.PropertyField(serializedObject.FindProperty("maxSacrificeLevel"), new GUIContent("生贄スキルレベル"), true);
        SerializedProperty sacrificeUpgradeCost = serializedObject.FindProperty("sacrificeUpgradeCost");
        if (sacrificeUpgradeCost != null)
        {
            EditorGUI.indentLevel++;
            EditorGUILayout.PropertyField(sacrificeUpgradeCost, new GUIContent("獻祭アップグレード花費"), true);
            EditorGUI.indentLevel--;
        }

        serializedObject.ApplyModifiedProperties();

    }
}

// 軍隊専用のEditor
[CustomEditor(typeof(MilitaryDataSO), true)]
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
        EditorGUILayout.PropertyField(serializedObject.FindProperty("skillAPCost"));
        EditorGUILayout.PropertyField(serializedObject.FindProperty("availableSkills"));

        EditorGUILayout.Space();
        EditorGUILayout.HelpBox("レベル0:初期、1:升級1、2:升級2、3:升級3", MessageType.Info);

        SerializedProperty attackPowerByLevel = serializedObject.FindProperty("attackPowerByLevel");
        if (attackPowerByLevel != null)
        {
            EditorGUILayout.PropertyField(attackPowerByLevel, new GUIContent("攻撃力レベル"), true);
            SerializedProperty attackPowerUpgradeCost = serializedObject.FindProperty("attackPowerUpgradeCost");
            if (attackPowerUpgradeCost != null)
            {
                EditorGUI.indentLevel++;
                EditorGUILayout.PropertyField(attackPowerUpgradeCost, new GUIContent("攻撃力アップグレード花費"), true);
                EditorGUI.indentLevel--;
            }
        }

        EditorGUILayout.PropertyField(serializedObject.FindProperty("hasAntiConversionSkill"), new GUIContent("魅惑耐性"), true);

        serializedObject.ApplyModifiedProperties();
    }
}

// 宣教師専用のEditor
[CustomEditor(typeof(MissionaryDataSO), true)]
public class MissionaryDataSOEditor : PieceDataSOEditor
{
    public override void OnInspectorGUI()
    {
        base.OnInspectorGUI();

        serializedObject.Update();

        EditorGUILayout.Space();
        EditorGUILayout.PropertyField(serializedObject.FindProperty("occupyAPCost"));

        EditorGUILayout.Space();
        EditorGUILayout.PropertyField(serializedObject.FindProperty("convertAPCost"));
        EditorGUILayout.PropertyField(serializedObject.FindProperty("conversionTurnDuration"));

        EditorGUILayout.Space();
        EditorGUILayout.PropertyField(serializedObject.FindProperty("baseConversionDefenseChance"));

        EditorGUILayout.Space();
        EditorGUILayout.HelpBox("レベル0:初期、1:升級1、2:升級2、3:升級3", MessageType.Info);

        EditorGUILayout.PropertyField(serializedObject.FindProperty("occupyEmptySuccessRateByLevel"), new GUIContent("空領地占領成功率"), true);
        EditorGUILayout.PropertyField(serializedObject.FindProperty("occupyEnemySuccessRateByLevel"), new GUIContent("敵領地占領成功率"), true);

        SerializedProperty occupyUpgradeCost = serializedObject.FindProperty("occupyUpgradeCost");
        if (occupyUpgradeCost != null)
        {
            EditorGUI.indentLevel++;
            EditorGUILayout.PropertyField(occupyUpgradeCost, new GUIContent("占領アップグレード花費"), true);
            EditorGUI.indentLevel--;
        }

        EditorGUILayout.PropertyField(serializedObject.FindProperty("convertMissionaryChanceByLevel"), new GUIContent("宣教師魅惑成功率"), true);
        EditorGUILayout.PropertyField(serializedObject.FindProperty("convertFarmerChanceByLevel"), new GUIContent("信徒魅惑成功率"), true);
        EditorGUILayout.PropertyField(serializedObject.FindProperty("convertMilitaryChanceByLevel"), new GUIContent("十字軍魅惑成功率"), true);

        SerializedProperty convertEnemyUpgradeCost = serializedObject.FindProperty("convertEnemyUpgradeCost");
        if (convertEnemyUpgradeCost != null)
        {
            EditorGUI.indentLevel++;
            EditorGUILayout.PropertyField(convertEnemyUpgradeCost, new GUIContent("魅惑アップグレード花費"), true);
            EditorGUI.indentLevel--;
        }

        EditorGUILayout.PropertyField(serializedObject.FindProperty("hasAntiConversionSkill"), new GUIContent("魅惑耐性"), true);

        serializedObject.ApplyModifiedProperties();
    }
}

// 教皇専用のEditor
[CustomEditor(typeof(PopeDataSO), true)]
public class PopeDataSOEditor : PieceDataSOEditor
{
    public override void OnInspectorGUI()
    {
        base.OnInspectorGUI();

        serializedObject.Update();

        EditorGUILayout.Space();
        EditorGUILayout.PropertyField(serializedObject.FindProperty("swapCooldown"));
        SerializedProperty swapCooldownUpgradeCost = serializedObject.FindProperty("swapCooldownUpgradeCost");
        if (swapCooldownUpgradeCost != null)
        {
            EditorGUI.indentLevel++;
            EditorGUILayout.PropertyField(swapCooldownUpgradeCost, new GUIContent("位置交換CDアップグレード花費"), true);
            EditorGUI.indentLevel--;
        }

        EditorGUILayout.Space();
        EditorGUILayout.PropertyField(serializedObject.FindProperty("hpBuff"));
        EditorGUILayout.PropertyField(serializedObject.FindProperty("atkBuff"));
        EditorGUILayout.PropertyField(serializedObject.FindProperty("convertBuff"));
        SerializedProperty buffUpgradeCost = serializedObject.FindProperty("buffUpgradeCost");
        if (buffUpgradeCost != null)
        {
            EditorGUI.indentLevel++;
            EditorGUILayout.PropertyField(buffUpgradeCost, new GUIContent("バフアップグレード花費"), true);
            EditorGUI.indentLevel--;
        }

        serializedObject.ApplyModifiedProperties();
    }
}

