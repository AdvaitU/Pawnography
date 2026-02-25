#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

/// <summary>
/// Custom Inspector for CardDatabase.
/// Adds an "Auto-Populate Cards" button that scans Assets/ScriptableObjects/Cards/
/// and adds all CardData assets found to the allCards list automatically.
/// This is an Editor-only script and has no effect on builds.
/// </summary>
[CustomEditor(typeof(CardDatabase))]
public class CardDatabaseEditor : Editor
{
    public override void OnInspectorGUI()
    {
        // Draw the default inspector fields as normal
        DrawDefaultInspector();

        CardDatabase database = (CardDatabase)target;

        EditorGUILayout.Space(10);
        EditorGUILayout.LabelField("── Editor Tools ──", EditorStyles.boldLabel);

        // ── Auto-populate Cards button ──
        if (GUILayout.Button("Auto-Populate Cards from ScriptableObjects/Cards/", GUILayout.Height(40)))
        {
            AutoPopulateCards(database);
        }

        // ── Auto-populate Categories button ──
        if (GUILayout.Button("Auto-Populate Categories from ScriptableObjects/Categories/", GUILayout.Height(40)))
        {
            AutoPopulateCategories(database);
        }

        // ── Clear list buttons ──
        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("Clear Cards List"))
        {
            if (EditorUtility.DisplayDialog("Clear Cards List",
                "Are you sure you want to clear the allCards list?", "Yes", "Cancel"))
            {
                Undo.RecordObject(database, "Clear Cards List");
                database.allCards.Clear();
                EditorUtility.SetDirty(database);
                Debug.Log("[CardDatabaseEditor] allCards list cleared.");
            }
        }
        if (GUILayout.Button("Clear Categories List"))
        {
            if (EditorUtility.DisplayDialog("Clear Categories List",
                "Are you sure you want to clear the allCategories list?", "Yes", "Cancel"))
            {
                Undo.RecordObject(database, "Clear Categories List");
                database.allCategories.Clear();
                EditorUtility.SetDirty(database);
                Debug.Log("[CardDatabaseEditor] allCategories list cleared.");
            }
        }
        EditorGUILayout.EndHorizontal();
    }

    /// <summary>
    /// Scans Assets/ScriptableObjects/Cards/ for all CardData assets
    /// and adds any that aren't already in the list.
    /// </summary>
    private void AutoPopulateCards(CardDatabase database)
    {
        Undo.RecordObject(database, "Auto-Populate Cards");

        // Find all CardData assets anywhere under Assets/ScriptableObjects/Cards/
        string[] guids = AssetDatabase.FindAssets("t:CardData", new[] { "Assets/ScriptableObjects/Cards" });

        int added = 0;
        int skipped = 0;

        foreach (string guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            CardData card = AssetDatabase.LoadAssetAtPath<CardData>(path);

            if (card == null) continue;

            if (!database.allCards.Contains(card))
            {
                database.allCards.Add(card);
                added++;
            }
            else
            {
                skipped++;
            }
        }

        EditorUtility.SetDirty(database);
        Debug.Log($"[CardDatabaseEditor] Auto-populate complete. Added {added} cards, skipped {skipped} already in list.");
    }

    /// <summary>
    /// Scans Assets/ScriptableObjects/Categories/ for all CardCategory assets
    /// and adds any that aren't already in the list.
    /// </summary>
    private void AutoPopulateCategories(CardDatabase database)
    {
        Undo.RecordObject(database, "Auto-Populate Categories");

        string[] guids = AssetDatabase.FindAssets("t:CardCategory", new[] { "Assets/ScriptableObjects/Categories" });

        int added = 0;
        int skipped = 0;

        foreach (string guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            CardCategory category = AssetDatabase.LoadAssetAtPath<CardCategory>(path);

            if (category == null) continue;

            if (!database.allCategories.Contains(category))
            {
                database.allCategories.Add(category);
                added++;
            }
            else
            {
                skipped++;
            }
        }

        EditorUtility.SetDirty(database);
        Debug.Log($"[CardDatabaseEditor] Auto-populate complete. Added {added} categories, skipped {skipped} already in list.");
    }
}
#endif