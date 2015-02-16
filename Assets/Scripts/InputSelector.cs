using UnityEngine;
using System.Collections;

public class InputSelector : MonoBehaviour {

	private Texture2D _backgroundTex;
	private GUIStyle _fontStyle;
	private float _counter;
	private float _waitingTime;

	private bool _enableGUI;
	private bool _done;
	private bool _inputSelected;


	// Use this for initialization
	void Start () {
		_enableGUI = true;
		_done = false;
		_inputSelected = false;

		_backgroundTex = (Texture2D) Resources.Load ("Textures/background", typeof(Texture2D));
		_fontStyle = new GUIStyle();
		_fontStyle.font = (Font) Resources.Load("Fonts/BNKGOTHM");
		_fontStyle.normal.textColor = Color.white;
		_fontStyle.fontSize = 30;

		_waitingTime = Time.time + 15;
	}
	
	// Update is called once per frame
	void Update () {
		if (_inputSelected) {
			if (_done) {
				_enableGUI = false;
				Application.LoadLevel("default");
			}
		}

		if (!_inputSelected) {

			float timeLeft = _waitingTime - Time.time; 
			if (timeLeft < 0) {
				timeLeft = 0;
			}
			//_counter = _counter  + Time.time;
			Debug.Log (timeLeft);

			/*After a certain amount of time change to Application with default settings*/
			if (timeLeft <= 0) {
				_enableGUI = false;
				loadDefaultSettings ();
				Application.LoadLevel ("default");
			}
		}
	}

	void OnGUI () {
		/*Draw Background tex*/
		GUI.DrawTexture (new Rect (0, 0, Screen.width, Screen.height), _backgroundTex);


		if (_enableGUI) {
			GUI.Label (new Rect (10, Screen.height / 1.2f, Screen.width, 50), "Input Selector", _fontStyle);
			if (GUI.Button (new Rect (10, Screen.height / 2f, 60, 60), "Hallo")) {
				_inputSelected = true;
			}
			if (GUI.Button (new Rect (10, Screen.height / 1.5f, 60, 60), "Done")) {
				_done = true;
			}

		} else {
			GUI.Label (new Rect (10, Screen.height / 1.2f, Screen.width, 50), "Input selected. Please put on your OVR-Device now.", _fontStyle);
		}
	}//end OnGUI

	/*Loading Default Settings if nothing was selected*/
	void loadDefaultSettings () {
	}
} //end class

