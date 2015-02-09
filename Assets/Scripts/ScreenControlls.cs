using UnityEngine;
using System.Collections.Generic;

public class ScreenControlls : MonoBehaviour
{
    private GameObject _screenL;
    private GameObject _screenR;

    public float Hit = 0;
    public float HitIncrement = 0.1f;

    public float ScreenDistance = 100;
    public float ScreenDistanceIncrement = 1;

    public float ScreenSize = 1;
    public float ScreenSizeIncement = 0.1f;

    public float KeysPerSecond = 10;
    private float _keyTimer = 0;

    private Vector3 _positionVectorScreenL;
    private Vector3 _positionVectorScreenR;

    private Vector3 _positionVector;
    private Vector3 _aspectRatio;

    private List<GameObject> _cameras;
    private Color _backgroundColor;

	// Use this for initialization
	void Start () {
	    _screenL = GameObject.Find("screenL");
        _screenR = GameObject.Find("screenR");

	    _positionVectorScreenL = _screenL.transform.position;
	    _positionVectorScreenR = _screenR.transform.position;

	    _positionVector = this.transform.position;
	    _aspectRatio = this.transform.localScale;


        _backgroundColor = Color.black;
        _cameras = new List<GameObject>();

        //Add Cameras here!
        _cameras.Add(GameObject.Find("/Main Camera"));
        _cameras.Add(GameObject.Find("LeftEyeAnchor"));
        _cameras.Add(GameObject.Find("RightEyeAnchor"));

        SetCamerasBackground(_cameras, _backgroundColor);
	}
	
	// Update is called once per frame
	void Update ()
	{
	    if (Input.anyKey)
	    {
	        if (_keyTimer >= 1/KeysPerSecond)
	        {
	            //HIT
	            if (Input.GetKey(KeyCode.LeftArrow))
	            {
	                Hit -= HitIncrement;
	            }
	            else if (Input.GetKey(KeyCode.RightArrow))
	            {
	                Hit += HitIncrement;
	            }

	            //Distance
	            if (Input.GetKey(KeyCode.UpArrow))
	            {
	                ScreenDistance += ScreenDistanceIncrement;
	            }
	            else if (Input.GetKey(KeyCode.DownArrow))
	            {
	                ScreenDistance -= ScreenDistanceIncrement;
	            }

	            //SIZE
	            if (Input.GetKey(KeyCode.PageUp))
	            {
	                ScreenSize += ScreenSizeIncement;
	            }
	            else if (Input.GetKey(KeyCode.PageDown))
	            {
	                ScreenSize -= ScreenSizeIncement;
	            }

	            //COLOR
	            if (Input.GetKey(KeyCode.Alpha1))
	            {
	                _backgroundColor = Color.black;
	            }
	            else if (Input.GetKey(KeyCode.Alpha2))
	            {
	                _backgroundColor = Color.gray;
	            }
	            else if (Input.GetKey(KeyCode.Alpha3))
	            {
	                _backgroundColor = Color.white;
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

                SetCamerasBackground(_cameras, _backgroundColor);

	            _keyTimer -= 1/KeysPerSecond;
	        }
	        _keyTimer += Time.deltaTime;
	    }
	    else
	    {
	        _keyTimer = 1/KeysPerSecond;
	    }

	}

    private void SetCamerasBackground(List<GameObject> cameras, Color color)
    {
        foreach (GameObject cam in cameras)
        {
            cam.camera.backgroundColor = color;
        }
    }
}
