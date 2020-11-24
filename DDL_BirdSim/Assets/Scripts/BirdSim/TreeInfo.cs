using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using DeepDesignLab.PointCloud;

namespace DeepDesignLab.BirdSim
{
    public enum BranchType { hidden, Undefined, exposed, lateral, deadTipLow, deadTipHigh, deadLateralLow, deadLateralHigh }

    public enum BranchPropertyType { Radius, Elevation, Inclination, Length, DistanceToTip, Exposure}

    public class TreeInfo
    {
        public Dictionary<int, BranchInfo> branches = new Dictionary<int, BranchInfo>();
        public Dictionary<int, BranchLineInfo> branchLines = new Dictionary<int, BranchLineInfo>();
        public List<List<Matrix4x4>> branchTransforms { get; }
        public List<List<Vector4>> branchColour { get; }
        public List<List<BranchLineInfo>> branchs { get; }
        public int id;
        public Vector3 origin;
        GameObject parentObject;


        public static BoundsOctree<BranchLineInfo> TreesInWorld;// = new BoundsOctree<BranchLineInfo>(10, origin, .1, 1);


        public TreeInfo(List<string[]> _text, int _id)
        {
            id = _id;
            Vector3 tempStart = Vector3.one;
            for (int i = 0; i < _text.Count; i++)
            {
                if (_text[i].Length == 23)
                {
                    BranchLineInfo newBranchLine = new BranchLineInfo(_text[i]);
                    BranchInfo newBranch = new BranchInfo(_text[i], this);
                    if (i == 0) tempStart = newBranchLine.startPt;
                    if (tempStart.y > newBranchLine.startPt.y)
                    {
                        tempStart = newBranchLine.startPt;
                    }
                    if (!branchLines.ContainsKey(newBranchLine.id))
                    {
                        branchLines.Add(newBranchLine.id, newBranchLine);
                    }

                    if (branches.ContainsKey(newBranch.id))
                    {
                        branches[newBranch.id].addLine(newBranchLine);
                        newBranchLine.setBranch(branches[newBranch.id]);
                    }
                    else
                    {
                        branches.Add(newBranch.id, newBranch);
                        newBranch.addLine(newBranchLine);
                        newBranchLine.setBranch(newBranch);
                    }
                }
            }
            // set branch transforms.
            foreach (var branch in branches)
            {
                branch.Value.SetTransform(this);
            }
            branchTransforms = new List<List<Matrix4x4>>();// new Matrix4x4[branchLines.Count];
            branchColour = new List<List<Vector4>>();
            branchs = new List<List<BranchLineInfo>>();
            int count = 0;
            int index = 0;
            branchTransforms.Add(new List<Matrix4x4>());
            branchColour.Add(new List<Vector4>());
            branchs.Add(new List<BranchLineInfo>());
            foreach (var branchLine in branchLines)
            {
                count++;
                if (count > 1020)
                {
                    branchTransforms.Add(new List<Matrix4x4>());
                    branchColour.Add(new List<Vector4>());
                    branchs.Add(new List<BranchLineInfo>());
                    count = 0;
                    index++;
                }
                branchTransforms[index].Add(branchLine.Value.transform);
                branchs[index].Add(branchLine.Value);
                /*
                switch (branchLine.Value.branch.bType)
                {
                    case BranchType.Hidden:
                        branchColour[index].Add((new Vector4(0.5f, 0.5f, 0.5f, 1)));
                        break;
                    case BranchType.Undefined:
                        branchColour[index].Add((new Vector4(0.5f, 0.5f, 0.5f, 1)));
                        break;
                    case BranchType.Exposed:
                        branchColour[index].Add((new Vector4(1, 0, 0, 1)));
                        break;
                    case BranchType.Lateral:
                        branchColour[index].Add((new Vector4(1, 0.5f, 1, 1)));
                        break;
                    case BranchType.DeadTipLow:
                        branchColour[index].Add((new Vector4(0, 0.5f, 1, 1)));
                        break;
                    case BranchType.DeadLateralLow:
                        branchColour[index].Add((new Vector4(0.5f, 0, 0.5f, 1)));
                        break;
                    default:
                        break;
                }
                */
                if (branchLine.Value.radius > 0.1)
                    branchColour[index].Add((new Vector4(1, 0, 0, 1)));
                else
                    branchColour[index].Add((new Vector4(0.5f, 0.5f, 0.5f, 1)));

                /*
                if (branchLine.Value.branch.isDead && branchLine.Value.inclanation < 45 && branchLine.Value.radius > 0.02)
                    branchColour[index].Add((new Vector4(1, 0, 0, 1)));
                else
                    branchColour[index].Add((new Vector4(0.5f, 0.5f, 0.5f, 1)));
                */
                //branchColour[index].Add((new Vector4(branchLine.Value.distanceToTip, branchLine.Value.elevation, branchLine.Value.inclanation, 1.0f)).normalized);
            }
            origin = tempStart;
            if (TreesInWorld==null) TreesInWorld = new BoundsOctree<BranchLineInfo>(30, origin, .1f, 1);
            foreach (var branchLine in branchLines)
            {
                TreesInWorld.Add(branchLine.Value, new Bounds((branchLine.Value.endPt + branchLine.Value.startPt)/2, branchLine.Value.endPt- branchLine.Value.startPt));
            }
        }


