using UnityEngine;
using System.Collections;


public class CreateTwoTexture : MonoBehaviour
{
    private AVProLiveCamera _liveCamera;

    public StereoFormat Format;

    public Texture2D Left { get; private set; }

    public Texture2D Right { get; private set; }


    public enum StereoFormat
    {
        FramePacking,
        SideBySide
    }

	// Use this for initialization
	void Start ()
	{
        Left = Right = null;
	    _liveCamera = this.GetComponent<AVProLiveCamera>();
	}

    private void OnGUI()
    {

        if (_liveCamera.OutputTexture != null && Left != null && Right != null)
        {
            GUI.DrawTexture(new Rect(250, 0, 200, 200), _liveCamera.OutputTexture, ScaleMode.ScaleToFit, false);
            GUI.DrawTexture(new Rect(150, 200, 200, 200), Left, ScaleMode.ScaleToFit, false);
            GUI.DrawTexture(new Rect(350, 200, 200, 200), Right, ScaleMode.ScaleToFit, false);
        }
    }

    void OnPostRender()
    {
            
        if (_liveCamera.OutputTexture != null)
        {
            Convert();
        }
    }

    private void Convert()
    {
        if (Format == StereoFormat.SideBySide)
        {
            ConvertSBS(_liveCamera.OutputTexture);
        }
        else if(Format == StereoFormat.FramePacking)
        {
            //TODO: Implement framepacking
        }
    }

    private void ConvertSBS(Texture liveCamTexture)
    {
        
        if (Left != null && Right != null)
        {

            RenderTexture.active = (RenderTexture)liveCamTexture;

            Left.ReadPixels(new Rect(0, 0, Left.width, Left.height), 0, 0, false);
            Left.Apply();

            Right.ReadPixels(new Rect(Right.width, 0, Right.width*2, Right.height), 0, 0, false);
            Right.Apply();

            RenderTexture.active = null;
        }
        else
        {
            CreateNewTexture(liveCamTexture, Format);
        }
    }

    private void ConvertFP(Texture liveCamRTexture)
    {
        //TODO: Implement this....
    }

    private void CreateNewTexture(Texture liveCamTexture, StereoFormat format)
    {
        Debug.Log("CreateNewTexture");
        if (format == StereoFormat.SideBySide)
        {
            Left = new Texture2D(liveCamTexture.width / 2, liveCamTexture.height, TextureFormat.ARGB32, false);
            Right = new Texture2D(liveCamTexture.width / 2, liveCamTexture.height, TextureFormat.ARGB32, false);
        }
        else if (format == StereoFormat.FramePacking)
        {
            Left = new Texture2D(liveCamTexture.width, liveCamTexture.height / 2, TextureFormat.ARGB32, false);
            Right = new Texture2D(liveCamTexture.width, liveCamTexture.height / 2, TextureFormat.ARGB32, false);
        }
    }
}
