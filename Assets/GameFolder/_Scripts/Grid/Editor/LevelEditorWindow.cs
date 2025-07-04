// Old One
using UnityEditor;
using UnityEngine;
using System.IO;
using System.Linq;
using System.Collections.Generic; // Make sure this is here
using SKC.Grid; // GridLevelData için

public class LevelEditorWindow : EditorWindow
{
    private GridLevelData currentGridData; // Artık GridDataScriptableObject değil
    private string currentJsonFilePath = "";

    private int editorGridSizeX = 10;
    private int editorGridSizeY = 10;
    private float editorCellSize = 1f;

    private GridContentType activeBrushType = GridContentType.Empty;

    // GUIStyles (değişmedi, sadece isimler)
    private GUIStyle nothingCellStyle;
    private GUIStyle emptyCellStyle;
    private GUIStyle blockedCellStyle;
    private GUIStyle playerStartCellStyle;
    private GUIStyle targetCellStyle;
    private GUIStyle brushButtonStyle;
    private GUIStyle selectedBrushButtonStyle;
    
    public static void ShowWindow()
    {
        GetWindow<LevelEditorWindow>("Level Editor");
    }

    private void OnEnable()
    {
        editorGridSizeX = EditorPrefs.GetInt("LevelEditor_GridSizeX", 10);
        editorGridSizeY = EditorPrefs.GetInt("LevelEditor_GridSizeY", 10);
        editorCellSize = EditorPrefs.GetFloat("LevelEditor_CellSize", 1f);
        currentJsonFilePath = EditorPrefs.GetString("LevelEditor_LastJsonPath", "");

        if (!string.IsNullOrEmpty(currentJsonFilePath))
        {
            LoadGridDataFromJsonFile(currentJsonFilePath);
        }
    }

    private void OnDisable()
    {
        EditorPrefs.SetString("LevelEditor_LastJsonPath", currentJsonFilePath);
    }

    private void InitializeGUIStyles()
    {
        brushButtonStyle = new GUIStyle(GUI.skin.button);
        brushButtonStyle.fixedWidth = 50;
        brushButtonStyle.fixedHeight = 50;

        selectedBrushButtonStyle = new GUIStyle(brushButtonStyle);
        selectedBrushButtonStyle.normal.background = MakeTex(64, 64, Color.yellow);
        selectedBrushButtonStyle.hover.background = MakeTex(64, 64, Color.yellow + new Color(0.1f,0.1f,0.1f));
        selectedBrushButtonStyle.border = new RectOffset(5,5,5,5); 

        nothingCellStyle = new GUIStyle(GUI.skin.button);
        nothingCellStyle.normal.background = MakeTex(64, 64, new Color(0.2f, 0.2f, 0.2f));
        nothingCellStyle.active.background = MakeTex(64, 64, new Color(0.4f, 0.4f, 0.4f));
        nothingCellStyle.fixedWidth = 0;
        nothingCellStyle.fixedHeight = 0;

        emptyCellStyle = new GUIStyle(GUI.skin.button);
        emptyCellStyle.normal.background = MakeTex(64, 64, new Color(0.7f, 0.7f, 0.7f));
        emptyCellStyle.active.background = MakeTex(64, 64, new Color(0.9f, 0.9f, 0.9f));
        emptyCellStyle.fixedWidth = 0;
        emptyCellStyle.fixedHeight = 0;

        blockedCellStyle = new GUIStyle(GUI.skin.button);
        blockedCellStyle.normal.background = MakeTex(64, 64, new Color(0.3f, 0.3f, 0.3f));
        blockedCellStyle.active.background = MakeTex(64, 64, new Color(0.5f, 0.5f, 0.5f));
        blockedCellStyle.fixedWidth = 0;
        blockedCellStyle.fixedHeight = 0;

        playerStartCellStyle = new GUIStyle(GUI.skin.button);
        playerStartCellStyle.normal.background = MakeTex(64, 64, Color.Lerp(Color.red, Color.yellow, 0.5f));
        playerStartCellStyle.active.background = MakeTex(64, 64, Color.yellow);
        playerStartCellStyle.fixedWidth = 0;
        playerStartCellStyle.fixedHeight = 0;

        targetCellStyle = new GUIStyle(GUI.skin.button);
        targetCellStyle.normal.background = MakeTex(64, 64, Color.blue); 
        targetCellStyle.active.background = MakeTex(64, 64, Color.cyan);
        targetCellStyle.fixedWidth = 0;
        targetCellStyle.fixedHeight = 0;
    }