        public static List<BranchLineInfo> GetVisibleBranhes(Camera _testCamera)
        {
            //List<BranchLineInfo> outList = new List<BranchLineInfo>();
            return TreesInWorld==null? new List<BranchLineInfo>():TreesInWorld.GetWithinFrustum(_testCamera);
            //return outList;
        }

        public void setGameObject(GameObject _parentObject)
        {
            parentObject = _parentObject;
        }

    }


    public class BranchInfo
    {
        public TreeInfo tree { get; }
        List<BranchLineInfo> _lines = new List<BranchLineInfo>();
        public BranchLineInfo[] lines { get { return _lines.ToArray(); } }
        //public BranchInfo parentBranch;
        public int id { get; }
        public int parentId { get; }
        public string typeName { get; }
        public BranchType bType { get; }
        public bool isDead { get; }
        public float radius { get; }
        static Vector2 radiusLimit;
        public float elevation { get; }
        static Vector2 elevationLimit;
        public int exposure { get; }
        static Vector2 exposureLimit;
        public float inclanation { get; }

        static Vector2 inclanationLimit;
        public float length { get; }
        static Vector2 lengthLimit;
        public float distanceToTip { get; }
        static Vector2 distanceToTipLimit;

        public float RadiusPercentage { get { return getPercentage(radius, radiusLimit); } }
        public float ElevationPercentage { get { return getPercentage(elevation, elevationLimit); } }
        public float ExposurePercentage { get { return getPercentage(exposure, exposureLimit); } }
        public float InclanationPercentage { get { return getPercentage(inclanation, inclanationLimit); } }
        public float LengthPercentage { get { return getPercentage(length, lengthLimit); } }
        public float DistanceToTipPercentage { get { return getPercentage(distanceToTip, distanceToTipLimit); } }

        public Vector3 startPt { get; private set; }
        public Vector3 endPt { get; private set; }
        public Vector3 position { get; private set; }
        public Vector3 scale { get; private set; }
        public Quaternion Orentation { get; private set; }
        public Matrix4x4 transform { get; private set; }

