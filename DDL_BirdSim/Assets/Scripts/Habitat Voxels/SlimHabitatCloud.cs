using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.Serialization.Formatters.Binary;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

[Serializable]
public struct SlimhabitatColoudInfo
{
    // Cloud info
    [SerializeField]
    public string name { get; }
    [SerializeField]
    public float minVoxelSize { get; }
    [SerializeField]
    public float bucketSize { get; }
    [SerializeField]
    public string[] headings { get; }
    [SerializeField]
    public int nVoxels { get; }
    [SerializeField]
    public string folderPath { get; }


    public SlimhabitatColoudInfo(float _minVoxelSize, float _bucketSize, string _name, string _folderPath,int _nVoxels)
    {
        nVoxels = _nVoxels;
        headings = new string[] { "X", "Y", "Z", "R", "G", "B", "Intensity", "Dip(degrees)", "Dip direction(degrees)", "Normal change rate(0.1)", "Roughness(0.1)", "Number of neighbors(r = 0.1)", "habitatClass", "treeClass", "Nx", "Ny", "Nz" };
        minVoxelSize = _minVoxelSize;
        bucketSize = _bucketSize;
        name = _name;
        folderPath = _folderPath;
    }
    public SlimhabitatColoudInfo(float _minVoxelSize, float _bucketSize, string _name, string _folderPath, int _nVoxels, string[] _headings)
    {
        nVoxels = _nVoxels;
        headings = _headings;
        minVoxelSize = _minVoxelSize;
        bucketSize = _bucketSize;
        name = _name;
        folderPath = _folderPath;
    }
}


[Serializable]
public class SlimHabitatCloud
{

    public SlimhabitatColoudInfo cloudInfo;

    Stopwatch timer = new Stopwatch();
    public string debugMessage = "";

    // Points
    [SerializeField]
    public Color32[] pColours;
    [SerializeField]
    public Vector3[] pPosition;
    [SerializeField]
    public Vector3[] pNormals;
    [SerializeField]
    public Vector3[] pStaticFields;
    bool filesOnDrive = false;

    public bool hasMessage = false;

    public SlimHabitatCloud(Dictionary<Vector3, List<double[]>> BucketOVoxels, float _minVoxelSize, float _bucketSize, string _name)
    {
        timer.Start();
        // Pointcloud import formats.
        //  0   X,Y,Z,   
        //  3   R,G,B,  
        //  6   Intensity,Dip (degrees),Dip direction (degrees),    
        //  9   Normal change rate (0.1),Roughness (0.1),Number of neighbors (r=0.1),   
        //  12  habitatClass,treeClass, 
        //  14  Nx,Ny,Nz
        //  17  nAveraged       //This is after averaged.
        //string folderPath = Application.dataPath + "/Large Files/" + _name;
        string folderPath = Directory.CreateDirectory(Application.dataPath).Parent.FullName + "/Large Files/" + _name;
        if (Directory.Exists(folderPath))
        {
            // DIRECTORY EXITS... WILL OVERWRITE!

            // Check if header exists...
            // check if files exists...
            readHeader(out cloudInfo, folderPath);
            timer.Stop();
            debugMessage += string.Format("{0}s to read header.", timer.ElapsedMilliseconds / 1000.0);
            timer.Reset();
            if (cloudInfo.nVoxels > 0)
            {
                filesOnDrive = true;
            }
            else
            {
                filesOnDrive = false;
            }
        }
        else
        {
            Directory.CreateDirectory(folderPath);
            filesOnDrive = false;
        }

        if (filesOnDrive)
        {
            timer.Start();

            readBuckets(pColours, pPosition, pNormals, pStaticFields, cloudInfo);

            timer.Stop();
            debugMessage += string.Format("{0}s to read all files.", timer.ElapsedMilliseconds / 1000.0);
            timer.Reset();
        }
        else
        {
            int nVoxels = 0;
            foreach (var bucket in BucketOVoxels)
            {
                nVoxels += bucket.Value.Count;
              //  pPosition.Concat(bucket.Value.Select(x => new Vector3((float)x[0], (float)x[1], (float)x[2])));
              //  pColours.Concat(bucket.Value.Select(x => new Color32((byte)x[3], (byte)x[4], (byte)x[5], 0)));
              //  pNormals.Concat(bucket.Value.Select(x => new Vector3((float)x[x.Length - 4], (float)x[x.Length - 3], (float)x[x.Length - 2])));
              //  pStaticFields.Concat(bucket.Value.Select(x => new Vector3((float)x[9], (float)x[12], (float)x[11])));
            }

            cloudInfo = new SlimhabitatColoudInfo(_minVoxelSize, _bucketSize, _name, folderPath, nVoxels);

            //writeVoxels(pColours, pPosition, pNormals, pStaticFields);
            writeHeader(cloudInfo);
            writeBuckets(BucketOVoxels, cloudInfo);
            filesOnDrive = true;
        }

        hasMessage = true;
    }

