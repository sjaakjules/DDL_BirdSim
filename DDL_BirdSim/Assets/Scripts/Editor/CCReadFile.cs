
using UnityEngine;
using UnityEditor;
using UnityEditorInternal;
using System.IO;
using System.Threading;
using System.Collections;
using System.Collections.Generic;
using System;
using System.Threading.Tasks;
using DeepDesignLab.PointCloud;


public class CCReadFile : EditorWindow {

    const int top_padding = 2;
    string messageText = "Please select the .txt file to process.\n\nYour cloudCompare file MUST be exported with header names AND number of points.\n";

    // STD dims, Button = 25

    static float lineHeight = 20;
    static float s_Setupheight = 200;
    static float s_ProgressHeight = 200;
    static float width = 300f;
    static float msgHeight = 200;
    static float s_winHeight = 500;
   // static Rect s_MessageRect = new Rect(0, s_topOffset, s_WinWidth, s_MsgLength);
   // static Rect s_listRect = new Rect(0, s_MsgLength+s_topOffset, s_WinWidth, s_MsgLength + s_winHeight);

    public static CCReader readPoints = new CCReader(2);
    

    static string path = null;
    //static string outputPath = null;
    static string newFolderName = null;


    public float progress = 0f;
    bool FinsihedJob = false;
    static bool startedJob = false;
    

    // Use this for initialization
    [MenuItem("Deep Design Lab/CloudCompare Files/Read txt File")]
    static void getPtsFiles() {

        //EditorWindow window = GetWindow(typeof(CustomMenu));
        CCReadFile window = EditorWindow.GetWindow<CCReadFile>(true, "Read CloudCompare files.");
        window.minSize = new Vector2(300,500);
        window.ShowPopup();
        window.Show();
    }
    

    private void OnInspectorUpdate() {
        Repaint();
    }

    static bool SetNewFile() {

        try {
            path = EditorUtility.OpenFilePanel("Load pts files", "", "");
            inputText = string.Format("Filename:\t {0}",path.Substring(path.LastIndexOf('/')));
            clearReader();
            return true;
        }
        catch (Exception e) {
            Debug.Log("You don't get the point do you...");
            Debug.LogError(e.Message);
        }
        clearReader();
        return false;
    }
    /*
    static bool SetNewFolder() {

        try {
            outputPath = EditorUtility.OpenFolderPanel("Load pts files", "", "");
            outputText = outputPath;
            return true;
        }
        catch (Exception e) {
            Debug.Log("You don't get the point do you...");
            Debug.LogError(e.Message);
        }
        return false;
    }
    */

    static void processFiles(string path) {
        try {
            if (path.EndsWith(".txt")) {
                readPoints = new CCReader(2);
                startedJob = true;
                readPoints.readFile(path);
            }
        }
        catch (Exception e) {
            Debug.Log("You don't get the point do you...");
            Debug.LogError(e.Message);
        }
    }

    static void clearReader() {
        readPoints = null;
        GC.Collect();
    }


    Vector2 buttonSzie = new Vector2(100, 20);

   // float outputStart = 35;
    float inputStart = 35;
    float buttonHeight = 15;
    float buttonWidth = 200;
    static string inputText = "Please select a PointCloud text file to process.";
    static string outputText = "Please select an output location.";

    float progressStart = 100;
    float msgStart = 115;

    string additionalText;

    

    private void OnGUI() {
        /*
        EditorGUI.LabelField(new Rect(0, 0, width, lineHeight), "Setup",EditorStyles.boldLabel);

        newFolderName = EditorGUI.TextField(new Rect(10, 15, width-20, buttonHeight), new GUIContent("Folder name:"), newFolderName);
               
        if (GUI.Button(new Rect(50, outputStart, buttonWidth, buttonHeight), "Select Output Folder", EditorStyles.miniButtonRight)) {
            SetNewFolder();
        }
        EditorGUI.LabelField(new Rect(50, outputStart + buttonHeight - 5, width, lineHeight), outputText, EditorStyles.centeredGreyMiniLabel);
        */
        if (GUI.Button(new Rect(50, inputStart, buttonWidth, buttonHeight), "Select PointCloud Text File", EditorStyles.miniButtonRight)) {
            SetNewFile();
        }
        EditorGUI.LabelField(new Rect(0, inputStart + buttonHeight - 5, width, lineHeight), inputText, EditorStyles.centeredGreyMiniLabel);



        EditorGUI.LabelField(new Rect(0, progressStart,width, lineHeight), "Process Files", EditorStyles.boldLabel);

        if (path != null ) {
            
            msgStart = 135;
            additionalText = "Press the button above to begin processing.\n\n";

            messageText = "";
            if (startedJob) {
                messageText = "";
                additionalText = "Press the above button to cancel job.\n\n";

                EditorGUI.ProgressBar(new Rect(10, msgStart + msgHeight + 10, width - 20, 15), readPoints.getProgressValue, string.Format("{0} days, {1} hrs, {2} mins, {3}secs", readPoints.timeRemaining.Days, readPoints.timeRemaining.Hours, readPoints.timeRemaining.Minutes, readPoints.timeRemaining.Seconds));

                if (GUI.Button(new Rect(50, msgStart - 20, buttonWidth, buttonHeight), "Cancel", EditorStyles.miniButtonRight)) {
                    readPoints.cancel();
                }

                if (readPoints.hasFinished) {
                    messageText += "All Finished.\n\nPress button to load a new file.\n\n";
                    inputText = "Please select a PointCloud text file to process.";
                    FinsihedJob = false;
                    startedJob = false;
                    EditorUtility.ClearProgressBar();
                    path = null;
                }
                messageText = messageText + readPoints.getMessages;
            }
            else {
                if (GUI.Button(new Rect(50, msgStart - 20, buttonWidth, buttonHeight), "Start Processing", EditorStyles.miniButtonRight)) {
                    processFiles(path);
                }
            }

        }

        EditorGUI.TextArea(new Rect(10, msgStart, width - 20, msgHeight), additionalText + messageText, EditorStyles.helpBox);
        additionalText = "";
       
        

    }


}




enum DataProperty {
    Invalid,
    X, Y, Z,
    R, G, B, A
}

class DataHeader {
    public List<DataProperty> properties = new List<DataProperty>();
    public int vertexCount = -1;
}

class DataBody {
    public Vector3[] verticesLocation;
    public Color32[] verticesColours;

    public DataBody(int vertexCount) {
        verticesLocation = new Vector3[vertexCount];
        verticesColours = new Color32[vertexCount];
    }
}
