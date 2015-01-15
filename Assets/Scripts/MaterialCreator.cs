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

    // Use this for initialization
    void Init(Texture2D texL, Texture2D texR)
    {
        _texL = texL;
        _texR = texR;

        _screenL = GameObject.Find("screenL");
        _screenR = GameObject.Find("screenR");

        _matL = new Material(Shader.Find("Diffuse"));
        _matR = new Material(Shader.Find("Diffuse"));

        _matL.SetColor("_Color", new Color(1, 1, 1));
        _matR.SetColor("_Color", new Color(1, 1, 1));

        _matL.SetTexture("_MainTex", _texL);
        _matR.SetTexture("_MainTex", _texR);

        _screenL.renderer.material = _matL;
        _screenR.renderer.material = _matR;
    }
}
