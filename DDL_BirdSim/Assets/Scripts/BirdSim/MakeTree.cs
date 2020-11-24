using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using DeepDesignLab.Base;
using DeepDesignLab.BirdSim;
using System;
using System.Threading.Tasks;
using System.Diagnostics;

/// <summary>
/// Landing site types for birds.
/// </summary>
public enum birdLandsingSites { huntingSite, foragingSite, nestingSite, territorySite, unspecified }

public class MakeTree : MonoBehaviour
{
    CSVreader CSVInfo;
    TreeInfo tree;
    GameObject treeGameObject;
    public Text debugText;

    Stopwatch profiler = new Stopwatch();
    long millSetColour = 0;
    long millDrawNow = 0;

    [Header("Drawing settings")]
    [Tooltip("The bool will set if tree is visible.")]
    public bool drawTree;

    [Tooltip("The object transformed for each branch segment. Must have a mesh and material.")]
    public GameObject treeBranchMesh;

    [Tooltip("The input data that describes each branch segment.")]
    public TextAsset branchCSVDocument;

    // bools for timing
    bool treeIsSetup = false;
    public bool drawLanding = false;
    public Gradient renderColour = new Gradient();
    //public string[] Groups = Enum.GetNames(typeof(BranchType));
    //public Color32[] colours = new Color32[6];
    public BranchPropertyType property = new BranchPropertyType();
    

    [Space(10)]
    [Header("BirdView Settings:")]
    bool viewPerchSites = false;
    public birdLandsingSites birdDesirability = new birdLandsingSites();
    //[Header("Inclination ")]
    bool useInclination = false;
    [Range(0,90)]
    public float desiredInclination = 20;
    [Range(0, 1)]
    public float inclinationSpread = .3f;

    //[Header("Radius settings")]
    bool useRadius = false;
    [Range(0, 0.6f)]
    public float desiredRadius = 20;
    [Range(0, 1)]
    public float radiusSpread = .3f;
    public int SelectedTree = 5;



    // Start is called before the first frame update
    private void Awake()
    {
        if (branchCSVDocument != null)
        {
            CSVInfo = new CSVreader(1);
            string fullPath = getFullPath(branchCSVDocument);
            CSVInfo.readFile(fullPath);
        }
        if (SelectedTree>0)
        {
            CSVInfo = new CSVreader(1);
            string fullPath = string.Format("http://julianrutten.com/UnityExports/BirdSim/tree{0}.csv", SelectedTree);
            CSVInfo.readFile(fullPath);
        }
    }
    void Start()
    {

        InvokeRepeating("UpdateDebugText", 0.5f, 0.5f);
    }

    // Update is called once per frame
    void Update()
    {
        SetupTree();
        DrawTree();
    }

    void SetupTree()
    {
        if (!treeIsSetup && CSVInfo.hasFinished)
        {
            // Read file, now to make tree.
            tree = new TreeInfo(new List<string[]>(CSVInfo.GetTable), 1);
            treeGameObject = new GameObject("Tree GameObject");
            tree.setGameObject(treeGameObject);
            treeIsSetup = true;
        }
    }