    public void OnGUI()
    {
        if (nothingCellStyle == null || nothingCellStyle.normal.background == null) InitializeGUIStyles();

        EditorGUILayout.LabelField("Grid Data Settings", EditorStyles.boldLabel);

        EditorGUILayout.BeginHorizontal();
        currentJsonFilePath = EditorGUILayout.TextField("JSON File Path", currentJsonFilePath);
        if (GUILayout.Button("Load JSON", GUILayout.Width(80)))
        {
            string path = EditorUtility.OpenFilePanel("Select Grid Data JSON", Application.dataPath, "json"); // Use Application.dataPath for project root
            if (!string.IsNullOrEmpty(path))
            {
                currentJsonFilePath = "Assets" + path.Substring(Application.dataPath.Length); // Convert to Unity-style path
                LoadGridDataFromJsonFile(currentJsonFilePath);
            }
        }
        if (GUILayout.Button("New JSON", GUILayout.Width(80)))
        {
            CreateNewGridDataJsonFile();
        }
        EditorGUILayout.EndHorizontal();

        if (currentGridData == null)
        {
            EditorGUILayout.HelpBox("No grid data loaded. Load an existing JSON or create a new one.", MessageType.Info);
            return;
        }

        // --- Grid Dimension Inputs ---
        EditorGUI.BeginChangeCheck();
        int newEditorGridSizeX = EditorGUILayout.IntField("Grid Size X", editorGridSizeX);
        int newEditorGridSizeY = EditorGUILayout.IntField("Grid Size Y", editorGridSizeY);
        float newEditorCellSize = EditorGUILayout.FloatField("Cell Size", editorCellSize);
        if (EditorGUI.EndChangeCheck())
        {
            editorGridSizeX = newEditorGridSizeX;
            editorGridSizeY = newEditorGridSizeY;
            editorCellSize = newEditorCellSize;

            EditorPrefs.SetInt("LevelEditor_GridSizeX", editorGridSizeX);
            EditorPrefs.SetInt("LevelEditor_GridSizeY", editorGridSizeY);
            EditorPrefs.SetFloat("LevelEditor_CellSize", editorCellSize);
            
            // Revalidate size on the currentGridData object.
            currentGridData.RevalidateSize(editorGridSizeX, editorGridSizeY, editorCellSize);
        }

        // --- Sync Editor Fields with Loaded Data ---
        // Accessing properties directly to avoid issues with null checks and potential outdated local fields.
        if (currentGridData.gridSizeX != editorGridSizeX || currentGridData.gridSizeY != editorGridSizeY || currentGridData.cellSize != editorCellSize)
        {
            editorGridSizeX = currentGridData.gridSizeX;
            editorGridSizeY = currentGridData.gridSizeY;
            editorCellSize = currentGridData.cellSize;
            Repaint();
        }
        
        // --- Initialize/Validate Grid Data ---
        // Check currentGridData.cellTypes.Length
        if (currentGridData.cellTypes == null || currentGridData.cellTypes.Length == 0 || currentGridData.cellTypes.Length != currentGridData.gridSizeX * currentGridData.gridSizeY)
        {
            EditorGUILayout.HelpBox("Grid data is empty or corrupted. Please Initialize to create a full grid.", MessageType.Warning);
            if (GUILayout.Button("Initialize Grid Data (All Nothing)"))
            {
                currentGridData.Initialize(editorGridSizeX, editorGridSizeY, editorCellSize);
            }
        }

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Grid Editing", EditorStyles.boldLabel);

        // --- Brush Selection Area ---
        DrawBrushSelection();

        EditorGUILayout.Space();

        // --- Grid Drawing Area ---
        DrawGridButtons();

        EditorGUILayout.Space();

        // --- Legend Section ---
        EditorGUILayout.LabelField("Legend", EditorStyles.boldLabel);
        DrawLegendItem("Nothing (Not part of grid shape)", nothingCellStyle);
        DrawLegendItem("Empty (Walkable Cell)", emptyCellStyle);
        DrawLegendItem("Blocked (Non-walkable Obstacle)", blockedCellStyle);
        DrawLegendItem("Player Start Point", playerStartCellStyle);
        DrawLegendItem("Target Point", targetCellStyle);

        EditorGUILayout.Space();

        if (GUILayout.Button("Save Grid Data to JSON"))
        {
            SaveGridDataToJsonFile();
        }
    }

