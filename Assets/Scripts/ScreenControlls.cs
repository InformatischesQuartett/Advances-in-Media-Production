using UnityEngine;
using System.Collections;

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

    private Vector3 _positionVectorScreenL;
    private Vector3 _positionVectorScreenR;

    private Vector3 _positionVector;
    private Vector3 _aspectRatio;

	// Use this for initialization
	void Start () {
	    _screenL = GameObject.Find("screenL");
        _screenR = GameObject.Find("screenR");

	    _positionVectorScreenL = _screenL.transform.position;
	    _positionVectorScreenR = _screenR.transform.position;

	    _positionVector = this.transform.position;
	    _aspectRatio = this.transform.localScale;
	}
	
	// Update is called once per frame
	void Update ()
	{
	    if (Input.GetKeyDown(KeyCode.LeftArrow))
	    {
	        Hit -= HitIncrement;
	    }
	    else if (Input.GetKeyDown(KeyCode.RightArrow))
	    {
	        Hit += HitIncrement;
	    }

	    if (Input.GetKeyDown(KeyCode.UpArrow))
	    {
	        ScreenDistance += ScreenDistanceIncrement;
	    }
        else if (Input.GetKeyDown(KeyCode.DownArrow))
	    {
	        ScreenDistance -= ScreenDistanceIncrement;
	    }

	    if (Input.GetKeyDown(KeyCode.PageUp))
	    {
	        ScreenSize += ScreenSizeIncement;
	    }
	    else if (Input.GetKeyDown(KeyCode.PageDown))
	    {
	        ScreenSize -= ScreenSizeIncement;
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

	    this.transform.localScale = _aspectRatio * ScreenSize;
	}
}
