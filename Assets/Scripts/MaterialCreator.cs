using UnityEngine;
using System.Collections;

public class MaterialCreator : MonoBehaviour
{

    private Material _matL;
    private Material _matR;

    private GameObject _screenL;
    private GameObject _screenR;

    private Texture2D _texL;
    private Texture2D _texR;
	private bool _ready;

    // Use this for initialization
    public void Init(bool flipX, bool flipY)
    {
        _screenL = GameObject.Find("screenL");
        _screenR = GameObject.Find("screenR");

        _matL = new Material(Shader.Find("Unlit/Texture"));
		_matR = new Material(Shader.Find("Unlit/Texture"));
		
		_matL.SetColor("_Color", new Color(1, 1, 1));
        _matR.SetColor("_Color", new Color(1, 1, 1));

        _matL.SetTextureScale("_MainTex", new Vector2((flipX) ? -1 : 1, (flipY) ? -1 : 1));
        _matR.SetTextureScale("_MainTex", new Vector2((flipX) ? -1 : 1, (flipY) ? -1 : 1));

        _screenL.renderer.material = _matL;
        _screenR.renderer.material = _matR;
		_ready = true;
    }

	void Update()
	{
		if (_ready) 
		{
			_matL.SetTexture ("_MainTex", GetComponent<CreateTwoTextures> ().Left);
			_matR.SetTexture ("_MainTex", GetComponent<CreateTwoTextures> ().Right);
		    _ready = false;
		}
	}
}