    private void LoadGridDataFromJsonFile(string path)
    {
        if (string.IsNullOrEmpty(path)) return;

        if (File.Exists(path))
        {
            try
            {
                string json = File.ReadAllText(path);
                currentGridData = JsonUtility.FromJson<GridLevelData>(json);
                if (currentGridData == null)
                {
                    Debug.LogError($"SKC.Grid.LevelEditorWindow: Failed to parse JSON. Parsed data is null. Check JSON format of {path}.");
                    return;
                }
                // Sync editor dimensions with loaded data immediately after loading.
                editorGridSizeX = currentGridData.gridSizeX;
                editorGridSizeY = currentGridData.gridSizeY;
                editorCellSize = currentGridData.cellSize;
                currentJsonFilePath = path;
                Debug.Log($"Loaded grid data from: {path}");
            }
            catch (System.Exception e)
            {
                Debug.LogError($"SKC.Grid.LevelEditorWindow: Failed to load grid data from {path}: {e.Message}");
                currentGridData = null;
            }
        }
        else
        {
            Debug.LogWarning($"SKC.Grid.LevelEditorWindow: File not found at: {path}. Please check the path or create a new file.");
            currentGridData = null;
        }
    }

    private void SaveGridDataToJsonFile()
    {
        if (currentGridData == null)
        {
            Debug.LogError("SKC.Grid.LevelEditorWindow: No grid data to save.");
            return;
        }

        if (string.IsNullOrEmpty(currentJsonFilePath) || !File.Exists(currentJsonFilePath))
        {
            string newPath = EditorUtility.SaveFilePanelInProject("Save Grid Data to JSON", "NewGridLevelData", "json", "Save grid data to a JSON file.");
            if (string.IsNullOrEmpty(newPath))
            {
                Debug.Log("SKC.Grid.LevelEditorWindow: Save operation cancelled.");
                return;
            }
            currentJsonFilePath = newPath;
        }
        
        try
        {
            // JsonUtility needs the object to be passed by value, not by reference from the Inspector,
            // to ensure it takes the current state.
            string json = JsonUtility.ToJson(currentGridData, true); // true = pretty print
            File.WriteAllText(currentJsonFilePath, json);
            Debug.Log($"SKC.Grid.LevelEditorWindow: Grid Data saved to: {currentJsonFilePath}");
            AssetDatabase.Refresh(); // Notify Unity about the new/changed file
        }
        catch (System.Exception e)
        {
            Debug.LogError($"SKC.Grid.LevelEditorWindow: Failed to save grid data to {currentJsonFilePath}: {e.Message}");
        }
    }

    private void CreateNewGridDataJsonFile()
    {
        currentGridData = new GridLevelData();
        currentGridData.Initialize(editorGridSizeX, editorGridSizeY, editorCellSize);
        currentJsonFilePath = ""; // Clear path until saved
        Debug.Log("SKC.Grid.LevelEditorWindow: New blank grid data created. Please save it to a JSON file.");
    }

    // DrawBrushSelection, DrawBrushButton, DrawLegendItem, MakeTex (değişmedi)
    private void DrawBrushSelection()
    {
        EditorGUILayout.BeginHorizontal();
        DrawBrushButton("Nothing", GridContentType.Nothing, nothingCellStyle);
        DrawBrushButton("Empty", GridContentType.Empty, emptyCellStyle);
        DrawBrushButton("Blocked", GridContentType.Blocked, blockedCellStyle);
        DrawBrushButton("Start", GridContentType.PlayerStart, playerStartCellStyle);
        DrawBrushButton("Target", GridContentType.Target, targetCellStyle);
        EditorGUILayout.EndHorizontal();
    }

    private void DrawBrushButton(string label, GridContentType type, GUIStyle style)
    {
        GUIStyle currentStyle = (activeBrushType == type) ? selectedBrushButtonStyle : brushButtonStyle;
        currentStyle.normal.background = style.normal.background;
        currentStyle.hover.background = style.hover.background;

        if (GUILayout.Button(label, currentStyle))
        {
            activeBrushType = type;
        }
    }

