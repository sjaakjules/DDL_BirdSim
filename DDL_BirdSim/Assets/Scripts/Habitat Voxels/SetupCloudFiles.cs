using DeepDesignLab.PointCloud;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Threading;
using System.Threading.Tasks;
//using UnityEditorInternal;
using System;
using System.IO;
using System.Linq;
using UnityEngine.Rendering;
using TC;
using UnityEngine.UI;
using UnityEditor;
using DeepDesignLab.Base;
using TC.Internal;

public class SetupCloudFiles : MonoBehaviour
{
    ReaderWriterLockSlim bucketLock = new ReaderWriterLockSlim();

    VoxTree_SetupFiles FileReader = new VoxTree_SetupFiles();

    SlimHabitatCloud cloud;

    public string FileLocation = "";
    public Text debugString;

    public bool _loadFile = false;
    public bool _getAve = false;
    public bool _saveFile = false;
    public bool _loadFromHDD = false;
    public bool _rederCloud = false;

    public string CloudName = "FirstDemo";

    public float[] averageSizes = { 0.02f, 0.16f, 0.64f };
    public float averageSize = 0.02f;
    public float bucketsize = Mathf.Pow(2, 6);

    public float nPoints, nBuckets;
    public string[] headings;

    Dictionary<float, Dictionary<Vector3, double[]>> sharedBucketedData = new Dictionary<float, Dictionary<Vector3, double[]>>();

    TC.PointCloudData renderedCloud;

    Dictionary<Vector3, Dictionary<Vector3, double[]>> sharedData = new Dictionary<Vector3, Dictionary<Vector3, double[]>>();
    Dictionary<Vector3, List<double[]>> bucketVoxels = new Dictionary<Vector3, List<double[]>>();
    public int writtenAverages = -1; // bool for work done, -1 not started, 0 started, 1 finished

    Vector3 key; // temp value for buckets

    public void Start()
    {
        
    }

    public void Update()
    {
        if (_loadFile)
        {
            _loadFile = false;
            if (FileLocation != "")
            {
                SetupCloud();
            }
        }
        if (_getAve)
        {
            _getAve = false;
            getAverages(FileReader, averageSize, bucketsize);

        }
        if (_saveFile)
        {
            _saveFile = false;
            //makePointCloudData();

            saveToFiles(sharedData, averageSize, bucketsize, CloudName);
        }
        if (_loadFromHDD)
        {
            _loadFromHDD = false;
            readFromFile(CloudName);
        }
        if (cloud != null && cloud.hasMessage)
        {
            debugString.text = cloud.debugMessage;
            cloud.hasMessage = false;
        }
        if (_rederCloud)
        {
            _rederCloud = false;
            makeCloud();
        }
        //getAverages(FileReader, averageSize, bucketsize);
       // saveToFiles(sharedData, averageSize, bucketsize, "FirstDemo");
       // makePointCloudData();



        // makePointCloudData();
        if (FileReader.isActive)
        {
            if (!debugString.text.Contains(FileReader.getMessages))
            {
                debugString.text = FileReader.getMessages;
            }
            
        }
    }


    public void makeCloud()
    {
        var data = new TC.PointCloudData();
        data.Initialize(cloud.pPosition, cloud.pNormals, cloud.pColours, 1, new Vector3(), new Vector3());
        data.name = CloudName;


        var system = gameObject.AddComponent<TCParticleSystem>();
        system.Emitter.Shape = EmitShapes.PointCloud;
        system.Emitter.PointCloud = data;
        system.Emitter.SetBursts(new[] { new BurstEmission { Time = 0, Amount = data.PointCount } });
        system.Emitter.EmissionRate = 0;
        system.Emitter.Lifetime = MinMaxRandom.Constant(-1.0f);
        system.Looping = false;
        system.MaxParticles = data.PointCount + 1000;
        system.Emitter.Size = MinMaxRandom.Constant(cloud.cloudInfo.minVoxelSize);
        system.Manager.NoSimulation = true;

        if (data.Normals != null)
        {
            system.ParticleRenderer.pointCloudNormals = true;
            system.ParticleRenderer.RenderMode = GeometryRenderMode.Mesh;

            var quadGo = GameObject.CreatePrimitive(PrimitiveType.Quad);
            system.ParticleRenderer.Mesh = quadGo.GetComponent<MeshFilter>().sharedMesh;
            DestroyImmediate(quadGo);
        }

        system.enabled = true;


    }