    void DrawTree()
    {
        if (treeIsSetup)
        {
            profiler.Reset();
            profiler.Start();
            /*
            Parallel.For(0, tree.branchColour.Count, i =>
            {
                Parallel.For(0, tree.branchColour[i].Count, j =>
                 {
                     //tree.branchColour[i][j] = new Vector4( colours[(int)tree.branchs[i][j].branch.bType].r, colours[(int)tree.branchs[i][j].branch.bType].g, 
                     //                                       colours[(int)tree.branchs[i][j].branch.bType].b, colours[(int)tree.branchs[i][j].branch.bType].a);
                     tree.branchColour[i][j] = new Vector4(tree.branchs[i][j].GetCloseness(goodInclination,inclinationSpread,BranchPropertyType.Inclination), 0,
                                                            0, 255);
                 });
            });
            */
            Color tempColour;
            for(int i = 0; i<tree.branchColour.Count; i++ )
            {
                for(int j=0; j<tree.branchColour[i].Count; j++)
                {
                    //tree.branchColour[i][j] = new Vector4( colours[(int)tree.branchs[i][j].branch.bType].r, colours[(int)tree.branchs[i][j].branch.bType].g, 
                    //                                       colours[(int)tree.branchs[i][j].branch.bType].b, colours[(int)tree.branchs[i][j].branch.bType].a);
                    if (drawLanding)
                    {
                        tempColour = renderColour.Evaluate(getDesirability(tree.branchs[i][j], birdDesirability));
                        tree.branchColour[i][j] = new Vector4(tempColour.r, tempColour.g, tempColour.b, 1);
                    }
                    else
                    {
                        tempColour = renderColour.Evaluate(getProperty(tree.branchs[i][j], property));
                        tree.branchColour[i][j] = new Vector4(tempColour.r, tempColour.g, tempColour.b, 1);
                    }
                    //tree.branchColour[i][j] = new Vector4(tree.branchs[i][j].GetCloseness(desiredInclination, inclinationSpread, BranchPropertyType.Inclination), 0,0, 1);
                }
            }
            millSetColour = profiler.ElapsedMilliseconds;
            for (int j = 0; j < tree.branchTransforms.Count; j++)
            {
                MaterialPropertyBlock props = new MaterialPropertyBlock();
                props.SetVectorArray("_Color", tree.branchColour[j]);
                Graphics.DrawMeshInstanced(treeBranchMesh.GetComponent<MeshFilter>().mesh, 0,
                                            treeBranchMesh.GetComponent<Renderer>().material,
                                            tree.branchTransforms[j], props);
            }
            millDrawNow = profiler.ElapsedMilliseconds;
        }
    }

    void UpdateDebugText()
    {
        debugText.text = "";
        debugText.text += string.Format("Timers:\nTo update colour of all branches:\t{0}\nTo draw all branches:           :\t{1}\n",millSetColour,millDrawNow);
                                                 
    }
    string getFullPath(TextAsset Document)
    {
        if (Application.isEditor)
        {
            return Path.Combine(Directory.GetParent(Application.dataPath).FullName, AssetDatabase.GetAssetPath(Document).Replace("/", "\\"));
        }
        else
        {
            
            return Path.Combine(Directory.GetParent(Application.dataPath).FullName, "/Resources/",AssetDatabase.GetAssetPath(Document).Replace("/", "\\"));
        }
    }

    float getDesirability(BranchLineInfo branch, birdLandsingSites landingType)
    {

        switch (landingType)
        {
            case birdLandsingSites.huntingSite:
                // Preference dead sites high up.
                // sum the individual desirability and rescale to 0-1.
                // elevation >15m. higher better
                // radius >0.02
                if (branch.branch.isDead && branch.elevation>15 && branch.radius>0.01)
                {
                    return (branch.ElevationPercentage + branch.RadiusPercentage).Remap(0,2,0,1);
                }
                break;
            case birdLandsingSites.foragingSite:
                // preference horizontal sites
                break;
            case birdLandsingSites.nestingSite:
                // preference horizontal with thick branches
                break;
            case birdLandsingSites.territorySite:
                // preference hidden sites
                break;
            case birdLandsingSites.unspecified:
                return getBranchType(branch);
            default:
                break;

        }


        return 0;
    }

    float getProperty(BranchLineInfo branch, BranchPropertyType type)
    {
        switch (type)
        {
            case BranchPropertyType.Radius:
                return 1-branch.RadiusPercentage;
            case BranchPropertyType.Elevation:
                return branch.ElevationPercentage;
            case BranchPropertyType.Inclination:
                return 1-branch.InclanationPercentage;
            case BranchPropertyType.Length:
                return branch.LengthPercentage;
            case BranchPropertyType.DistanceToTip:
                return 1-branch.DistanceToTipPercentage;
            case BranchPropertyType.Exposure:
                return branch.branch.ExposurePercentage;
            default:
                break;
        }
        return 0;
    }


    float getBranchType(BranchLineInfo branch)
    {
        switch (branch.branch.bType)
        {
            case BranchType.hidden:
                return 0;
            case BranchType.Undefined:
                return 0;
            case BranchType.exposed:
                return 0.2f;
            case BranchType.lateral:
                return 0.4f;
            case BranchType.deadTipLow:
                return 0.6f;
            case BranchType.deadLateralLow:
                return 0.6f;
            case BranchType.deadTipHigh:
                return 1f;
            case BranchType.deadLateralHigh:
                return 0.9f;
            default:
                break;
        }
        return 0;
    }

}
