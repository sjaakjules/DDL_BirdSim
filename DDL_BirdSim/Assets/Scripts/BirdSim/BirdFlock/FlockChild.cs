/**************************************									
	Copyright Unluck Software	
 	www.chemicalbliss.com								
***************************************/

using UnityEngine;
using DeepDesignLab.Base;
using DeepDesignLab.BirdSim;
using System.Collections.Generic;
using System.Linq;

public class hotspot
{
    public float heat { get; private set; }
    public Vector3 location { get; private set; }

    public hotspot(float _heat, Vector3 pos)
    {
        heat = heat;
        location = pos;
    }

    public hotspot(List<hotspot> spots)
    {
        heat = spots.Sum(x => x.heat);
        foreach (var spot in spots)
        {
            location += spot.location * spot.heat;
        }
        location = location / heat;
    }

    public void addHotspot(hotspot newSpot)
    {
        float oldHeat = heat;
        heat = oldHeat + newSpot.heat;
        location = (location * oldHeat + newSpot.location * newSpot.heat) / heat;
    }

    /// <summary>
    /// This returns the bucket size given a linear increase.
    /// </summary>
    /// <param name="minBucket"></param> Minimum bucket size. std is 0.1m
    /// <param name="minDistance"></param> Size under which the minimum bucket isze is used. above this distance the bucket size increases linearly as specified by 1m at "oneMeterDistance".
    /// <param name="oneMeterDistance"></param> The distance where bucket is 1m large. 
    /// <param name="position1"></param> One point to calculate distance.
    /// <param name="position2"></param> Second point to calcluate distance.
    /// <returns></returns>
    public static float GetBucketSize(float minBucket, float minDistance, float oneMeterDistance, Vector3 position1, Vector3 position2)
    {
        return (Vector3.Distance(position1, position2) * (1 - minBucket) + minBucket * oneMeterDistance - minDistance) / (oneMeterDistance - minDistance);
    }

    public static float GetCombinedHeat(List<hotspot> spots, Vector3 position)
    {
        return spots.Sum(x => (x.heat / Vector3.Distance(position, x.location)));
    }
}

public class FlockChild:MonoBehaviour{
    [HideInInspector] 
    public FlockController _spawner;			//Reference to the flock controller that spawned this bird
    [HideInInspector] 
    public Vector3 _wayPoint;				//Waypoint used to steer towards
    public float _speed;						//Current speed of bird
    [HideInInspector] 		
    public bool _dived =true;				//Indicates if this bird has recently performed a dive movement
    [HideInInspector] 
    public float _stuckCounter;				//prevents looping around a waypoint by increasing minimum distance to waypoint
    [HideInInspector] 
    public float _damping;						//Damping used for steering (steer speed)
    [HideInInspector] 
    public bool _soar = true;				// Indicates if this is soaring
    [HideInInspector] 
    public bool _landing;                   // Indicates if bird is landing or sitting idle
    [HideInInspector] 
    public float _targetSpeed;					// Max bird speed
    [HideInInspector] 
    public bool _move = true;				// Indicates if bird can fly
    public GameObject _model;					// Reference to bird model
    public Transform _modelT;					// Reference to bird model transform (caching tranform to avoid any extra getComponent calls)
    [HideInInspector] 
    public float _avoidValue;					//Random value used to check for obstacles. Randomized to lessen uniformed behaviour when avoiding
    [HideInInspector] 
    public float _avoidDistance;				//How far from an obstacle this can be before starting to avoid it
    float _soarTimer;	
    bool _instantiated;
    static int _updateNextSeed = 0;		
    int _updateSeed = -1;
    [HideInInspector] 
    public bool _avoid = true;
    public Transform _thisT;                    //Reference to the transform component

	public Vector3 _landingPosOffset;

