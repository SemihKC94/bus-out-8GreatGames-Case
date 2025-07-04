using UnityEditor;
using UnityEngine;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using SKC.Grid;
using SKC.Bus;
using SKC.GameLogic;
using SKC.Level;

public class LevelAndBusEditorWindow : EditorWindow
{
    #region Variables
    
    private bool isLevelLoaded = false;
    private LevelData currentlyEditingLevelData;
    private string currentlyEditingLevelPath = "";
    
    private GridLevelData currentGridData;
    private BusLevelData currentBusData;
    private DestinationLevelData currentDestinationData;
    private float currentLevelDuration = 60.0f;
    
    private string gridDataPath, busDataPath, destinationDataPath;
    
    private enum EditorMode { GridPainting, BusPlacement, DestinationPlacement }
    private EditorMode currentMode = EditorMode.GridPainting;
    private GridContentType activeBrushType = GridContentType.Empty;
    private BusColor currentSharedColor = BusColor.Blue;
    private bool isPlacingBus = false;
    private List<GridPosition> currentBusPath = new List<GridPosition>();

    private Vector2 scrollPosition;
    #endregion

    #region GUIStyles
    private GUIStyle nothingCellStyle, emptyCellStyle, blockedCellStyle;
    private GUIStyle headCellStyle_Legend, bodyCellStyle_Legend, tailCellStyle_Legend, destinationCellStyle_Legend;
    #endregion

    [MenuItem("SKC/Level Editor V2")]
    public static void ShowWindow()
    {
        GetWindow<LevelAndBusEditorWindow>("Level Editor V2");
    }

    #region Unity Methods
    private void OnEnable()
    {
        isLevelLoaded = false;
        currentlyEditingLevelData = null;
    }

    private void OnGUI()
    {
        if (nothingCellStyle == null) InitializeGUIStyles();
        
        scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);

        if (!isLevelLoaded)
        {
            DrawWelcomeScreen();
        }
        else
        {
            DrawMainEditor();
        }

