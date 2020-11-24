using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace DeepDesignLab.PointCloud {
    public class Voxtree : PointOctree<Voxel_Habitat> {

       // Voxel_Habitat averageVoxel;

        /// <summary>
        /// Contrsucts an Octree.
        /// </summary>
        /// <param name="initialWorldSize"></param>
        /// <param name="initialWorldPos"></param>
        /// <param name="minNodeSize"></param>
        public Voxtree(float initialWorldSize, Vector3 initialWorldPos, float minNodeSize) : base(initialWorldSize, initialWorldPos, minNodeSize) {

        }


        void CalculateAverageNodes() {
            (rootNode as VoxTreeNode).calculateAverage();
        }


    }
}

