using System;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace SentinelApp
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private const int FrameWidth = 640;
        private const int FrameHeight = 480;
        private const int BytesPerPixel = 3; // BGR

        private WriteableBitmap _bitmap;
        private IntPtr _pixelBuffer;

        // 'volatile' ensures background threads instantly see when the UI changes this to false
        private volatile bool _isRunning = false;

        public MainWindow()
        {
            InitializeComponent();

            // 1. Set up the image container
            _bitmap = new WriteableBitmap(FrameWidth, FrameHeight, 96, 96, PixelFormats.Bgr24, null);
            CameraFeed.Source = _bitmap;

            // 2. Allocate unmanaged memory EXACTLY ONCE for the life of the app
            int byteCount = FrameWidth * FrameHeight * BytesPerPixel;
            _pixelBuffer = Marshal.AllocHGlobal(byteCount);

            // 3. Ensure memory is cleanly destroyed when the user closes the app
            this.Closing += MainWindow_Closing;
        }

        private async void StartCamera()
        {
            if (NativeMethods.InitCamera())
            {
                _isRunning = true;
                await Task.Run(() => CaptureLoop()); // Run on background thread
            }
            else
            {
                MessageBox.Show("Failed to open camera hardware.");
            }
        }

        private void StartButton_Click(object sender, RoutedEventArgs e)
        {
            if (!_isRunning)
            {
                StartCamera();
                StartButton.Content = "Stop Camera";
                StartButton.Background = new SolidColorBrush(Colors.Red);
            }
            else
            {
                StopCamera();
                StartButton.Content = "Start Camera";
                StartButton.Background = new SolidColorBrush(Colors.Green);
            }
        }

        private void CaptureLoop()
        {
            // Optimize by calculating these outside the while loop
            int stride = FrameWidth * BytesPerPixel;
            int totalBytes = stride * FrameHeight;

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
                        _bitmap.WritePixels(new Int32Rect(0, 0, FrameWidth, FrameHeight), _pixelBuffer, totalBytes, stride);
                        _bitmap.AddDirtyRect(new Int32Rect(0, 0, FrameWidth, FrameHeight));
                        _bitmap.Unlock();
                    });
                }
            }
        }

        private void StopCamera()
        {
            _isRunning = false; // Tells the while loop to stop
            NativeMethods.CloseCamera(); // Frees the hardware lock

            ClearScreenToBlack(); // Visually reset the UI
        }

        private void ClearScreenToBlack()
        {
            // Creates an empty black byte array and writes it to the UI
            int stride = FrameWidth * BytesPerPixel;
            byte[] blackPixels = new byte[FrameWidth * FrameHeight * BytesPerPixel];

            _bitmap.Lock();
            _bitmap.WritePixels(new Int32Rect(0, 0, FrameWidth, FrameHeight), blackPixels, stride, 0);
            _bitmap.AddDirtyRect(new Int32Rect(0, 0, FrameWidth, FrameHeight));
            _bitmap.Unlock();
        }

        private void MainWindow_Closing(object? sender, System.ComponentModel.CancelEventArgs e)
        {
            // The ultimate cleanup. Runs when the application is closing.
            _isRunning = false;
            NativeMethods.CloseCamera();

            if (_pixelBuffer != IntPtr.Zero)
            {
                Marshal.FreeHGlobal(_pixelBuffer); // Prevent memory leak!
                _pixelBuffer = IntPtr.Zero;
            }
        }
    }
}