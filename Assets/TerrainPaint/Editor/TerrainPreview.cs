using UnityEngine;
using UnityEditor;
using System;
using System.Collections;
using System.Collections.Generic;

public class TerrainPreview 
{
	string previewShader =  " Shader \"Vegetation\" {"+
							" Properties { " +
							" 	_Color (\"Main Color\", Color) = (1, 1, 1, 1)" +
							" 	_MainTex (\"Base (RGB) Alpha (A)\", 2D) = \"white\" {}" +
							" 	_Cutoff (\"Base Alpha cutoff\", Range (0,.9)) = .5" +
							" }" +
							" SubShader {" +
							"	Tags {\"IgnoreProjector\"=\"True\" \"RenderType\"= \"TransparentCutout\"}"+
							" 	Material {" +
							" 		Diffuse [_Color]" +
							" 		Ambient [_Color]" +
							" 		Emission [_Color]" +
							" 	}" +
							" 	Lighting On" +
							" 	Cull Off" +
							" 	Pass {" +
							" 		AlphaTest Greater [_Cutoff]" +
							" 		SetTexture [_MainTex] {" +
							" 			combine texture * primary DOUBLE, texture * primary" +
							" 		}" +
							" 	}" +
							" 	Pass {" +
							" 		ZWrite off" +
							" 		ZTest Less" +
							" 		AlphaTest LEqual [_Cutoff]" +
							" 		Blend SrcAlpha OneMinusSrcAlpha" +
							" 		SetTexture [_MainTex] {	combine texture * primary DOUBLE, texture * primary}" +
							" 	}" +
							" }" +
							" } ";
	
	
	public bool changed = true;
	GameObject previewMesh;
	
	public void Dispose()
    {
        if (previewMesh != null)
        {
            UnityEngine.Object.DestroyImmediate(this.previewMesh);
            previewMesh = null;
        }
    }
 
	public GameObject CreatePreviewmesh(GameObject prefab) {
		previewMesh = EditorUtility.CreateGameObjectWithHideFlags("MeshPreview", HideFlags.HideAndDontSave);
		
		Material m = new Material(previewShader);
		
		Texture2D preview = AssetPreview.GetAssetPreview(prefab);
		if (preview != null) {
			Color background = preview.GetPixel(0,0);
			for (int x = 0; x < preview.width; x++) {
				for (int y = 0; y < preview.height; y++) {
					Color c = preview.GetPixel(x,y);
					if (c == background) {
						preview.SetPixel(x,y, new Color(c.r,c.g,c.b,0));
					}
				}
			}
			preview.Apply();
		} else {
			preview = new Texture2D(256,256);
			for (int x = 0; x < preview.width; x++) {
				for (int y = 0; y < preview.height; y++) {
					Color c = Color.gray;
					preview.SetPixel(x,y, new Color(c.r,c.g,c.b,0));	
				}
			}
		}
		m.mainTexture = preview;
		
		previewMesh.AddComponent<MeshFilter>();
		MeshRenderer mr = previewMesh.AddComponent<MeshRenderer>();
		mr.receiveShadows = false;
		mr.castShadows = false;
		
		previewMesh.renderer.material = m;
		return previewMesh;
	}
	
	
}

