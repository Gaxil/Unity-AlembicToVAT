using UnityEngine;
using UnityEditor;
using System.Collections;
using System.Collections.Generic;
using Unity.EditorCoroutines.Editor;

public enum TopologyType
{
	Undefined,
	Variable,
	Fixed	
}

public class ATV_Editor : EditorWindow
{  
	public string ExportPath = "Assets/Export/";
	public string ExportFilename = "AlembicVAT";
	public float StartTime = 0.0f;
	public float EndTime = 10.0f;
	public float SampleRate = 24.0f;
	public TopologyType VariableTopology = TopologyType.Undefined;

	public Transform MeshToBake;
	public Material ReferenceMaterial;

    [MenuItem("Window/Alembic to VAT")]
    public static void ShowWindow()
    {
        EditorWindow.GetWindow(typeof(ATV_Editor));
    }
    
	public void Awake()
    {
		ReferenceMaterial = Resources.Load<Material>("VATMaterial");
	}

    void OnGUI()
    {
        GUILayout.Label ("Source", EditorStyles.boldLabel);
		EditorGUI.BeginChangeCheck();
 		MeshToBake = EditorGUILayout.ObjectField("Hierarchy to bake", MeshToBake, typeof(Transform), true) as Transform;
		if (EditorGUI.EndChangeCheck())
		{
			Debug.Log("Object to bake has changed ... updating data");
			currentBaking = EditorCoroutineUtility.StartCoroutine(UpdateFromAlembic(), this);
		}
        GUILayout.Label ("Drop one object UNDER the Alembic player");
 		ReferenceMaterial = EditorGUILayout.ObjectField("Reference material", ReferenceMaterial, typeof(Material), true) as Material;
        GUILayout.Space(10);
        GUILayout.Label ("Export", EditorStyles.boldLabel);
        ExportPath = EditorGUILayout.TextField ("Export path", ExportPath);
        ExportFilename = EditorGUILayout.TextField ("Export filename", ExportFilename);
        GUILayout.Space(2);
        GUILayout.Label ("Final path : "+ ExportPath + "Resources/"+ExportFilename+"_xxx.xxx");
        GUILayout.Space(10);
        GUILayout.Label ("Animation info", EditorStyles.boldLabel);
		if (VariableTopology!=TopologyType.Undefined)
		{
			StartTime = EditorGUILayout.FloatField("Start time", StartTime);
			EndTime = EditorGUILayout.FloatField("End time", EndTime);
			SampleRate = EditorGUILayout.FloatField("Sample rate", SampleRate);
		}
		switch (VariableTopology)
		{
			case TopologyType.Undefined:
		        GUILayout.Label ("Undefined topology");
				break;
			case TopologyType.Fixed:
		        GUILayout.Label ("Fixed topology (morphing mesh)");
				break;
			case TopologyType.Variable:
		        GUILayout.Label ("Variable topology (mesh sequence)");
				break;
		}
        GUILayout.Space(10);

		if (VariableTopology!=TopologyType.Undefined)
		{
			if (bakingInProgress)
			{
				if (GUILayout.Button("Cancel bake"))
				{
					CancelBake();
				}
			}
			else
			{
				if (GUILayout.Button("Bake mesh"))
				{
					BakeMesh();
				}
			}
		}
    }

	SerializedProperty timeProp = null;
	SerializedProperty startTimeProp = null;
	SerializedProperty endTimeProp = null;
	SerializedObject alembicObject = null;
	EditorCoroutine currentBaking = null;

	bool bakingInProgress = false;
	int maxTriangleCount=0;
	int minTriangleCount=10000000;

	SerializedObject InitAlembic()
	{
		if (MeshToBake==null)
		{
			Debug.LogError("No mesh to bake!");
			return null;
		}

		if (MeshToBake.parent==null)
		{
			Debug.LogError("Transform should be under an Alembic player");
			return null;
		}

		GameObject parent = MeshToBake.parent.gameObject;
		if (parent==null)
		{
			Debug.LogError("No parent!");
			return null;
		}

        var alembicPlayer = parent.gameObject.GetComponent("AlembicStreamPlayer");
		if (alembicPlayer == null)
		{
			Debug.LogError("Alembic player!");
			return null;
		}
		alembicObject = new SerializedObject(alembicPlayer);

		timeProp = alembicObject.FindProperty("currentTime");
		startTimeProp = alembicObject.FindProperty("startTime");
		endTimeProp = alembicObject.FindProperty("endTime");

		return alembicObject;
	}

