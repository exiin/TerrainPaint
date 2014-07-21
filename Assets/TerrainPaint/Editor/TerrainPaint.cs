using UnityEngine;
using UnityEditor;
using System.Collections;
using System.IO;
using System;

[CustomEditor(typeof (TerrainScript))]
public class TerrainPaint : Editor
{
    private static string appTitle = "Terrain painting tools v.3.2";

    // global variables
    private bool needToSave = false;
    private Texture2D mixMap;
    private Material currentMaterial;
    private GameObject targetObject;
    private Material mat;

    // paint variable
    private static Texture2D brushTexture;
    private static float brushSize = 2;
    private static float hardness = 1f;
    private static bool isPainting = false; 
    private TextureBrush currentTextureBrush;
    private static int currentSelectedBrush = 0;
    private static int currentSelectedTexture = 1; 
    private Texture2D[] sBrushTextures;
    private Texture2D[] sTextures; 
    private GameObject newPrototype;

    public static int AspectSelectionGrid(int selected, Texture[] textures, int approxSize, GUIStyle style,
                                          string emptyString, out bool doubleClick)
    {
        var options = new GUILayoutOption[] {GUILayout.MinHeight(10f)};
        GUILayout.BeginVertical("box", options);
        doubleClick = false;
        var num = 0;
        if (textures != null)
        {
            if (textures.Length != 0)
            {
                var num2 = (Screen.width - 20)/approxSize;
                var num3 = (int) Mathf.Ceil(((float) textures.Length)/num2);
                var aspectRect = GUILayoutUtility.GetAspectRect(num2/((float) num3));
                var current = Event.current;
                if (((current.type == EventType.MouseDown) && (current.clickCount == 2)) &&
                    aspectRect.Contains(current.mousePosition))
                {
                    doubleClick = true;
                    current.Use();
                }
                num = GUI.SelectionGrid(aspectRect, selected, textures, (Screen.width - 20)/approxSize, style);
            }
            else
            {
                GUILayout.Label(emptyString, new GUILayoutOption[0]);
            }
        }
        GUILayout.EndVertical();

        return num;
    }