    // Veriables for energy bird. 
    public bool _isUsingEnergy = false;
    public bool _isUsingCamera = false;
    public float _energy = 100;
    public Vector2 _energyLoss = new Vector2(0, 1);
    LandingSpotController _landerController;
    List<LandingSpot> _landingSites = new List<LandingSpot>();
    public Camera _camera;
    public Transform _cameraT;

    // variables for tree info
    float minLandingSize = 0.1f;
    float minMemoryDistance = 5;
    float oneMeterMemoryDistance = 20;
    List<TreeInfo> _seenTrees = new List<TreeInfo>();
    //List<BranchLineInfo> _seenBranches = new List<BranchLineInfo>();
    Vector2 _desiredInclination = new Vector2(50, 10); // X value is max, y value is min. 
    Vector2 _desiredRadius = new Vector2(.05f, 0.01f); // X value is max, y value is min.  
                                                       // Dictionary<Vector3, LandingSpot> DesiredLandings = new Dictionary<Vector3, LandingSpot>();

    public Dictionary<Bounds, hotspot> TreeDesirability = new Dictionary<Bounds, hotspot>();

    BoundsOctree<hotspot> primeLanding;
    BoundsOctree<hotspot> AveragedHotspots;
    public int nLandingSpots;



    public void Start(){
    	FindRequiredComponents();			//Check if references to transform and model are set (These should be set in the prefab to avoid doind this once a bird is spawned, click "Fill" button in prefab)
    	Wander(0.0f);
    	SetRandomScale();
    	_thisT.position = findWaypoint();	
    	RandomizeStartAnimationFrame();	
    	InitAvoidanceValues();
    	_speed = _spawner._minSpeed;
    	_spawner._activeChildren++;
    	_instantiated = true;
    	if(_spawner._updateDivisor > 1){
    		int _updateSeedCap = _spawner._updateDivisor -1;
    		_updateNextSeed++;
    	    this._updateSeed = _updateNextSeed;
    	    _updateNextSeed = _updateNextSeed % _updateSeedCap;
    	}
        setUpEnergyContent();
    }
    
    public void Update() {
    	//Skip frames
    	if (_spawner._updateDivisor <=1 || _spawner._updateCounter == _updateSeed){
    		SoarTimeLimit();
    		CheckForDistanceToWaypoint();
    		RotationBasedOnWaypointOrAvoidance();
    	    LimitRotationOfModel();
            UpdateEnergy();
    	}
    }
    
    public void OnDisable() {
    	CancelInvoke();
    	_spawner._activeChildren--;
    }
    
    public void OnEnable() {
    	if(_instantiated){
    		_spawner._activeChildren++;
    		if(_landing){
    			_model.GetComponent<Animation>().Play(_spawner._idleAnimation);
    		}else{
    			_model.GetComponent<Animation>().Play(_spawner._flapAnimation);
    		}		
    	}
    }

    public void UpdateEnergy()
    {
        if (_isUsingEnergy)
        {
            _energy -= Random.Range(_energyLoss.x, _energyLoss.y);

        }
    }

    private void OnDrawGizmos()
    {
        primeLanding.DrawAllObjects();
        AveragedHotspots.DrawAllBounds();
        //primeLanding.DrawAllBounds();
    }

    public void RefreshMemory()
    {
        //bool hasFinished = true;
        List<hotspot> allLanding = primeLanding.GetAllItems();
        nLandingSpots = allLanding.Count;
        foreach (var spot in allLanding)
        {
            List<hotspot> colliding;
            primeLanding.GetColliding(out colliding, new Bounds(spot.location, spot.heat * Vector3.one));
            if (colliding.Count>1)
            {
                hotspot newSpot = new hotspot(colliding);
                primeLanding.Add(newSpot, new Bounds(newSpot.location, hotspot.GetBucketSize(minLandingSize,minMemoryDistance,oneMeterMemoryDistance, newSpot.location,_thisT.position) * Vector3.one));
                foreach (var item in colliding)
                {
                    primeLanding.Remove(item);
                }
                //hasFinished = false;
                break;
            }
        }
    }