    public void OnGUI()
    {
        
    }

    public void SetupCloud()
    {
        if (!FileReader.isActive)
        {
            FileReader.readFile(FileLocation);
        }
    }

    // Pointcloud import formats.
    //  0   X,Y,Z,   
    //  3   R,G,B,  
    //  6   Intensity,Dip (degrees),Dip direction (degrees),    
    //  9   Normal change rate (0.1),Roughness (0.1),Number of neighbors (r=0.1),   
    //  12  habitatClass,treeClass, 
    //  14  Nx,Ny,Nz
    //  17  nAveraged       //This is after averaged.

    void makeRenderer()
    {

    }
    void saveToFiles(Dictionary<Vector3, Dictionary<Vector3,double[]>> BucketOVoxels, float _minVoxelSize, float _bucketSize, string _name)
    {
        if (writtenAverages == 1)
        {
            writtenAverages = 2;
            foreach (var bucket in BucketOVoxels)
            {
                bucketVoxels.Add(bucket.Key, new List<double[]>(bucket.Value.Values));
            }
            cloud = new SlimHabitatCloud(bucketVoxels, _minVoxelSize, _bucketSize, _name);
        }
    
    }

    void readFromFile(string _name)
    {
        cloud = new SlimHabitatCloud(_name);
    }


    void makePointCloudData()
    {
        if (writtenAverages == 1)
        {
            writtenAverages = 2;

            Debug.Log("making asset...");
            List<Vector3> pos = new List<Vector3>();
            List<Color32> col = new List<Color32>();
            List<Vector3> nor = new List<Vector3>();
            List<Vector3> sFi = new List<Vector3>();

            //int totalSize = 0;
            foreach (var bucket in sharedData)
            {
                pos.Concat(bucket.Value.Select(item => new Vector3((float)item.Value[0], (float)item.Value[1], (float)item.Value[2])).ToList());
                col.Concat(bucket.Value.Select(item => new Color32((byte)item.Value[3], (byte)item.Value[4], (byte)item.Value[5], 0)).ToList());
                nor.Concat(bucket.Value.Select(item => new Vector3((float)item.Value[item.Value.Length - 4], (float)item.Value[item.Value.Length - 5], (float)item.Value[item.Value.Length - 6])).ToList());
                sFi.Concat(bucket.Value.Select(item => new Vector3((float)item.Value[9], (float)item.Value[12], (float)item.Value[13])).ToList());
            }

            Debug.Log(string.Format("nPosition: {0}\nnColour: {1}\nnnormal: {2}", pos.Count,col.Count,nor.Count));
            Debug.Log("combined position, colour and normals...Now making asset");
            //var data = ScriptableObject.CreateInstance<TC.PointCloudData>();

            TC.PointCloudData asset = ScriptableObject.CreateInstance<TC.PointCloudData>();
            asset.Initialize(pos.ToArray(), nor.ToArray(), col.ToArray(), 1, new Vector3(), new Vector3());
            asset.name = "TestCloud0001";

            string path = "Assets/Large Files";// Application.dataPath + "/Large Files";
            if (path == "")
            {
                path = "Assets";
            }
            else if (Path.GetExtension(path) != "")
            {
                //    path = path.Replace(Path.GetFileName(AssetDatabase.GetAssetPath(Selection.activeObject)), "");
            }

            string assetPathAndName = AssetDatabase.GenerateUniqueAssetPath(path + "/TestSave001" + ".asset");

            AssetDatabase.CreateAsset(asset, assetPathAndName);

            AssetDatabase.SaveAssets();
            // AssetDatabase.Refresh();

            //renderedCloud = new TC.PointCloudData();
            //renderedCloud.Initialize(pos.ToArray(), nor.ToArray(), col.ToArray(), 1, new Vector3(), new Vector3());
            // renderedCloud.name = "TestCloud0001";
            //return renderedCloud;
        }
    }

