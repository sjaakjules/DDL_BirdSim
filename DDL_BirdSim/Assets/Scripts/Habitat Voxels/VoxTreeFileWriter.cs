using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DeepDesignLab.Base;
using System.ComponentModel;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;

namespace DeepDesignLab.PointCloud {
    public class VoxTreeFileWriter : BackgroundFileWriter {

        List<double[]> RawData;
        
        public VoxTreeFileWriter(List<double[]> data, string filePath) : base(true) {
            RawData = data;
            WriteFile(filePath);
        }

        protected override void ProcessData(BinaryWriter writer, BackgroundWorker worker, DoWorkEventArgs e) {
            BinaryFormatter formatter = new BinaryFormatter();
            formatter.Serialize(writer.BaseStream, RawData);

            base.ProcessData(writer, worker, e);
        }
    }
}