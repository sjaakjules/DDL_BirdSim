using System.Collections;
using System.Collections.Generic;
using System;
using Unity;
using UnityEngine;

namespace DeepDesignLab.Base {
    // If it is dependent on ScriptableObject then it should be saved as reference when serialised.
    // The IEquatable allows this object to be compared, is one equal to another. Read https://docs.microsoft.com/en-us/dotnet/api/system.iequatable-1?view=netframework-4.8
    // TODO: Check the references are saved.... 
    [Serializable]
    public abstract class VoxelBase: IEquatable<VoxelBase> {
        private readonly long ID;
        private static long nIDs;

        [SerializeField]
        protected Vector3 centroid;

        [SerializeField]
        protected Color colour;

        [SerializeField]
        protected Vector3 normal;

        public VoxelBase() {
            ID = nIDs;
            nIDs++;
        }


        // This allows the equals oporation to test if the index are the same. 
        // NOT TESTED but should speed things up.
        public override bool Equals(object obj) {
            if (obj == null) return false;
            VoxelBase objAsPart = obj as VoxelBase;
            if (objAsPart == null) return false;
            else return Equals(objAsPart);
        }
        public override int GetHashCode() {
            return (int)ID;
        }
        public bool Equals(VoxelBase other) {
            if (other == null) return false;
            return (this.ID.Equals(other.ID));
        }

    }

}
