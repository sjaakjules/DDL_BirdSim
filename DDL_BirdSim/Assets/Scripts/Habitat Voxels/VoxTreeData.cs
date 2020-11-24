using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using DeepDesignLab.Base;
using DeepDesignLab.PointCloud;
using UnityEngine;

public class VoxTreeData {
    // This is a thread safe class to hold shared variables. 
    // You can read more about the lock here: https://docs.microsoft.com/en-us/dotnet/api/system.threading.readerwriterlockslim?view=netframework-4.8
    private ReaderWriterLockSlim VoxelListLock = new ReaderWriterLockSlim();

    private Voxtree voxCloud;
    float initialSize = 100;    // in meters
    float minVoxSize = 0.1f;    // in meters
    Vector3 cloudCentre = Vector3.zero;

    public VoxTreeData() {
        voxCloud = new Voxtree(initialSize, cloudCentre, minVoxSize);
    }


    public int getNumberOfVoxels {
        get {
            VoxelListLock.EnterReadLock();
            try {
                return voxCloud.Count;
            }
            finally {
                VoxelListLock.ExitReadLock();
            }
        }
    }

    public void clearVoxels() {
        VoxelListLock.EnterWriteLock();
        try {
            voxCloud = new Voxtree(initialSize, cloudCentre, minVoxSize);
        }
        finally {
            VoxelListLock.ExitWriteLock();
        }
    }

    public Voxel_Habitat getNear(Vector3 location) {
        VoxelListLock.EnterReadLock();
        try {
            return null;
        }
        finally {
            VoxelListLock.ExitReadLock();
        }
    }

    public void ForceAddVoxel(Voxel_Habitat newVoxel) {
        VoxelListLock.EnterWriteLock();
        try {
            voxCloud.Add(newVoxel, newVoxel.getPosition);
        }
        finally {
            VoxelListLock.ExitWriteLock();
        }
    }

    public void AddVoxel(Voxel_Habitat newVoxel) {
        VoxelListLock.EnterUpgradeableReadLock();
        try {
            VoxelListLock.EnterWriteLock();
            try {
                voxCloud.Add(newVoxel, newVoxel.getPosition);
            }
            finally {
                VoxelListLock.ExitWriteLock();
            }

        }
        finally {
            VoxelListLock.ExitUpgradeableReadLock();
        }
    }

    /// <summary>
    /// Deconstructor to remove the lock.
    /// </summary>
    ~VoxTreeData() {
         if (VoxelListLock != null) VoxelListLock.Dispose();
    }
}
