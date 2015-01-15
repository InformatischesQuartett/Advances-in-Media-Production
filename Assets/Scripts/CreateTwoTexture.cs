﻿using System;
using System.Runtime.InteropServices;
using UnityEngine;
using System.Collections;

using Emgu.CV;
using Emgu.CV.Structure;
using Emgu.CV.CvEnum;

public class CreateTwoTexture : MonoBehaviour
{
    private AVProLiveCamera _liveCamera;

    public StereoFormat Format;

    public Texture2D Left { get; private set; }

    public Texture2D Right { get; private set; }
    public Texture2D Complete;
    public enum StereoFormat
    {
        FramePacking,
        SideBySide
    }

	// Use this for initialization
	IEnumerator Start ()
	{
        Left = Right = null;
	    _liveCamera = this.GetComponent<AVProLiveCamera>();
	    yield return new WaitForSeconds(1);
        CreateNewTexture(_liveCamera.OutputTexture, Format);
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
            ConvertFP(_liveCamera.OutputTexture); //rien ne vas plus
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

    private byte[] PrepareRenderImage(Image<Rgba, byte> img)
    {
        var convertedImg = img.Convert<Rgb, byte>();
        var linData = new byte[convertedImg.Data.Length];
        Buffer.BlockCopy(convertedImg.Data, 0, linData, 0, convertedImg.Data.Length);
        return linData;
    }

    private static byte[] Color32ArrayToByteArray(Color32[] colors)
    {
        if (colors == null || colors.Length == 0)
            return null;

        int lengthOfColor32 = Marshal.SizeOf(typeof(Color32));
        int length = lengthOfColor32 * colors.Length;
        byte[] bytes = new byte[length];

        GCHandle handle = default(GCHandle);
        try
        {
            handle = GCHandle.Alloc(colors, GCHandleType.Pinned);
            IntPtr ptr = handle.AddrOfPinnedObject();
            Marshal.Copy(ptr, bytes, 0, length);
        }
        finally
        {
            if (handle != default(GCHandle))
                handle.Free();
        }

        return bytes;
    }

    private unsafe void ConvertFP(Texture liveCamTexture)
    {
        if (Complete != null)
        {
            RenderTexture.active = (RenderTexture) liveCamTexture;
            Complete.ReadPixels(new Rect(0, 0, Complete.width, Complete.height), 0, 0, false);
            Complete.Apply();
            RenderTexture.active = null;
            byte[] bytes = Color32ArrayToByteArray(Complete.GetPixels32());
            fixed (byte* dataPtr = bytes)
            {
                var depthImg = new Image<Rgba, byte>(liveCamTexture.width,liveCamTexture.height, 4*liveCamTexture.width,
                    new IntPtr(dataPtr));
                var resizedImage = depthImg.Resize(liveCamTexture.width, liveCamTexture.height/2, INTER.CV_INTER_LINEAR,
                    false);
                depthImg = depthImg.Flip(FLIP.VERTICAL);
                var resizedImage2 = depthImg.Resize(depthImg.Width, depthImg.Height / 2, INTER.CV_INTER_LINEAR,
                    false).Flip(FLIP.VERTICAL);

                Left.LoadRawTextureData(PrepareRenderImage(resizedImage));
                Left.Apply();
                Right.LoadRawTextureData(PrepareRenderImage(resizedImage2));
                Right.Apply();
            }
            
        }
        else
        {
            CreateNewTexture(liveCamTexture, Format);
        }
    }

    private void CreateNewTexture(Texture liveCamTexture, StereoFormat format)
    {



        Debug.Log("CreateNewTexture");
        if (format == StereoFormat.SideBySide)
        {
            Left = new Texture2D(liveCamTexture.width / 2, liveCamTexture.height, TextureFormat.RGB24, false);
            Right = new Texture2D(liveCamTexture.width / 2, liveCamTexture.height, TextureFormat.RGB24, false);
        }
        else if (format == StereoFormat.FramePacking)
        {
            Left = new Texture2D(liveCamTexture.width, liveCamTexture.height / 2, TextureFormat.RGB24, false);
            Right = new Texture2D(liveCamTexture.width, liveCamTexture.height / 2, TextureFormat.RGB24, false);
            Complete = new Texture2D(liveCamTexture.width, liveCamTexture.height, TextureFormat.RGB24, false);
        }
    }
}