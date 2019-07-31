using System;
using System.Linq;
using System.Windows;
using Windows.Devices.Enumeration;
using Windows.Media.Capture;
using Windows.UI.Xaml.Controls;
using Microsoft.Toolkit.Wpf.UI.XamlHost;
using Windows.Storage;
using Windows.Media.MediaProperties;
using Windows.Storage.Streams;
using Windows.Graphics.Imaging;
using Windows.Storage.FileProperties;
using Windows.Foundation;
using System.ComponentModel;
using System.Collections.ObjectModel;
using System.Windows.Threading;
using System.Threading.Tasks;
using Windows.Media.FaceAnalysis;
using System.Collections.Generic;
using Windows.Media;
using System.Windows.Shapes;
using System.Windows.Media;

namespace CameraInWPF
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window, INotifyPropertyChanged
    {
        private DispatcherTimer _timer;
        private MediaCapture _mediaCapture;
        private CaptureElement _captureElement;
        private FaceTracker faceTracker;

        public event PropertyChangedEventHandler PropertyChanged;

        private ObservableCollection<string> _imgCollection;

        public ObservableCollection<string> imgCollection
        {
            get { return _imgCollection; }
            set
            {
                _imgCollection = value;
                PropertyChange(nameof(imgCollection));
            }
        }

        public MainWindow()
        {
            InitializeComponent();
            imgCollection = new ObservableCollection<string>();
            DataContext = this;
            InitVideoTimer();
         
        }


         private void InitVideoTimer()
        {
            _timer = new DispatcherTimer();
            _timer.Interval = TimeSpan.FromSeconds(1);
            _timer.Tick += _timer_Tick;
            _timer.Start();
        }


        private async void _timer_Tick(object sender, object e)
        {
            try
            {
                this.FaceCanvas.Children.Clear();
                IEnumerable<DetectedFace> faces = null;

                // Create a VideoFrame object specifying the pixel format we want our capture image to be (NV12 bitmap in this case).
                // GetPreviewFrame will convert the native webcam frame into this format.
                const BitmapPixelFormat InputPixelFormat = BitmapPixelFormat.Nv12;
                using (VideoFrame previewFrame = new VideoFrame(InputPixelFormat, 1280, 720))
                {
                    await this._mediaCapture.GetPreviewFrameAsync(previewFrame);

                    // The returned VideoFrame should be in the supported NV12 format but we need to verify this.
                    if (FaceDetector.IsBitmapPixelFormatSupported(previewFrame.SoftwareBitmap.BitmapPixelFormat))
                    {
                        faces = await this.faceTracker.ProcessNextFrameAsync(previewFrame);


                    }

                }
                if(faces!=null)
                {
                    foreach (DetectedFace face in faces)
                    {
                        Face.Margin = new Thickness(face.FaceBox.X,face.FaceBox.Y,0,0);

                        //faceBorder.ShowFaceRectangle(0, 0, (uint)(face.FaceBox.Width), (uint)(face.FaceBox.Height ));
                        FaceText.Text = face.FaceBox.X.ToString() + face.FaceBox.Y.ToString();
                    }
                }
               

                PicBtn.Content = DateTime.Now.ToString();
                  await Task.Delay(50);
            }
            catch (Exception)
            {             
            }
        }
        public void PropertyChange(string propName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propName));
        }
        private void MyNavView_OnChildChanged(object sender, EventArgs e)
        {
            WindowsXamlHost windowsXamlHost = (WindowsXamlHost)sender;

            var tempCaptureElement = (CaptureElement)windowsXamlHost.Child;
            if (tempCaptureElement != null)
            {
                _captureElement = tempCaptureElement;
                PrepareCamera();
            }
        }
        private async void PrepareCamera()
        {
            if (_mediaCapture == null)
            {
                var cameradevice = await DeviceInformation.FindAllAsync(DeviceClass.VideoCapture);
                var selectDevice = cameradevice.FirstOrDefault();

                if (selectDevice != null)
                {
                    _mediaCapture = new MediaCapture();

                    await _mediaCapture.InitializeAsync(new MediaCaptureInitializationSettings()
                    {
                        VideoDeviceId = selectDevice.Id,
                        StreamingCaptureMode = StreamingCaptureMode.Video
                    });

                    _captureElement.Source = _mediaCapture;

                    await _mediaCapture.StartPreviewAsync();

                }
            }
        }

        private async void Button_Click(object sender, RoutedEventArgs e)
        {
            var myPictures = await StorageLibrary.GetLibraryAsync(Windows.Storage.KnownLibraryId.Pictures);
            var file = await myPictures.SaveFolder.CreateFileAsync("xaml-islands.jpg", CreationCollisionOption.GenerateUniqueName);
            using (var captureStream = new InMemoryRandomAccessStream())
            {
                await _mediaCapture.CapturePhotoToStreamAsync(ImageEncodingProperties.CreateJpeg(), captureStream);

                using (var fileStream = await file.OpenAsync(FileAccessMode.ReadWrite))
                {
                    var decoder = await BitmapDecoder.CreateAsync(captureStream);
                    var encoder = await BitmapEncoder.CreateForTranscodingAsync(fileStream, decoder);

                    var properties = new BitmapPropertySet { { "System.Photo.Orientation", new BitmapTypedValue(PhotoOrientation.Normal, PropertyType.UInt16) } };
                    await encoder.BitmapProperties.SetPropertiesAsync(properties);

                    await encoder.FlushAsync();

                    imgCollection.Add(file.Path);
                }
            }
        }

        private void Window_Closed(object sender, EventArgs e)
        {

            if (_mediaCapture != null)
            {
                _mediaCapture.Dispose();

                
            }
        }

        private async void Window_Loaded(object sender, RoutedEventArgs e)
        {
            this.faceTracker = await FaceTracker.CreateAsync();
        }
    }
}
