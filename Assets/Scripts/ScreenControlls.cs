using UnityEngine;
using System.Collections.Generic;

public class ScreenControlls : MonoBehaviour
{
    private GameObject _screenL;
    private GameObject _screenR;

    private float Hit;
    private float ScreenDistance;
    private float ScreenSize;

    public float KeysPerSecond = 1;
    private float _keyTimer = 0;

    private Vector3 _positionVectorScreenL;
    private Vector3 _positionVectorScreenR;

    private Vector3 _positionVector;
    private Vector3 _aspectRatio;

    private List<GameObject> _cameras;

    private List<Color> _avalavailableColors;
    private int _currentColorIndex;

	// Use this for initialization
	void Start () {
	    _screenL = GameObject.Find("screenL");
        _screenR = GameObject.Find("screenR");

	    _positionVectorScreenL = _screenL.transform.position;
	    _positionVectorScreenR = _screenR.transform.position;

	    _positionVector = this.transform.position;
	    _aspectRatio = this.transform.localScale;
        Debug.Log(this.transform.localScale);
        _cameras = new List<GameObject>();
	    _avalavailableColors = Config.Colors;

        //Add Cameras here!
        _cameras.Add(GameObject.Find("/Main Camera"));
        _cameras.Add(GameObject.Find("LeftEyeAnchor"));
        _cameras.Add(GameObject.Find("RightEyeAnchor"));

        SetCamerasBackground(_cameras, _avalavailableColors[_currentColorIndex]);

        //Initial Screen settings
	    ScreenDistance = Config.ScreenDistanceDefault;
	    ScreenSize = Config.ScreenSizeDefault;
        Hit = Config.HitDefault;

	}
	
	// Update is called once per frame
	void Update ()
	{
        if (Input.anyKey)
	    {
	        if (_keyTimer >= 1/KeysPerSecond)
	        {
	            //HIT
                Hit += Input.GetAxis("HIT");

	            //Distance

	            if (Input.GetAxis("Screen Distance") > 0 )
	            {
	                ScreenDistance += 0.5f;
	            }
	            else if (Input.GetAxis("Screen Distance") < 0 && ScreenDistance > 0)
	            {
	                ScreenDistance -= 0.5f;
	            }
	            // ScreenDistance += Input.GetAxis("Screen Distance");
                
                //SIZE
                if (Input.GetAxis("Screen Size") > 0)
                {
                    ScreenSize += 0.1f;
                }
                else if (Input.GetAxis("Screen Size") < 0 && ScreenSize > 0.0f)
                {
                    ScreenSize -= 0.1f;
                }


	            //COLOR
	            if (Input.GetAxis("Color Select") > 0)
	            {
	                if (_currentColorIndex == _avalavailableColors.Count - 1)
	                    _currentColorIndex = 0;
	                else
	                    _currentColorIndex++;
	            }
	            else if (Input.GetAxis("Color Select") < 0)
	            {
	                if (_currentColorIndex == 0)
	                    _currentColorIndex = _avalavailableColors.Count - 1;
	                else
	                    _currentColorIndex--;
	            }

	            _positionVectorScreenL.x = Hit/2f;
	            _positionVectorScreenL.y = 0;
	            _positionVectorScreenL.z = 0;
	            _screenL.transform.localPosition = _positionVectorScreenL;

	            _positionVectorScreenR.x = -Hit/2f;
	            _positionVectorScreenR.y = 0;
	            _positionVectorScreenR.z = 0;
	            _screenR.transform.localPosition = _positionVectorScreenR;

	            _positionVector.x = 0;
	            _positionVector.y = 0;
	            _positionVector.z = ScreenDistance;
	            this.transform.position = _positionVector;

	            this.transform.localScale = _aspectRatio*ScreenSize;
                
                SetCamerasBackground(_cameras, _avalavailableColors[_currentColorIndex]);

	            _keyTimer -= 1/KeysPerSecond;
	        }
	        _keyTimer += Time.deltaTime;
	    }
	    else
	    {
	        _keyTimer = 1/KeysPerSecond;
	    }
        if (Input.GetAxis("TrackerReset") > 0)
            OVRManager.display.RecenterPose();
	}

    private void SetCamerasBackground(List<GameObject> cameras, Color color)
    {
        foreach (GameObject cam in cameras)
        {
            cam.camera.backgroundColor = color;
        }
    }


}