        EditorGUILayout.EndScrollView();
    }
    #endregion

    #region Main UI Sections
    private void DrawWelcomeScreen()
    {
        EditorGUILayout.LabelField("Level Editor", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox("Choose an option to begin.", MessageType.Info);
        EditorGUILayout.Space(20);

        if (GUILayout.Button("Edit Existing Level", GUILayout.Height(50)))
        {
            PromptToLoadLevelData();
        }

        EditorGUILayout.Space(10);

        if (GUILayout.Button("Create New Level", GUILayout.Height(50)))
        {
            SetupNewLevel();
        }
    }

    private void DrawMainEditor()
    {
        EditorGUILayout.BeginHorizontal();
        string editingLabel = currentlyEditingLevelData != null ? $"Editing: {currentlyEditingLevelData.name}" : "Editing: New Level";
        EditorGUILayout.LabelField(editingLabel, EditorStyles.boldLabel);
        if (GUILayout.Button("Close Level", GUILayout.Width(100)))
        {
            isLevelLoaded = false;
            currentlyEditingLevelData = null;
            return;
        }
        EditorGUILayout.EndHorizontal();
        
        EditorGUILayout.Space();
        
        DrawTopLevelSettingsUI();

        EditorGUILayout.LabelField("Editor Mode", EditorStyles.boldLabel);
        currentMode = (EditorMode)EditorGUILayout.EnumPopup("Current Mode", currentMode);
        EditorGUILayout.Space();

        switch (currentMode)
        {
            case EditorMode.GridPainting: DrawGridPaintingUI(); break;
            case EditorMode.BusPlacement: DrawBusEditingUI(); break;
            case EditorMode.DestinationPlacement: DrawDestinationEditingUI(); break;
        }

        if (currentGridData != null && currentGridData.cellTypes != null && currentGridData.cellTypes.Length > 0)
        {
            DrawGridButtons();
        }
        else
        {
            EditorGUILayout.HelpBox("Grid data is not initialized. Set a grid size.", MessageType.Warning);
        }
        
        EditorGUILayout.Space();
        DrawLegend();
        EditorGUILayout.Space();

        GUI.backgroundColor = new Color(0.6f, 1f, 0.6f);
        if (GUILayout.Button("Create / Update LevelData Asset", GUILayout.Height(40)))
        {
            SaveAndCreateLevelDataAsset();
        }
        GUI.backgroundColor = Color.white;
    }
    #endregion

    #region Workflow Logic
    private void PromptToLoadLevelData()
    {
        string path = EditorUtility.OpenFilePanel("Load Level Data Asset", "Assets/", "asset");
        if (string.IsNullOrEmpty(path)) return;

        if (path.StartsWith(Application.dataPath))
        {
            path = "Assets" + path.Substring(Application.dataPath.Length);
        }

        LevelData data = AssetDatabase.LoadAssetAtPath<LevelData>(path);
        if (data != null)
        {
            LoadDataFromScriptableObject(data, path);
        }
        else
        {
            EditorUtility.DisplayDialog("Error", "Selected file is not a valid LevelData asset.", "OK");
        }
    }

    private void LoadDataFromScriptableObject(LevelData data, string path)
    {
        currentlyEditingLevelData = data;
        currentlyEditingLevelPath = path;
        
        try
        {
            currentGridData = (data.gridData != null) ? JsonUtility.FromJson<GridLevelData>(data.gridData.text) : new GridLevelData();
            currentBusData = (data.busData != null) ? JsonUtility.FromJson<BusLevelData>(data.busData.text) : new BusLevelData();
            currentDestinationData = (data.destinationData != null) ? JsonUtility.FromJson<DestinationLevelData>(data.destinationData.text) : new DestinationLevelData();
            
            // Load extra data from the ScriptableObject
            currentLevelDuration = data.duration;

            isLevelLoaded = true;
            Repaint();
        }
        catch (System.Exception e)
        {
            EditorUtility.DisplayDialog("Error", $"Failed to parse data from LevelData asset '{data.name}'.\nError: {e.Message}", "OK");
            isLevelLoaded = false;
        }
    }

    private void SetupNewLevel()
    {
        currentGridData = new GridLevelData();
        currentGridData.Initialize(10, 10, 1f);
        currentBusData = new BusLevelData();
        currentDestinationData = new DestinationLevelData();
        
        // Reset to defaults
        currentLevelDuration = 60.0f;
        currentlyEditingLevelData = null;
        currentlyEditingLevelPath = "";
        gridDataPath = busDataPath = destinationDataPath = "";

        isLevelLoaded = true;
        Repaint();
    }
    
    private void SaveAndCreateLevelDataAsset()
    {
        string saveFolderPath;
        string levelName;
        
        bool isUpdating = currentlyEditingLevelData != null;

        if (isUpdating && !string.IsNullOrEmpty(currentlyEditingLevelPath))
        {
            saveFolderPath = Path.GetDirectoryName(currentlyEditingLevelPath);
            levelName = Path.GetFileNameWithoutExtension(currentlyEditingLevelPath);
        }
        else
        {
            saveFolderPath = EditorUtility.OpenFolderPanel("Select Folder to Save Level Files", "Assets/", "");
            if (string.IsNullOrEmpty(saveFolderPath)) return;

             if (saveFolderPath.StartsWith(Application.dataPath))
            {
                saveFolderPath = "Assets" + saveFolderPath.Substring(Application.dataPath.Length);
            }
            levelName = "NewLevel_" + System.DateTime.Now.ToString("yyyyMMdd_HHmmss");
        }

        gridDataPath = $"{saveFolderPath}/{levelName}_grid.json";
        busDataPath = $"{saveFolderPath}/{levelName}_bus.json";
        destinationDataPath = $"{saveFolderPath}/{levelName}_dest.json";

        File.WriteAllText(gridDataPath, JsonUtility.ToJson(currentGridData, true));
        File.WriteAllText(busDataPath, JsonUtility.ToJson(currentBusData, true));
        File.WriteAllText(destinationDataPath, JsonUtility.ToJson(currentDestinationData, true));
        
        AssetDatabase.Refresh(); 
        
        LevelData levelDataToSave;
        string soPath;

        if (isUpdating)
        {
            levelDataToSave = currentlyEditingLevelData;
            soPath = currentlyEditingLevelPath;
        }
        else
        {
            soPath = $"{saveFolderPath}/{levelName}.asset";
            levelDataToSave = ScriptableObject.CreateInstance<LevelData>();
            AssetDatabase.CreateAsset(levelDataToSave, soPath);
        }
        
        levelDataToSave.gridData = AssetDatabase.LoadAssetAtPath<TextAsset>(gridDataPath);
        levelDataToSave.busData = AssetDatabase.LoadAssetAtPath<TextAsset>(busDataPath);
        levelDataToSave.destinationData = AssetDatabase.LoadAssetAtPath<TextAsset>(destinationDataPath);
        
        levelDataToSave.duration = currentLevelDuration;
        levelDataToSave.CamPositionY = (1.5f * currentGridData.gridSizeX) + 1.5f; // Automatic calculation

        EditorUtility.SetDirty(levelDataToSave);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        EditorUtility.FocusProjectWindow();
        Selection.activeObject = levelDataToSave;
    }
    #endregion
    
    #region UI Drawing & Helper Methods
    
    private void InitializeGUIStyles()
    {
        nothingCellStyle = new GUIStyle(GUI.skin.button) { normal = { background = MakeTex(1, 1, new Color(0.2f, 0.2f, 0.2f)) } };
        emptyCellStyle = new GUIStyle(GUI.skin.button) { normal = { background = MakeTex(1, 1, new Color(0.7f, 0.7f, 0.7f)) } };
        blockedCellStyle = new GUIStyle(GUI.skin.button) { normal = { background = MakeTex(1, 1, new Color(0.3f, 0.3f, 0.3f)) } };
        headCellStyle_Legend = new GUIStyle { normal = { background = MakeTex(1, 1, Color.Lerp(Color.gray, Color.white, 0.3f)) } };
        bodyCellStyle_Legend = new GUIStyle { normal = { background = MakeTex(1, 1, Color.gray) } };
        tailCellStyle_Legend = new GUIStyle { normal = { background = MakeTex(1, 1, Color.Lerp(Color.gray, Color.black, 0.3f)) } };
        destinationCellStyle_Legend = new GUIStyle { normal = { background = MakeTex(1, 1, Color.cyan) } };
    }
    
    private void DrawTopLevelSettingsUI()
    {
        EditorGUILayout.LabelField("Level Settings", EditorStyles.boldLabel);
        
        EditorGUI.BeginChangeCheck();
        int newGridSizeX = EditorGUILayout.IntField("Grid Size X", currentGridData?.gridSizeX ?? 10);
        int newGridSizeY = EditorGUILayout.IntField("Grid Size Y", currentGridData?.gridSizeY ?? 10);
        if (EditorGUI.EndChangeCheck())
        {
            Undo.RecordObject(this, "Resize Grid");
            currentGridData.Initialize(newGridSizeX, newGridSizeY, currentGridData.cellSize);
            Repaint();
        }

        currentLevelDuration = EditorGUILayout.FloatField("Level Duration (seconds)", currentLevelDuration);

        EditorGUILayout.Space();
    }

    private void DrawGridPaintingUI()
    {
        EditorGUILayout.LabelField("Grid Painting", EditorStyles.boldLabel);
        DrawBrushSelection();
    }

    private void DrawBusEditingUI()
    {
        EditorGUILayout.LabelField("Bus Editing", EditorStyles.boldLabel);
        currentSharedColor = (BusColor)EditorGUILayout.EnumPopup("New Bus Color", currentSharedColor);

        if (isPlacingBus)
        {
            EditorGUILayout.HelpBox("Click on adjacent empty cells to draw the bus path.", MessageType.Info);
            EditorGUILayout.BeginHorizontal();
            GUI.backgroundColor = Color.green;
            if (GUILayout.Button("Finish Placing Bus")) FinishPlacingBus();
            GUI.backgroundColor = Color.red;
            if (GUILayout.Button("Cancel Placement")) CancelPlacingBus();
            GUI.backgroundColor = Color.white;
            EditorGUILayout.EndHorizontal();
        }
        else
        {
            if (GUILayout.Button("Start Placing New Bus")) StartPlacingBus();
        }

        EditorGUILayout.Space();
        if (currentBusData != null && currentBusData.buses.Count > 0)
        {
            EditorGUILayout.LabelField("Placed Buses:", EditorStyles.boldLabel);
            for (int i = 0; i < currentBusData.buses.Count; i++)
            {
                BusInfo bus = currentBusData.buses[i];
                if (bus == null) continue;
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField($"  - ID: {bus.busId}", GUILayout.Width(100));
                bus.busColor = (BusColor)EditorGUILayout.EnumPopup(bus.busColor);
                GUI.backgroundColor = new Color(1f, 0.6f, 0.6f);
                if (GUILayout.Button("X", GUILayout.Width(30)))
                {
                    Undo.RecordObject(this, "Delete Bus");
                    currentBusData.buses.RemoveAt(i);
                    Repaint();
                    break; 
                }
                GUI.backgroundColor = Color.white;
                EditorGUILayout.EndHorizontal();
            }
        }
    }

    private void DrawDestinationEditingUI()
    {
        EditorGUILayout.LabelField("Destination Editing", EditorStyles.boldLabel);
        currentSharedColor = (BusColor)EditorGUILayout.EnumPopup("Destination Color", currentSharedColor);
        EditorGUILayout.HelpBox("Click on an 'Empty' grid cell to place or remove a destination of the selected color.", MessageType.Info);

        EditorGUILayout.Space();
        if (currentDestinationData != null && currentDestinationData.destinations.Count > 0)
        {
            EditorGUILayout.LabelField("Placed Destinations:", EditorStyles.boldLabel);
            for (int i = 0; i < currentDestinationData.destinations.Count; i++)
            {
                DestinationData dest = currentDestinationData.destinations[i];
                if (dest == null) continue;
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField($"  - Color: {dest.targetColor}, Pos: {dest.position}");
                GUI.backgroundColor = new Color(1f, 0.6f, 0.6f);
                if (GUILayout.Button("X", GUILayout.Width(30)))
                {
                    Undo.RecordObject(this, "Delete Destination");
                    currentDestinationData.destinations.RemoveAt(i);
                    Repaint();
                    break;
                }
                GUI.backgroundColor = Color.white;
                EditorGUILayout.EndHorizontal();
            }
        }
    }

    private void DrawGridButtons()
    {
        float buttonSize = Mathf.Min(40, (position.width - 40) / currentGridData.gridSizeX);

        for (int y = currentGridData.gridSizeY - 1; y >= 0; y--)
        {
            EditorGUILayout.BeginHorizontal();
            for (int x = 0; x < currentGridData.gridSizeX; x++)
            {
                GridPosition currentPos = new GridPosition(x, y);
                Color originalGUIColor = GUI.backgroundColor;
                string buttonText = "";
                GUIStyle baseStyle = emptyCellStyle;

                DestinationData destOnCell = GetDestinationAtPosition(currentPos);
                BusPartData partOnCell = GetBusPartAtPosition(currentPos);

                if (destOnCell != null)
                {
                    GUI.backgroundColor = BusColorConverter.ToUnityColor(destOnCell.targetColor);
                    buttonText = "D";
                }
                else if (partOnCell != null)
                {
                    BusInfo bus = currentBusData.buses.FirstOrDefault(b => b.busId == partOnCell.busId);
                    if (bus != null) GUI.backgroundColor = BusColorConverter.ToUnityColor(bus.busColor);
                    if (partOnCell.partType == BusPartType.Head) GUI.backgroundColor = Color.Lerp(GUI.backgroundColor, Color.white, 0.3f);
                    if (partOnCell.partType == BusPartType.Tail) GUI.backgroundColor = Color.Lerp(GUI.backgroundColor, Color.black, 0.3f);
                }
                else if (isPlacingBus && currentBusPath.Contains(currentPos))
                {
                    GUI.backgroundColor = Color.magenta;
                }
                else
                {
                    baseStyle = GetGridCellStyle(currentGridData.GetCellType(currentPos));
                }

                if (GUILayout.Button(buttonText, baseStyle, GUILayout.Width(buttonSize), GUILayout.Height(buttonSize)))
                {
                    HandleCellClick(currentPos);
                }
                GUI.backgroundColor = originalGUIColor;
            }
            EditorGUILayout.EndHorizontal();
        }
    }

    private void DrawBrushSelection()
    {
        EditorGUILayout.BeginHorizontal();
        DrawBrushButton("Nothing", GridContentType.Nothing);
        DrawBrushButton("Empty", GridContentType.Empty);
        DrawBrushButton("Blocked", GridContentType.Blocked);
        EditorGUILayout.EndHorizontal();
    }
    
    private void DrawBrushButton(string label, GridContentType type)
    {
        bool isSelected = activeBrushType == type;
        GUI.backgroundColor = isSelected ? Color.yellow : Color.white;
        if (GUILayout.Button(label, GUILayout.Height(30)))
        {
            activeBrushType = type;
        }
        GUI.backgroundColor = Color.white;
    }

    private void DrawLegend()
    {
        EditorGUILayout.LabelField("Legend", EditorStyles.boldLabel);

        DrawLegendItem("Nothing", new Color(0.2f, 0.2f, 0.2f));
        DrawLegendItem("Empty", new Color(0.7f, 0.7f, 0.7f));
        DrawLegendItem("Blocked", new Color(0.3f, 0.3f, 0.3f));
        
        EditorGUILayout.Space(5);

        DrawLegendItem("Bus Body (Example)", Color.gray);
        DrawLegendItem("Bus Head (Lighter)", Color.Lerp(Color.gray, Color.white, 0.4f));
        DrawLegendItem("Bus Tail (Darker)", Color.Lerp(Color.gray, Color.black, 0.4f));
        
        EditorGUILayout.Space(5);

        DrawLegendItem("Destination Point ('D')", Color.cyan);
    }

    private void DrawLegendItem(string label, Color color)
    {
        EditorGUILayout.BeginHorizontal();
        
        Rect colorRect = EditorGUILayout.GetControlRect(GUILayout.Width(20), GUILayout.Height(20));
        EditorGUI.DrawRect(colorRect, color);
        Rect borderRect = new Rect(colorRect.x - 1, colorRect.y - 1, colorRect.width + 2, colorRect.height + 2);
        EditorGUI.DrawRect(borderRect, new Color(0.1f, 0.1f, 0.1f));
        EditorGUI.DrawRect(colorRect, color);
        EditorGUILayout.LabelField(label);
        
        EditorGUILayout.EndHorizontal();
    }
    
    private void HandleCellClick(GridPosition pos)
    {
        switch (currentMode)
        {
            case EditorMode.GridPainting:
                HandleGridPaintingClick(pos);
                break;
            case EditorMode.BusPlacement:
                if (isPlacingBus) HandleBusPathDrawingClick(pos);
                break;
            case EditorMode.DestinationPlacement:
                HandleDestinationPlacementClick(pos);
                break;
        }
    }

    private void HandleGridPaintingClick(GridPosition pos)
    {
        if (GetBusPartAtPosition(pos) != null || GetDestinationAtPosition(pos) != null) return;
        Undo.RecordObject(this, "Paint Grid Cell");
        currentGridData.UpdateCellType(pos, activeBrushType);
        Repaint();
    }

    private void HandleBusPathDrawingClick(GridPosition pos)
    {
        if (currentGridData.GetCellType(pos) != GridContentType.Empty || GetDestinationAtPosition(pos) != null) return;
        if (currentBusPath.Contains(pos)) return;

        if (currentBusPath.Count > 0)
        {
            GridPosition lastPos = currentBusPath.Last();
            if (Mathf.Abs(lastPos.x - pos.x) + Mathf.Abs(lastPos.y - pos.y) != 1) return;
        }
        currentBusPath.Add(pos);
        Repaint();
    }

    private void HandleDestinationPlacementClick(GridPosition pos)
    {
        if (currentGridData.GetCellType(pos) != GridContentType.Empty || GetBusPartAtPosition(pos) != null) return;
        Undo.RecordObject(this, "Place/Remove Destination");

        DestinationData existingDest = GetDestinationAtPosition(pos);
        if (existingDest != null)
        {
            currentDestinationData.destinations.Remove(existingDest);
        }
        else
        {
            int nextId = (currentDestinationData.destinations.Any()) ? currentDestinationData.destinations.Max(d => d.destinationId) + 1 : 1;
            currentDestinationData.destinations.Add(new DestinationData { destinationId = nextId, position = pos, targetColor = currentSharedColor });
        }
        Repaint();
    }
    
    private void StartPlacingBus()
    {
        isPlacingBus = true;
        currentBusPath.Clear();
    }
    private void FinishPlacingBus() 
    { 
        if (currentBusPath.Count < 2) { CancelPlacingBus(); return; }
        Undo.RecordObject(this, "Place New Bus");
        int nextId = (currentBusData.buses.Any()) ? currentBusData.buses.Max(b => b.busId) + 1 : 1;

        BusInfo newBus = new BusInfo { busId = nextId, busColor = currentSharedColor };
        newBus.parts.Add(new BusPartData { busId = nextId, partType = BusPartType.Head, position = currentBusPath.First() });
        for (int i = 1; i < currentBusPath.Count - 1; i++)
        {
            newBus.parts.Add(new BusPartData { busId = nextId, partType = BusPartType.Body, position = currentBusPath[i] });
        }
        newBus.parts.Add(new BusPartData { busId = nextId, partType = BusPartType.Tail, position = currentBusPath.Last() });
        currentBusData.buses.Add(newBus);
        CancelPlacingBus();
    }
    private void CancelPlacingBus() { isPlacingBus = false; currentBusPath.Clear(); Repaint(); }
    
    private GUIStyle GetGridCellStyle(GridContentType type)
    {
        switch (type)
        {
            case GridContentType.Nothing: return nothingCellStyle;
            case GridContentType.Empty: return emptyCellStyle;
            case GridContentType.Blocked: return blockedCellStyle;
            default: return emptyCellStyle;
        }
    }

    private BusPartData GetBusPartAtPosition(GridPosition pos)
    {
        return currentBusData?.buses.SelectMany(b => b.parts).FirstOrDefault(p => p.position.Equals(pos));
    }
    
    private DestinationData GetDestinationAtPosition(GridPosition pos)
    {
        return currentDestinationData?.destinations.FirstOrDefault(d => d.position.Equals(pos));
    }

    private Texture2D MakeTex(int width, int height, Color col)
    {
        Color[] pix = new Color[width * height];
        for (int i = 0; i < pix.Length; ++i) pix[i] = col;
        Texture2D result = new Texture2D(width, height);
        result.SetPixels(pix);
        result.Apply();
        return result;
    }
    #endregion
}