    void makeAssetFile()
    {
        TC.PointCloudData asset = ScriptableObject.CreateInstance<TC.PointCloudData>();

        string path = Application.dataPath+ "/Large Files";
        if (path == "")
        {
            path = "Assets";
        }
        else if (Path.GetExtension(path) != "")
        {
        //    path = path.Replace(Path.GetFileName(AssetDatabase.GetAssetPath(Selection.activeObject)), "");
        }

        string assetPathAndName = AssetDatabase.GenerateUniqueAssetPath(path + "/TestSave001"+ ".asset");

        AssetDatabase.CreateAsset(asset, assetPathAndName);

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
      //  EditorUtility.FocusProjectWindow();
       // Selection.activeObject = asset;
    }

    void getAverages(VoxTree_SetupFiles Cloud, float averageSizes, float bucketSize)
    {
        //float[] _AverageSizes = new float[averageSizes.Length];
        //averageSizes.CopyTo(_AverageSizes, 0);


        if (Cloud.hasFinished && Cloud.getData != null && writtenAverages < 0)
        {
            if (headings == null) headings = Cloud.headerNames.Clone() as string[];
            writtenAverages = 0;
            Debug.Log("getting averages...");
            foreach (var subCloud in Cloud.getData)
            //Parallel.ForEach(Cloud.getData, (subCloud) =>
            {

                Debug.Log("Averaging next column...");
                    // create temp dictionary for each bucket.
                    Dictionary<Vector3, double[]> tempAveData = new Dictionary<Vector3, double[]>();
                Vector3 tempKey;

                for (int i = 0; i < subCloud.Value.Count; i++)
                {
                    tempKey = GetBucketKey(subCloud.Value[i], averageSizes);
                    if (tempAveData.ContainsKey(tempKey))
                    {
                        getAveragedPoint(tempAveData[tempKey], subCloud.Value[i], tempKey);
                    }
                    else
                    {
                        tempAveData.Add(tempKey, getAveragedPoint(null, subCloud.Value[i], tempKey));
                    }
                }

                    // add each threaded buckets to the shared memory.

                    AddOrUpdate(ref sharedData, tempAveData, averageSizes, bucketSize);
                    //bucketVoxels.Add()
                    /*
                    foreach (var item in tempAveData)
                    {
                        AddOrUpdate(ref sharedData, tempAveData, averageSizes, bucketSize);
                    }
                    */
            }
            // Used to write multiple buckets at once.
            /*
            Parallel.ForEach(Cloud.getData, (subCloud) =>
            {
                // create temp dictionary for each bucket.
                Dictionary<float, Dictionary<Vector3, double[]>> tempBucketedData = new Dictionary<float, Dictionary<Vector3, double[]>>();
                Vector3 tempKey;
                for (int j = 0; j < _AverageSizes.Length; j++)
                {
                    if (!tempBucketedData.ContainsKey(_AverageSizes[j]))
                    {
                        tempBucketedData.Add(_AverageSizes[j], new Dictionary<Vector3, double[]>());
                    }
                }

                for (int i = 0; i < subCloud.Value.Count; i++)
                {
                    for (int j = 0; j < _AverageSizes.Length; j++)
                    {
                        tempKey = GetBucketKey(subCloud.Value[i], _AverageSizes[j]);
                        if (tempBucketedData[_AverageSizes[j]].ContainsKey(tempKey))
                        {
                            getAveragedPoint(tempBucketedData[_AverageSizes[j]][tempKey], subCloud.Value[i], tempKey);
                        }
                        else
                        {
                            tempBucketedData[_AverageSizes[j]].Add(tempKey, getAveragedPoint(null, subCloud.Value[i], tempKey));
                            //tempBucketedData[_AverageSizes[j]][tempKey].Add(subCloud.Value[i]);
                        }
                    }
                }

                // add each threaded buckets to the shared memory.
                foreach (var item in tempBucketedData)
                {
                    AddOrUpdate(ref sharedBucketedData, item.Key, item.Value);
                }
            });
            */
            writtenAverages = 1;
            //averageBucketz(ref sharedBucketedData);

            Debug.Log("Finished averaging...");

            foreach (var item in sharedData)
            {
                nPoints += item.Value.Count;
            }
            nBuckets = sharedData.Count;
        }

    }