    private void DrawGridButtons()
    {
        // Safety check: Only draw buttons if data is properly initialized.
        if (currentGridData == null || currentGridData.cellTypes == null || currentGridData.cellTypes.Length == 0 || currentGridData.cellTypes.Length != currentGridData.gridSizeX * currentGridData.gridSizeY)
        {
            EditorGUILayout.HelpBox("Grid data is invalid for drawing. Please initialize or load correctly.", MessageType.Error);
            return;
        }

        float buttonSize = (position.width - 20) / currentGridData.gridSizeX;
        buttonSize = Mathf.Min(buttonSize, 40);

        for (int y = currentGridData.gridSizeY - 1; y >= 0; y--)
        {
            EditorGUILayout.BeginHorizontal();
            for (int x = 0; x < currentGridData.gridSizeX; x++)
            {
                GridPosition currentPos = new GridPosition(x, y);
                GridContentType cellType = currentGridData.GetCellType(currentPos); 
                
                GUIStyle cellStyle;
                switch (cellType)
                {
                    case GridContentType.Nothing: cellStyle = nothingCellStyle; break;
                    case GridContentType.Empty: cellStyle = emptyCellStyle; break;
                    case GridContentType.Blocked: cellStyle = blockedCellStyle; break;
                    case GridContentType.PlayerStart: cellStyle = playerStartCellStyle; break;
                    case GridContentType.Target: cellStyle = targetCellStyle; break;
                    default: cellStyle = GUI.skin.button; break;
                }

                if (GUILayout.Button("", cellStyle, GUILayout.Width(buttonSize), GUILayout.Height(buttonSize)))
                {
                    Undo.RecordObject(this, "Change Cell Type"); // Undo for the EditorWindow itself
                    
                    GridContentType newType = activeBrushType;

                    // Specific handling for PlayerStart and Target (only one allowed)
                    if (newType == GridContentType.PlayerStart)
                    {
                        // Find existing PlayerStart and change its type to Empty
                        int existingPlayerStartIndex = -1;
                        for(int i=0; i<currentGridData.cellTypes.Length; i++)
                        {
                            if (currentGridData.cellTypes[i] == GridContentType.PlayerStart)
                            {
                                existingPlayerStartIndex = i;
                                break;
                            }
                        }
                        if (existingPlayerStartIndex != -1)
                        {
                            // Get GridPosition from index to use UpdateCellType
                            GridPosition oldPlayerStartPos = new GridPosition(existingPlayerStartIndex % currentGridData.gridSizeX, existingPlayerStartIndex / currentGridData.gridSizeX);
                            currentGridData.UpdateCellType(oldPlayerStartPos, GridContentType.Empty);
                        }
                    }
                    else if (newType == GridContentType.Target)
                    {
                        // Find existing Target and change its type to Empty
                        int existingTargetIndex = -1;
                        for(int i=0; i<currentGridData.cellTypes.Length; i++)
                        {
                            if (currentGridData.cellTypes[i] == GridContentType.Target)
                            {
                                existingTargetIndex = i;
                                break;
                            }
                        }
                        if (existingTargetIndex != -1)
                        {
                             GridPosition oldTargetPos = new GridPosition(existingTargetIndex % currentGridData.gridSizeX, existingTargetIndex / currentGridData.gridSizeX);
                            currentGridData.UpdateCellType(oldTargetPos, GridContentType.Empty);
                        }
                    }

                    currentGridData.UpdateCellType(currentPos, newType); // Update the clicked cell's type
                    // No need for EditorUtility.SetDirty(this) after each cell change, only on Save.
                }
            }
            EditorGUILayout.EndHorizontal();
        }
    }

    private void DrawLegendItem(string label, GUIStyle style)
    {
        EditorGUILayout.BeginHorizontal();
        GUILayout.Box("", style, GUILayout.Width(20), GUILayout.Height(20));
        EditorGUILayout.LabelField(label);
        EditorGUILayout.EndHorizontal();
    }

    private Texture2D MakeTex(int width, int height, Color col)
    {
        Color[] pix = new Color[width * height];
        for (int i = 0; i < pix.Length; i++)
        {
            pix[i] = col;
        }
        Texture2D result = new Texture2D(width, height);
        result.SetPixels(pix);
        result.Apply();
        return result;
    }
}