        public BranchInfo(string[] _data, TreeInfo _parentTree)
        {

            //[0]   rowID,      parentRowID,    startPtX,   startPtY,   startPtZ,   endPtX, endPtY, endPtZ, rowRadius,  rowElevation,   rowInclination, rowLength,  rowDistToTip,
            //[13]  segmentID,  parentSegmentID,    segType,    segIsDead,  segRadius,  segElevation,   segExposure,    segInclination, segLength,  segDistToTip

            if (_data.Length <= 23)
            {
                id = (int.Parse(_data[13]));
                parentId = (int.Parse(_data[14]));
                typeName = _data[15];
                if (Enum.TryParse(typeName, out BranchType _Type)) bType = _Type; else bType = BranchType.Undefined;
                isDead = bool.Parse(_data[16]);
                length = (float.Parse(_data[21]));
                if (length < lengthLimit.x) lengthLimit.x = length;
                if (length > lengthLimit.y) lengthLimit.y = length;
                radius = (float.Parse(_data[17]));
                if (radius < radiusLimit.x) radiusLimit.x = radius;
                if (radius > radiusLimit.y) radiusLimit.y = radius;
                elevation = (float.Parse(_data[18]));
                if (elevation < elevationLimit.x) elevationLimit.x = elevation;
                if (elevation > elevationLimit.y) elevationLimit.y = elevation;
                exposure = (int.Parse(_data[19]));
                if (exposure < exposureLimit.x) exposureLimit.x = exposure;
                if (exposure > exposureLimit.y) exposureLimit.y = exposure;
                inclanation = (float.Parse(_data[20]));
                if (inclanation < inclanationLimit.x) inclanationLimit.x = inclanation;
                if (inclanation > inclanationLimit.y) inclanationLimit.y = inclanation;
                distanceToTip = (float.Parse(_data[22]));
                if (distanceToTip < distanceToTipLimit.x) distanceToTipLimit.x = distanceToTip;
                if (distanceToTip > distanceToTipLimit.y) distanceToTipLimit.y = distanceToTip;
                tree = _parentTree;
            }
            else
            {
                id = int.MinValue;
            }
        }

        public void addLine(BranchLineInfo newLine)
        {
            _lines.Add(newLine);
        }

        /// <summary>
        /// Sets the transform info once all branch lines have been loaded. 
        /// </summary>
        public void SetTransform(TreeInfo tree)
        {
            if (_lines.Count > 0)
            {
                int baseID = _lines[0].id;
                int baseParent = _lines[0].parentId;
                foreach (var line in _lines)
                {
                    bool containsParent = false;
                    bool containsChild = false;
                    foreach (var line2 in _lines)
                    {
                        if (line2.id == line.parentId)
                        {
                            containsParent = true;

                        }
                        if (line.id == line2.parentId)
                        {
                            containsChild = true;
                        }
                    }
                    if (!containsParent)
                    {
                        startPt = line.startPt;
                    }
                    if (!containsChild)
                    {
                        endPt = line.endPt;
                    }
                }

                position = startPt + (endPt - startPt) / 2;
                //Orentation = Quaternion.LookRotation((endPt- startPt).normalized);
                Orentation = Quaternion.FromToRotation(Vector3.up, (endPt - startPt).normalized);
                scale = new Vector3(radius, length / 2, radius);
                transform = Matrix4x4.TRS(position, Orentation, scale);
            }
        }


        public float Closeness(float value, float targetValue, float spread, Vector2 originalSpread)
        {
            if (value > targetValue)
            {
                return (1 - ((value - targetValue) / (Mathf.Clamp(targetValue + (originalSpread.y - originalSpread.x) * spread, originalSpread.x, originalSpread.y) - targetValue)));
            }
            else
            {
                return (1 - ((targetValue - value) / (targetValue - Mathf.Clamp(targetValue - (originalSpread.y - originalSpread.x) * spread, originalSpread.x, originalSpread.y))));
            }
        }

        /// <summary>
        /// Returns the closeness to the specifed value of a specified property. Output is a percentage within the specified range as a byte (100%=255). 
        /// The output will clamp to max/min values of the given range without exceeding the original range.
        /// </summary>
        /// <param name="value"></param> Target value. In original units of the specifies properties. Ie inclination is degrees [0,90].
        /// <param name="spread"></param> The spread from taget value. Input values must be from 0 to 1.0. This is mapped to the range of the specified property. Ie 0.2 for inclination is +-18 degrees (original range is 0 to 90)
        /// <param name="property"></param> The desired property.
        /// <returns></returns>
        public float GetCloseness(float value, float spread, BranchPropertyType property)
        {
            switch (property)
            {
                case BranchPropertyType.Radius:
                    return Closeness(radius, value, spread, radiusLimit);
                case BranchPropertyType.Elevation:
                    return Closeness(elevation, value, spread, elevationLimit);
                case BranchPropertyType.Inclination:
                    return Closeness(inclanation, value, spread, inclanationLimit);
                case BranchPropertyType.Length:
                    return Closeness(length, value, spread, lengthLimit);
                case BranchPropertyType.DistanceToTip:
                    return Closeness(distanceToTip, value, spread, distanceToTipLimit);
                case BranchPropertyType.Exposure:
                    return Closeness(exposure, value, spread, exposureLimit);
                default:
                    return 0;
            }
        }


