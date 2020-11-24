using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace DeepDesignLab.PointCloud {
    public class VoxTreeNode : PointOctreeNode<Voxel_Habitat> {

        public VoxTreeNode(float baseLengthVal, float minSizeVal, Vector3 centerVal) : base(baseLengthVal, minSizeVal, centerVal) { }

        public void calculateAverage() {
            // if children, calculate children averages than this average from the averaged children.
            if (children != null) {
                for (int i = 0; i < 8; i++) {
                    (children[i] as VoxTreeNode).calculateAverage();
                }
                averageObject = Average(children as VoxTreeNode[]);
            }// if no children, get average of objects.
            else {
                averageObject = Average(new VoxTreeNode[] { this });
            }
            
        }



        public Voxel_Habitat Average(VoxTreeNode[] PointTreeNodeList) {
            Voxel_Habitat aveVoxel = null;
            // if calculated from children, use the average values.
            if (PointTreeNodeList.Length > 1) {
                aveVoxel = PointTreeNodeList[0].averageObject + PointTreeNodeList[1].averageObject;

                for (int i = 2; i < PointTreeNodeList.Length; i++) {
                    aveVoxel = aveVoxel + PointTreeNodeList[i].averageObject;
                }
                aveVoxel = aveVoxel / PointTreeNodeList.Length;
            } // else if list is 1 entry its end of node tree. So calculate from objects.
            else if (PointTreeNodeList.Length ==1) {
                if (PointTreeNodeList[0].objects.Count > 1) {                                               // if there is atleast 2 voxels in the list of objects.
                    aveVoxel = PointTreeNodeList[0].objects[0].Obj+ PointTreeNodeList[0].objects[1].Obj;    // Combine the first 2 voxels.
                    for (int i = 2; i < PointTreeNodeList[0].objects.Count; i++) {                          
                        aveVoxel = aveVoxel + PointTreeNodeList[0].objects[i].Obj;                          // Combine each voxel after the first two
                    }
                    aveVoxel = aveVoxel / PointTreeNodeList[0].objects.Count;
                }
                else {
                    return new Voxel_Habitat(PointTreeNodeList[0].objects[0].Obj);
                }
            }
            return aveVoxel;
        }



    }
}
