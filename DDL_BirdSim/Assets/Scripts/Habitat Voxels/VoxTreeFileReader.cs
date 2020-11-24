using System.Collections;
using System.Collections.Generic;
using DeepDesignLab.Base;
using System.ComponentModel;
using System;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;

namespace DeepDesignLab.PointCloud {
    public class VoxTreeFileReader : BackgroundFileReader {
        bool fileReady = false;
        //VoxTreeData container;

        List<double[]> RawData = new List<double[]>();

        public List<double[]> getData { get { if (base.hasFinished) return RawData; return null; } }
        

        public VoxTreeFileReader(string path) : base(true) {   //The Base() calls the parent constructor

            fileReady = true;
            readFile(path);

        }

        public VoxTreeFileReader(bool readBinary) : base(readBinary) {   //The Base() calls the parent constructor

            fileReady = true;

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
        protected override void ProcessData(BinaryReader reader, BackgroundWorker worker, DoWorkEventArgs e) {
            if (fileReady) {

                BinaryFormatter formatter = new BinaryFormatter();
                RawData = (List<double[]>)formatter.Deserialize(reader.BaseStream);
            }

            base.ProcessData(reader, worker, e);
        }



    }

}

