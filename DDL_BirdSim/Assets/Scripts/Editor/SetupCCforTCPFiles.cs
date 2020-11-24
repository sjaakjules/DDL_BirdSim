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


public class SetupCCforTCParticleFiles : EditorWindow
{

    VoxTree_SetupFiles FileReader = new VoxTree_SetupFiles();


    const int top_padding = 2;
    string messageText = "Please select the .txt file to process.\n\nYour cloudCompare file MUST be exported with header names AND number of points.\n";

    // STD dims, Button = 25

    float lineHeight = 20;
    float s_Setupheight = 200;
    float s_ProgressHeight = 200;
    float width = 300f;
    float msgHeight = 200;
    float s_winHeight = 500;


    static string path = null;
    //static string outputPath = null;
    string newFolderName = null;


    public float progress = 0f;
    bool FinsihedJob = false;
    bool startedJob = false;
    bool loadedAsset = false;

    // Use this for initialization
    [MenuItem("Deep Design Lab/CloudCompare Files/Setup Cloud")]
    static void getPtsFiles()
    {
        //EditorWindow window = GetWindow(typeof(CustomMenu));
        CCLoadFile window = EditorWindow.GetWindow<CCLoadFile>(true, "Read CloudCompare files.");
        window.minSize = new Vector2(300, 500);
        window.ShowPopup();
        window.Show();
    }

    private void OnInspectorUpdate()
    {
        Repaint();
    }


    Vector2 buttonSzie = new Vector2(100, 20);

    float outputStart = 15;
    float inputStart = 50;
    float buttonHeight = 15;
    float buttonWidth = 200;

    string SecondButtonText = "Please select a PointCloud text file to process.";
    string FirstButtonText = "Please select an output location.";

    float progressStart = 100;
    float msgStart = 115;

    string additionalText;

    bool SetNewAsset()
    {

        try
        {
            /*
            path = EditorUtility.OpenFilePanel("Load pts files", "", "");
            string relPath = path.Substring(Application.dataPath.Length - "Assets".Length);
            LoadedData = AssetDatabase.LoadAssetAtPath(relPath, typeof(PointCloudData)) as PointCloudData;
            relPath = AssetDatabase.GetAssetPath(LoadedData);
            if (LoadedData)
            {
                EditorPrefs.SetString("ObjectPath", relPath);
                FirstButtonText = string.Format("Filename:\t {0}", path.Substring(path.LastIndexOf('/')));
                return true;
            }
            */
        }
        catch (Exception e)
        {
            Debug.Log("You don't get the point do you...");
            Debug.LogError(e.Message);
        }
        return false;
    }

    void processFiles(string path)
    {
        try
        {
            if (path.EndsWith(".txt"))
            {
                FileReader = new VoxTree_SetupFiles();
                startedJob = true;
                FileReader.readFile(path);
            }
        }
        catch (Exception e)
        {
            Debug.Log("You don't get the point do you...");
            Debug.LogError(e.Message);
        }
    }
    bool SetNewFile()
    {

        try
        {
            path = EditorUtility.OpenFilePanel("Load pts files", "", "");
            SecondButtonText = string.Format("Filename:\t {0}", path.Substring(path.LastIndexOf('/')));
            //clearReader();
            return true;
        }
        catch (Exception e)
        {
            Debug.Log("You don't get the point do you...");
            Debug.LogError(e.Message);
        }
        //clearReader();
        return false;
    }


    void OnGUI()
    {
        /*
        EditorGUI.LabelField(new Rect(0, 0, width, lineHeight), "Setup",EditorStyles.boldLabel);

        newFolderName = EditorGUI.TextField(new Rect(10, 15, width-20, buttonHeight), new GUIContent("Folder name:"), newFolderName);
             */
        // first button
        // 
        if (GUI.Button(new Rect(50, outputStart, buttonWidth, buttonHeight), "Find PointCloudData Assest", EditorStyles.miniButtonRight))
        {
            loadedAsset = SetNewAsset();
        }
        EditorGUI.LabelField(new Rect(0, outputStart + buttonHeight - 5, width, lineHeight), FirstButtonText, EditorStyles.centeredGreyMiniLabel);

        // Second button.
        if (GUI.Button(new Rect(50, inputStart, buttonWidth, buttonHeight), "Select PointCloud Text File", EditorStyles.miniButtonRight))
        {
            SetNewFile();
        }
        EditorGUI.LabelField(new Rect(0, inputStart + buttonHeight - 5, width, lineHeight), SecondButtonText, EditorStyles.centeredGreyMiniLabel);



        EditorGUI.LabelField(new Rect(0, progressStart, width, lineHeight), "Process Files", EditorStyles.boldLabel);

        if (path != null)
        {

            msgStart = 135;
            additionalText = "Press the button above to begin processing.\n\n";

            messageText = "";
            if (startedJob)
            {
                messageText = "";
                additionalText = "Press the above button to cancel job.\n\n";

               // EditorGUI.ProgressBar(new Rect(10, msgStart + msgHeight + 10, width - 20, 15), readPoints.getProgressValue, string.Format("{0} days, {1} hrs, {2} mins, {3}secs", readPoints.timeRemaining.Days, readPoints.timeRemaining.Hours, readPoints.timeRemaining.Minutes, readPoints.timeRemaining.Seconds));

                if (GUI.Button(new Rect(50, msgStart - 20, buttonWidth, buttonHeight), "Cancel", EditorStyles.miniButtonRight))
                {
                   // readPoints.cancel();
                }
                /*
                if (!readPoints.isActive)
                {
                    messageText += "All Finished.\n\nPress button to load a new file.\n\n";
                    SecondButtonText = "Please select a PointCloud text file to process.";
                    FinsihedJob = false;
                    startedJob = false;
                    EditorUtility.ClearProgressBar();
                    path = null;
                }
                messageText = messageText + readPoints.getMessages;
                */
            }
            else if (loadedAsset)
            {
                if (GUI.Button(new Rect(50, msgStart - 20, buttonWidth, buttonHeight), "Start Processing", EditorStyles.miniButtonRight))
                {
                    processFiles(path);
                }
            }
            else
            {
                if (GUI.Button(new Rect(50, msgStart - 20, buttonWidth, buttonHeight), "Create new Asset", EditorStyles.miniButtonRight))
                {
                   // loadedAsset = createAsset();
                }
            }

        }
        else
        {
            /*
            if (readPoints != null && LoadedData != null && readPoints.hasFinished)
            {
                additionalText = string.Format("Have read file.\n{0} voxels within shared memory.\n", LoadedData.getNumberOfVoxels);
                //  AssetDatabase.SaveAssets();
            }
            */

        }

        EditorGUI.TextArea(new Rect(10, msgStart, width - 20, msgHeight), additionalText + messageText, EditorStyles.helpBox);
        additionalText = "";

        if (GUI.changed)
        {
            //EditorUtility.SetDirty(LoadedData);
        }

    }

}
