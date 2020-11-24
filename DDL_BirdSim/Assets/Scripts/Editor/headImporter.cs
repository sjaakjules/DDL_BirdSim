
using UnityEditor;
using UnityEditor.Experimental.AssetImporters;
using UnityEngine;
using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.CompilerServices;
using TC;



[ScriptedImporter(1, "head")]
internal class headImporter : ScriptedImporter
{
	[Header("Point Cloud Data Settings")]
	public float Scale = 1;
	public Vector3 PivotOffset;
	public Vector3 NormalRotation;
	[Header("Default Renderer")]
	public float DefaultPointSize = 0.1f;

	public override void OnImportAsset(AssetImportContext context)
	{
		// ComputeBuffer container
		// Create a prefab with PointCloudRenderer.
		var gameObject = new GameObject();
		var data = ImportAsPointCloudData(context.assetPath);
        if (data == null)
        {
			Debug.LogError("data is null... importing didnt work...");
        }

		var system = gameObject.AddComponent<TCParticleSystem>();
		system.Emitter.Shape = EmitShapes.PointCloud;
		system.Emitter.PointCloud = data;
		system.Emitter.SetBursts(new[] { new BurstEmission { Time = 0, Amount = data.PointCount } });
		system.Emitter.EmissionRate = 0;
		system.Emitter.Lifetime = MinMaxRandom.Constant(-1.0f);
		system.Looping = false;
		system.MaxParticles = data.PointCount + 1000;
		system.Emitter.Size = MinMaxRandom.Constant(DefaultPointSize);
		system.Manager.NoSimulation = true;

		if (data.Normals != null)
		{
			system.ParticleRenderer.pointCloudNormals = true;
			system.ParticleRenderer.RenderMode = GeometryRenderMode.Mesh;

			var quadGo = GameObject.CreatePrimitive(PrimitiveType.Quad);
			system.ParticleRenderer.Mesh = quadGo.GetComponent<MeshFilter>().sharedMesh;
			DestroyImmediate(quadGo);
		}

		context.AddObjectToAsset("prefab", gameObject);
		if (data != null)
		{
			context.AddObjectToAsset("data", data);
		}

		context.SetMainObject(gameObject);
	}
	static Material GetDefaultMaterial()
	{
		return AssetDatabase.LoadAssetAtPath<Material>(
			"Assets/Pcx/Editor/Default Point.mat"
		);
	}
	PointCloudData ImportAsPointCloudData(string path)
	{
		SlimHabitatCloud cloud = new SlimHabitatCloud(path, true);
		//var stream = File.Open(path, FileMode.Open, FileAccess.Read, FileShare.Read);
		//var reader = new PclBinaryReader(ReadFully(stream));

		//var header = ReadDataHeader(reader);
		//var body = ReadDataBody(header, reader);

		var data = ScriptableObject.CreateInstance<PointCloudData>();
		data.Initialize(cloud.pPosition.Clone() as Vector3[], cloud.pNormals.Clone() as Vector3[], cloud.pColours.Clone() as Color32[], Scale, PivotOffset, NormalRotation);
		data.name = Path.GetFileNameWithoutExtension(path); //cloud.cloudInfo.name;

		return data;
	}
}
