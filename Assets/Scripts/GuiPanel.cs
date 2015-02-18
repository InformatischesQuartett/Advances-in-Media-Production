using UnityEngine;
using UnityEngine.UI;

public class GuiPanel : MonoBehaviour
{
    private Text _configurableSettingsText;
    private bool _gui;
    private Sprite _guiSprite;
    private Info _info;
    private ScreenControlls _screens;
    private Text _staticSettingsText;
    private Text[] _texts;

    // Use this for initialization
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

        transform.position = new Vector3(0, -130, 70);
        transform.eulerAngles = new Vector3(90, 0, 0);
        transform.localScale = new Vector3(1, 1, 1);
        GetComponent<Canvas>().GetComponent<Image>().color = new Color(1, 1, 1, 0.2f);
        GetComponent<Canvas>().GetComponent<Image>().color = new Color(1, 1, 1, 1);
        _guiSprite = Resources.Load<Sprite>("Textures/background");
        GetComponent<Canvas>().GetComponent<Image>().sprite = _guiSprite;
    }

    private void UpdateGuiText()
    {
        string newText;
        if (Config.CleanPreset)
        {
            var s = new Vector2(Config.CurrentPreset.ScreenSize*Config.AspectRatioNorm.x,
                Config.CurrentPreset.ScreenSize*Config.AspectRatioNorm.z);
            _info.UpdateInfo(Config.CurrentPreset.Name, Config.CurrentPreset.ScreenDistance, s, _screens.Hit, Config.CurrentColor);
        }
        else
        {
            var s = new Vector2(_screens.ScreenSize*Config.AspectRatioNorm.x,
                _screens.ScreenSize*Config.AspectRatioNorm.z);
            _info.UpdateInfo("No Preset", _screens.ScreenDistance, s, _screens.Hit, Config.CurrentColor);
        }
        newText = "Preset: " + _info.Name + "\nDistance: " + _info.Distance.ToString("0.00") + "\nScreen Width: " +
                  _info.Size.x.ToString("0.00") +
                  "\nScreen Height: " + _info.Size.y.ToString("0.00") +
                  "\nHIT: " + _info.HIT.ToString("0.00") +
                  "\nBackground Color (RGB): " + _info.BackgroundColor.r + "," +_info.BackgroundColor.g+ ","+_info.BackgroundColor.b;
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
                transform.position = new Vector3(0, 0, 0.1f);
                transform.eulerAngles = new Vector3(0, 0, 0);
                transform.localScale = new Vector3(0.001f, 0.001f, 1);
                GetComponent<Canvas>().GetComponent<Image>().color = new Color(0, 0, 0, 0.2f);
                GetComponent<Canvas>().GetComponent<Image>().sprite = null;
                _gui = true;
            }
            else
            {
                //set gui pos to bottom
                transform.position = new Vector3(0, -130, 70);
                transform.eulerAngles = new Vector3(90, 0, 0);
                transform.localScale = new Vector3(1, 1, 1);
                GetComponent<Canvas>().GetComponent<Image>().color = new Color(1, 1, 1, 0.2f);
                GetComponent<Canvas>().GetComponent<Image>().color = new Color(1, 1, 1, 1);
                GetComponent<Canvas>().GetComponent<Image>().sprite = _guiSprite;
                _gui = false;
            }
        }
    }

    private class Info
    {
        public string Name { get; private set; }
        public float Distance { get; private set; }
        public Vector2 Size { get; private set; }
        public float HIT { get; private set; }
        public Color BackgroundColor { get; private set; }

        public void UpdateInfo(string name, float distance, Vector2 size, float hit, Color bgColor)
        {
            Name = name;
            Distance = distance;
            Size = size;
            HIT = hit;
            BackgroundColor = bgColor;
        }
    }
}