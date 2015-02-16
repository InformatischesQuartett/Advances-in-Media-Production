using UnityEngine;
using System.Collections;

public class InputSelector : MonoBehaviour {

	private Texture2D _backgroundTex;
	private GUIStyle _fontStyle;
	private float _counter;
	private float _waitingTime;

	private bool _enableGUI;

	// Use this for initialization
	void Start () {
		_enableGUI = true;
		_backgroundTex = (Texture2D) Resources.Load ("Textures/background", typeof(Texture2D));
		_fontStyle = new GUIStyle();
		_fontStyle.font = (Font) Resources.Load("Fonts/BNKGOTHM");
		_fontStyle.normal.textColor = Color.white;
		_fontStyle.fontSize = 30;

		_waitingTime = 50f;
		_counter = 0f;
	}
	
	// Update is called once per frame
	void Update () {
		_counter = _counter + Time.time;

		/*After a certain amount of time change to Application with default settings*/
		if (_counter >= _waitingTime) {
			_enableGUI = false;
		}
		if (_counter >= _waitingTime + 10) {
			Application.LoadLevel("default");
		}
	}

	void OnGUI () {
		/*Draw Background tex*/
		GUI.DrawTexture (new Rect (0, 0, Screen.width, Screen.height), _backgroundTex);


		if (_enableGUI) {
			GUI.Label (new Rect (10, Screen.height / 1.2f, Screen.width, 50), "Input Selector", _fontStyle);
			if (GUI.Button (new Rect (10, Screen.height / 2f, 60, 60), "Hallo")) {
			}
		} else {
			GUI.Label (new Rect (10, Screen.height / 1.2f, Screen.width, 50), "Input selected. Please put on your OVR-Device now.", _fontStyle);
		}
	}//end OnGUI
} //end class

