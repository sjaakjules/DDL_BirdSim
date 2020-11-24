using System.Collections;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using DeepDesignLab.Base;

namespace DeepDesignLab.PointCloud {

    public enum VoxelTypes { Unclassified, tree, branch, leaf, ground, debris}
    public enum VoxelInfo { Position, Colour, Normal, Intensity, Roughness, HabitatType, TreeClass }
    public enum CloudCompareFileType { HabitatTreeWithNormal, undefined }

    // This is a habitat voxel that is used with the scans of trees. 
    // Assume the size is in meters.
    [Serializable]
    public class Voxel_Habitat : VoxelBase, IEquatable<Voxel_Habitat> {
        // As it is a child of VoxelBase it has a centroid, normal and colour.

            [SerializeField]
        private static int nVoxels;

        // The following are properties of a voxel, they are private so as to not accedently edit them. 
        [SerializeField]
        private readonly int id;
        [SerializeField]
        private int clusterID;
        [SerializeField]
        private int intensity;

        [SerializeField]
        private float[] typeIntensityValue = new float[Enum.GetNames(typeof(VoxelTypes)).Length];

        [NonSerialized]
        //  private List<Voxel_Habitat> neighbours = new List<Voxel_Habitat>();

        Dictionary<VoxelInfo, int> infoIndex = new Dictionary<VoxelInfo, int>();

        public Vector3 offset = Vector3.zero;

        // This is the public accessors. this allows informaiton to go one way.
        public Vector3 getPosition { get { return centroid; } }
        public Vector3 getNormal { get { return normal; } }
        public Color getColour { get { return colour; } }
      //  public Voxel_Habitat[] getNeighbours { get { return neighbours.ToArray(); } }

        public Vector3 renderPosition { get { return centroid + offset; } }
        public int getID { get { return id; } }


        // This allows the equals oporation to test if the index are the same. 
        // NOT TESTED but should speed things up.
        public override bool Equals(object obj) {
            if (obj == null) return false;
            Voxel_Habitat objAsPart = obj as Voxel_Habitat;
            if (objAsPart == null) return false;
            else return Equals(objAsPart);
        }
        public override int GetHashCode() {
            return id;
        }
        public bool Equals(Voxel_Habitat other) {
            if (other == null) return false;
            return (this.id.Equals(other.id));
        }


        ///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        //                                        Constructor functions
        ///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        /// <summary>
        /// Create a new voxel using the CloudCompare data, {X,Y,Z,R,G,B,Scalar Field,Nx,Ny,Nz}
        /// </summary>
        /// <param name="CCtextData"></param> Double array in the form {X,Y,Z,R,G,B,Scalar Field,Nx,Ny,Nz}
        /// <param name="newID"></param> New id, MUST BE UNIQUE!
        public Voxel_Habitat(double[] CCtextData) {
            centroid = new Vector3((float)CCtextData[0], (float)CCtextData[1], (float)CCtextData[2]);
            colour = new Color((float)CCtextData[3] / 255, (float)CCtextData[4] / 255, (float)CCtextData[5] / 255);
            normal = new Vector3((float)CCtextData[7], (float)CCtextData[8], (float)CCtextData[9]);
            intensity = (int)CCtextData[6];
            nVoxels++;
            id = nVoxels;
        }

        public Voxel_Habitat(double[] CCtextData,string[] headings, CloudCompareFileType filetype)
        {
            if (filetype == CloudCompareFileType.HabitatTreeWithNormal)
            {// Sample columns from cloudcompare. X,Y,Z, R,G,B,Intensity,Dip (degrees),Dip direction (degrees),Normal change rate (0.1),Roughness (0.1),Number of neighbors (r=0.1),habitatClass,treeClass,Nx,Ny,Nz
                centroid = new Vector3((float)CCtextData[0], (float)CCtextData[1], (float)CCtextData[2]);
                colour = new Color((float)CCtextData[3] / 255, (float)CCtextData[4] / 255, (float)CCtextData[5] / 255);
                normal = new Vector3((float)CCtextData[CCtextData.Length-3], (float)CCtextData[CCtextData.Length - 2], (float)CCtextData[CCtextData.Length - 1]);
                intensity = (int)CCtextData[6];
                nVoxels++;
                id = nVoxels;

            }

        }

