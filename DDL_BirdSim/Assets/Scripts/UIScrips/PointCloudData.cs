using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using DeepDesignLab.Base;
using UnityEngine;

namespace DeepDesignLab.PointCloud
{

    [Serializable]
    public class PointCloudData : ScriptableObject
    {
        // This is a thread safe class to hold shared variables. 
        // You can read more about the lock here: https://docs.microsoft.com/en-us/dotnet/api/system.threading.readerwriterlockslim?view=netframework-4.8
        private ReaderWriterLockSlim VoxelListLock = new ReaderWriterLockSlim();

        [SerializeField]
        private List<Voxel_Habitat> listOfVoxels = new List<Voxel_Habitat>(8000000);


        [SerializeField]
        public int getNumberOfVoxels { get { return listOfVoxels.Count; } }

        public void clearVoxels()
        {
            VoxelListLock.EnterWriteLock();
            try
            {
                listOfVoxels.Clear();
            }
            finally
            {
                VoxelListLock.ExitWriteLock();
            }
        }

        public Voxel_Habitat getIndex(int index)
        {
            VoxelListLock.EnterReadLock();
            try
            {
                if (listOfVoxels.Count > index)
                {
                    return listOfVoxels[index];
                }
                else
                {
                    return null;
                }
            }
            finally
            {
                VoxelListLock.ExitReadLock();
            }
        }

        public void ForceAddVoxel(Voxel_Habitat newVoxel)
        {
            VoxelListLock.EnterWriteLock();
            try
            {
                listOfVoxels.Add(newVoxel);
            }
            finally
            {
                VoxelListLock.ExitWriteLock();
            }
        }

        public void AddVoxel(Voxel_Habitat newVoxel)
        {
            VoxelListLock.EnterUpgradeableReadLock();
            try
            {
                if (!listOfVoxels.Contains(newVoxel))
                {
                    VoxelListLock.EnterWriteLock();
                    try
                    {
                        listOfVoxels.Add(newVoxel);
                    }
                    finally
                    {
                        VoxelListLock.ExitWriteLock();
                    }
                }
            }
            finally
            {
                VoxelListLock.ExitUpgradeableReadLock();
            }
        }

        /// <summary>
        /// Deconstructor to remove the lock.
        /// </summary>
        ~PointCloudData()
        {
            // if (VoxelListLock != null) VoxelListLock.Dispose();
        }



    }
}