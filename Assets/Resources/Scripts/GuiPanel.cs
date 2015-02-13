using System.Collections.Generic;
using UnityEngine;
using System.Collections;
using UnityEngine.UI;

public class ScreenInfo
{
    public float Distance { get; set; }
    public float ScreenSize { get; set; }
}

public class GuiPanel : MonoBehaviour
{

    private ScreenControlls _screens;
    private Text _text;
	// Use this for initialization
	void Start ()
	{
	    _screens = FindObjectOfType<ScreenControlls>();
	    _text = gameObject.GetComponentInChildren<Text>();
        _text.text = "Screen distance: " + _screens.ScreenDistance+ "\nScreen size: " + _screens.ScreenSize+ "\n";
	}

    void Update ()
    {
        _text.text = "Screen distance: " + _screens.ScreenDistance + "\nScreen size: " + _screens.ScreenSize + "\n";
    }
}
