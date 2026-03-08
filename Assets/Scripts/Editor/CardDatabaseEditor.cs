/*
 * ============================================================
 * SCRIPT:      CardDatabaseEditor.cs
 * GAMEOBJECT:  Editor only — not present in any scene or build.
 *              Extends the Inspector for the CardDatabase
 *              component on GameManager.
 * ------------------------------------------------------------
 * FUNCTION:
 *   Custom Unity Editor Inspector for CardDatabase. Adds two
 *   auto-populate buttons that scan the ScriptableObjects/Cards/
 *   and ScriptableObjects/Categories/ folders respectively and
 *   add all found assets to the allCards and allCategories lists,
 *   skipping any already present.
 * ------------------------------------------------------------
 * REFERENCED BY:
 *   Unity Editor only
 * ------------------------------------------------------------
 * METHODS CALLED BY OTHER SCRIPTS:   None 
 * ------------------------------------------------------------
 * OPTIMISATION NOTES:
 *   Wrapped in #if UNITY_EDITOR — zero impact on runtime or 
 *   build size.
 * ============================================================
 */

#if UNITY_EDITOR                // Script is stripped from builds entirely - only runs in Editor
using UnityEngine;
using UnityEditor;
using System.Collections.Generic;


[CustomEditor(typeof(CardDatabase))]
public class CardDatabaseEditor : Editor    // Inherits from Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();         // Draw the default inspector fields as normal

        CardDatabase database = (CardDatabase)target;

        EditorGUILayout.Space(10);
        EditorGUILayout.LabelField("Editor Tools", EditorStyles.boldLabel);

        // Buttons ---------------------------------------------------------------------
        // Categories button
        if (GUILayout.Button("Auto-Populate Categories from ScriptableObjects/Categories/", GUILayout.Height(40)))
        {
            AutoPopulateCategories(database);
        }
        // Cards button
        if (GUILayout.Button("Auto-Populate Cards from ScriptableObjects/Cards/", GUILayout.Height(40)))
        {
            AutoPopulateCards(database);
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

    // METHODS ================================================================================================
    // AutoPopulateCards() ----------------------------------------------
    // Takes CardDatabase object as argument. Called in OnInspectorGUI() when button is clicked.
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

    // AutoPopulateCategories() -----------------------------------------------------------------
    // Takes CardDatabase object as argument. Called in OnInspectorGUI() when button is clicked.
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