using System.Collections;
using System.Collections.Generic;
using DeepDesignLab.Base;
using System.ComponentModel;

namespace DeepDesignLab.PointCloud {
    public class CCtoVoxelReader : CCReader {
        bool staticFileReady = false;
        PointCloudData container;
        public CCtoVoxelReader(PointCloudData newContainer) {
            container = newContainer;
            if (!(container.getNumberOfVoxels == 0)) {
                container.clearVoxels();
            }
            staticFileReady = true;
        }
        protected override void ProcessLineValues(double[] rowValues, string line, int lineNumber, BackgroundWorker worker, DoWorkEventArgs e) {
            if (staticFileReady) {
                container.ForceAddVoxel(new Voxel_Habitat(rowValues));
            }
            base.ProcessLineValues(rowValues, line, lineNumber, worker, e); // This is an empty function of the base class.
        }


    }

}