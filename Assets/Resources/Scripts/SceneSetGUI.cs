using UnityEngine;
using System.Collections;

/**
 * GUI Script inherits from VRGUI, a GUI package that supports easy oculus gui building
 * Put it on one camera.
 * 
 * You can choose to have the GUI displayed on a flat surface or a curved surface (the "Use Curved Surface" field)
 * You can choose where to position the GUI (the "GUI Position" field)
 * You can choose how much to scale the GUI (the "GUI Size" field)
 * You can choose to let the GUI accept mouse and keyboard events (the "Accept Mouse" field)
 **/

public class SceneSetGUI : VRGUI {

	/*Press Tab to change boolean value*/
	private bool enableGUI = true;

	// Use this for initialization
	void Start () {
	
	}
	
	// Update is called once per frame
	void Update () {
		/*If Tab Key is pressed, boolean value gets changed*/
		if (Input.GetKeyDown (KeyCode.Tab)) {
			enableGUI = !enableGUI;
		}
	
	}

	public override void OnVRGUI () {
		GUILayout.BeginArea (new Rect (2f, 200f, Screen.width, Screen.height));

		/*Show GUI if enableGUI is true*/
		if (enableGUI) {
			if (GUILayout.Button ("Click me!")) {

			}
			GUILayout.EndArea ();
		}
	}
}