    public void setUpEnergyContent()
    {
        primeLanding = new BoundsOctree<hotspot>(oneMeterMemoryDistance, _thisT.position, minLandingSize, 1);
        AveragedHotspots = new BoundsOctree<hotspot>(oneMeterMemoryDistance, _thisT.position, 1, 1);
        //InvokeRepeating("RefreshMemory", 1, 0.1f);
        InvokeRepeating("FindLanding", 1, 0.1f);
        InvokeRepeating("GetBestLanding", 10,10);
    }

    public Vector3 GetBestLanding()
    {
        AveragedHotspots = new BoundsOctree<hotspot>(oneMeterMemoryDistance, _thisT.position, 1, 1);
        List<hotspot> allLanding = primeLanding.GetAllItems();
        Bounds memoryLimit = primeLanding.GetMaxBounds();
        Vector3 testSpot;
        float temoHeat;
        for (int i = (int)memoryLimit.min.y; i <= (int)memoryLimit.max.x; i++)
        {
            for (int j = (int)memoryLimit.min.y; j < (int)memoryLimit.max.y; j++)
            {
                for (int k = (int)memoryLimit.min.z; k < (int)memoryLimit.max.z; k++)
                {
                    testSpot = new Vector3(i, j, k);
                    temoHeat = hotspot.GetCombinedHeat(allLanding, testSpot);
                    AveragedHotspots.Add(new hotspot(temoHeat, testSpot), new Bounds(testSpot, Vector3.one));
                }
            }
        }
        return Vector3.zero;
    }

    public void FindLanding()
    {
        if (_camera != null)
        {
            //Vector3 key;
            foreach (var branchLine in TreeInfo.GetVisibleBranhes(_camera))
            {
                if (!_seenTrees.Contains(branchLine.branch.tree))
                {
                    _seenTrees.Add(branchLine.branch.tree);
                }
                /*
                 * Makes gameObjets... too slow...
                key=branchLine.startPt.bucket(minLandingSize);
                if (!DesiredLandings.ContainsKey(key))
                {
                    DesiredLandings.Add(key, new LandingSpot());
                }
                /**/
                if (!branchLine.hasSeen)
                {
                    branchLine.hasSeen = true;
                    if (branchLine.inclanation < _desiredInclination.x && branchLine.inclanation > _desiredInclination.y &&
                        branchLine.radius < _desiredRadius.x && branchLine.radius > _desiredRadius.y)
                    {
                        nLandingSpots++;
                        //_seenBranches.Add(branchLine);
                        float desirability = branchLine.length;
                        primeLanding.Add(new hotspot(desirability, branchLine.position),
                                            new Bounds(branchLine.position, branchLine.endPt-branchLine.startPt));
                                            //new Bounds(branchLine.position, hotspot.GetBucketSize(minLandingSize, minMemoryDistance, oneMeterMemoryDistance, branchLine.position, _thisT.position) * Vector3.one));
                    }
                }
            }
        }
    }
    
    public void FindRequiredComponents(){
    	if(_thisT == null)		_thisT = transform;	
    	if(_model == null)		_model = _thisT.Find("Model").gameObject;	
    	if(_modelT == null)	_modelT = _model.transform;
        if (_isUsingCamera)
        {
            if (_camera == null) _camera = _thisT.GetComponentInChildren<Camera>();
            if (_cameraT == null) _cameraT = _camera.transform;
        }

    }
    
    public void RandomizeStartAnimationFrame(){
    	foreach(AnimationState state in _model.GetComponent<Animation>()) {
    	 	state.time = Random.value * state.length;
    	}
    }
    
    public void InitAvoidanceValues(){
    	_avoidValue = Random.Range(.3f, .1f);	
    	if(_spawner._birdAvoidDistanceMax != _spawner._birdAvoidDistanceMin){
    		_avoidDistance = Random.Range(_spawner._birdAvoidDistanceMax , _spawner._birdAvoidDistanceMin);
    		return;
    	}
    	_avoidDistance = _spawner._birdAvoidDistanceMin;
    }
    
