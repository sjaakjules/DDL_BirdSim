using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SlimHabitatVoxel
{
    Vector3Int position;
    Color32 colour;
    Vector3 normal;
    Vector3 staticFields;



    public SlimHabitatVoxel(double[] CCData, float averageSize)
    {

        // Pointcloud import formats.
        //  0   X,Y,Z,   
        //  3   R,G,B,  
        //  6   Intensity,Dip (degrees),Dip direction (degrees),    
        //  9   Normal change rate (0.1),Roughness (0.1),Number of neighbors (r=0.1),   
        //  12  habitatClass,treeClass, 
        //  14  Nx,Ny,Nz
        //  17  nAveraged       //This is after averaged.
        position = new Vector3Int((int)(CCData[0] * averageSize), (int)(CCData[1] * averageSize), (int)(CCData[2] * averageSize));
        colour = new Color32((byte)CCData[3], (byte)CCData[4], (byte)CCData[5], 0);
        normal = new Vector3((float)CCData[CCData.Length - 4], (float)CCData[CCData.Length - 3], (float)CCData[CCData.Length - 2]);
        staticFields = new Vector3((float)CCData[9], (float)CCData[12], (float)CCData[13]);
    }
}