        public float getPercentage(float value, Vector2 Range)
        {
            return (value - Range.x) / (Range.y - Range.x);
        }
        /*

        public byte getDistance(float value, float spread, Vector2 originalSpread)
        {
            return (byte)(byte.MaxValue * (value - Mathf.Clamp(value - (originalSpread.y - originalSpread.x) * spread, originalSpread.x, originalSpread.y)) /
                                                    (Mathf.Clamp(value + (originalSpread.y - originalSpread.x) * spread, originalSpread.x, originalSpread.y)
                                                    - Mathf.Clamp(value - (originalSpread.y - originalSpread.x) * spread, originalSpread.x, originalSpread.y)));
        }



        /// <summary>
        /// returns a byte value (0 to 255) of the input value with the input range. Will clamp to max/min values of the original range.
        /// </summary>
        /// <param name="value"></param> In original units of the specifies properties. Ie inclination is degrees [0,90].
        /// <param name="range"></param> The range from 0 to 1.0. This is mapped to the range of the specified property. Ie 0.2 for inclination is +-18 degrees (original range is 0 to 90)
        /// <param name="property"></param> The desired property.
        /// <returns></returns>
        public byte GetBytePercentage(float value, float range, BranchPropertyType property)
        {
            switch (property)
            {
                case BranchPropertyType.Radius:
                    return getDistance(value, range,radiusLimit);
                case BranchPropertyType.Elevation:
                    return getDistance(value, range,elevationLimit);
                case BranchPropertyType.Inclination:
                    return getDistance(value, range,inclanationLimit);
                case BranchPropertyType.Length:
                    return getDistance(value, range,lengthLimit);
                case BranchPropertyType.DistanceToTip:
                    return getDistance(value, range,distanceToTipLimit);
                case BranchPropertyType.Exposure:
                    return getDistance(value, range,exposureLimit);
                default:
                    return 0;
            }
        }

        */


    }


    public class BranchLineInfo
    {
        public BranchInfo branch;
        //BranchInfo parentBranch;

        public int id { get; }
        public int parentId { get; }
        public Vector3 startPt { get; }
        public Vector3 endPt { get; }
        public float length { get; }
        public float radius { get; }
        public float elevation { get; }
        public float inclanation { get; }
        public float distanceToTip { get; }

        public bool hasSeen = false;

        public float RadiusPercentage { get { return getPercentage(radius, radiusLimit); } }
        public float ElevationPercentage { get { return getPercentage(elevation, elevationLimit); } }
        public float InclanationPercentage { get { return getPercentage(inclanation, inclanationLimit); } }
        public float LengthPercentage { get { return getPercentage(length, lengthLimit); } }
        public float DistanceToTipPercentage { get { return getPercentage(distanceToTip, distanceToTipLimit); } }

        static Vector2 radiusLimit;
        static Vector2 elevationLimit;
        static Vector2 inclanationLimit;
        static Vector2 lengthLimit;
        static Vector2 distanceToTipLimit;


        // Info for meshing
        public Vector3 position { get; }
        public Vector3 scale { get; }
        public Quaternion Orentation { get; }
        public Matrix4x4 transform { get; }

