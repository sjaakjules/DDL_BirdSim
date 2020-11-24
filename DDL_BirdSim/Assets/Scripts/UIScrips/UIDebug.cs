using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEngine.UI;
using DeepDesignLab.PointCloud;

namespace DeepDesignLab.PointCloud
{


    public class UIDebug : MonoBehaviour
    {
        PointCloudData LoadedData;
        // Use this for initialization
        public Text text;
        public string AssetLocation = "Assets/DeepDesignLab/PointCloud.asset";

        void Start()
        {
            LoadedData = AssetDatabase.LoadAssetAtPath(AssetLocation, typeof(PointCloudData)) as PointCloudData;
            if (LoadedData)
            {
                text.text = string.Format("The number of voxels loaded is: {0}", LoadedData.getNumberOfVoxels);
            }
            // text.text = string.Format("The number of voxels loaded is: {0}", CCLoadFile .getNumberOfVoxels);

        }

        // Update is called once per frame
        void Update()
        {

            text.text = string.Format("The number of voxels loaded is: {0}", LoadedData.getNumberOfVoxels);
            // text.text = string.Format("The number of voxels loaded is: {0}", PointCloudData.getNumberOfVoxels);
        }
    }
}