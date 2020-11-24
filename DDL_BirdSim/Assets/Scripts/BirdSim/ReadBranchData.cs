using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DeepDesignLab.Base;
using UnityEditor;
using System.IO;
using UnityEngine.UI;
using System.ComponentModel;
using Unity.Collections;
using System;
using DeepDesignLab.BirdSim;

public class ReadBranchData : MonoBehaviour
{
    CSVreader[] branchInfo;
    TreeInfo[] trees;

    GameObject[] treeGameObjects;

    public Text debugText;
    public GameObject treeOriginObject;
    public GameObject mesh;
    public bool drawTree;
    public TextAsset[] branchCSVDocuments;

    // For display
    public int documentSelector;
    public int rowSelector;
    public bool updateSelection;
    string[] SelectedHeader;
    string[] SelectedRow;

   
    public bool haveReadAll;
    public bool haveLoadedTreeData;
    public bool placedTrees;
    public bool instantiateTrees;
    public bool useBranchLines;

    // Start is called before the first frame update
    void Start()
    {
        //Set all arrays.
        branchInfo = new CSVreader[branchCSVDocuments.Length];
        trees = new TreeInfo[branchCSVDocuments.Length];
        treeGameObjects = new GameObject[branchCSVDocuments.Length];

        // Get paths of CSV data to read at game time.
        for (int i = 0; i < branchInfo.Length; i++)
        {
            branchInfo[i] = new CSVreader(1);
            string fullPath = getFullPath(branchCSVDocuments[i]);// Path.Combine(Directory.GetParent(Application.dataPath).FullName, AssetDatabase.GetAssetPath(branchCSVDocuments[i]).Replace("/", "\\"));
            //AssetDatabase.get
            //debugText.text = string.Format("Reading CSV {0} from: \"{1}\"",i, fullPath) + debugText.text;
            branchInfo[i].readFile(fullPath);
        }

        // Force debug text to screen.
        InvokeRepeating("UpdateSelection", 0.5f, 0.5f);
    }

    // Update is called once per frame
    void Update()
    {
        // Force update of Debug text to screen. This is called every 0.5s ^^ InvokeRepeating() above.
        if (updateSelection)
        {
            updateSelection = false;
            UpdateSelection();
        }

        // Read every CSV
        if (!haveReadAll)
        {
            int count = 0;
            for (int i = 0; i < branchInfo.Length; i++)
            {
                if (branchInfo[i].hasFinished)
                {
                    count++;
                }
            }
            if (count == branchInfo.Length)
            {
                haveReadAll = true;
            }
        }

        // Create treeInfo for each CSV read.
        if (haveReadAll && !haveLoadedTreeData)
        {
            for (int i = 0; i < trees.Length; i++)
            {
                trees[i] = new TreeInfo(new List<string[]>(branchInfo[i].GetTable), i);
            }

            haveLoadedTreeData = true;
        }

        // Create a game object at the base of all read trees.
        if (haveReadAll && haveLoadedTreeData && !placedTrees)
        {
            for (int i = 0; i < trees.Length; i++)
            {
                treeGameObjects[i] = Instantiate(treeOriginObject, trees[i].origin, Quaternion.identity, this.transform);
            }
            placedTrees = true;
        }

        // creaet gameObjects at each branch or Branch line. Only work once files are read and treeData is loaded.
        if (haveReadAll && haveLoadedTreeData && placedTrees && instantiateTrees)
        {
            for (int i = 0; i < treeGameObjects.Length; i++)
            {
                if (i != documentSelector||
                    treeGameObjects[documentSelector].transform.childCount != trees[documentSelector].branches.Count && !useBranchLines ||
                    treeGameObjects[documentSelector].transform.childCount != trees[documentSelector].branchLines.Count && useBranchLines)
                {
                    foreach (Transform child in treeGameObjects[i].transform)
                    {
                        GameObject.Destroy(child.gameObject);
                    }
                }
            }
            if (treeGameObjects[documentSelector].transform.childCount ==0)
            {
                if (useBranchLines)
                {
                    foreach (var branchLine in trees[documentSelector].branchLines)
                    {
                        //Graphics.DrawMeshNow(((MeshFilter)mesh.GetComponent("MeshFilter")).mesh, branchLine.Value.transform);
                        Graphics.DrawMesh(mesh.GetComponent<MeshFilter>().mesh, branchLine.Value.transform, mesh.GetComponent<Renderer>().material, 0);
                        //(Instantiate(mesh, branchLine.Value.position, branchLine.Value.Orentation, treeGameObjects[documentSelector].transform)).transform.localScale = branchLine.Value.scale;
                    }
                }
                else
                {
                    foreach (var branch in trees[documentSelector].branches)
                    {
                        //Graphics.DrawMeshNow(((MeshFilter)mesh.GetComponent("MeshFilter")).mesh, branch.Value.transform);
                        Graphics.DrawMesh(mesh.GetComponent<MeshFilter>().mesh, branch.Value.transform, mesh.GetComponent<Renderer>().material, 0);
                        //(Instantiate(mesh, branch.Value.position, branch.Value.Orentation, treeGameObjects[documentSelector].transform)).transform.localScale = branch.Value.scale;
                    }
                }
            }
        }
        else if (haveReadAll && haveLoadedTreeData && placedTrees && !instantiateTrees)
        {

            for (int i = 0; i < treeGameObjects.Length; i++)
            {
                foreach (Transform child in treeGameObjects[i].transform)
                {
                    GameObject.Destroy(child.gameObject);
                }
            }
        }

        if (drawTree && haveLoadedTreeData)
        {
            
            for (int i = 0; i < trees.Length; i++)
            {
                
                //int count = (int)Math.Ceiling(trees[i].branchTransforms.Length / 1000.0);
                //int rem = trees[i].branchTransforms.Length % 1000;
                for (int j = 0; j < trees[i].branchTransforms.Count; j++)
                {
                    MaterialPropertyBlock props = new MaterialPropertyBlock();
                    /*
                    Vector4[] rndColour = new Vector4[trees[i].branchTransforms[j].Count];
                    for (int k = 0; k < trees[i].branchTransforms[j].Count; k++)
                    {
                        float r = UnityEngine.Random.Range(0.0f, 1.0f);
                        float g = UnityEngine.Random.Range(0.0f, 1.0f);
                        float b = UnityEngine.Random.Range(0.0f, 1.0f);
                        rndColour[k] = new Vector4(r, g, b,1);
                    }
                    //props.SetColor("_Color", new Color(r, g, b));
                    */
                    props.SetVectorArray("_Color", trees[i].branchColour[j]);
                    Graphics.DrawMeshInstanced(mesh.GetComponent<MeshFilter>().mesh,
                                                0,
                                                mesh.GetComponent<Renderer>().material,
                                                trees[i].branchTransforms[j],//[j*(j==count? rem:1000)...(j+1) * (j == count ? rem : 1000)], 
                                                props);
                }

                /*
                foreach (var branchLine in trees[i].branchLines)
                {
                    //Graphics.DrawMesh(((MeshFilter)mesh.GetComponent("MeshFilter")).mesh, branchLine.Value.transform, mat, 0);
                    //  Because DrawMesh does not draw mesh immediately, modifying material properties between calls to this function won't 
                    //  make the meshes pick up them. If you want to draw series of meshes with the same material, but slightly different properties 
                    // (e.g. change color of each mesh), use MaterialPropertyBlock parameter.
                    float r = Random.Range(0.0f, 1.0f);
                    float g = Random.Range(0.0f, 1.0f);
                    float b = Random.Range(0.0f, 1.0f);
                    props.SetColor("_Color", new Color(r, g, b));
                    Graphics.DrawMesh(mesh.GetComponent<MeshFilter>().mesh, branchLine.Value.transform, mesh.GetComponent<Renderer>().material,0);
                    //Graphics.DrawMeshNow(mesh.GetComponent<MeshFilter>().mesh, branchLine.Value.transform);
                }
                */

            }
        }
        
    }