        public BranchLineInfo(string[] _data)
        {
            //[0]   rowID,      parentRowID,    startPtX,   startPtY,   startPtZ,   endPtX, endPtY, endPtZ, rowRadius,  rowElevation,   rowInclination, rowLength,  rowDistToTip,
            //[12]  segmentID,  parentSegmentID,    segType,    segIsDead,  segRadius,  segElevation,   segExposure,    segInclination, segLength,  segDistToTip

            id = int.Parse(_data[0]);
            parentId = int.Parse(_data[1]);
            startPt = new Vector3(float.Parse(_data[2]), float.Parse(_data[4]), float.Parse(_data[3]));
            endPt = new Vector3(float.Parse(_data[5]), float.Parse(_data[7]), float.Parse(_data[6]));
            length = (float.Parse(_data[11]));
            if (length < lengthLimit.x) lengthLimit.x = length;
            if (length > lengthLimit.y) lengthLimit.y = length;
            radius = (float.Parse(_data[8]));
            if (radius < radiusLimit.x) radiusLimit.x = radius;
            if (radius > radiusLimit.y) radiusLimit.y = radius;
            elevation = (float.Parse(_data[9]));
            if (elevation < elevationLimit.x) elevationLimit.x = elevation;
            if (elevation > elevationLimit.y) elevationLimit.y = elevation;
            inclanation = (float.Parse(_data[10]));
            if (inclanation < inclanationLimit.x) inclanationLimit.x = inclanation;
            if (inclanation > inclanationLimit.y) inclanationLimit.y = inclanation;
            distanceToTip = (float.Parse(_data[12]));
            if (distanceToTip < distanceToTipLimit.x) distanceToTipLimit.x = distanceToTip;
            if (distanceToTip > distanceToTipLimit.y) distanceToTipLimit.y = distanceToTip;



            position = (endPt + startPt) / 2;
            //Orentation = Quaternion.LookRotation((endPt- startPt).normalized);
            Orentation = Quaternion.FromToRotation(Vector3.up, (endPt - startPt).normalized);
            scale = new Vector3(radius * 2, length / 2, radius * 2);
            transform = Matrix4x4.TRS(position, Orentation, scale);
        }

        public void setBranch(BranchInfo _branch)
        {
            branch = _branch;
        }


        public float Closeness(float value, float targetValue, float spread, Vector2 originalSpread)
        {
            if (value > targetValue)
            {
                return (1 - ((value - targetValue) / (Mathf.Clamp(targetValue + (originalSpread.y - originalSpread.x) * spread, originalSpread.x, originalSpread.y) - targetValue)));
            }
            else
            {
                return (1 - ((targetValue - value) / (targetValue - Mathf.Clamp(targetValue - (originalSpread.y - originalSpread.x) * spread, originalSpread.x, originalSpread.y))));
            }
        }

        public float getPercentage(float value, Vector2 Range)
        {
            return (value - Range.x) / (Range.y - Range.x);
        }


        /// <summary>
        /// Returns the closeness to the specifed value of a specified property. Output is 0 to 1 with 0 being furthest and 1 being closest.
        /// The output will clamp to max/min values of the given range without exceeding the original range.
        /// </summary>
        /// <param name="value"></param> Target value. In original units of the specifies properties. Ie inclination is degrees [0,90].
        /// <param name="spread"></param> The spread from taget value. Input values must be from 0 to 1.0. This is mapped to the range of the specified property. Ie 0.2 for inclination is +-18 degrees (original range is 0 to 90)
        /// <param name="property"></param> The desired property.
        /// <returns></returns>
        public float GetCloseness(float value, float spread, BranchPropertyType property)
        {
            switch (property)
            {
                case BranchPropertyType.Radius:
                    return Closeness(radius, value, spread, radiusLimit);
                case BranchPropertyType.Elevation:
                    return Closeness(elevation, value, spread, elevationLimit);
                case BranchPropertyType.Inclination:
                    return Closeness(inclanation, value, spread, inclanationLimit);
                case BranchPropertyType.Length:
                    return Closeness(length, value, spread, lengthLimit);
                case BranchPropertyType.DistanceToTip:
                    return Closeness(distanceToTip, value, spread, distanceToTipLimit);
                case BranchPropertyType.Exposure:
                    return branch.GetCloseness(value, spread, property);
                default:
                    return 0;
            }
        }


