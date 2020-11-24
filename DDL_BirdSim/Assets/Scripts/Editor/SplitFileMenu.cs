
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


public class SplitFileMenu : EditorWindow {

    const int top_padding = 2;
    string messageText = "*******************************\n    THIS IS NOT WORKING\n*******************************\nPlease finish setup.\n\t1)  Name the new output folder.\n\t2)  Select the output parent folder.\n\t3)  Select the .pts file to process.\n";

    // STD dims, Button = 25

    static float lineHeight = 20;
    static float s_Setupheight = 200;
    static float s_ProgressHeight = 300;
    static float width = 300f;
    static float msgHeight = 200;
    static float s_winHeight = 500;
   // static Rect s_MessageRect = new Rect(0, s_topOffset, s_WinWidth, s_MsgLength);
   // static Rect s_listRect = new Rect(0, s_MsgLength+s_topOffset, s_WinWidth, s_MsgLength + s_winHeight);

    static CCReader readPoints = new CCReader(2);
    

    static string path = null;
    static string outputPath = null;
    static string newFolderName = null;


    public float progress = 0f;
    bool FinsihedJob = false;
    static bool startedJob = false;
    

    // Use this for initialization
    [MenuItem("Deep Design Lab/CloudCompare Files/Split txt File")]
    static void getPtsFiles() {

        //EditorWindow window = GetWindow(typeof(CustomMenu));
        SplitFileMenu window = EditorWindow.GetWindow<SplitFileMenu>(true, "CC txt file to load");
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
            return true;
        }
        catch (Exception e) {
            Debug.Log("You don't get the point do you...");
            Debug.LogError(e.Message);
        }
        return false;
    }
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


    static void processFiles(string path) {
        try {
            if (path.EndsWith(".txt")) {

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

    float outputStart = 35;
    float inputStart = 70;
    float buttonHeight = 15;
    float buttonWidth = 200;
    static string inputText = "Please select a PointCloud text file to process.";
    static string outputText = "Please select an output location.";

    float progressStart = 100;
    float msgStart = 115;

    string additionalText;

    

    private void OnGUI() {
        EditorGUI.LabelField(new Rect(0, 0, width, lineHeight), "Setup",EditorStyles.boldLabel);

        newFolderName = EditorGUI.TextField(new Rect(10, 15, width-20, buttonHeight), new GUIContent("Folder name:"), newFolderName);
               
        if (GUI.Button(new Rect(50, outputStart, buttonWidth, buttonHeight), "Select Output Folder", EditorStyles.miniButtonRight)) {
            SetNewFolder();
        }
        EditorGUI.LabelField(new Rect(50, outputStart + buttonHeight - 5, width, lineHeight), outputText, EditorStyles.centeredGreyMiniLabel);

        if (GUI.Button(new Rect(50, inputStart, buttonWidth, buttonHeight), "Select PointCloud Text File", EditorStyles.miniButtonRight)) {
            SetNewFile();
        }
        EditorGUI.LabelField(new Rect(50, inputStart + buttonHeight - 5, width, lineHeight), inputText, EditorStyles.centeredGreyMiniLabel);



        EditorGUI.LabelField(new Rect(0, progressStart,width, lineHeight), "Process Files", EditorStyles.boldLabel);

        if (path != null && outputPath!= null) {
            
            msgStart = 135;
            if (Directory.Exists(outputPath + "/" + newFolderName)) {
                additionalText =   "                      *****DANGER*****\n" +
                                "                      NEW FOLDER EXISTS\n" +
                                "  THE FOLDER IS DELETED WHEN PROCESSING BEGINS\n\n" +
                                "To keep all files, please change the name of the desired output in the text box at the start of the setup section.\n\n";
            }
            else {
                additionalText = "Press the button above to begin processing.\n\n";
            }

            if (startedJob) {
                messageText = "";
                
                EditorGUI.ProgressBar(new Rect(10, msgStart + msgHeight + 10, width - 20, 15), readPoints.getProgressValue, string.Format("{0} days, {1} hrs, {2} mins, {3}secs", readPoints.timeRemaining.Days, readPoints.timeRemaining.Hours, readPoints.timeRemaining.Minutes, readPoints.timeRemaining.Seconds));

                if (GUI.Button(new Rect(50, msgStart - 20, buttonWidth, buttonHeight), "Cancel", EditorStyles.miniButtonRight)) {
                    readPoints.cancel();
                }

                if (readPoints.hasFinished) {
                    messageText += "\nAll Finished.\n\nPress button to load a new file.";
                    inputText = "Please select a PointCloud text file to process.";
                    FinsihedJob = false;
                    startedJob = false;
                    EditorUtility.ClearProgressBar();
                    path = null;
                }
                messageText = readPoints.getMessages;
            }
            else {
                if (GUI.Button(new Rect(50, msgStart - 20, buttonWidth, buttonHeight), "Start Processing", EditorStyles.miniButtonRight)) {
                    processFiles(path);
                }
            }

        }

        EditorGUI.TextArea(new Rect(10, msgStart, width - 20, msgHeight), additionalText + messageText, EditorStyles.helpBox);
        additionalText = "";
       // messageText = "Please finish setup.\n\t1)  Name the new output folder.\n\t2)  Select the output parent folder.\n\t3)  Select the .pts file to process.\n"; 
        // GUI.skin.verticalScrollbar
        //Vector2 ScrollPos = Vector2.zero;
        // scrollPos = EditorGUILayout.BeginScrollView(scrollPos, GUILayout.Width(100), GUILayout.Height(100));
        // ScrollPos = EditorGUILayout.BeginScrollView(ScrollPos, GUILayout.Height(msgHeight), GUILayout.Width(width - 20));
        // EditorGUILayout.EndScrollView();


        // EditorGUI.PrefixLabel(new Rect(0, s_topOffset * 0, s_WinWidth, s_topOffset), new GUIContent("This is a prefix lable."));
        //EditorGUI.TextField(new Rect(0, s_topOffset *1, s_WinWidth, s_topOffset), "This is a text field.");
        //EditorGUI.TextArea(new Rect(0, s_topOffset * 2,  s_WinWidth, s_topOffset), "This is a text area.");
        //  EditorGUI.LabelField(new Rect(0, s_topOffset * 3,  s_WinWidth, s_topOffset), "This is a lable.");
        //  GUI.Button(new Rect(0, s_topOffset * 4, s_WinWidth, 15), "Set your output folder");

        /*
        if (startedJob) {
            // update progress bar if not finished... 

            messageText = readPoints.getMessages;

            if (FinsihedJob) {
                messageText += "\nAll Finished.\n\nPress button to load a new file.";
                FinsihedJob = false;
                startedJob = false;
                EditorUtility.ClearProgressBar();
                path = null;
            }
            EditorGUI.ProgressBar(new Rect(10, msgStart + msgHeight + 10, width - 20, 15), readPoints.getProgressValue, readPoints.timeRemaining.ToString("c"));
        }

        if (!messageRead && startedRead) {
            if (readPoints.hasFinished) {
                messageText += "\nAll Finished.\n\nPress button to load a new file.";
                startedRead = false;
                EditorUtility.ClearProgressBar();
                path = null;
            }
        }
        if (path == null) {

            if (GUILayout.Button("Add path from browser")) {
                if (loadFiles()) {
                    messageText = "Loaded a new file!\n\n";
                    messageText += string.Format("File:\t {0}\n\nPress the button above to process document.\n",path);
                }
                messageRead = false;
            }
        }
        else {
            if (GUILayout.Button("Process the loaded file")) {
                processFiles(path);
                messageRead = false;
            }
        }
        */




    }


}




