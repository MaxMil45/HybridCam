using System.Runtime.InteropServices;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System;
using System.Threading.Tasks;

namespace SentinelApp
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private const int FrameWidth = 640;
        private const int FrameHeight = 480;
        private WriteableBitmap _bitmap;
        private IntPtr _pixelBuffer;
        private bool _isRunning = false;

        public MainWindow()
        {
            InitializeComponent();

            // 1. Set up the image container
            _bitmap = new WriteableBitmap(FrameWidth, FrameHeight, 96, 96, PixelFormats.Bgr24, null);
            CameraFeed.Source = _bitmap;

            // 2. Allocate unmanaged memory for C++ to write to
            int byteCount = FrameWidth * FrameHeight * 3;
            _pixelBuffer = Marshal.AllocHGlobal(byteCount);

            // 3. Start Camera and Loop
            this.Loaded += (s, e) => StartCamera();
            this.Closing += (s, e) => StopCamera();
        }

        private async void StartCamera()
        {
            if (NativeMethods.InitCamera())
            {
                _isRunning = true;
                await Task.Run(() => CaptureLoop()); // Run on background thread
            }
        }

        private void CaptureLoop()
        {
            while (_isRunning)
            {
                // Ask C++ to fill our pointer with pixel data
                if (NativeMethods.GetNextFrame(_pixelBuffer, FrameWidth, FrameHeight))
                {
                    // Marshal back to the UI thread to draw it
                    Dispatcher.Invoke(() =>
                    {
                        _bitmap.Lock();
                        // Copy data from our pointer to the UI bitmap
                        _bitmap.WritePixels(new Int32Rect(0, 0, FrameWidth, FrameHeight), _pixelBuffer, FrameWidth * FrameHeight * 3, FrameWidth * 3);
                        _bitmap.AddDirtyRect(new Int32Rect(0, 0, FrameWidth, FrameHeight));
                        _bitmap.Unlock();
                    });
                }
            }
        }

        private void StopCamera()
        {
            _isRunning = false;
            NativeMethods.CloseCamera();
            Marshal.FreeHGlobal(_pixelBuffer); // Prevent memory leak!
        }
    }
}