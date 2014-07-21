using UnityEngine;
using UnityEditor;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Xml;


public class TerrainScript : MonoBehaviour {
	 
	
	private Vector2 SizeOfMesh;
	
	[SerializeField]	 
	
	public Vector2 getSizeOfMesh() {
		if (SizeOfMesh == Vector2.zero) {
			MeshFilter mf = gameObject.GetComponent<MeshFilter>();
			Vector2 result = Vector2.zero;
			if (mf != null) {
				result.x = mf.sharedMesh.bounds.size.x;
			 	result.y = mf.sharedMesh.bounds.size.y;
			}
			SizeOfMesh = result;	
		}
		
		return SizeOfMesh;
	}
	
	public bool isInsideOfBounds(Vector3 position) {
		Ray ray = new Ray(new Vector3(position.x, 10f,position.z), Vector3.down);
		if (Physics.Raycast(ray))
			return true;
		
		return false;
		
	}   
	 /*
	void OnDrawGizmos()
    {
		if (terrainData != null) {
	    	foreach (MyTreeInstance t in terrainData.treeInstances) {
				Gizmos.DrawWireSphere(t.position,1f );
			}
		} 
    }
    */
	
}


