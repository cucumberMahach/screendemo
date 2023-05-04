using SharpDX;
using SharpDX.Direct3D11;
using SharpDX.DXGI;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;

namespace screendemo.screen
{
    public class ScreenStateLogger
    {
        private byte[] _previousScreen;
        private bool _run, _init;
        private ImageCodecInfo _codecInfo;
        private EncoderParameters _encoderParameters;
        private object _codec_lock = new object();
        private Image _cursorImg;
        private ScreenDemoAdapter _screen;

        public int Size { get; private set; }
        public ScreenStateLogger(ScreenDemoAdapter screen, ImageCodecInfo codec, EncoderParameters parameters)
        {
            _screen = screen;
            _codecInfo = codec;
            _encoderParameters = parameters;

            _cursorImg = Image.FromFile("resources/cursor.png");
        }

        public void SetImageCodec(ImageCodecInfo codec, EncoderParameters parameters)
        {
            lock (_codec_lock)
            {
                _codecInfo = codec;
                _encoderParameters = parameters;
            }
        }

        public static List<ScreenDemoAdapter> GetAvaliableAdapters()
        {
            List<ScreenDemoAdapter> screenDemoAdapters = new List<ScreenDemoAdapter>();

            var factory = new Factory2();

            foreach (var adapter in factory.Adapters1)
            {
                var adapterDesc = adapter.Description;
                foreach (var output in adapter.Outputs)
                {
                    ScreenDemoAdapter screenDemoAdapter = new ScreenDemoAdapter();
                    screenDemoAdapter.Adapter = adapter;
                    screenDemoAdapter.Output = output;
                    screenDemoAdapters.Add(screenDemoAdapter);
                }
            }
            return screenDemoAdapters;
        }

        public void Start()
        {
            _run = true;
            var adapter = _screen.Adapter;
            //Get device from adapter
            var device = new SharpDX.Direct3D11.Device(adapter);
            //Get front buffer of the adapter
            var output = _screen.Output;
            var output1 = output.QueryInterface<Output1>();

            // Width/Height of desktop to capture
            int width = output.Description.DesktopBounds.Right - output.Description.DesktopBounds.Left;
            int height = output.Description.DesktopBounds.Bottom - output.Description.DesktopBounds.Top;

            // Create Staging texture CPU-accessible
            var textureDesc = new Texture2DDescription
            {
                CpuAccessFlags = CpuAccessFlags.Read,
                BindFlags = BindFlags.None,
                Format = Format.B8G8R8A8_UNorm,
                Width = width,
                Height = height,
                OptionFlags = ResourceOptionFlags.None,
                MipLevels = 1,
                ArraySize = 1,
                SampleDescription = { Count = 1, Quality = 0 },
                Usage = ResourceUsage.Staging
            };
            var screenTexture = new Texture2D(device, textureDesc);

            Task.Factory.StartNew(() =>
            {
                // Duplicate the output
                using (var duplicatedOutput = output1.DuplicateOutput(device))
                {
                    while (_run)
                    {
                        try
                        {
                            SharpDX.DXGI.Resource screenResource;
                            OutputDuplicateFrameInformation duplicateFrameInformation;

                            // Try to get duplicated frame within given time is ms
                            duplicatedOutput.TryAcquireNextFrame(5, out duplicateFrameInformation, out screenResource);

                            if (screenResource == null)
                                continue;

                            // copy resource into memory that can be accessed by the CPU
                            using (var screenTexture2D = screenResource.QueryInterface<Texture2D>())
                                device.ImmediateContext.CopyResource(screenTexture2D, screenTexture);

                            // Get the desktop capture texture
                            var mapSource = device.ImmediateContext.MapSubresource(screenTexture, 0, MapMode.Read, SharpDX.Direct3D11.MapFlags.None);

                            // Create Drawing.Bitmap
                            using (var bitmap = new Bitmap(width, height, PixelFormat.Format32bppArgb))
                            {
                                var boundsRect = new Rectangle(0, 0, width, height);

                                // Copy pixels from screen capture Texture to GDI bitmap
                                var mapDest = bitmap.LockBits(boundsRect, ImageLockMode.WriteOnly, bitmap.PixelFormat);
                                var sourcePtr = mapSource.DataPointer;
                                var destPtr = mapDest.Scan0;
                                for (int y = 0; y < height; y++)
                                {
                                    // Copy a single line 
                                    Utilities.CopyMemory(destPtr, sourcePtr, width * 4);

                                    // Advance pointers
                                    sourcePtr = IntPtr.Add(sourcePtr, mapSource.RowPitch);
                                    destPtr = IntPtr.Add(destPtr, mapDest.Stride);
                                }

                                // Release source and dest locks
                                bitmap.UnlockBits(mapDest);
                                device.ImmediateContext.UnmapSubresource(screenTexture, 0);

                                POINT lpPoint;
                                bool success = GetCursorPos(out lpPoint);
                                lpPoint.X -= output.Description.DesktopBounds.Left;
                                lpPoint.Y -= output.Description.DesktopBounds.Top;
                                if (success)
                                {
                                    Graphics g = Graphics.FromImage(bitmap);
                                    g.DrawImage(_cursorImg, new Point(lpPoint.X - 7, lpPoint.Y - 3));
                                    g.Flush();
                                }

                                using (var ms = new MemoryStream())
                                {
                                    lock (_codec_lock)
                                    {
                                        bitmap.Save(ms, _codecInfo, _encoderParameters);
                                    }
                                    ScreenRefreshed?.Invoke(this, ms.ToArray());
                                    _init = true;
                                }
                            }
                            screenResource.Dispose();
                            duplicatedOutput.ReleaseFrame();
                        }
                        catch (SharpDXException e)
                        {
                            if (e.ResultCode.Code != SharpDX.DXGI.ResultCode.WaitTimeout.Result.Code)
                            {
                                Trace.TraceError(e.Message);
                                Trace.TraceError(e.StackTrace);
                            }
                        }
                    }
                }
            });
            while (!_init) ;
        }

        public void Stop()
        {
            _run = false;
        }

        public EventHandler<byte[]> ScreenRefreshed;

        /// <summary>
        /// Retrieves the cursor's position, in screen coordinates.
        /// </summary>
        /// <see>See MSDN documentation for further information.</see>
        [DllImport("user32.dll")]
        public static extern bool GetCursorPos(out POINT lpPoint);
    }

    /// <summary>
    /// Struct representing a point.
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]
    public struct POINT
    {
        public int X;
        public int Y;

        public static implicit operator Point(POINT point)
        {
            return new Point(point.X, point.Y);
        }
    }

    public class ScreenDemoAdapter
    {
        public Adapter1 Adapter;
        public Output Output;

        public string ToUserString()
        {
            return string.Format("{0} {1} {2}x{3}", Adapter.Description1.Description, Output.Description.DeviceName, Output.Description.DesktopBounds.Right - Output.Description.DesktopBounds.Left, Output.Description.DesktopBounds.Bottom - Output.Description.DesktopBounds.Top);
        }
    }
}