    public SlimHabitatCloud(string _filePath, bool filesOnDrive)
    {
        timer.Start();
        // Pointcloud import formats.
        //  0   X,Y,Z,   
        //  3   R,G,B,  
        //  6   Intensity,Dip (degrees),Dip direction (degrees),    
        //  9   Normal change rate (0.1),Roughness (0.1),Number of neighbors (r=0.1),   
        //  12  habitatClass,treeClass, 
        //  14  Nx,Ny,Nz
        //  17  nAveraged       //This is after averaged.
        //string folderPath = Application.dataPath + "/Large Files/" + _name;
        //string folderPath = Directory.CreateDirectory(Application.dataPath).Parent.FullName + "/Large Files/" + _name;
        if (Directory.Exists(_filePath))
        {
            // DIRECTORY EXITS... WILL OVERWRITE!

            // Check if header exists...
            // check if files exists...
            if (readHeader(_filePath))
            {
                timer.Stop();
                debugMessage += string.Format("\n{0}s to read header.", timer.ElapsedMilliseconds / 1000.0);
                timer.Reset();
                if (cloudInfo.nVoxels > 0)
                {
                    debugMessage += "\nPoints found...";
                    filesOnDrive = true;
                }
                else
                {
                    debugMessage += "\nHeader does not contain points.";
                    filesOnDrive = false;
                }
            }
            else
            {
                debugMessage += "\nCan not read header file...";
            }
        }
        else
        {
            debugMessage += "\nDirecctory does not exist. Can not create cloud.";
            //Directory.CreateDirectory(folderPath);
            filesOnDrive = false;
        }

        if (filesOnDrive)
        {

            debugMessage += "\nreading *.vbucket files";
            timer.Start();

            readBuckets(pColours, pPosition, pNormals, pStaticFields, cloudInfo);

            timer.Stop();
            debugMessage += string.Format("\n{0}s to read all files.", timer.ElapsedMilliseconds / 1000.0);
            timer.Reset();
        }
        else
        {
            debugMessage += "\nFiles are not on drive. Can not create cloud.";
        }
        hasMessage = true;
    }

    public SlimHabitatCloud(string _name)
    {
        timer.Start();
        // Pointcloud import formats.
        //  0   X,Y,Z,   
        //  3   R,G,B,  
        //  6   Intensity,Dip (degrees),Dip direction (degrees),    
        //  9   Normal change rate (0.1),Roughness (0.1),Number of neighbors (r=0.1),   
        //  12  habitatClass,treeClass, 
        //  14  Nx,Ny,Nz
        //  17  nAveraged       //This is after averaged.
        //string folderPath = Application.dataPath + "/Large Files/" + _name;
        string folderPath = Directory.CreateDirectory(Application.dataPath).Parent.FullName + "/Large Files/" + _name;
        if (Directory.Exists(folderPath))
        {
            // DIRECTORY EXITS... WILL OVERWRITE!

            // Check if header exists...
            // check if files exists...
            if(readHeader(out cloudInfo, folderPath))
            {
                timer.Stop();
                debugMessage += string.Format("\n{0}s to read header.", timer.ElapsedMilliseconds / 1000.0);
                timer.Reset();
                if (cloudInfo.nVoxels > 0)
                {
                    debugMessage += "\nPoints found...";
                    filesOnDrive = true;
                }
                else
                {
                    debugMessage += "\nHeader does not contain points.";
                    filesOnDrive = false;
                }
            }
            else
            {
                debugMessage += "\nCan not read header file...";
            }
        }
        else
        {
            debugMessage += "\nDirecctory does not exist. Can not create cloud.";
            //Directory.CreateDirectory(folderPath);
            filesOnDrive = false;
        }

        if (filesOnDrive)
        {

            debugMessage += "\nreading *.vbucket files";
            timer.Start();

            readBuckets(pColours, pPosition, pNormals, pStaticFields, cloudInfo);

            timer.Stop();
            debugMessage += string.Format("\n{0}s to read all files.", timer.ElapsedMilliseconds / 1000.0);
            timer.Reset();
        }
        else
        {
            debugMessage += "\nFiles are not on drive. Can not create cloud.";
        }
        hasMessage = true;
    }

    void writeVoxels(Color32[] _pColours,Vector3[] _pPosition,Vector3[] _pNormals,Vector3[] _pStaticFields)
    {

    }