        /// <summary>
        /// Create new voxel using sepcified values. This is private and internal only.
        /// </summary>
        /// <param name="colour"></param>Colour value
        /// <param name="centroid"></param>Centroid vector
        /// <param name="normal"></param>Normal vector
        /// <param name="intensity"></param>Intensity value
        private Voxel_Habitat(Color colour, Vector3 centroid, Vector3 normal, int intensity) {
            this.centroid = centroid;
            this.colour = colour;
            this.normal = normal;
            this.intensity = intensity;
            nVoxels++;
            id = nVoxels;
        }

        public Voxel_Habitat(Voxel_Habitat Vox):this(Vox.colour,Vox.centroid,Vox.normal,Vox.intensity) {  }

        /// <summary>
        /// Updates the colour of the base voxel.
        /// </summary>
        /// <param name="newColour"></param>
        public void updateColour(Color newColour) {
            colour = newColour;
        }


        ///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        //                                        Classification functions
        ///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

            public void ActivateType(VoxelTypes newType) {
            typeIntensityValue[(int)newType] = 1;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        //                                        Average functions
        ///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////


        /// <summary>
        /// This creates a new voxel with the two vectors added together. The colour is combined using the Unity standard add function. The offset is zero.
        /// TODO: Make sure the add colour works when multiplle voxels are being averaged.
        /// </summary>
        /// <param name="a"></param>
        /// <param name="b"></param>
        /// <returns></returns>
        public static Voxel_Habitat operator +(Voxel_Habitat a, Voxel_Habitat b) {
            Voxel_Habitat summedVox = new Voxel_Habitat(a.colour + b.colour, (a.centroid + b.centroid), (a.normal + b.normal), a.intensity + b.intensity);
            summedVox.typeIntensityValue = a.typeIntensityValue.Add(b.typeIntensityValue);
            return summedVox;
        }

        /// <summary>
        /// This will modify the input vector. It will also return the input voxel with the updated values.
        /// The colour is not changed. The offset is zero.
        /// </summary>
        /// <param name="a"></param>input vector
        /// <param name="b"></param>amount
        /// <returns></returns>
        public static Voxel_Habitat operator /(Voxel_Habitat a, float b) {
            a.centroid = a.centroid / b;
            a.normal = a.normal / b;
            a.intensity = (int)((float)a.intensity / b);
            a.typeIntensityValue.Multiply(b);
            return a;
        }
        /// <summary>
        /// This will modify the input vector. It will also return the input voxel with the updated values.
        /// The colour is not changed. The offset is zero.
        /// </summary>
        /// <param name="a"></param>input vector
        /// <param name="b"></param>amount
        /// <returns></returns>
        public static Voxel_Habitat operator *(Voxel_Habitat a, float b) {
            a.centroid = a.centroid * b;
            a.normal = a.normal * b;
            a.intensity = (int)((float)a.intensity * b);
            return a;
        }


        public static Voxel_Habitat AverageVoxels(List<Voxel_Habitat> VoxelList) {
            Voxel_Habitat averageVox = null;
            if (VoxelList.Count > 1) {
                averageVox = VoxelList[0] + VoxelList[1];
                for (int i = 2; i < VoxelList.Count; i++) {
                    averageVox = averageVox + VoxelList[i];
                }
                averageVox = averageVox / (float)VoxelList.Count;
            }
            return averageVox;
        }
        /*
                public void setNeighbour(Voxel_Habitat newNeighbour) {
                    if (!neighbours.Contains(newNeighbour)) {
                        neighbours.Add(newNeighbour);
                    }
                }

                public bool isNeighbour(Voxel_Habitat VoxelToCheck) {
                    return neighbours.Contains(VoxelToCheck);
                }
                */
    }

}