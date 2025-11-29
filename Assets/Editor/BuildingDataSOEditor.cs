using UnityEngine;
using UnityEditor;
using GameData;

[CustomEditor(typeof(BuildingDataSO), true)]
public class BuildingDataSOEditor : Editor
{
    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        EditorGUILayout.PropertyField(serializedObject.FindProperty("religion"));
        EditorGUILayout.PropertyField(serializedObject.FindProperty("buildingName"));
        EditorGUILayout.PropertyField(serializedObject.FindProperty("maxHp"));
        EditorGUILayout.PropertyField(serializedObject.FindProperty("buildingResourceCost"));
        EditorGUILayout.PropertyField(serializedObject.FindProperty("resourceGenInterval"));
        EditorGUILayout.PropertyField(serializedObject.FindProperty("cellType"));

        EditorGUILayout.Space();

        EditorGUILayout.PropertyField(serializedObject.FindProperty("isSpecialBuilding"));
        EditorGUILayout.PropertyField(serializedObject.FindProperty("maxUnitCapacity"));

        EditorGUILayout.Space();

        EditorGUILayout.PropertyField(serializedObject.FindProperty("generationType"));
        EditorGUILayout.PropertyField(serializedObject.FindProperty("baseProductionAmount"));
        EditorGUILayout.PropertyField(serializedObject.FindProperty("goldenProductionAmount"));
        EditorGUILayout.PropertyField(serializedObject.FindProperty("productionMultiplier"));

        EditorGUILayout.Space();

        EditorGUILayout.HelpBox("レベル0:初期、1:升級1、2:升級2\n升級1: 攻撃範囲1獲得\n升級2: 攻撃範囲2、スロット数5に増加", MessageType.Info);

        EditorGUILayout.PropertyField(serializedObject.FindProperty("maxHpByLevel"), new GUIContent("最大HP"), true);
        SerializedProperty hpUpgradeCost = serializedObject.FindProperty("hpUpgradeCost");
        if (hpUpgradeCost != null)
        {
            EditorGUI.indentLevel++;
            EditorGUILayout.PropertyField(hpUpgradeCost, new GUIContent("血量アップグレード花費"), true);
            EditorGUI.indentLevel--;
        }

        EditorGUILayout.PropertyField(serializedObject.FindProperty("attackRangeByLevel"), new GUIContent("攻撃範囲"), true);
        SerializedProperty attackRangeUpgradeCost = serializedObject.FindProperty("attackRangeUpgradeCost");
        if (attackRangeUpgradeCost != null)
        {
            EditorGUI.indentLevel++;
            EditorGUILayout.PropertyField(attackRangeUpgradeCost, new GUIContent("攻撃範囲アップグレード花費"), true);
            EditorGUI.indentLevel--;
        }

        EditorGUILayout.PropertyField(serializedObject.FindProperty("maxSlotsByLevel"), new GUIContent("最大スロット数"), true);
        SerializedProperty slotsUpgradeCost = serializedObject.FindProperty("slotsUpgradeCost");
        if (slotsUpgradeCost != null)
        {
            EditorGUI.indentLevel++;
            EditorGUILayout.PropertyField(slotsUpgradeCost, new GUIContent("祭壇格子数アップグレード花費"), true);
            EditorGUI.indentLevel--;
        }

        EditorGUILayout.Space();

        // Prefabのみを使用（外見はPrefabに直接設定）
        EditorGUILayout.PropertyField(serializedObject.FindProperty("buildingPrefab"));

        serializedObject.ApplyModifiedProperties();
    }
}
