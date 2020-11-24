using System.Collections;
using System.Collections.Generic;
using DeepDesignLab.Base;
using System.ComponentModel;
using UnityEngine;
using System;
using System.Dynamic;

namespace DeepDesignLab.PointCloud {
    //public enum CloudCompareFileType { HabitatTreeWithNormal, Other }
    public class VoxTree_SetupFiles : CCReader {
        bool fileReady = false;
        //VoxTreeData container;
        //public CloudCompareFileType file = CloudCompareFileType.Other;
        
        //public float minBucketSize { get;}

        // Other Properties
        Dictionary<Vector2, List<double[]>> RawData = new Dictionary<Vector2, List<double[]>>();
        public Dictionary<Vector2, List<double[]>> getData { get { if (base.hasFinished) return RawData; return null; } }
        Vector3 key; //TEMP VALUES

        // HabitatTreeWithNormal properties
        // X,Y,Z,R,G,B,Intensity,Dip (degrees),Dip direction (degrees),Normal change rate (0.1),Roughness (0.1),Number of neighbors (r=0.1),habitatClass,treeClass,Nx,Ny,Nz
        //Dictionary<Vector3, List<>



        public VoxTree_SetupFiles() : base() {   //The Base() calls the parent constructor

            fileReady = true;
           //minBucketSize = 0.2f;
        }

        /*
       public VoxTreeFileReader(VoxTreeData newContainer):base() {   //The Base() calls the parent constructor

           container = newContainer;
           if (!(container.getNumberOfVoxels == 0)) {
               container.clearVoxels();
           }
          
        fileReady = true;  
            
        }
         */
        protected override void ProcessLineValues(double[] rowValues, string line, int lineNumber, BackgroundWorker worker, DoWorkEventArgs e) {
            if (fileReady) {

                key = new Vector2((float)Math.Round(rowValues[0], 0, MidpointRounding.AwayFromZero), (float)Math.Round(rowValues[1], 0, MidpointRounding.AwayFromZero));
                //key = new Vector3((float)Math.Round(rowValues[0],0,MidpointRounding.AwayFromZero),(float)Math.Round(rowValues[1],0, MidpointRounding.AwayFromZero),(float)Math.Round(rowValues[2], 0,MidpointRounding.AwayFromZero));
                if (RawData.ContainsKey(key)) {
                    RawData[key].Add(rowValues);
                }
                else {
                    RawData.Add(key, new List<double[]>());
                    RawData[key].Add(rowValues);
                }
                //container.ForceAddVoxel(new Voxel_Habitat(rowValues));      // CloudCompare data, {X,Y,Z,R,G,B,Scalar Field,Nx,Ny,Nz}
            }
            base.ProcessLineValues(rowValues, line, lineNumber, worker, e); // This is an empty function of the base class.
        }


        private Vector3 GetBucketKey(double[] rowValues, float spatialBucket)
        {
            spatialBucket = Mathf.Abs(spatialBucket);

            // Assume X,Y,Z is [0,1,2]
            if (rowValues != null && rowValues.Length >= 3)
            {
                return new Vector3((float)Math.Round(Math.Abs(rowValues[0] / spatialBucket)) * spatialBucket * Math.Sign(rowValues[0]),
                                    (float)Math.Round(Math.Abs(rowValues[1] / spatialBucket)) * spatialBucket * Math.Sign(rowValues[1]),
                                    (float)Math.Round(Math.Abs(rowValues[2] / spatialBucket)) * spatialBucket * Math.Sign(rowValues[2]));                
            }
            return new Vector3(float.NaN, float.NaN, float.NaN); ;
        }


    }

}

