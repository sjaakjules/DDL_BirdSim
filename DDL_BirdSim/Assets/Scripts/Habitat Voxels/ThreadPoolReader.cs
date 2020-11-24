using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System;
using System.ComponentModel;
using System.Diagnostics;
using UnityEngine;
using System.Text;
using Easy.Common.Extensions;
using System.Runtime.Serialization.Formatters.Binary;

namespace DeepDesignLab.PointCloud {
    public class ThreadPoolReader {

        private ManualResetEvent _doneEvent;

        string[] filesToRead;
        string readFilePath;
        

        public bool HaveFinished {
            get {
                for (int i = 0; i < doneEvents.Count; i++) {
                    if (!WaitHandle.WaitAll(doneEvents[i], 1)) {
                        return false;
                    }
                }
                return true;
            }
        }

        List<ManualResetEvent[]> doneEvents = new List<ManualResetEvent[]>();
        public readBinaryFile[] readers;

        public ThreadPoolReader(string filePath) {
            filesToRead = Directory.GetFiles(filePath);
        }

        public void ReadFiles() {
            readers = new readBinaryFile[filesToRead.Length];

            for (int i = 0; i < filesToRead.Length; i++) {
                if (i%64 == 0) {
                    if (i/64 == filesToRead.Length/64) {

                        doneEvents.Add(new ManualResetEvent[filesToRead.Length%64]);
                    }
                    else {
                        doneEvents.Add(new ManualResetEvent[64]);
                    }
                }
                doneEvents[doneEvents.Count-1][i%64] = new ManualResetEvent(false);
                var tempReader = new readBinaryFile(filesToRead[i], doneEvents[doneEvents.Count - 1][i % 64]);
                readers[i] = tempReader;
                ThreadPool.QueueUserWorkItem(tempReader.threadPoolCallback);
            }
        }


    }


    public class readBinaryFile {

        private ManualResetEvent _doneEvent;

        protected Stopwatch timer = new Stopwatch();
        public List<double[]> RawData { get; private set; }
        public string Path { get; }

        public TimeSpan ElapsedTime { get; private set; }

        public readBinaryFile(string path, ManualResetEvent doneEvent) {
            _doneEvent = doneEvent;
            Path = path;
        }

        public void threadPoolCallback(System.Object stateInfo) {
            try {
                // begin
                timer.Reset();
                timer.Start();
                using (FileStream fs = File.Open(Path, FileMode.Open, FileAccess.Read, FileShare.Read))
                using (BufferedStream bs = new BufferedStream(fs, 4 * 1024 * 1024))
                using (BinaryReader reader = new BinaryReader(bs)) {
                    BinaryFormatter formatter = new BinaryFormatter();
                    RawData = (List<double[]>)formatter.Deserialize(reader.BaseStream);
                }
                ElapsedTime = TimeSpan.FromMilliseconds(timer.ElapsedMilliseconds);
                timer.Stop();
                _doneEvent.Set();
            }
            catch (Exception er) {
                //throw;
            }
        }

    }

}