    void writeBuckets(Dictionary<Vector3, List<double[]>> BucketOVoxels, SlimhabitatColoudInfo cloudInfo)
    {
        Parallel.ForEach(BucketOVoxels, (bucket) =>
        {
            if (Directory.Exists(cloudInfo.folderPath))
            {
                using (FileStream fs = File.Open(cloudInfo.folderPath + "/" + getVoxelFileName(bucket.Key, cloudInfo) + ".vbucket", FileMode.Create, FileAccess.Write, FileShare.Write))
                using (BufferedStream bs = new BufferedStream(fs, 4 * 1024 * 1024))
                using (BinaryWriter writer = new BinaryWriter(bs))
                {
                    writer.Write(bucket.Value.Count);
                    for (int i = 0; i < bucket.Value.Count; i++)
                    {
                        // Position
                        writer.Write((float)bucket.Value[i][0]); 
                        writer.Write((float)bucket.Value[i][1]); 
                        writer.Write((float)bucket.Value[i][2]);
                        // Colour
                        writer.Write((byte)bucket.Value[i][3]);
                        writer.Write((byte)bucket.Value[i][4]);
                        writer.Write((byte)bucket.Value[i][5]);
                        // Normal
                        writer.Write((float)bucket.Value[i][bucket.Value[i].Length - 4]); 
                        writer.Write((float)bucket.Value[i][bucket.Value[i].Length - 3]); 
                        writer.Write((float)bucket.Value[i][bucket.Value[i].Length - 2]);
                        // Static Fields
                        writer.Write((float)bucket.Value[i][9]);
                        writer.Write((float)bucket.Value[i][12]);
                        writer.Write((float)bucket.Value[i][11]);
                    }
                }
            }
        });
    }

    bool readBuckets(Color32[] _pColours, Vector3[] _pPosition, Vector3[] _pNormals, Vector3[] _pStaticFields, SlimhabitatColoudInfo cloudInfo)
    {
        _pColours = new Color32[cloudInfo.nVoxels];
        _pPosition = new Vector3[cloudInfo.nVoxels];
        _pNormals = new Vector3[cloudInfo.nVoxels];
        _pStaticFields = new Vector3[cloudInfo.nVoxels];

        int tempPosition = 0;

        if (Directory.Exists(cloudInfo.folderPath))
        {
            string[] files = System.IO.Directory.GetFiles(cloudInfo.folderPath, "*.vbucket");
            if (files.Length == 1)
            {
                //ReaderWriterLockSlim combinerLock = new ReaderWriterLockSlim();
                debugMessage += string.Format("\n{0} files found.", files.Length);
                //Parallel.ForEach(files, (file) =>
                foreach (var file in files)
                {
                    using (FileStream fs = File.Open(file, FileMode.Open, FileAccess.Read, FileShare.Read))
                    using (BufferedStream bs = new BufferedStream(fs, 4 * 1024 * 1024))
                    using (BinaryReader reader = new BinaryReader(bs))
                    {
                        int count = reader.ReadInt32();
                        Vector3[] _pos = new Vector3[count];
                        Vector3[] _norm = new Vector3[count];
                        Vector3[] _sField = new Vector3[count];
                        Color32[] _col = new Color32[count];

                        for (int i = 0; i < count; i++)
                        {
                            _pos[i].x = reader.ReadSingle();
                            _pos[i].y = reader.ReadSingle();
                            _pos[i].z = reader.ReadSingle();

                            _col[i].r = reader.ReadByte();
                            _col[i].g = reader.ReadByte();
                            _col[i].b = reader.ReadByte();
                            _col[i].a = 255;

                            _norm[i].x = reader.ReadSingle();
                            _norm[i].y = reader.ReadSingle();
                            _norm[i].z = reader.ReadSingle();

                            _sField[i].x = reader.ReadSingle();
                            _sField[i].y = reader.ReadSingle();
                            _sField[i].z = reader.ReadSingle();
                        }

                        //combinerLock.EnterWriteLock();
                        try
                        {
                            Array.Copy(_pos, 0, _pPosition, tempPosition, count);
                            Array.Copy(_col, 0, _pColours, tempPosition, count);
                            Array.Copy(_norm, 0, _pNormals, tempPosition, count);
                            Array.Copy(_sField, 0, _pStaticFields, tempPosition, count);
                            tempPosition += count;
                        }
                        catch (Exception e)
                        {
                            debugMessage += "\n" + e.Message;
                        }
                        finally
                        {
                            //combinerLock.ExitWriteLock();
                        }

                    }
                    //});
                }

            }

        }

        debugMessage += string.Format("\n{0} points saved to RAM", _pPosition.Length);
        if (tempPosition == cloudInfo.nVoxels)
        {
            return true;
        }
        return false;
    }

