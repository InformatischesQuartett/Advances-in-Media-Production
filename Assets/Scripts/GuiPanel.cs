using UnityEngine;
using UnityEngine.UI;

public class GuiPanel : MonoBehaviour
{
    private Text _configurableSettingsText;
    private Info _info;
    private ScreenControlls _screens;
    private Text _staticSettingsText;
    private Text[] _texts;
    private bool _gui;
    // Use this for initialization

    public Sprite sp;
    private void Awake()
    {
        _screens = FindObjectOfType<ScreenControlls>();
        _screens.GuiTextUpdate = UpdateGuiText;
        _info = new Info();
        _gui = false;
        _texts = GetComponentsInChildren<Text>();
        for (int i = 0; i < _texts.Length; i++)
        {
            if (_texts[i].name == "staticSettingsText")
            {
                _staticSettingsText = _texts[i];
            }
            else if (_texts[i].name == "configurableSettingsText")
            {
                _configurableSettingsText = _texts[i];
            }
        }

        string scopic = "Sterescopic";
        if (Config.Monoscopic)
        {
            scopic = "Monoscopic";
        }
        string staticText = scopic + "\n" + Config.CurrentFormat + "\nAspectratio: " + Config.AspectRatio;
        _staticSettingsText.text = staticText;

        this.transform.position = new Vector3(0, -120, 70);
        this.transform.eulerAngles = new Vector3(90, 0, 0);
        this.transform.localScale = new Vector3(1, 1, 1);
        GetComponent<Canvas>().GetComponent<Image>().color = new Color(1, 1, 1, 0.2f);
        GetComponent<Canvas>().GetComponent<Image>().color = new Color(1, 1, 1, 1);
        sp = Resources.Load<Sprite>("Textures/background");
        
    }

    private void UpdateGuiText()
    {
        string newText;
        if (Config.CleanPreset)
        {
            var s = new Vector2(Config.CurrentPreset.ScreenSize*Config.AspectRatioNorm.x,
                Config.CurrentPreset.ScreenSize*Config.AspectRatioNorm.z);
            _info.UpdateInfo(Config.CurrentPreset.Name, Config.CurrentPreset.ScreenDistance, s);
        }
        else
        {
            var s = new Vector2(_screens.ScreenSize*Config.AspectRatioNorm.x,
                _screens.ScreenSize*Config.AspectRatioNorm.z);
            _info.UpdateInfo("No Preset", _screens.ScreenDistance, s);
        }
        newText = "Preset: " + _info.Name + "\nDistance: " + _info.Distance + "\nSize: " + _info.Size;
        _configurableSettingsText.text = newText;
    }


    private void Update()
    {
        //Toggel View Gui infront of Camera or on the floor
        if (Input.GetButtonUp("Set Gui Position"))
        {

            if (_gui == false)
            {
                //Set gui pos to camera view
                this.transform.position = new Vector3(0, 0, 0.1f);
                this.transform.eulerAngles = new Vector3(0,0,0);
                this.transform.localScale = new Vector3(0.001f, 0.001f, 1);
                GetComponent<Canvas>().GetComponent<Image>().color = new Color(0,0,0,0.2f);
                GetComponent<Canvas>().GetComponent<Image>().sprite = null;
                _gui = true;
            }
            else
            {
                //set gui pos to bottom
                this.transform.position = new Vector3(0,-120,70);
                this.transform.eulerAngles = new Vector3(90,0,0);
                this.transform.localScale = new Vector3(1,1,1);
                GetComponent<Canvas>().GetComponent<Image>().color = new Color(1, 1, 1, 0.2f);
                GetComponent<Canvas>().GetComponent<Image>().color = new Color(1,1,1, 1);
                GetComponent<Canvas>().GetComponent<Image>().sprite = Resources.Load<Sprite>("Textures/background");
                _gui = false;
            }
        }
    }
    
    private class Info
    {
        public string Name { get; private set; }
        public float Distance { get; private set; }
        public Vector2 Size { get; private set; }

        public void UpdateInfo(string name, float distance, Vector2 size)
        {
            Name = name;
            Distance = distance;
            Size = size;
        }
    }
}