	private void BakeMesh()
	{
		Debug.Log("Start baking mesh!");
        currentBaking = EditorCoroutineUtility.StartCoroutine(ExportFrames(), this);
	}

	IEnumerator UpdateFromAlembic()
	{
		Debug.Log("Get time from Alembic!");
		VariableTopology = TopologyType.Undefined;

		SerializedObject alembic = InitAlembic();
		if (alembic!=null)
		{

			maxTriangleCount=0;
			minTriangleCount=10000000;

			{
				Debug.Log("Checking max triangle count");
				int framesCount = Mathf.RoundToInt((EndTime-StartTime) * SampleRate + 0.5f);	

				for(int frame = 0;frame<framesCount;frame++)
				{
					float timing = StartTime + ((float)frame)/SampleRate;
					timeProp.floatValue = timing; 
					alembicObject.ApplyModifiedProperties();
					yield return null;

					int triangleCount = 0;
					for(int i=0;i<MeshToBake.childCount;i++)
					{
						MeshFilter localMeshFilter = MeshToBake.GetChild(i).GetComponent<MeshFilter>();
						if (localMeshFilter != null)
						{
							triangleCount += localMeshFilter.sharedMesh.triangles.Length/3;
						}
					}
					if (triangleCount>maxTriangleCount)
						maxTriangleCount = triangleCount;
					if (triangleCount<minTriangleCount)
						minTriangleCount = triangleCount;
				}
				Debug.Log("Max triangles count : " + maxTriangleCount);
				Debug.Log("Min triangles count : " + minTriangleCount);
			}
			yield return null;
			StartTime = 0.0f;
			SampleRate = 1.0f/startTimeProp.floatValue;
			EndTime = endTimeProp.floatValue;
			VariableTopology = (maxTriangleCount == minTriangleCount)?TopologyType.Fixed : TopologyType.Variable;
			yield return null;
		}
	}

	private void CancelBake()
	{
		Debug.Log("Cancel current baking!");
		EditorCoroutineUtility.StopCoroutine(currentBaking);
	}

	Vector2 getUV(int Xpos, int Ypos, int Xsize, int Ysize)
	{
		Vector2 uv = new Vector2();

		uv.x = (0.5f + (float)Xpos)/(float)Xsize;
		uv.y = (0.5f + (float)Ypos)/(float)Ysize;

		return uv;
	}

	Vector2Int getCoord(int Xindex, int Yindex, int Xsize, int Ysize, int columnSize)
	{
		Vector2Int uv = new Vector2Int();
		
		int columnIndex = Yindex/Ysize;
		int verticalIndex = Yindex % Ysize;

		uv.x = Xindex + columnIndex*columnSize;
		uv.y = verticalIndex;

		return uv;
	}

