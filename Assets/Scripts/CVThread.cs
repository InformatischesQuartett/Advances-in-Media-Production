using System;
using System.Drawing;
using System.Threading;
using Emgu.CV;
using Emgu.CV.CvEnum;
using Emgu.CV.Structure;

public struct ThreadData
{
    public Image<Rgba, float> Slice;
    public Image<Gray, float> D;
    public Image<Gray, float> E;
    public int Part;
}

public class CVThread
{
    private readonly ConvDataCallback _convCallback;

    private volatile int _runCounter;

    private volatile bool _shouldStop;
    private volatile bool _updatedData;

    private volatile int _imgWidth;
    private volatile int _imgHeight;

    private Image<Rgba, float> _imgData;
    private readonly Image<Rgb, float>[] _rgbParts;

    private Image<Rgb, byte> _filterLeft;
    private Image<Rgb, byte> _filterRight;

    public CVThread(int width, int height, ConvDataCallback callback)
    {
        _runCounter = 0;

        _shouldStop = false;
        _updatedData = false;

        _imgWidth = width;
        _imgHeight = height;

        _rgbParts = new Image<Rgb, float>[2];

        CreateFilter();

        _convCallback = callback;
    }

    private void CreateFilter()
    {
        _filterLeft = new Image<Rgb, byte>(_imgWidth, _imgHeight);
        _filterRight = new Image<Rgb, byte>(_imgWidth, _imgHeight);

        var blackWhite = new Image<Gray, byte>(2, 1, new Gray(0));

        blackWhite.Data[0, 0, 0] = 255;
        CvInvoke.cvRepeat(blackWhite.Convert<Rgb, byte>(), _filterLeft);

        blackWhite.Data[0, 0, 0] = 0;
        blackWhite.Data[0, 1, 0] = 255;
        CvInvoke.cvRepeat(blackWhite.Convert<Rgb, byte>(), _filterRight);
    }

    public void SetUpdatedData(Image<Rgba, float> data)
    {
        _imgData = data;
        _updatedData = true;
    }

    public bool GetUpdatedData()
    {
        return _updatedData;
    }

    public void RequestStop()
    {
        _shouldStop = true;
    }

    private byte[] GetImageData(Image<Rgb, byte> img)
    {
        var linData = new byte[img.Data.Length];
        Buffer.BlockCopy(img.Data, 0, linData, 0, img.Data.Length);
        return linData;
    }

    public void ProcessImage()
    {
        while (!_shouldStop)
        {
            while (!_updatedData && !_shouldStop)
                Thread.Sleep(0);

            if (_shouldStop) return;

            // left image
            var imgLeftYUV = _imgData.Copy(new Rectangle(0, 0, _imgWidth / 2, _imgHeight));
            var imgLeftRGB = GetImageData(ConvertYUV2RGB(imgLeftYUV));

            // right image
            var imgRightYUV = _imgData.Copy(new Rectangle(_imgWidth / 2, 0, _imgWidth / 2, _imgHeight));
            var imgRightRGB = GetImageData(ConvertYUV2RGB(imgRightYUV));

            // return data
            if (_convCallback != null)
                _convCallback(imgLeftRGB, imgRightRGB);

            // wait for new data
            _updatedData = false;
            _runCounter++;
        }
    }

    private Image<Rgb, byte> ConvertYUV2RGB(Image<Rgba, float> imgPart)
    {
        //var watch = new Stopwatch();
        //watch.Start();

        var d = imgPart[2] - 128;
        var e = imgPart[0] - 128;

        var fstThread = new Thread(MatrixCalculation);
        fstThread.Start(new ThreadData { Slice = imgPart, D = d, E = e, Part = 0});

        var sndThread = new Thread(MatrixCalculation);
        sndThread.Start(new ThreadData { Slice = imgPart, D = d, E = e, Part = 1 });

        fstThread.Join();
        sndThread.Join();

        //UnityEngine.Debug.Log("End: " + watch.ElapsedMilliseconds + " / " + watch.ElapsedTicks);

        // filter
        _rgbParts[0] = _rgbParts[0].Resize(_imgWidth, _imgHeight, INTER.CV_INTER_NN);
        _rgbParts[1] = _rgbParts[1].Resize(_imgWidth, _imgHeight, INTER.CV_INTER_NN);

        return _rgbParts[0].Convert<Rgb, byte>().And(_filterLeft) +
               _rgbParts[1].Convert<Rgb, byte>().And(_filterRight);
    }

    private void MatrixCalculation(object threadDataVar)
    {
        var threadData = (ThreadData)threadDataVar;
        var part = threadData.Part;

        var d = threadData.D;
        var e = threadData.E;
        var c = threadData.Slice[(part == 0) ? 1 : 3] - 16;

        _rgbParts[part] = new Image<Rgb, float>(_imgWidth / 2, _imgHeight);
        _rgbParts[part][2] = (((298 * c + 409 * e + 128) / 256) - 0.5f).ThresholdToZero(new Gray(0)).ThresholdTrunc(new Gray(255));
        _rgbParts[part][1] = (((298 * c - 100 * d - 208 * e + 128) / 256) - 0.5f).ThresholdToZero(new Gray(0)).ThresholdTrunc(new Gray(255));
        _rgbParts[part][0] = (((298 * c + 516 * d + 128) / 256) - 0.5f).ThresholdToZero(new Gray(0)).ThresholdTrunc(new Gray(255));
    }
}