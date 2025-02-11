using System.Buffers;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Media.Media3D;
using System.Windows.Navigation;
using System.Windows.Shapes;
using Microsoft.Azure.Kinect.Sensor;
using Microsoft.Azure.Kinect.BodyTracking;

using TrackerFrame = Microsoft.Azure.Kinect.BodyTracking.Frame;
using System;
using System.Runtime.InteropServices;

namespace BalloonGame {
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window {
        private Device? _kinect;
        private bool _loop = true;
        private WriteableBitmap _writeableBitmap;
        private Tracker? _bodyTracker;
        private Calibration _calibration;

        public MainWindow() {
            InitializeComponent();

            Closing += Window_Closing;

            _writeableBitmap = new WriteableBitmap(1920, 1080, 96, 96, PixelFormats.Bgra32, null);
            MainCanvas.Source = _writeableBitmap;

            Task.Run(() => {
                InitKinect();
                StartKinectCapture();
                Dispatcher.Invoke(new Action(() => {
                    MessageText.Visibility = Visibility.Hidden;
                }));
            });
        }

        private void InitKinect() {
            while (_kinect == null) {
                try {
                    _kinect = Device.Open();
                } catch (Exception) {
                    Thread.Sleep(3000);
                }
            }

            var deviceConfig = new DeviceConfiguration {
                ColorResolution = ColorResolution.Off,
                DepthMode = DepthMode.NFOV_2x2Binned,
            };
            _kinect.StartCameras(deviceConfig);

            _calibration = _kinect.GetCalibration(deviceConfig.DepthMode, deviceConfig.ColorResolution);
            var trackerConfig = new TrackerConfiguration {
                ProcessingMode = TrackerProcessingMode.Gpu,       //GPUがない場合はCpuを指定
                SensorOrientation = SensorOrientation.Default
            };
            _bodyTracker = Tracker.Create(_calibration, trackerConfig);
        }

        private async void StartKinectCapture() {
            while (_loop) {
                if (_kinect == null || _bodyTracker == null) {
                    continue;
                }

                using (Capture capture = await Task.Run(() => _kinect.GetCapture()).ConfigureAwait(true)) {
                    _bodyTracker.EnqueueCapture(capture);

                    //unsafe {
                    //    using (MemoryHandle pin = capture.Depth.Memory.Pin()) {
                    //        var buffer = (ushort*)pin.Pointer;
                    //        var width = capture.Depth.WidthPixels;
                    //        var height = capture.Depth.HeightPixels;
                    //        var stride = capture.Depth.StrideBytes;

                    //        Dispatcher.Invoke(new Action(() => {
                    //            _writeableBitmap.Lock();
                    //            _writeableBitmap.WritePixels(new Int32Rect(0, 0, width, height), buffer, stride, 0);
                    //            _writeableBitmap.Unlock();
                    //        }));
                    //    }
                    //}
                }

                using (TrackerFrame frame = _bodyTracker.PopResult(TimeSpan.Zero, false)) {
                    if (frame != null && frame.NumberOfBodies > 0) {
                        var skeleton = frame.GetBodySkeleton(0);
                        Dispatcher.Invoke(new Action(() => {
                            try {
                                _writeableBitmap.Lock();
                                var width = 8;
                                var height = 8;
                                var stride = (width * _writeableBitmap.Format.BitsPerPixel + 7) / 8;
                                var buffer = Enumerable.Range(0, stride * height).Select(i => (byte)0).ToArray();
                                Transformation transformation = _calibration.CreateTransformation();
                                foreach (JointId id in Enum.GetValues(typeof(JointId))) {
                                    var joint = skeleton.GetJoint(id);
                                    // Position.X / Y がマイナスになるのでなんとかしてカラー座標に変換
                                    var rect = new Int32Rect((int)joint.Position.X, (int)joint.Position.Y, width, height);
                                    _writeableBitmap.WritePixels(rect, buffer, stride, 0);
                                }
                            } catch (Exception) {
                            } finally {
                                _writeableBitmap.Unlock();
                            }
                        }));
                    }
                }
            }
            _kinect?.StopCameras();
            _kinect?.Dispose();
            _bodyTracker?.Shutdown();
            _bodyTracker?.Dispose();
        }

        private void Window_Closing(object? sender, System.ComponentModel.CancelEventArgs e) {
            _loop = false;
        }
    }
}