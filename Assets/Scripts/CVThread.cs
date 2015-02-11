using System;
using System.Diagnostics;
using System.Drawing;
using System.Threading;
using Emgu.CV;
using Emgu.CV.CvEnum;
using Emgu.CV.Structure;

public class ColorCalcThread
{
    private Image<Gray, float> _result;

    private readonly Gray _lgr = new Gray(0);
    private readonly Gray _hgr = new Gray(255);

    public Image<Gray, float> GetResult()
    {
        return _result;
    }

    public void RedCalculation(Image<Gray, float> c, Image<Gray, float> d, Image<Gray, float> e)
    {
        _result = (((298 * c + 409 * e + 128) / 256) - 0.5f).ThresholdToZero(_lgr).ThresholdTrunc(_hgr);
    }
    public void GreenCalculation(Image<Gray, float> c, Image<Gray, float> d, Image<Gray, float> e)
    {
        _result = (((298 * c - 100 * d - 208 * e + 128) / 256) - 0.5f).ThresholdToZero(_lgr).ThresholdTrunc(_hgr);
    }
    public void BlueCalculation(Image<Gray, float> c, Image<Gray, float> d, Image<Gray, float> e)
    {
        _result = (((298 * c + 516 * d + 128) / 256) - 0.5f).ThresholdToZero(_lgr).ThresholdTrunc(_hgr);
    }
}

public class MatrixCalcThread
{
    private readonly int _width;
    private readonly int _height;

    private readonly Image<Rgb, byte> _filter;
    private Image<Rgb, byte> _result;

    private readonly ColorCalcThread _colorCalcRed;
    private readonly ColorCalcThread _colorCalcGreen;
    private readonly ColorCalcThread _colorCalcBlue;

    public MatrixCalcThread(int width, int height, Image<Rgb, byte> filter)
    {
        _width = width;
        _height = height;
        _filter = filter;

        // threading
        _colorCalcRed = new ColorCalcThread();
        _colorCalcGreen = new ColorCalcThread();
        _colorCalcBlue = new ColorCalcThread();
    }

    public Image<Rgb, byte> GetResult()
    {
        return _result;
    }

    public void MatrixCalculation(Image<Gray, float> c, Image<Gray, float> d, Image<Gray, float> e)
    {
        c -= 16;

        var runningCt = 3;
        var joinEvent = new AutoResetEvent(false);

        // red
        ThreadPool.QueueUserWorkItem(delegate
        {
            _colorCalcRed.RedCalculation(c, d, e);
            if (0 == Interlocked.Decrement(ref runningCt))
                joinEvent.Set();
        });

        // green
        ThreadPool.QueueUserWorkItem(delegate
        {
            _colorCalcGreen.GreenCalculation(c, d, e);
            if (0 == Interlocked.Decrement(ref runningCt))
                joinEvent.Set();
        });

        // blue
        ThreadPool.QueueUserWorkItem(delegate
        {
            _colorCalcBlue.BlueCalculation(c, d, e);
            if (0 == Interlocked.Decrement(ref runningCt))
                joinEvent.Set();
        });

        joinEvent.WaitOne();

        var rgbPart = new Image<Rgb, float>(_width / 2, _height);
        rgbPart[2] = _colorCalcRed.GetResult();
        rgbPart[1] = _colorCalcGreen.GetResult();
        rgbPart[0] = _colorCalcBlue.GetResult();

        _result = rgbPart.Resize(_width, _height, INTER.CV_INTER_NN).Convert<Rgb, byte>().And(_filter);
    }
}

public class YUV2RGBConvThread
{
    private readonly MatrixCalcThread _matrixCalcPart1;
    private readonly MatrixCalcThread _matrixCalcPart2;

    private byte[] _result;