    public void SetRandomScale(){
    	float sc = Random.Range(_spawner._minScale, _spawner._maxScale);
    	_thisT.localScale=new Vector3(sc,sc,sc);
    }
    
    //Soar Timeout - Limits how long a bird can soar
    public void SoarTimeLimit(){	
    	if(this._soar && _spawner._soarMaxTime > 0){ 		
       		if(_soarTimer > _spawner._soarMaxTime){
       			this.Flap();
       			_soarTimer = 0.0f;
       		}else {
       			_soarTimer+=_spawner._newDelta;
       		}
       	}
    }
    
    public void CheckForDistanceToWaypoint(){
    	if(!_landing && (_thisT.position - _wayPoint).magnitude < _spawner._waypointDistance+_stuckCounter){
            Wander(0.0f);
            _stuckCounter=0.0f;
        }else if(!_landing){
        	_stuckCounter+=_spawner._newDelta;
        }else{
        	_stuckCounter=0.0f;
        }
    }
    
    public void RotationBasedOnWaypointOrAvoidance(){
		
    	Vector3 lookit = _wayPoint - _thisT.position;
        if(_targetSpeed > -1 && lookit != Vector3.zero){
        Quaternion rotation = Quaternion.LookRotation(lookit);
    	
    	_thisT.rotation = Quaternion.Slerp(_thisT.rotation, rotation, _spawner._newDelta * _damping);
    	}
    	
    	if(_spawner._childTriggerPos){
    		if((_thisT.position - _spawner._posBuffer).magnitude < 1){
    			_spawner.SetFlockRandomPosition();
    		}
    	}
    	_speed = Mathf.Lerp(_speed, _targetSpeed, _spawner._newDelta* 2.5f);
    	//Position forward based on object rotation
    	if(_move){
    		_thisT.position += _thisT.forward*_speed*_spawner._newDelta;
    		if(_avoid && _spawner._birdAvoid) 
    		Avoidance();
    	}
    }
    
    public bool Avoidance() {
    	RaycastHit hit = new RaycastHit();
    	Vector3 fwd = _modelT.forward;
    	bool r = false;
    	Quaternion rot = Quaternion.identity;
    	Vector3 rotE = Vector3.zero;
    	Vector3 pos = Vector3.zero;
    	pos = _thisT.position;
    	rot = _thisT.rotation;
    	rotE = _thisT.rotation.eulerAngles;
    	if (Physics.Raycast(_thisT.position, fwd+(_modelT.right*_avoidValue), out hit, _avoidDistance, _spawner._avoidanceMask)){	
    		rotE.y -= _spawner._birdAvoidHorizontalForce*_spawner._newDelta*_damping;
    		rot.eulerAngles = rotE;
    		_thisT.rotation = rot;
    		r= true;
    	}else if (Physics.Raycast(_thisT.position,fwd+(_modelT.right*-_avoidValue), out hit, _avoidDistance, _spawner._avoidanceMask)){
    		rotE.y += _spawner._birdAvoidHorizontalForce*_spawner._newDelta*_damping;
    		rot.eulerAngles = rotE;
    		_thisT.rotation = rot;
    		r= true;		
    	}
    	if (_spawner._birdAvoidDown && !this._landing && Physics.Raycast(_thisT.position, -Vector3.up, out hit, _avoidDistance, _spawner._avoidanceMask)){			
    		rotE.x -= _spawner._birdAvoidVerticalForce*_spawner._newDelta*_damping;
    		rot.eulerAngles = rotE;
    		_thisT.rotation = rot;				
    		pos.y += _spawner._birdAvoidVerticalForce*_spawner._newDelta*.01f;
    		_thisT.position = pos;
    		r= true;			
    	}else if (_spawner._birdAvoidUp && !this._landing && Physics.Raycast(_thisT.position, Vector3.up, out hit, _avoidDistance, _spawner._avoidanceMask)){			
    		rotE.x += _spawner._birdAvoidVerticalForce*_spawner._newDelta*_damping;
    		rot.eulerAngles = rotE;
    		_thisT.rotation = rot;
    		pos.y -= _spawner._birdAvoidVerticalForce*_spawner._newDelta*.01f;
    		_thisT.position = pos;
    		r= true;			
    	}
    	return r;
    }
    