    private Vector3 GetBucketKey(double[] rowValues, float voxelSize, float spatialBucket)
    {
        // spatial bucket, such as 2^8 (64)
        spatialBucket = Mathf.Abs(spatialBucket);

        // Assume X,Y,Z is [0,1,2]
        if (rowValues != null && rowValues.Length >= 3)
        {
            return new Vector3((float)Math.Round(Math.Abs(rowValues[0] / spatialBucket)) * spatialBucket * Math.Sign(rowValues[0]),
                                (float)Math.Round(Math.Abs(rowValues[1] / spatialBucket)) * spatialBucket * Math.Sign(rowValues[1]),
                                (float)Math.Round(Math.Abs(rowValues[2] / spatialBucket)) * spatialBucket * Math.Sign(rowValues[2]));
        }
        return new Vector3(float.NaN, float.NaN, float.NaN); ;
    }
    private Vector3 GetBucketKey(double[] rowValues, float spatialBucket)
    {
        // spatial bucket, such as 2^8 (64)
        spatialBucket = Mathf.Abs(spatialBucket);

        // Assume X,Y,Z is [0,1,2]
        if (rowValues != null && rowValues.Length >= 3)
        {
            return new Vector3((float)Math.Round(Math.Abs(rowValues[0] / spatialBucket)) * spatialBucket * Math.Sign(rowValues[0]),
                                (float)Math.Round(Math.Abs(rowValues[1] / spatialBucket)) * spatialBucket * Math.Sign(rowValues[1]),
                                (float)Math.Round(Math.Abs(rowValues[2] / spatialBucket)) * spatialBucket * Math.Sign(rowValues[2]));
        }
        return new Vector3(float.NaN, float.NaN, float.NaN); ;
    }

    private double[] getAveragedPoint(double[] currentAverage, double[] newPoint, Vector3 voxelLocation)
    {
        if (currentAverage == null && newPoint != null && newPoint.Length > 0)
        {
            // new point, add a column on end.
            double[] vectorOut = new double[newPoint.Length + 1];
            newPoint.CopyTo(vectorOut, 0);
            vectorOut[vectorOut.Length - 1] = 1;
            vectorOut[0] = voxelLocation.x;
            vectorOut[1] = voxelLocation.y;
            vectorOut[2] = voxelLocation.z;
            return vectorOut;
        }
        else if (currentAverage != null && newPoint != null && newPoint.Length + 1 == currentAverage.Length)
        {
            addToAverage(currentAverage, newPoint);
            return currentAverage;
        }
        return null;
    }

    // Pointcloud import formats.
    //  0   X,Y,Z,   
    //  3   R,G,B,  
    //  6   Intensity,Dip (degrees),Dip direction (degrees),    
    //  9   Normal change rate (0.1),Roughness (0.1),Number of neighbors (r=0.1),   
    //  12  habitatClass,treeClass, 
    //  14  Nx,Ny,Nz
    //  17  nAveraged       //This is after averaged.

    void addToAverage(double[] currentAverage, double[] newPoint)
    {
        if (newPoint.Length + 1 == currentAverage.Length)
        {
            Parallel.For(0, newPoint.Length,
                i =>
                {
                    if (i > 2 && i < 6)
                    {
                        currentAverage[i] = Math.Sqrt((Math.Pow(currentAverage[i], 2) * currentAverage[currentAverage.Length - 1] + newPoint[i] * newPoint[i]) / (currentAverage[currentAverage.Length - 1] + 1));
                    }
                    else if (i > 5)
                    {
                        currentAverage[i] = (currentAverage[i] * currentAverage[currentAverage.Length - 1] + newPoint[i]) / (currentAverage[currentAverage.Length - 1] + 1);
                    }
                });
            currentAverage[currentAverage.Length - 1] += 1;
        }
        else if (newPoint.Length == currentAverage.Length)
        {
            Parallel.For(0, newPoint.Length,
                i =>
                {
                    if (i > 2 && i < 6)
                    {
                        currentAverage[i] = Math.Sqrt((Math.Pow(currentAverage[i], 2) * currentAverage[currentAverage.Length - 1] + Math.Pow(newPoint[i], 2) * newPoint[newPoint.Length - 1]) /
                                                        (currentAverage[currentAverage.Length - 1] + newPoint[newPoint.Length - 1]));
                    }
                    else if (i > 5)
                    {
                        currentAverage[i] = (currentAverage[i] * currentAverage[currentAverage.Length - 1] + newPoint[i] * newPoint[newPoint.Length - 1]) / (currentAverage[currentAverage.Length - 1] + newPoint[newPoint.Length - 1]);
                    }
                });
        }
    }

