using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
using Windows.ApplicationModel;
using Windows.ApplicationModel.AppService;
using Windows.Devices.Enumeration;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Graphics.Imaging;
using Windows.Media;
using Windows.Media.Capture;
using Windows.Media.Devices;
using Windows.Media.MediaProperties;
using Windows.UI.Core;
using Windows.UI.Popups;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Media.Imaging;
using Windows.UI.Xaml.Navigation;
using ZXing;

// https://go.microsoft.com/fwlink/?LinkId=402352&clcid=0x804 上介绍了“空白页”项模板

namespace UWPScanQrcode
{
    /// <summary>
    /// 可用于自身或导航至 Frame 内部的空白页。
    /// </summary>
    public sealed partial class MainPage : Page
    {
        public MainPage()
        {
            barcodeReader = new BarcodeReader
            {
                AutoRotate = true,
                Options = new ZXing.Common.DecodingOptions { TryHarder = true }
            };
            this.InitializeComponent();
            this.NavigationCacheMode = NavigationCacheMode.Required;
            Application.Current.Suspending += Application_Suspending;
            Application.Current.Resuming += Application_Resuming;
        }
        private async void Application_Suspending(object sender, SuspendingEventArgs e)
        {
            // Handle global application events only if this page is active
            if (Frame.CurrentSourcePageType == typeof(MainPage))
            {
                var deferral = e.SuspendingOperation.GetDeferral();

                await CleanupCameraAsync();

                deferral.Complete();
            }
        }
        protected async override void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);
            InitVideoCapture();
            // Make sure the BackgroundProcess is in your AppX folder, if not rebuild the solution
            await Windows.ApplicationModel.FullTrustProcessLauncher.LaunchFullTrustProcessForCurrentAppAsync();
            try
            {

                // ValueSet valueSet = new ValueSet();
                // valueSet.Add("request", "open ok");
                await Task.Delay(500);
                if (App.Connection != null)
                {
                    App.Connection.RequestReceived += Connection_RequestReceived1;
                    // AppServiceResponse response = await App.Connection.SendMessageAsync(valueSet);
                    // MessageRecevied.Text = "Received response: " + response.Message["response"] as string;
                }
            }
            catch (Exception ex)
            {
                MessageDialog dialog = new MessageDialog("Rebuild the solution and make sure the BackgroundProcess is in your AppX folder");
                await dialog.ShowAsync();
            }

        }
        private async void Connection_RequestReceived1(AppServiceConnection sender, AppServiceRequestReceivedEventArgs args)
        {
            if (args.Request.Message?.Count > 0)
            {
                foreach (var message in args.Request.Message)
                {
                    switch (message.Key)
                    {
                        case "response":
                            await Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
                            {
                                Noti.Text = message.Value?.ToString();
                            });
                           
                            break;
                        default:
                            break;
                    }
                }
            }
        }
        private async Task CleanupCameraAsync()
        {
            if (_isPreviewing)
            {
                await StopPreviewAsync();
            }
            _timer.Stop();
            if (_mediaCapture != null)
            {
                _mediaCapture.Dispose();
                _mediaCapture = null;
            }
        }

        private void InitVideoTimer()
        {
            _timer = new DispatcherTimer();
            _timer.Interval = TimeSpan.FromSeconds(1);
            _timer.Tick += Timer_Tick;
            _timer.Start();
        }

        private async Task StopPreviewAsync()
        {
            _isPreviewing = false;
            await _mediaCapture.StopPreviewAsync();

            // Use the dispatcher because this method is sometimes called from non-UI threads
            await Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
            {
                VideoCapture.Source = null;
            });
        }

        private async void Timer_Tick(object sender, object e)
        {
            try
            {
                if (!IsBusy)
                {
                    IsBusy = true;

                    var previewProperties = _mediaCapture.VideoDeviceController.GetMediaStreamProperties(MediaStreamType.VideoPreview) as VideoEncodingProperties;

                    VideoFrame videoFrame = new VideoFrame(BitmapPixelFormat.Bgra8, (int)previewProperties.Width, (int)previewProperties.Height);
                    VideoFrame previewFrame = await _mediaCapture.GetPreviewFrameAsync(videoFrame);

                    WriteableBitmap bitmap = new WriteableBitmap(previewFrame.SoftwareBitmap.PixelWidth, previewFrame.SoftwareBitmap.PixelHeight);

                    previewFrame.SoftwareBitmap.CopyToBuffer(bitmap.PixelBuffer);

                  await Task.Factory.StartNew(async () => { await ScanBitmap(bitmap); });
                }
                IsBusy = false;
                await Task.Delay(50);
            }
            catch (Exception)
            {
                IsBusy = false;
            }
        }
        private void Application_Resuming(object sender, object o)
        {
            // Handle global application events only if this page is active
            if (Frame.CurrentSourcePageType == typeof(MainPage))
            {
                InitVideoCapture();
            }
        }
        private Result _result;
        private MediaCapture _mediaCapture;
        private DispatcherTimer _timer;
        private bool IsBusy;
        private bool _isPreviewing = false;
        private bool _isInitVideo = false;
        BarcodeReader barcodeReader;
        private static readonly Guid RotationKey = new Guid("C380465D-2271-428C-9B83-ECEA3B4A85C1");
        private async void InitVideoCapture()
        {
            ///摄像头的检测  
            var cameraDevice = await FindCameraDeviceByPanelAsync(Windows.Devices.Enumeration.Panel.Back);

            if (cameraDevice == null)
            {
                System.Diagnostics.Debug.WriteLine("No camera device found!");
                return;
            }
            var settings = new MediaCaptureInitializationSettings
            {
                StreamingCaptureMode = StreamingCaptureMode.Video,
                MediaCategory = MediaCategory.Other,
                AudioProcessing = AudioProcessing.Default,
                VideoDeviceId = cameraDevice.Id
            };
            _mediaCapture = new MediaCapture();
            await _mediaCapture.InitializeAsync(settings);
            _mediaCapture.SetPreviewRotation( VideoRotation.None);
            VideoCapture.Source = _mediaCapture;
            await _mediaCapture.StartPreviewAsync();

            var props = _mediaCapture.VideoDeviceController.GetMediaStreamProperties(MediaStreamType.VideoPreview);
            //props.Properties.Add(RotationKey, 90);

            await _mediaCapture.SetEncodingPropertiesAsync(MediaStreamType.VideoPreview, props, null);

            var focusControl = _mediaCapture.VideoDeviceController.FocusControl;

            if (focusControl.Supported)
            {
                await focusControl.UnlockAsync();
                var setting = new FocusSettings { Mode = FocusMode.Continuous, AutoFocusRange = AutoFocusRange.FullRange };
                focusControl.Configure(setting);
                await focusControl.FocusAsync();
            }

            _isPreviewing = true;
            _isInitVideo = true;
            InitVideoTimer();
        }
        private static async Task<DeviceInformation> FindCameraDeviceByPanelAsync(Windows.Devices.Enumeration.Panel desiredPanel)
        {
            var allVideoDevices = await DeviceInformation.FindAllAsync(DeviceClass.VideoCapture);

            DeviceInformation desiredDevice = allVideoDevices.FirstOrDefault(x => x.EnclosureLocation != null && x.EnclosureLocation.Panel == desiredPanel);

            return desiredDevice ?? allVideoDevices.FirstOrDefault();
        }

        /// <summary>
        /// 解析二维码图片
        /// </summary>
        /// <param name="writeableBmp">图片</param>
        /// <returns></returns>
        private async Task ScanBitmap(WriteableBitmap writeableBmp)
        {
            try
            {
                await Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
                {
                    _result = barcodeReader.Decode(writeableBmp.PixelBuffer.ToArray(), writeableBmp.PixelWidth, writeableBmp.PixelHeight, RGBLuminanceSource.BitmapFormat.Unknown);
                    if (_result != null)
                    {
                        //TODO: 扫描结果：_result.Text
                        res.Text = _result.Text;
                    }
                });

            }
            catch (Exception)
            {
            }
        }

        private async void GetPrinter_Click(object sender, RoutedEventArgs e)
        {
            ValueSet valueSet = new ValueSet();
            valueSet.Add("request","");

            if (App.Connection != null)
            {
                AppServiceResponse response = await App.Connection.SendMessageAsync(valueSet);
                Noti.Text = "Received response: " + response.Message["response"] as string;
                if (!string.IsNullOrEmpty((string)response.Message["response"]) && ((string)response.Message["response"]).Contains("Print"))
                {
                    Noti.Text = "获取打印机列表成功";
                    string res = response.Message["response"] as string;
                    List<string> pr = Newtonsoft.Json.JsonConvert.DeserializeObject<List<string>>(res);
                    PrintList.ItemsSource = pr;
                }

            }
        }

        private async void PrintDoc_Click(object sender, RoutedEventArgs e)
        {
            ValueSet valueSet = new ValueSet();
            valueSet.Add("print", (string)PrintList.SelectedItem);

            if (App.Connection != null)
            {
                AppServiceResponse response = await App.Connection.SendMessageAsync(valueSet);
                //Noti.Text = "Received response: " + response.Message["response"] as string;
                //if (!string.IsNullOrEmpty((string)response.Message["response"]) && ((string)response.Message["response"]).Contains("ok"))
                //{
                //    Noti.Text = "打印成功";
                //}

            }
        }
    }
}