	IEnumerator ExportFrames()
	{
		Mesh bakedMesh=null;
		Vector3[] vertices;
		Vector2[] uv;
		Vector3[] normals;
		Vector4[] tangents;
		Color[] colors;
		int[] triangles;
		int verticesCount = 0;
		int trianglesIndexCount = 0;

		string finalExportPath = ExportPath + "Resources/";

		SerializedObject alembic = InitAlembic();

		timeProp.floatValue = StartTime; 
		alembicObject.ApplyModifiedProperties();
		yield return null;


		bakedMesh = new Mesh();
		bakedMesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;

		verticesCount = 0;
		trianglesIndexCount = 0;

		if ((MeshToBake != null) && (MeshToBake.childCount!=0))
		{
			bool hasNormal = false;
			bool hasUVs = false;
			bool hasColors = false;

			if (VariableTopology == TopologyType.Variable)
			{
				hasNormal = true;
				verticesCount = maxTriangleCount*3;
				trianglesIndexCount = maxTriangleCount*3;
			}
			else
			{
				for(int i=0;i<MeshToBake.childCount;i++)
				{
					MeshFilter localMeshFilter = MeshToBake.GetChild(i).GetComponent<MeshFilter>();

					if (localMeshFilter != null)
					{
						verticesCount += localMeshFilter.sharedMesh.vertices.Length;
						trianglesIndexCount += localMeshFilter.sharedMesh.triangles.Length;

						hasNormal |= (localMeshFilter.sharedMesh.normals.Length>0);
						hasColors |= (localMeshFilter.sharedMesh.colors.Length>0);
						hasUVs |= (localMeshFilter.sharedMesh.uv.Length>0);				
					}
				}		
			}

			vertices = new Vector3[verticesCount];
			uv = new Vector2[verticesCount];
			normals = new Vector3[verticesCount];
			colors = new Color[verticesCount];
			triangles = new int[trianglesIndexCount];

			int currentTrianglesIndex = 0;
			int verticesOffset = 0;

			if (VariableTopology == TopologyType.Variable)
			{
				for(int i=0;i<verticesCount;i++)	// everything is initialized to 0
				{
					triangles[i] = i;
					vertices[i] = Vector3.zero;
					normals[i] = Vector3.up;
				}
			}
			else
			{
				for(int i=0;i<MeshToBake.childCount;i++)
				{
					MeshFilter localMeshFilter = MeshToBake.GetChild(i).GetComponent<MeshFilter>();
					if (localMeshFilter != null)
					{
						float random = Random.value;				
						for(int j=0;j<localMeshFilter.sharedMesh.vertices.Length;j++)
						{
							if (hasUVs)
								uv[j + verticesOffset] = localMeshFilter.sharedMesh.uv[j];
							if (hasColors)
								colors[j + verticesOffset] = localMeshFilter.sharedMesh.colors[j];

							vertices[j + verticesOffset] = localMeshFilter.sharedMesh.vertices[j];
						}

						for(int j=0;j<localMeshFilter.sharedMesh.triangles.Length;j++)
						{
							triangles[currentTrianglesIndex++] = localMeshFilter.sharedMesh.triangles[j] + verticesOffset;
						}

						verticesOffset += localMeshFilter.sharedMesh.vertices.Length;
					}
				}
			}
			
			bakedMesh.vertices = vertices;
			if (hasUVs)
				bakedMesh.uv = uv;
			if (hasNormal)
				bakedMesh.normals = normals;
			if (hasColors)
				bakedMesh.colors = colors;
			bakedMesh.triangles = triangles;

			int[] textureSize = {32,64,128,256,512,1024,2048,4096,8192,16384};
			bakedMesh.RecalculateBounds();

			int columns = -1;
			int textureHeight=-1;
			int textureWidth=-1;

			int vertexCount = vertices.Length;
			int framesCount = Mathf.RoundToInt((EndTime-StartTime) * SampleRate + 0.5f);	
			int adjustedFramesCount = framesCount +2; // = space between columns

			Debug.Log("Frames count : "+framesCount);
			Debug.Log("Vertices count : "+vertexCount);

			bool exportVAT = true;

			columns = Mathf.CeilToInt(Mathf.Sqrt((float)vertexCount/(float)adjustedFramesCount));
			Debug.Log("Initial columns : "+columns);
			int textureHeightAdjusted =  Mathf.CeilToInt(((float)vertexCount/(float)columns));
			for(int i=0;i<textureSize.Length;i++)
			{
				if ((textureHeight==-1) && (textureHeightAdjusted<=textureSize[i]))
					textureHeight = textureSize[i];
			}
			Debug.Log("Wanted height : "+textureHeightAdjusted+" - next POW 2 : "+textureHeight);
			if (textureHeight==-1)
			{
				Debug.LogError("Alembic too big to be encoded in VAT format ... too high");
				exportVAT=false;
			}

			if (exportVAT)
			{
				columns = Mathf.CeilToInt(((float)vertexCount/(float)textureHeight));

				Debug.Log("Adjusted columns : "+columns);
				for(int i=0;i<textureSize.Length;i++)
				{
					if ((textureWidth==-1) && ((adjustedFramesCount*columns)<=textureSize[i]))
						textureWidth = textureSize[i];
				}
				Debug.Log("Wanted width : "+(adjustedFramesCount*columns)+" - next POW 2 : "+textureWidth);

				if (textureWidth==-1)
				{
					Debug.LogError("Alembic too big to be encoded in VAT format ... too wide");
					exportVAT=false;
				}
			}

			if (exportVAT)
			{
				Bounds newBounds = new Bounds();
				Vector3 minBounds = new Vector3(1e9f, 1e9f, 1e9f);
				Vector3 maxBounds = new Vector3(-1e9f, -1e9f, -1e9f);

				Vector2[] uv2 = new Vector2[verticesCount];

				Debug.Log("Texture size : "+textureWidth+" x "+textureHeight+" Vertices : "+vertexCount+" Frames : "+framesCount);

				Texture2D positionTexture= new Texture2D(textureWidth, textureHeight, TextureFormat.RGBAHalf, true, true);
				if (VariableTopology == TopologyType.Variable)
					positionTexture.filterMode = FilterMode.Point;
				positionTexture.wrapMode = TextureWrapMode.Clamp;
				Texture2D normalTexture= new Texture2D(textureWidth, textureHeight, TextureFormat.RGBAHalf, true, true);
				if (VariableTopology == TopologyType.Variable) 
					normalTexture.filterMode = FilterMode.Point;
				normalTexture.wrapMode = TextureWrapMode.Clamp;

				for(int frame = 0;frame<framesCount;frame++)
				{
					float timing = StartTime + ((float)frame)/SampleRate;
					Debug.Log("Encoding frame "+frame+" / "+framesCount+" ("+timing+")");
					timeProp.floatValue = timing; 
					alembicObject.ApplyModifiedProperties();
					yield return null;

					if (VariableTopology == TopologyType.Variable)
					{
						Debug.Log("Doing "+maxTriangleCount*3);
						MeshFilter localMeshFilter = MeshToBake.GetChild(0).GetComponent<MeshFilter>();
						if (localMeshFilter != null)
						{
							List<Vector3> local_vertices = new List<Vector3>(); 
							localMeshFilter.sharedMesh.GetVertices(local_vertices);
							List<Vector3> local_normals = new List<Vector3>(); 
							localMeshFilter.sharedMesh.GetNormals(local_normals);
							int[] local_index= localMeshFilter.sharedMesh.GetTriangles(0);

							for(int targetIndex=0 ; targetIndex<maxTriangleCount*3 ;targetIndex++)
							{
								Vector2Int coordinates = getCoord(frame, targetIndex, textureWidth, textureHeight, adjustedFramesCount);
								Vector2Int coordinates_0 = getCoord(0, targetIndex, textureWidth, textureHeight, adjustedFramesCount);
								Vector2 uvCoord = getUV(coordinates_0.x, coordinates_0.y, textureWidth, textureHeight);

								uv2[targetIndex] = uvCoord;

								Vector3 newVertexPos = Vector3.zero;
								Vector3 newVertexNrm  = Vector3.up;

								if (targetIndex<local_index.Length)
								{
									int vtxIndex = local_index[targetIndex];
									newVertexPos = local_vertices[vtxIndex];
									newVertexNrm = local_normals[vtxIndex];
								}

								minBounds = Vector3.Min(minBounds, newVertexPos);
								maxBounds = Vector3.Max(maxBounds, newVertexPos);

								positionTexture.SetPixel(coordinates.x, coordinates.y, new Color(newVertexPos.x, newVertexPos.y, newVertexPos.z, 1.0f));	
								normalTexture.SetPixel(coordinates.x, coordinates.y, new Color(newVertexNrm.x, newVertexNrm.y, newVertexNrm.z, 1.0f));	
							}

						}
					}
					else
					{
						Debug.Log("Doing animated solid meshes ");
						verticesOffset = 0;
						for(int i=0;i<MeshToBake.childCount;i++)
						{
							MeshFilter localMeshFilter = MeshToBake.GetChild(i).GetComponent<MeshFilter>();
							if (localMeshFilter != null)
							{
								List<Vector3> local_vertices = new List<Vector3>(); 
								localMeshFilter.sharedMesh.GetVertices(local_vertices);
								List<Vector3> local_normals = new List<Vector3>(); 
								localMeshFilter.sharedMesh.GetNormals(local_normals);
								int[] local_index= localMeshFilter.sharedMesh.GetTriangles(0);


								for(int j=0;j<local_vertices.Count;j++)
								{
									int targetIndex = j + verticesOffset;

									Vector2Int coordinates = getCoord(frame, targetIndex, textureWidth, textureHeight, adjustedFramesCount);
									Vector2Int coordinates_0 = getCoord(0, targetIndex, textureWidth, textureHeight, adjustedFramesCount);
									Vector2 uvCoord = getUV(coordinates_0.x, coordinates_0.y, textureWidth, textureHeight);

									uv2[targetIndex] = uvCoord;

									Vector3 newVertexPos = local_vertices[j];
									Vector3 refVertexPos = vertices[targetIndex];
									newVertexPos -= refVertexPos;
									Vector3 newVertexNrm = local_normals[j];

									minBounds = Vector3.Min(minBounds, newVertexPos);
									maxBounds = Vector3.Max(maxBounds, newVertexPos);

									positionTexture.SetPixel(coordinates.x, coordinates.y, new Color(newVertexPos.x, newVertexPos.y, newVertexPos.z, 1.0f));	
									normalTexture.SetPixel(coordinates.x, coordinates.y, new Color(newVertexNrm.x, newVertexNrm.y, newVertexNrm.z, 1.0f));	
								}
								verticesOffset += local_vertices.Count;
							}
						}
					}
				}

				newBounds.max = maxBounds;
				newBounds.min = minBounds;
				Debug.Log("Min bounds : "+minBounds.x+" , "+minBounds.y+" , "+minBounds.z);
				Debug.Log("Max bounds : "+maxBounds.x+" , "+maxBounds.y+" , "+maxBounds.z);

				bakedMesh.bounds = newBounds;

				positionTexture.Apply();
				normalTexture.Apply();

				Debug.Log("Saving positions texture asset at "+finalExportPath+ExportFilename+"_position.asset");
				AssetDatabase.CreateAsset(positionTexture,finalExportPath+ExportFilename+"_position.asset" );
				AssetDatabase.SaveAssets();
				Debug.Log("Saving normals texture asset at "+finalExportPath+ExportFilename+"_normal.asset");
				AssetDatabase.CreateAsset(normalTexture,finalExportPath+ExportFilename+"_normal.asset" );
				AssetDatabase.SaveAssets();

				bakedMesh.uv2 = uv2;
			}

			Debug.Log("Saving merged mesh asset at "+finalExportPath+ExportFilename+"_mesh.asset");
			AssetDatabase.CreateAsset(bakedMesh,finalExportPath+ExportFilename+"_mesh.asset" );
			AssetDatabase.SaveAssets();
			yield return null;			

			Debug.Log("Create prefab");

			Debug.Log("Saving material asset");
			Material newMaterial = new Material(ReferenceMaterial.shader);
			newMaterial.name = ExportFilename+"_material";
			newMaterial.SetFloat("_Framecount", (float)framesCount);

			Texture2D resPosTexture = Resources.Load<Texture2D >(ExportFilename+"_position");
			Texture2D resNormalTexture = Resources.Load<Texture2D >(ExportFilename+"_normal");
			if (resPosTexture == null)
				Debug.Log("Can't load position texture "+finalExportPath+ExportFilename+"_position.asset");
			if (resNormalTexture == null)
				Debug.Log("Can't load position texture "+finalExportPath+ExportFilename+"_normal.asset");
			
			newMaterial.SetTexture("_VAT_positions",resPosTexture );
			newMaterial.SetTexture("_VAT_normals",resNormalTexture);

			AssetDatabase.CreateAsset(newMaterial,finalExportPath+ExportFilename+"_material.mat" );
			AssetDatabase.SaveAssets();

			GameObject newGameObject = new GameObject(ExportFilename+"_Object");

			Mesh resMesh = Resources.Load<Mesh>(ExportFilename+"_mesh");
			if (resMesh == null)
				Debug.Log("Unable to reload created mesh");

			Material resMaterial = Resources.Load<Material>(ExportFilename+"_material");
			if (resMaterial == null)
				Debug.Log("Unable to reload material");

			MeshFilter meshFilter = newGameObject.AddComponent<MeshFilter>();
			meshFilter.mesh = resMesh;
			MeshRenderer meshRenderer = newGameObject.AddComponent<MeshRenderer>();
			meshRenderer.material = resMaterial;

			PrefabUtility.SaveAsPrefabAsset(newGameObject, finalExportPath+ExportFilename+"_prefab.prefab");

			DestroyImmediate(newGameObject);
		}		
	}
}