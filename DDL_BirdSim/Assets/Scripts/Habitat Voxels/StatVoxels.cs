using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DeepDesignLab.PointCloud;
using System.IO;
using UnityEditor;
//using MathNet.Numerics.Statistics;

public class StatVoxels : MonoBehaviour {


    public TextAsset[] ClassifiedClouds = new TextAsset[1];
    public VoxelTypes[] Classifications = new VoxelTypes[1];

    private Dictionary<Vector3, List<double[]>> RawData = new Dictionary<Vector3, List<double[]>>();

    public string filePath = "";

    public int currentRow = -1;

    bool hasReadFile = false;
    bool readingFile = false;

    private void OnGUI() {

        filePath = Path.Combine(Directory.GetCurrentDirectory(), AssetDatabase.GetAssetPath(ClassifiedClouds[0]));
        filePath = filePath.Replace("/", "\\");
    }

    // Use this for initialization
    void Start () {

        filePath = Path.Combine(Directory.GetCurrentDirectory(), AssetDatabase.GetAssetPath(ClassifiedClouds[0]));
        filePath = filePath.Replace("/", "\\");


    }
	
	// Update is called once per frame
	void Update () {
        if (!readingFile && !hasReadFile) {
            StartCoroutine("readFile", filePath);
        }

	}


    IEnumerator readFile(string fileName) {
        readingFile = true;
        List<string> ColumnNames = new List<string>();
        int nColumns = -1;
        string ret = "\r";
        char columnChar = ',';

        

        int nNotRead = -1;

        int nPoints = -1;

        using (StreamReader reader = new StreamReader(filePath)) {
            currentRow = 0;

            string line;
            string[] rowTexts;
            double ReadValue;

            Vector3 key;

            // READ HEADER LINE
            line = reader.ReadLine();
            rowTexts = line.Split(columnChar);
            for (int i = 0; i < rowTexts.Length; i++) {
                rowTexts[i] = rowTexts[i].Replace(ret, "");
            }
            ColumnNames = new List<string>(rowTexts);
            nColumns = ColumnNames.Count;


            // READ FIRST LINE AS N POINTS
            line = reader.ReadLine();
            if (!int.TryParse(line, out nPoints)) {
                Debug.LogError("Error, Unknown number of points. CSV must start with number of points to read.");
                Debug.Log("Line: " + line);
                //throw new System.Exception("Put your error message here.");
            }


            // READ DATA
            for (int i = 0; i < nPoints; i++) {
                line = reader.ReadLine();

                rowTexts = line.Split(columnChar);
                if (rowTexts.Length == nColumns) {

                    double[] rowValues = new double[rowTexts.Length];

                    for (int k = 0; k < rowTexts.Length; k++) {
                        if (double.TryParse(rowTexts[k], out ReadValue)) {
                            rowValues[k] = ReadValue;
                        }
                        else {
                            rowValues[k] = double.NaN;
                        }
                    }      
                    key = new Vector3(((int)(rowValues[0] * 10)) * 0.1f, ((int)(rowValues[1] * 10)) * 0.1f, ((int)(rowValues[2] * 10)) * 0.1f);
                    
                    if (RawData.ContainsKey(key)) {
                        RawData[key].Add(rowValues);
                    }
                    else {
                        RawData.Add(key, new List<double[]>());
                        RawData[key].Add(rowValues);
                    }

                }
                else {
                    nNotRead++;
                }
                currentRow++;
                yield return null;
            }
            hasReadFile = true;

        }

    }
    

}