    string getVoxelFileName(Vector3 key, SlimhabitatColoudInfo coloudInfo)
    {
        return string.Format("{0:D}_{1:D}_{2:D}", key.x /(coloudInfo.minVoxelSize*cloudInfo.bucketSize), key.y / (coloudInfo.minVoxelSize * cloudInfo.bucketSize), key.z /( coloudInfo.minVoxelSize * cloudInfo.bucketSize));
    }

    void ReadVoxels()
    {

    }

    void writeHeader(SlimhabitatColoudInfo cloudInfo)
    {
        if (Directory.Exists(cloudInfo.folderPath))
        {
            using (FileStream fs = File.Open(cloudInfo.folderPath + "/" + cloudInfo.name + ".head", FileMode.Create, FileAccess.Write, FileShare.Write))
            using (BufferedStream bs = new BufferedStream(fs, 4 * 1024 * 1024))
            using (BinaryWriter writer = new BinaryWriter(bs))
            {
                writer.Write(cloudInfo.minVoxelSize);
                writer.Write(cloudInfo.bucketSize);
                writer.Write(cloudInfo.nVoxels);
                writer.Write(cloudInfo.name);
                writer.Write(cloudInfo.folderPath);
                writer.Write(cloudInfo.headings.Length);
                for (int i = 0; i < cloudInfo.headings.Length; i++)
                {
                    writer.Write(cloudInfo.headings[i]);
                }
            }
        }
    }

    bool readHeader(out SlimhabitatColoudInfo cloudInfo, string _FolderPath)
    {
        if (Directory.Exists(_FolderPath))
        {
            string[] files = System.IO.Directory.GetFiles(_FolderPath, "*.head");
            if (files.Length == 1)
            {

                using (FileStream fs = File.Open(files[0], FileMode.Open, FileAccess.Read, FileShare.Read))
                using (BufferedStream bs = new BufferedStream(fs, 4 * 1024 * 1024))
                using (BinaryReader reader = new BinaryReader(bs))
                {
                    float _minVoxelSize = reader.ReadSingle();
                    //writer.Write(cloudInfo.minVoxelSize);

                    float _bucketSize = reader.ReadSingle();
                    //writer.Write(cloudInfo.bucketSize);

                    int _nVoxels = reader.ReadInt32();
                    //writer.Write(cloudInfo.nVoxels);

                    string _name = reader.ReadString();
                    //writer.Write(cloudInfo.name);

                    string _folderPath = reader.ReadString();
                    //writer.Write(cloudInfo.folderPath);

                    int nHeadings = reader.ReadInt32();
                    string[] _headings = new string[nHeadings];
                    for (int i = 0; i < nHeadings; i++)
                    {
                        _headings[i] = reader.ReadString();
                        //writer.Write(cloudInfo.headings[i]);
                    }
                    cloudInfo = new SlimhabitatColoudInfo(_minVoxelSize, _bucketSize, _name, _folderPath, _nVoxels, _headings);
                    return true;
                }
            }
        }
        cloudInfo = new SlimhabitatColoudInfo();
        return false;
    }

    public bool readHeader(string _FilePath)
    {
        //cloudInfo = new SlimhabitatColoudInfo();
        if (Directory.Exists(_FilePath))
        {
            string[] files = System.IO.Directory.GetFiles(_FilePath);
            if (files.Length == 1)
            {

                using (FileStream fs = File.Open(files[0], FileMode.Open, FileAccess.Read, FileShare.Read))
                using (BufferedStream bs = new BufferedStream(fs, 4 * 1024 * 1024))
                using (BinaryReader reader = new BinaryReader(bs))
                {
                    float _minVoxelSize = reader.ReadSingle();
                    //writer.Write(cloudInfo.minVoxelSize);

                    float _bucketSize = reader.ReadSingle();
                    //writer.Write(cloudInfo.bucketSize);

                    int _nVoxels = reader.ReadInt32();
                    //writer.Write(cloudInfo.nVoxels);

                    string _name = reader.ReadString();
                    //writer.Write(cloudInfo.name);

                    string _folderPath = reader.ReadString();
                    //writer.Write(cloudInfo.folderPath);

                    int nHeadings = reader.ReadInt32();
                    string[] _headings = new string[nHeadings];
                    for (int i = 0; i < nHeadings; i++)
                    {
                        _headings[i] = reader.ReadString();
                        //writer.Write(cloudInfo.headings[i]);
                    }
                    this.cloudInfo = new SlimhabitatColoudInfo(_minVoxelSize, _bucketSize, _name, _folderPath, _nVoxels, _headings);
                    return true;
                }
            }
        }
        return false;
    }

}