using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DeepDesignLab.Base;
using DeepDesignLab.Debug;
using Unity.Collections;
using System;
using System.Linq;
using System.IO;

namespace DeepDesignLab.Sensors {
    public class DataCleanerManager : MonoBehaviour {

        public string filePath = null;
        [ReadOnly] public bool hasLoadedValues = false;
        bool startedJob = false;
        public bool readFile = false;
        public bool wroteFile = false;

        CSV_CleanerReader fileReader = new CSV_CleanerReader(1);
        CSVWriter fileWriter;

        // Use this for initialization
        void Start() {

            fileReader.readFile(filePath);
            readFile = false;
            startedJob = true;
            hasLoadedValues = false;

            UnityEngine.Debug.Log(fileReader.getErrors);
        }

        // Update is called once per frame
        void Update() {

            if (!hasLoadedValues && fileReader.hasFinished) {
                
                // One time run once it has finished reading the values...
                fileWriter = new CSVWriter(fileReader.GetHeaderTable,fileReader.GetDataTable);
                fileWriter.WriteFile(Path.GetDirectoryName(filePath )+"\\"+ Path.GetFileNameWithoutExtension(filePath) + "_Cleaned" + Path.GetExtension(filePath));

                readFile = true;
                hasLoadedValues = true;
                UnityEngine.Debug.Log(fileReader.getErrors);
            }
            if (hasLoadedValues && !wroteFile && fileWriter.hasFinished) {
                //Do something with the data... ie graph it
                /*
                if (Counter < AverageValues[ReadingType.Lux].Count) {
                    DebugGUI.Graph("Lux", (float)AverageValues[ReadingType.Lux][Counter]);
                }
                Counter++;
                */
                wroteFile = true;
                UnityEngine.Debug.Log(fileWriter.getErrors);
            }
        }
    }
}
