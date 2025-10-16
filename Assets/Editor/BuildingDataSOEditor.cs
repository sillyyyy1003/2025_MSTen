using UnityEngine;
using UnityEditor;
using GameData;

[CustomEditor(typeof(BuildingDataSO), true)]
public class BuildingDataSOEditor : Editor
{
    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        EditorGUILayout.PropertyField(serializedObject.FindProperty("buildingName"));
        EditorGUILayout.PropertyField(serializedObject.FindProperty("maxHp"));
        EditorGUILayout.PropertyField(serializedObject.FindProperty("buildStartAPCost"));
        EditorGUILayout.PropertyField(serializedObject.FindProperty("buildingAPCost"));
        EditorGUILayout.PropertyField(serializedObject.FindProperty("resourceGenInterval"));

        EditorGUILayout.Space();

        EditorGUILayout.PropertyField(serializedObject.FindProperty("isSpecialBuilding"));
        EditorGUILayout.PropertyField(serializedObject.FindProperty("maxUnitCapacity"));

        EditorGUILayout.Space();

        EditorGUILayout.PropertyField(serializedObject.FindProperty("generationType"));
        EditorGUILayout.PropertyField(serializedObject.FindProperty("baseProductionAmount"));
        EditorGUILayout.PropertyField(serializedObject.FindProperty("apCostperTurn"));
        EditorGUILayout.PropertyField(serializedObject.FindProperty("productionMultiplier"));

        EditorGUILayout.Space();

        EditorGUILayout.HelpBox("レベル0:初期、1:升級1、2:升級2\n升級1: 攻撃範囲1獲得\n升級2: 攻撃範囲2、スロット数5に増加", MessageType.Info);

        EditorGUILayout.PropertyField(serializedObject.FindProperty("maxHpByLevel"), new GUIContent("最大HP"), true);
        EditorGUILayout.PropertyField(serializedObject.FindProperty("attackRangeByLevel"), new GUIContent("攻撃範囲"), true);
        EditorGUILayout.PropertyField(serializedObject.FindProperty("maxSlotsByLevel"), new GUIContent("最大スロット数"), true);
        EditorGUILayout.PropertyField(serializedObject.FindProperty("buildingAPCostByLevel"), new GUIContent("建築APコスト"), true);

        EditorGUILayout.Space();

        EditorGUILayout.PropertyField(serializedObject.FindProperty("buildingSprite"));
        EditorGUILayout.PropertyField(serializedObject.FindProperty("buildingMesh"));
        EditorGUILayout.PropertyField(serializedObject.FindProperty("buildingMaterial"));
        EditorGUILayout.PropertyField(serializedObject.FindProperty("buildingPrefab"));
        EditorGUILayout.PropertyField(serializedObject.FindProperty("buildingColor"));
        EditorGUILayout.PropertyField(serializedObject.FindProperty("animatorController"));

        serializedObject.ApplyModifiedProperties();
    }
}