    public enum AddOrUpdateStatus
    {
        Added,
        Updated,
        Unchanged
    };



    public void AddOrUpdate(ref Dictionary<Vector3, Dictionary<Vector3, double[]>> SharedData, Dictionary<Vector3, double[]> aveVoxels, float aveSize,float bucketSize)
    {
        bucketSize *= aveSize;
        Vector3 tempBucketKey;
        foreach (var voxel in aveVoxels)
        {
            tempBucketKey = GetBucketKey(voxel.Value, bucketSize);

            bucketLock.EnterUpgradeableReadLock();
            try
            {
                //Dictionary <Vector3,double[]> bucketsOfVoxels = null;
                if (SharedData.ContainsKey(tempBucketKey))//SharedData.TryGetValue(tempBucketKey, out bucketsOfVoxels))
                {
                    // There is a bucket already created in shared data. either voxel is new or one exists. if exists add averages together.
                    bucketLock.EnterWriteLock();
                    try
                    {
                        if (SharedData[tempBucketKey].ContainsKey(voxel.Key))
                        {
                            // average exists in shared cloud. need to add new average to old. 
                            addToAverage(SharedData[tempBucketKey][voxel.Key], voxel.Value);

                        }
                        else
                        {
                            SharedData[tempBucketKey].Add(voxel.Key, voxel.Value.Clone() as double[]);
                        }
                    }
                    finally
                    {
                        bucketLock.ExitWriteLock();
                    }
                }
                else
                {
                    // There is no bucket in shared data. 
                    bucketLock.EnterWriteLock();
                    try
                    {
                        // There is no key in dictionary... so add a new one!
                        SharedData.Add(tempBucketKey, CloneDictionaryCloningValues(aveVoxels));

                    }
                    finally
                    {
                        bucketLock.ExitWriteLock();
                    }
                }
            }
            finally
            {
                bucketLock.ExitUpgradeableReadLock();
            }
        }
    }
    
    public AddOrUpdateStatus AddOrUpdate(ref Dictionary<float, Dictionary<Vector3, double[]>> SharedData, float key, Dictionary<Vector3, double[]> cloudColumnDicPerBucket)
    {
        bucketLock.EnterUpgradeableReadLock();
        try
        {
            Dictionary<Vector3, double[]> dicPerBucket = null;
            if (SharedData.TryGetValue(key, out dicPerBucket))
            {
                if (dicPerBucket == cloudColumnDicPerBucket)
                {
                    // value is the same... so do nothing...
                    return AddOrUpdateStatus.Unchanged;

                }
                else
                {
                    bucketLock.EnterWriteLock();
                    try
                    {
                        // Value is there... need to add to the dictionary.
                        //innerCache[key] = value;
                        foreach (var voxelCloud in cloudColumnDicPerBucket)
                        {
                            if (dicPerBucket.ContainsKey(voxelCloud.Key))
                            {
                                // average exists in shared cloud. need to add new average to old. 
                                addToAverage(dicPerBucket[voxelCloud.Key], voxelCloud.Value);
                            }
                            else
                            {
                                dicPerBucket.Add(voxelCloud.Key, voxelCloud.Value.Clone() as double[]);
                            }
                        }
                    }
                    finally
                    {
                        bucketLock.ExitWriteLock();
                    }
                    return AddOrUpdateStatus.Updated;
                }
            }
            else
            {
                bucketLock.EnterWriteLock();
                try
                {
                    // There is no key in dictionary... so add a new one!
                    SharedData.Add(key, CloneDictionaryCloningValues(cloudColumnDicPerBucket));

                }
                finally
                {
                    bucketLock.ExitWriteLock();
                }
                return AddOrUpdateStatus.Added;
            }
        }
        finally
        {
            bucketLock.ExitUpgradeableReadLock();
        }
    }

    public static Dictionary<TKey, TValue> CloneDictionaryCloningValues<TKey, TValue>
   (Dictionary<TKey, TValue> original) where TValue : ICloneable
    {
        Dictionary<TKey, TValue> ret = new Dictionary<TKey, TValue>(original.Count,
                                                                original.Comparer);
        foreach (KeyValuePair<TKey, TValue> entry in original)
        {
            ret.Add(entry.Key, (TValue)entry.Value.Clone());
        }
        return ret;
    }