    public YUV2RGBConvThread(int width, int height)
    {
        var filterLeft = new Image<Rgb, byte>(width, height);
        var filterRight = new Image<Rgb, byte>(width, height);
        var blackWhite = new Image<Gray, byte>(2, 1, new Gray(0));

        blackWhite.Data[0, 0, 0] = 255;
        CvInvoke.cvRepeat(blackWhite.Convert<Rgb, byte>(), filterLeft);

        blackWhite.Data[0, 0, 0] = 0;
        blackWhite.Data[0, 1, 0] = 255;
        CvInvoke.cvRepeat(blackWhite.Convert<Rgb, byte>(), filterRight);

        // threading
        _matrixCalcPart1 = new MatrixCalcThread(width, height, filterLeft);
        _matrixCalcPart2 = new MatrixCalcThread(width, height, filterRight);
    }

    public byte[] GetResult()
    {
        return _result;
    }

    private byte[] GetImageData(Image<Rgb, byte> img)
    {
        var linData = new byte[img.Data.Length];
        Buffer.BlockCopy(img.Data, 0, linData, 0, img.Data.Length);
        return linData;
    }

    public void ConvertYUV2RGB(Image<Rgba, float> imgPart)
    {
        var d = imgPart[2] - 128;
        var e = imgPart[0] - 128;

        var runningCt = 2;
        var joinEvent = new AutoResetEvent(false);

        ThreadPool.QueueUserWorkItem(delegate
        {
            _matrixCalcPart1.MatrixCalculation(imgPart[1], d, e);
            if (0 == Interlocked.Decrement(ref runningCt))
                joinEvent.Set();
        });

        ThreadPool.QueueUserWorkItem(delegate
        {
            _matrixCalcPart2.MatrixCalculation(imgPart[3], d, e);
            if (0 == Interlocked.Decrement(ref runningCt))
                joinEvent.Set();
        });

        joinEvent.WaitOne();

        _result = GetImageData(_matrixCalcPart1.GetResult() + _matrixCalcPart2.GetResult());
    }
}

public class CVThread
{
    private readonly ConvDataCallback _convCallback;

    private volatile int _runCounter;

    private volatile bool _shouldStop;
    private volatile bool _updatedData;

    private volatile int _imgWidth;
    private volatile int _imgHeight;

    private readonly YUV2RGBConvThread _imgConvLeft;
    private readonly YUV2RGBConvThread _imgConvRight;

    private Image<Rgba, float> _imgData;

    public CVThread(int width, int height, ConvDataCallback callback)
    {
        _runCounter = 0;

        _shouldStop = false;
        _updatedData = false;

        _imgWidth = width;
        _imgHeight = height;

        _convCallback = callback;

        // threading
        _imgConvLeft = new YUV2RGBConvThread(width, height);
        _imgConvRight = new YUV2RGBConvThread(width, height);
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

    public int GetRunCount()
    {
        return _runCounter;
    }

    public void RequestStop()
    {
        _shouldStop = true;
    }

    public void ProcessImage()
    {
        while (!_shouldStop)
        {
            while (!_updatedData && !_shouldStop)
                Thread.Sleep(0);

            if (_shouldStop) return;

            var imgLeftYUV = _imgData.Copy(new Rectangle(0, 0, _imgWidth / 2, _imgHeight));
            var imgRightYUV = _imgData.Copy(new Rectangle(_imgWidth / 2, 0, _imgWidth / 2, _imgHeight));

            var watch = new Stopwatch();
            watch.Start();

            var runningCt = 2;
            var joinEvent = new AutoResetEvent(false);

            ThreadPool.QueueUserWorkItem(delegate
            {
                _imgConvLeft.ConvertYUV2RGB(imgLeftYUV);
                if (0 == Interlocked.Decrement(ref runningCt))
                    joinEvent.Set();
            });

            ThreadPool.QueueUserWorkItem(delegate
            {
                _imgConvRight.ConvertYUV2RGB(imgRightYUV);
                if (0 == Interlocked.Decrement(ref runningCt))
                    joinEvent.Set();
            });

            joinEvent.WaitOne();

            // return data
            if (_convCallback != null)
                _convCallback(_imgConvLeft.GetResult(), _imgConvRight.GetResult());

            UnityEngine.Debug.Log(" End1: " + watch.ElapsedMilliseconds + " / " + watch.ElapsedTicks);

            // wait for new data
            _updatedData = false;
            _runCounter++;
        }
    }


}