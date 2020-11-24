using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using DeepDesignLab.PointCloud;
using System.IO;
using UnityEngine.UI;

public class VoxTreeDataManager : MonoBehaviour {

    //VoxTreeData pointCloud;
    VoxTree_SetupFiles setupFileReader;
    List<VoxTreeFileWriter> fileWriters = new List<VoxTreeFileWriter>();
    List<VoxTreeFileReader> fileReaders = new List<VoxTreeFileReader>();
    ThreadPoolReader poolReader;

    float[] readTimers; 
    float[] writeTimers;


    protected System.Diagnostics.Stopwatch timer = new System.Diagnostics.Stopwatch();


    public TextAsset[] ClassifiedClouds = new TextAsset[1];
    public VoxelTypes[] Classifications = new VoxelTypes[1];

    string importFilePath, exportFilePath;
    public Text text;
    float lastGameTime = 0;

    bool hasVoxTreeFiles;
    public bool forceVoxTreeFilesSetup = false;

    bool hasReadSetupFile = false;
    int voxTreeDataWrittingProgress = -1; // -1 not started. 0 started not finished. 1 is finished.
    int voxTreeDataReadingProgress = -1;
    int doStuffProgress = -1;

    string folderExport = "VoxTreeData";

    // TEMP DATA
    string[] filesToRead;

    // Use this for initialization
    void Start() {
        //pointCloud = new VoxTreeData();
        setupFileReader = new VoxTree_SetupFiles();
        //Debug.Log("Current path: " + Directory.GetCurrentDirectory());
        importFilePath = Path.Combine(Directory.GetCurrentDirectory(), AssetDatabase.GetAssetPath(ClassifiedClouds[0]));
        importFilePath = importFilePath.Replace("/", "\\");
        exportFilePath = Path.Combine(Directory.GetCurrentDirectory(), folderExport, Path.GetFileNameWithoutExtension(importFilePath));

        if (!Directory.Exists(exportFilePath)) {
            Directory.CreateDirectory(exportFilePath);
        }
        text.text = "Importing CSV from:\t" + importFilePath;
        Debug.Log("Importing CSV from:\t " + importFilePath);
        text.text = "\nExporting Files to:\t" + exportFilePath;
        Debug.Log("Exporting Files to:\t " + importFilePath);

        // TODO:
        // Have checksum file. 
        // nFiles; first file name/size; last file name/size; random file name/size 
        // Also cloud info. 
        // nVoxels; Header info; date of construction; date of access

        if (Directory.GetFiles(exportFilePath).Length > 1) {
            Debug.Log(string.Format("{0} Files found in directory.", Directory.GetFiles(exportFilePath).Length));
            hasVoxTreeFiles = true;
        }
        else {
            hasVoxTreeFiles = false;
        }

        if (forceVoxTreeFilesSetup) {
            
            hasVoxTreeFiles = false;
        }

        if (!hasVoxTreeFiles) {
            setupFileReader.readFile(importFilePath);
        }
    }

    // Update is called once per frame
    void Update() {
        CheckFileSetupReader();
        CheckVoxTreeFilesWriter();
        CheckVoxTreeFilesReader();
        doStuffWithData();
    }
    
    void doStuffWithData() {
        if (voxTreeDataReadingProgress == 1 && doStuffProgress == -1) {
            doStuffProgress = 0;
            timer.Reset(); // start timer
            timer.Start(); // start timer
            int nVoxelsRead = 0;
            foreach (var data in fileReaders) {
                nVoxelsRead += data.getData.Count;
            }
            Debug.Log("Number of Voxels read:\t" + nVoxelsRead.ToString());
        }
        if (voxTreeDataReadingProgress == 1 && doStuffProgress == 0) {
            doStuffProgress = 1;
            int nVoxelsRead = 0;
            poolReader = new ThreadPoolReader(exportFilePath);
            poolReader.ReadFiles();
        }
        if (voxTreeDataReadingProgress == 1 && doStuffProgress == 1 && poolReader.HaveFinished) {
           doStuffProgress = 2;
        }
        if (voxTreeDataReadingProgress == 1 && doStuffProgress == 2) {
            string report="";
            for (int i = 0; i < poolReader.readers.Length; i++) {
                report += poolReader.readers[i].Path + "\t pts: " + poolReader.readers[i].RawData.Count.ToString()+ "\tTime: " + poolReader.readers[i].ElapsedTime.TotalSeconds + "s\t Size: " + ((new FileInfo(poolReader.readers[i].Path)).Length*1.0/(1024*1024)).ToString() +"Mb\n";
            }
            timer.Stop();
            Debug.Log("Read with Pool all to file!");
            Debug.Log(string.Format("Reading with pool took {0} seconds", timer.ElapsedMilliseconds * 1.0 / 1000.0));// Time.fixedTime - writeTimers[0]));
            Debug.Log(report);
            doStuffProgress = 3;
        }
    }