    private void OnPostRender()
    {
        if (drawTree && haveLoadedTreeData)
        {
            //if (mesh.GetComponent<Renderer>().material.SetPass(0))
            //{
                for (int i = 0; i < trees.Length; i++)
                {
                    foreach (var branchLine in trees[i].branchLines)
                    {
                       // Graphics.DrawMesh((mesh.GetComponent<MeshFilter>()).mesh, branchLine.Value.transform, mesh.GetComponent<Renderer>().material, 0);
                        //  Because DrawMesh does not draw mesh immediately, modifying material properties between calls to this function won't 
                        //  make the meshes pick up them. If you want to draw series of meshes with the same material, but slightly different properties 
                        // (e.g. change color of each mesh), use MaterialPropertyBlock parameter.

                        //Graphics.DrawMeshNow(mesh, trees[i].branchLines[j].transform);
                    }
                }
            //}
        }
    }
    
    string getFullPath(TextAsset Document)
    {
        return Path.Combine(Directory.GetParent(Application.dataPath).FullName, AssetDatabase.GetAssetPath(Document).Replace("/", "\\"));
    }

    void UpdateSelection()
    {
        SelectedHeader = branchInfo[documentSelector].getHeader;
        SelectedRow = branchInfo[documentSelector].GetTable[rowSelector];
        debugText.text = "";

        debugText.text = "-----------------------------------------------\n" + debugText.text;
        //debugText.text = branchInfo[documentSelector].getErrors + debugText.text;
        debugText.text = branchInfo[documentSelector].getMessages + debugText.text;
        debugText.text = "\n-----------------------------------------------\nMessages from threaded CSV reader:\n-----------------------------------------------\n" + debugText.text;
        debugText.text = "\n" + debugText.text;

        // Get paths of CSV data to read at game time.
        for (int i = 0; i < branchInfo.Length; i++)
        {
            debugText.text = string.Format("Read CSV {0} from: \"{1}\"", i, getFullPath(branchCSVDocuments[i])) + debugText.text;
        }
        string tempString = "";

        tempString = "";
        for (int i = 0; i < SelectedRow.Length; i++)
        {
            tempString += "\t" + SelectedRow[i];
        }
        debugText.text = tempString + debugText.text;
        debugText.text = "\n" + debugText.text;

        tempString = "";
        for (int i = 0; i < SelectedHeader.Length; i++)
        {
            tempString += "\t" + SelectedHeader[i];
        }
        debugText.text = tempString + debugText.text;
        debugText.text = "\n" + debugText.text;


        if (haveLoadedTreeData)
        {
            for (int i = 0; i < trees.Length; i++)
            {
                debugText.text = string.Format("Tree {0}: {1} braches, {2} branch lines\n", i, trees[i].branches.Count, trees[i].branchLines.Count) + debugText.text;
            }
            debugText.text = "\n" + debugText.text;
            debugText.text = string.Format("Have read {0} trees.\n", trees.Length) + debugText.text;
        }
    }


}