        /*

        public byte getDistance(float value, float spread, Vector2 originalSpread)
        {
            return (byte)(byte.MaxValue * (value - Mathf.Clamp(value - (originalSpread.y - originalSpread.x) * spread, originalSpread.x, originalSpread.y)) /
                                                    (Mathf.Clamp(value + (originalSpread.y - originalSpread.x) * spread, originalSpread.x, originalSpread.y)
                                                    - Mathf.Clamp(value - (originalSpread.y - originalSpread.x) * spread, originalSpread.x, originalSpread.y)));
        }

        byte GetRadiusPercentage(float value) { return getPercentage(value, radiusLimit); }
        byte GetElevationPercentage(float value) { return getPercentage(value, elevationLimit); }
        byte GetInclanationPercentage(float value) { return getPercentage(value, inclanationLimit); }
        byte GetLengthPercentage(float value) { return getPercentage(value, lengthLimit); }
        byte GetdistanceToTipPercentage(float value) { return getPercentage(value, distanceToTipLimit); }


        byte GetRadiusPercentage(float value, float spread) { return getDistance(value, spread, radiusLimit); }
        byte GetElevationPercentage(float value, float spread) { return getDistance(value, spread, elevationLimit); }
        byte GetInclanationPercentage(float value, float spread) { return getDistance(value, spread, inclanationLimit); }
        byte GetLengthPercentage(float value, float spread) { return getDistance(value, spread, lengthLimit); }
        byte GetdistanceToTipPercentage(float value, float spread) { return getDistance(value, spread, distanceToTipLimit); }


        /// <summary>
        /// returns a byte (0 to 255) of the input value within the input range. The output will clamp to max/min values of the given range without exceeding the original range.
        /// </summary>
        /// <param name="value"></param> In original units of the specifies properties. Ie inclination is degrees [0,90].
        /// <param name="range"></param> The range from 0 to 1.0. This is mapped to the range of the specified property. Ie 0.2 for inclination is +-18 degrees (original range is 0 to 90)
        /// <param name="property"></param> The desired property.
        /// <returns></returns>
        public byte GetBytePercentage(float value, float range, BranchPropertyType property)
        {
            switch (property)
            {
                case BranchPropertyType.Radius:
                    return GetRadiusPercentage(value, range);
                case BranchPropertyType.Elevation:
                    return GetElevationPercentage(value, range);
                case BranchPropertyType.Inclination:
                    return GetInclanationPercentage(value, range);
                case BranchPropertyType.Length:
                    return GetLengthPercentage(value, range);
                case BranchPropertyType.DistanceToTip:
                    return GetdistanceToTipPercentage(value, range);
                case BranchPropertyType.Exposure:
                    return branch.GetBytePercentage(value, range,BranchPropertyType.Exposure);
                default:
                    return 0;
            }
        }

        /// <summary>
        /// returns a byte (0 to 255) of the specified property over a specified range. The output is a percentage with 100% = 255. It will clamp to max/min values of the given range without exceeding the original range.
        /// </summary>
        /// <param name="range"></param>
        /// <param name="property"></param>
        /// <returns></returns>
        public byte GetBytePercentage(float range, BranchPropertyType property)
        {
            switch (property)
            {
                case BranchPropertyType.Radius:
                    return GetRadiusPercentage(radius, range);
                case BranchPropertyType.Elevation:
                    return GetElevationPercentage(elevation, range);
                case BranchPropertyType.Inclination:
                    return GetInclanationPercentage(inclanation, range);
                case BranchPropertyType.Length:
                    return GetLengthPercentage(length, range);
                case BranchPropertyType.DistanceToTip:
                    return GetdistanceToTipPercentage(distanceToTip, range);
                case BranchPropertyType.Exposure:
                    return branch.GetBytePercentage(branch.exposure, range, BranchPropertyType.Exposure);
                default:
                    return 0;
            }
        }

        */


    }
}