    IEnumerable UpdateDisplayText() {
        for (; ; )
        {
            lastGameTime = Time.fixedTime;

            text.text = "File Path:\t" + importFilePath;
            text.text += "\nTime left: " + setupFileReader.timeRemaining.ToString(@"dd\.hh\:mm\:ss");
            text.text += "\nMsg:    \t" + setupFileReader.getMessages;

            yield return new WaitForSeconds(0.5f);
        }

    }

    void CheckFileSetupReader() {
        if (hasVoxTreeFiles) {
            hasReadSetupFile = true;
        }
        if (setupFileReader.hasFinished && !hasReadSetupFile) {
            hasReadSetupFile = true;
            Debug.Log("Error:\t " + setupFileReader.getErrors);
            Debug.Log("Message:\t " + setupFileReader.getMessages);
        }
    }

    void CheckVoxTreeFilesWriter() {
        if (hasVoxTreeFiles) {
            voxTreeDataWrittingProgress = 1;
        }
        else {
            if (hasReadSetupFile && voxTreeDataWrittingProgress == -1) {
                voxTreeDataWrittingProgress = 0;
                timer.Reset(); // start timer
                timer.Start(); // start timer
                Debug.Log("Files to write: " + setupFileReader.getData.Count.ToString());
                writeTimers = new float[setupFileReader.getData.Count + 1];
                writeTimers[0] = Time.fixedTime;
                foreach (KeyValuePair<Vector2, List<double[]>> bucket in setupFileReader.getData) {
                    fileWriters.Add(new VoxTreeFileWriter(bucket.Value, Path.Combine(exportFilePath, string.Format("{0:0}_{1:0}.bin", bucket.Key.x, bucket.Key.y))));
                }
                Debug.Log("Writing file at: " + exportFilePath);
            }
            if (voxTreeDataWrittingProgress == 0 && fileWriters.Count > 0) {
                int check = 0;
                for (int i = 0; i < fileWriters.Count; i++) {
                    if (!fileWriters[i].hasFinished) {
                        check++;
                        break;
                    }
                    else {
                        // Has finished this one!
                        if (writeTimers[i + 1] == 0) {
                            writeTimers[i + 1] = timer.ElapsedMilliseconds*1.0f/1000.0f;
                        }
                    }
                }
                if (check == 0) {
                    voxTreeDataWrittingProgress = 1;
                    hasVoxTreeFiles = true;
                    timer.Stop();
                    Debug.Log("Written all to file!");
                    Debug.Log(string.Format("Writing took {0} seconds", timer.ElapsedMilliseconds * 1.0 / 1000.0));// Time.fixedTime - writeTimers[0]));
                    Debug.Log(string.Join("\t", writeTimers));
                }
                else {
                    text.text = "Writing Setup Files at Path:\t" + exportFilePath;
                    text.text += "\nPercentage Complete: " + check*1.0f/ fileWriters.Count;

                }
            }
        }
    }

    void CheckVoxTreeFilesReader() {
        if (hasVoxTreeFiles) {
            if (voxTreeDataWrittingProgress == 1 && voxTreeDataReadingProgress == -1) {
                voxTreeDataReadingProgress = 0;
                timer.Reset(); // start timer
                timer.Start(); // start timer
                filesToRead = Directory.GetFiles(exportFilePath);
                Debug.Log("Files to read: " + filesToRead.Length.ToString());
                Debug.Log("Starting multiThread reader: ");
                readTimers = new float[filesToRead.Length + 1];
                readTimers[0] = Time.fixedTime;
                foreach (string path in filesToRead) {
                    fileReaders.Add(new VoxTreeFileReader(path));
                }
            }
            if (voxTreeDataReadingProgress == 0 && fileReaders.Count > 0) {
                int check = 0;
                for (int i = 0; i < fileReaders.Count; i++) {
                    if (!fileReaders[i].hasFinished) {
                        check++;
                        break;
                    }
                    else {
                        // Has finished this one!
                        if (readTimers[i + 1] == 0) {
                            readTimers[i + 1] = Time.fixedTime - readTimers[0];
                        }


                    }
                }
                if (check == 0) {
                    voxTreeDataReadingProgress = 1;
                    timer.Stop();
                    Debug.Log("Read all to file!");
                    Debug.Log(string.Format("Reading MultiThreads took {0} seconds", timer.ElapsedMilliseconds * 1.0 / 1000.0));// Time.fixedTime - readTimers[0]));
                    Debug.Log(string.Join("\t", readTimers));
                }
                else {
                    text.text = "Reading Setup Files at Path:\t" + exportFilePath;
                    text.text += "\nPercentage Complete: " + check * 1.0f / fileReaders.Count;

                }
            }
        }
    }

    void OnApplicationQuit() {
        Debug.Log("Application ending after " + Time.time + " seconds");
        Debug.Log("Error:\t " + setupFileReader.getErrors);
        setupFileReader.cancel();
        foreach (var item in fileWriters) {
            item.cancel();
        }
        Debug.Log("File reader stopped. " + Time.time + " seconds");
    }






}