    public void LimitRotationOfModel(){
    	Quaternion rot = Quaternion.identity;
    	Vector3 rotE = Vector3.zero;
    	rot = _modelT.localRotation;
    	rotE = rot.eulerAngles;	
    	if((_soar && _spawner._flatSoar|| _spawner._flatFly && !_soar)&& _wayPoint.y > _thisT.position.y||_landing){	
    		rotE.x = Mathf.LerpAngle(_modelT.localEulerAngles.x, -_thisT.localEulerAngles.x, _spawner._newDelta * 1.75f);
    		rot.eulerAngles = rotE;
    		_modelT.localRotation = rot;
    	}else{	
    		rotE.x = Mathf.LerpAngle(_modelT.localEulerAngles.x, 0.0f, _spawner._newDelta * 1.75f);
    		rot.eulerAngles = rotE;
    		_modelT.localRotation = rot;
    	}
    }
    
    public void Wander(float delay){
    	if(!_landing){
    		_damping = Random.Range(_spawner._minDamping, _spawner._maxDamping);       
    	    _targetSpeed = Random.Range(_spawner._minSpeed, _spawner._maxSpeed);
    	    Invoke("SetRandomMode", delay);
    	}
    }
    
    public void SetRandomMode(){
    	CancelInvoke("SetRandomMode");
    	if(!_dived && Random.value < _spawner._soarFrequency){
    	   	 	Soar();
    		}else if(!_dived && Random.value < _spawner._diveFrequency){	
    			Dive();
    		}else{	
    			Flap();
    		}
    }
    
    public void Flap(){
    	if(_move){
    	 	if(this._model != null) _model.GetComponent<Animation>().CrossFade(_spawner._flapAnimation, .5f);
    		_soar=false;
    		animationSpeed();
    		_wayPoint = findWaypoint();
    		_dived = false;
    	}
    }
    
    public Vector3 findWaypoint(){
    	Vector3 t = Vector3.zero;
    	t.x = Random.Range(-_spawner._spawnSphere, _spawner._spawnSphere) + _spawner._posBuffer.x;
    	t.z = Random.Range(-_spawner._spawnSphereDepth, _spawner._spawnSphereDepth) + _spawner._posBuffer.z;
    	t.y = Random.Range(-_spawner._spawnSphereHeight, _spawner._spawnSphereHeight) + _spawner._posBuffer.y;
    	return t;
    }
    
    public void Soar(){
    	if(_move){
    		 _model.GetComponent<Animation>().CrossFade(_spawner._soarAnimation, 1.5f);
    	   	_wayPoint= findWaypoint();
    	    _soar = true;
        }
    }
    
    public void Dive(){
    	if(_spawner._soarAnimation!=null){
    		_model.GetComponent<Animation>().CrossFade(_spawner._soarAnimation, 1.5f);
    	}else{
    		foreach(AnimationState state in _model.GetComponent<Animation>()) {
       	 		if(_thisT.position.y < _wayPoint.y +25){
       	 			state.speed = 0.1f;
       	 		}
       	 	}
     	}
     	_wayPoint= findWaypoint();
    	_wayPoint.y -= _spawner._diveValue;
    	_dived = true;
    }
    
    public void animationSpeed(){
    	foreach(AnimationState state in _model.GetComponent<Animation>()) {
    		if(!_dived && !_landing){
    			state.speed = Random.Range(_spawner._minAnimationSpeed, _spawner._maxAnimationSpeed);
    		}else{
    			state.speed = _spawner._maxAnimationSpeed;
    		}   
    	}
    }
}