    void averageBucketz(ref Dictionary<float, Dictionary<Vector3, List<double[]>>> SharedData)
    {
        if (writtenAverages > 0)
        {
            bucketLock.EnterUpgradeableReadLock();
            try
            {
                Parallel.ForEach(SharedData, (bucketCloud) =>
                {

                });
            }

            finally
            {
                bucketLock.ExitUpgradeableReadLock();
            }
        }
    }


    double[] averagePoint(List<double[]> points)
    {
        ReaderWriterLockSlim aveValLock = new ReaderWriterLockSlim();
        ReaderWriterLockSlim pointsLock = new ReaderWriterLockSlim();
        double[] aveValues = new double[points[0].Length];

        Parallel.For(0, points[0].Length,
            j =>
            {
                pointsLock.EnterReadLock();
                try
                {
                    double average = 0;
                    for (int i = 0; i < points.Count; i++)
                    {
                        if (j == 3 || j == 4 || j == 5)
                        {
                            average += points[i][j] * points[i][j];
                        }
                        else
                        {
                            average += points[i][j];
                        }
                    }
                    average = average / points.Count;
                    if (j == 3 || j == 4 || j == 5)
                    {
                        average = Math.Sqrt(average);
                    }
                    aveValLock.EnterWriteLock();
                    try
                    {
                        aveValues[j] = average;
                    }
                    finally
                    {
                        aveValLock.ExitWriteLock();
                    }

                }
                finally
                {
                    pointsLock.ExitReadLock();
                }
            });

        return aveValues;
    }

    public void saveBucketsToFile(List<double[]> points, string fileLocation, string FileName)
    {
        if (points.Count > 1 && points[0].Length == 17)
        {

            Texture2D _positionMap;
            Texture2D _colorMap;
            Texture2D _normalMap;
            Texture2D _PropertyMap;
            int _pointCount = points.Count;

            var width = Mathf.CeilToInt(Mathf.Sqrt(_pointCount));

            _positionMap = new Texture2D(width, width, TextureFormat.RGBAHalf, false);
            _positionMap.name = "Position Map";
            _positionMap.filterMode = FilterMode.Point;

            _colorMap = new Texture2D(width, width, TextureFormat.RGBA32, false);
            _colorMap.name = "Color Map";
            _colorMap.filterMode = FilterMode.Point;

            _normalMap = new Texture2D(width, width, TextureFormat.RGBAHalf, false);
            _normalMap.name = "Normal Map";
            _normalMap.filterMode = FilterMode.Point;

            _PropertyMap = new Texture2D(width, width, TextureFormat.RGBAHalf, false);
            _PropertyMap.name = "Normal Map";
            _PropertyMap.filterMode = FilterMode.Point;

            var i1 = 0;
            var i2 = 0U;

            for (var y = 0; y < width; y++)
            {
                for (var x = 0; x < width; x++)
                {
                    var i = i1 < _pointCount ? i1 : (int)(i2 % _pointCount);
                    //var p = positions[i];
                    //var n = normal[i];

                    _positionMap.SetPixel(x, y, new Color((float)points[i][0], (float)points[i][1], (float)points[i][2]));
                    _colorMap.SetPixel(x, y, new Color((float)points[i][3], (float)points[i][4], (float)points[i][5]));
                    _normalMap.SetPixel(x, y, new Color((float)points[i][14], (float)points[i][15], (float)points[i][16]));
                    _PropertyMap.SetPixel(x, y, new Color((float)points[i][9], (float)points[i][12], (float)points[i][13]));
                    i1++;
                    i2 += 132049U; // prime
                }
            }

            _positionMap.Apply(false, true);
            _colorMap.Apply(false, true);
            _normalMap.Apply(false, true);
            _PropertyMap.Apply(false, true);

            System.IO.File.WriteAllBytes(fileLocation + "/" + FileName + "_pos", _positionMap.EncodeToPNG());
            System.IO.File.WriteAllBytes(fileLocation + "/" + FileName + "_col", _colorMap.EncodeToPNG());
            System.IO.File.WriteAllBytes(fileLocation + "/" + FileName + "_nor", _normalMap.EncodeToPNG());
            System.IO.File.WriteAllBytes(fileLocation + "/" + FileName + "_pro", _PropertyMap.EncodeToPNG());
        }
    }



}
