using UnityEngine;
using System.Collections;

public class myGUI : MonoBehaviour {

	public GUIStyle myStyle;

	private bool enableGUI = false;

	/* horizontal slider value */
	private float sliderValue = 0.0f;

	/* Textures */
	private Texture2D chairTex;
	public Texture2D screenTex;
	private Texture2D backgroundTex;

	/* distance between icons */
	private int iconDist = 44;

	/* the left and right camerascreen objects, that need to be moved */
	private GameObject screenLeft;
	private GameObject screenRight;

	/* fill in the names of the two screen objects that need to be moved */
	private string screenLObjectName = "screenL";
	private string screenRObjectName = "screenR";

	/* The original zPos of the Screens*/
	private float originalZPosL = 0.0f;
	private float originalZPosR = 0.0f;

	/* Use this for initialization */
	void Start () {
		/* Settings */
		myStyle.normal.textColor = Color.white; //setzt Textfarbe auf schwarz
		myStyle.fontSize = 20; //setzt textgröße auf 20

		/* Find Objects */
		screenLeft = GameObject.Find (screenLObjectName);
		screenRight = GameObject.Find (screenRObjectName);

		/* Save original Screen Position */
		originalZPosL = screenLeft.transform.position.z;
		originalZPosR = screenRight.transform.position.z;

		/* Load Textures */
		backgroundTex = (Texture2D) Resources.Load ("Textures/BackgroundGUI");
		chairTex = (Texture2D) Resources.Load ("Textures/ChairIcon");

	}//end start
	
	// Update is called once per frame
	void Update () {

		/* Takes Key input and sets the GUI boolean according to it */
		if (Input.GetKeyDown (KeyCode.Tab)) {
			enableGUI = !enableGUI;
		}

		moveScreens ();

	}//end update

	/**
	 * Renders GUI Elements. GUI is called with tab.
	 **/ 
	void OnGUI () {
		if (enableGUI) {

			/* creates a box for the gui elements to be framed in. Using GUIContent to display an image and a string */
			//GUI.Box (new Rect (10, 10, 400, 90), new GUIContent("Menu", backgroundTex), myStyle);
			GUI.Box (new Rect (10, 10, 400, 90), "Menu");

			/* Chair Icon Label*/
			GUI.Label (new Rect (25, 25, 30, 30), chairTex);

			/* Horizontal Slider */
			sliderValue = GUI.HorizontalSlider (new Rect (70, 40, 100, 30), sliderValue, 0.0f, 10.0f);
		}

	}//end ONGUI

	/**
	 * 	Moves the two screens according the sliderValue
	 **/ 
	void moveScreens () {
		if (enableGUI == false ) return;

		float tempZL = originalZPosL + sliderValue;
		float tempZR = originalZPosR + sliderValue;

		screenLeft.transform.position = new Vector3 (screenLeft.transform.position.x, screenLeft.transform.position.y, tempZL);
		screenRight.transform.position = new Vector3 (screenRight.transform.position.x, screenRight.transform.position.y, tempZR);
	}

} //end class
