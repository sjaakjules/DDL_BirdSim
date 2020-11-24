using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using DeepDesignLab.PointCloud;

public class CreatePointCloudData {
   // [MenuItem("Assets/Create/DeepDesign Lab/Create PointCloudData")]
    public static PointCloudData Create() {
        PointCloudData asset = ScriptableObject.CreateInstance<PointCloudData>();

        AssetDatabase.CreateAsset(asset, "Assets/PointCloud.asset");
        AssetDatabase.SaveAssets();
        return asset;
    }
    
}