    private void CreateMixTexture()
    {
        if (needToSave)
        {
            var path = EditorUtility.SaveFilePanel("Save texture as?", "", "mixTexture.png", "png");
            if (path.Length != 0)
            {
                mixMap = new Texture2D(256, 256);
                var c = new Color[256*256];
                for (var i = 0; i < c.Length; i++)
                {
                    c[i] = new Color(0, 0, 0, 0);
                }
                mixMap.SetPixels(c);
                var data = mixMap.EncodeToPNG();
                File.WriteAllBytes(path, data);
                AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
                path = path.Substring(path.IndexOf("Asset"));
                mixMap = (Texture2D) AssetDatabase.LoadAssetAtPath(path, typeof (Texture2D));
                var textureImporter = AssetImporter.GetAtPath(path) as TextureImporter;
                textureImporter.textureFormat = TextureImporterFormat.ARGB32;
                if (textureImporter.isReadable == false)
                {
                    textureImporter.isReadable = true;
                    AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);
                }
                currentMaterial.SetTexture("_Control", mixMap);
                needToSave = false;
            }
        }
        else
        {
            if (mixMap != null)
            {
                var path = AssetDatabase.GetAssetPath(mixMap);
                if (path.Length > 0)
                {
                    var textureBytes = mixMap.EncodeToPNG();
                    File.WriteAllBytes(path, textureBytes);
                }
                AssetDatabase.Refresh();
            }
        }
    }

    private void CheckMixTexture()
    {
        if (mixMap != null)
        {
            string p = AssetDatabase.GetAssetPath(mixMap);
            TextureImporter ti = TextureImporter.GetAtPath(p) as TextureImporter;
            ti.textureFormat = TextureImporterFormat.ARGB32;
            ti.isReadable = true;
            AssetDatabase.ImportAsset(p, ImportAssetOptions.ForceUpdate);
        }

    }

    private void ClearMap()
    {
        var path = AssetDatabase.GetAssetPath(mixMap);
        var c = new Color[512*512];
        for (var i = 0; i < c.Length; i++)
        {
            c[i] = new Color(0, 0, 0, 0);
        }
        mixMap.SetPixels(c);
        var textureBytes = mixMap.EncodeToPNG();
        File.WriteAllBytes(path, textureBytes);
        AssetDatabase.Refresh();

    }

    private void LoadTextures()
    {
        var list = new ArrayList();
        list.Add(currentMaterial.GetTexture("_Splat3") as Texture2D);
        list.Add(currentMaterial.GetTexture("_Splat2") as Texture2D);
        list.Add(currentMaterial.GetTexture("_Splat1") as Texture2D);
        list.Add(currentMaterial.GetTexture("_Splat0") as Texture2D);
        sTextures = list.ToArray(typeof (Texture2D)) as Texture2D[];
    }

    private void LoadBrushIcons()
    {

        //var texture = (Texture) EditorGUIUtility.Load("Brushes/builtin_brush_" + 1 + ".png");
        //Debug.Log(texture.name);
        //var skin = EditorGUIUtility.GetBuiltinSkin(EditorSkin.Scene);
        //foreach (var t in skin.customStyles)
        //{
        //    Debug.Log(t.name);
        //}

        
        var list = new ArrayList();
        var num = 1;
        Texture texture = null;
        do
        {
            //Resources.GetBuiltinResource()
            texture = (Texture) EditorGUIUtility.Load("Brushes/builtin_brush_" + num + ".png");
            if (texture != null)
            {
                list.Add(texture);
            }
            num++;
        } while (texture != null);
        num = 0;
        do
        {
            texture = EditorGUIUtility.FindTexture("brush_" + num + ".png");
            if (texture != null)
            {
                list.Add(texture);
            }
            num++;
        } while (texture != null);
        sBrushTextures = list.ToArray(typeof (Texture2D)) as Texture2D[];
    }
 

    private void loadBrush()
    {
        brushTexture = sBrushTextures[currentSelectedBrush];
        if (currentTextureBrush != null)
            currentTextureBrush.Dispose();
        currentTextureBrush = new TextureBrush();
        currentTextureBrush.Load(brushTexture, 64);
    }

    private void loadBrush(int index)
    {
        brushTexture = sBrushTextures[index];
        if (currentTextureBrush != null)
            currentTextureBrush.Dispose();
        currentTextureBrush = new TextureBrush();
        currentTextureBrush.Load(brushTexture, 64);
    }

    private void paintOnTexture()
    {
        Event e = Event.current;
        RaycastHit hit;
        Ray ray = HandleUtility.GUIPointToWorldRay(e.mousePosition);
        if (Physics.Raycast(ray, out hit))
        {
            if (hit.collider.gameObject.name == target.name)
            {
                float xCenterNormalized = hit.textureCoord.x;
                float yCenterNormalized = hit.textureCoord.y;

				int size = (int) ( 64* brushSize );
                int num = Mathf.FloorToInt(xCenterNormalized*mixMap.width);
                int num2 = Mathf.FloorToInt(yCenterNormalized*mixMap.height);
                int num3 = Mathf.RoundToInt((float) size)/2;
                int num4 = Mathf.RoundToInt((float) size)%2;
                int x = Mathf.Clamp(num - num3, 0, mixMap.width - 1);
                int y = Mathf.Clamp(num2 - num3, 0, mixMap.height - 1);
                int num7 = Mathf.Clamp((num + num3) + num4, 0, mixMap.width);
                int num8 = Mathf.Clamp((num2 + num3) + num4, 0, mixMap.height);
                int width = num7 - x;
                int height = num8 - y;
                Color[] srcPixels = mixMap.GetPixels(x, y, width, height, 0);
                for (int i = 0; i < height; i++)
                {
                    for (int j = 0; j < width; j++)
                    {
                        Color targetColor = currentTextureBrush.GetColor(currentSelectedTexture);
                        int ix = (x + j) - ((num - num3) + num4);
                        int iy = (y + i) - ((num2 - num3) + num4);
                        int index = (i*width) + j;
						float blendFactor = currentTextureBrush.GetStrengthInt((int)(ix/brushSize), (int) (iy/ brushSize))*
                                            hardness;
                        srcPixels[index] = Color.Lerp(srcPixels[index], targetColor, blendFactor);
                    }
                }

                mixMap.SetPixels(x, y, width, height, srcPixels, 0);


            }
            mixMap.Apply();
        }
    }

    private void UpdatePreviewBrush()
    {
        TerrainScript targetScript = ((TerrainScript) target);

        Vector3 normal = Vector3.zero;
        Vector3 hitPos = Vector3.zero;
        Vector2 vector;
        Vector3 vector2 = Vector3.zero;
        float m_Size = 16*brushSize;
        Projector previewProjector = currentTextureBrush.GetPreviewProjector();
        float num = 1f;

        float num2 = mixMap.width/mixMap.height;

        Vector2 size = targetScript.getSizeOfMesh(); // terrain.getSizeOfMesh();

        int terrainSizeX = (int) size.x;
        int terrainSizeZ = (int) size.y;

        Transform PPtransform = previewProjector.transform;
        bool flag = true;

        Vector2 newMousePostion = Event.current.mousePosition;
        newMousePostion.y = Screen.height - (Event.current.mousePosition.y + 35);
        Ray ray = Camera.current.ScreenPointToRay(newMousePostion);
        RaycastHit hit;

        if (Physics.Raycast(ray, out hit, 1000f))
        {
            vector2 = hit.point;
            hitPos = hit.point;
            normal = hit.normal;
            float num4 = ((m_Size%2) != 0) ? 0.5f : 0f;
            int alphamapWidth = 64;
            int alphamapHeight = 64;
            vector.x = (Mathf.Floor(hit.textureCoord.x*alphamapWidth) + num4)/((float) alphamapWidth);
            vector.y = (Mathf.Floor(hit.textureCoord.y*alphamapHeight) + num4)/((float) alphamapHeight);
            vector2.x = vector.x*-terrainSizeX + (terrainSizeX/2);
            vector2.z = vector.y*-terrainSizeZ + (terrainSizeZ/2);
            vector2 += Selection.activeGameObject.transform.position;
            num = ((m_Size*0.5f)/((float) alphamapWidth))*terrainSizeX;
            num2 = ((float) alphamapWidth)/((float) alphamapHeight);
        }
        else
        {
            flag = false;
        }

        previewProjector.enabled = flag;
        if (flag)
        {
            PPtransform.position = hitPos + (normal*100);
            PPtransform.rotation = Quaternion.LookRotation(normal);
        }
        previewProjector.orthographicSize = num/num2;
        previewProjector.aspectRatio = num2;
    }

    private void GUI_Textures()
    {
        GUILayout.Label("Textures", EditorStyles.boldLabel, new GUILayoutOption[0]);
        bool flag;
        currentSelectedTexture = AspectSelectionGrid(currentSelectedTexture, sTextures, 0x40, "gridlist",
                                                     "No textures defined.", out flag);
        if (flag)
        {

        }
    }

    private void GUI_Brushes()
    {
        GUILayout.Label("Brushes", EditorStyles.boldLabel, new GUILayoutOption[0]);
        bool flag;
        if (sBrushTextures == null)
            LoadTextures();
        currentSelectedBrush = AspectSelectionGrid(currentSelectedBrush, sBrushTextures, 0x20, "gridlist",
                                                   "No brushes defined.", out flag);
        if (flag)
        {
            loadBrush();
        }
    }

    private void GUI_Settings()
    {
        GUILayout.Label("Settings", EditorStyles.boldLabel, new GUILayoutOption[0]);
        EditorGUIUtility.LookLikeControls(100f, 90f);
		brushSize =  EditorGUILayout.Slider("brushSize", brushSize, 0.01f, 5f);
        hardness = EditorGUILayout.Slider("brushHardness", hardness, 0.01f, 1f);


    }  
 
    public override void OnInspectorGUI()
    {
		GUILayout.Label(appTitle, EditorStyles.boldLabel);
		GUILayout.Label("martijn.pixelstudio@gmail.com", EditorStyles.boldLabel);
		GUILayout.Label("webmaster@exiin.com", EditorStyles.boldLabel);
         
            GUILayout.Label("Textures");
            currentMaterial = targetObject.renderer.sharedMaterial;

            if (currentMaterial.shader.name == "TerrainPaint/Lightmap-FirstPass")
            {
                if (isPainting)
                {
                    GUI_Brushes();
                    GUI_Textures();
                    GUI_Settings();
                    if (GUILayout.Button("Clear mixmap"))
                    {
                        ClearMap();
                    }
                    if (GUILayout.Button("Stop painting"))
                    {
                        if (currentTextureBrush != null)
                            currentTextureBrush.Dispose();
                        CreateMixTexture();
                        isPainting = false;
                    }
                }
                else
                {
                    if (GUILayout.Button("Start painting (ctrl/cmd + click)"))
                    {
                        mixMap = currentMaterial.GetTexture("_Control") as Texture2D;
                        CheckMixTexture();
                        LoadBrushIcons();
                        loadBrush();
                        LoadTextures();
                        needToSave = mixMap == null;
                        isPainting = true;
                    }

                    mixMap = currentMaterial.GetTexture("_Control") as Texture2D;
                    if (mixMap == null)
                    {
                        CreateMixTexture();
                    }
                }

            }
            else
            {
                GUILayout.Label("Shader type is not correct! unable to paint");
            }

        
    }
 

    private void DisableProjector()
    {
        if (currentTextureBrush != null)
        {
            currentTextureBrush.GetPreviewProjector().enabled = false;
        }
    }

    private void OnSceneGUI()
    {
        var ctrlID = GUIUtility.GetControlID(appTitle.GetHashCode(), FocusType.Passive);

 
        if ((isPainting) )
        {
            switch (Event.current.type)
            {
                case EventType.mouseDrag:
                    if (Event.current.control || Event.current.command)
                    {
						if (isPainting) paintOnTexture();
                        Event.current.Use();
                    }
                    break;
                case EventType.layout:
                    HandleUtility.AddDefaultControl(ctrlID);
                    break;
                case EventType.MouseMove:
                    HandleUtility.Repaint();
                    break;
                case EventType.Repaint:
                    if (isPainting) DisableProjector();
                    break;
            }
            if (currentTextureBrush != null)
                UpdatePreviewBrush();
        }
 
    }

    private void OnEnable()
    {
        targetObject = Selection.activeGameObject;
    }

    private void OnDisable()
    {
        CreateMixTexture();
        isPainting = false; 
        if (currentTextureBrush != null)
        {
            currentTextureBrush.Dispose();
        }
 

    }

    public bool Raycast(out Vector2 uv, out Vector3 pos)
    {
        RaycastHit hit;
        Ray ray = HandleUtility.GUIPointToWorldRay(Event.current.mousePosition);
        if (Physics.Raycast(ray, out hit, float.PositiveInfinity))
        {
            uv = hit.textureCoord;
            pos = hit.point;
            return true;
        }
        uv = Vector2.zero;
        pos = Vector3.zero;
        return false;
    }


}
