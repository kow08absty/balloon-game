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
using System;
using System.Runtime.InteropServices;
using System.Drawing;

using TrackerFrame = Microsoft.Azure.Kinect.BodyTracking.Frame;
using System.Reflection.Metadata;

namespace BalloonGame {
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window {
        /// <summary>
        /// Kinect 接続用インスタンス
        /// </summary>
        private Device? _kinect;
        /// <summary>
        /// ループ中なら true
        /// </summary>
        private bool _loop = true;
        /// <summary>
        /// 体のトラッキング用インスタンス
        /// </summary>
        private Tracker? _bodyTracker;
        /// <summary>
        /// 現在何人参加しているか覚えておく
        /// </summary>
        private uint _trackingCount = 0;
        /// <summary>
        /// 最後に風船が出現した時間 [ミリ秒]
        /// </summary>
        private long _lastBalloonSpawnTime = 0;
        /// <summary>
        /// 最後にアニメーションを実行した時間 [ミリ秒]
        /// </summary>
        private long _lastAnimationTime = TimeUtils.CurrentTimeMillis();
        /// <summary>
        /// Kinect トラッキングを実際に実行しているタスクを覚えておく
        /// </summary>
        private Task? _kinectCaptureTask;

        /// <summary>
        /// 手の大きさ
        /// </summary>
        const int HAND_SIZE = 40;
        /// <summary>
        /// 風船の大きさ
        /// </summary>
        const int BALLOON_SIZE = 40;
        /// <summary>
        /// 風船が出現する間隔 [ミリ秒]
        /// </summary>
        const int SPAWN_TIME_INTERVAL = 2000;
        /// <summary>
        /// リフレッシュレート [フレーム/秒]
        /// </summary>
        const int ANIMATION_REFRESH_RATE = 60;

        private HelixToolkit.Wpf.ModelImporter _modelImporter = new HelixToolkit.Wpf.ModelImporter();

        /// <summary>
        /// 描画ターゲットの大きさ
        /// </summary>
        private System.Windows.Size orthoSize = new System.Windows.Size(30, 30);

        private static readonly Hand mouse = new Hand(-HAND_SIZE, -HAND_SIZE, HAND_SIZE);
        //private static readonly List<Hand> hands = new List<Hand>();
        //private static readonly List<Balloon> balloons = new List<Balloon>();
        private static readonly List<Balloon3D> paperBalloons = new List<Balloon3D>();
        private static readonly List<Hand3D> hand3D = new List<Hand3D>();

        public MainWindow() {
            InitializeComponent();

            Closing += Window_Closing;
            MouseMove += Window_MouseMove;

            ModelViewport.IsMoveEnabled = false;
            ModelViewport.IsPanEnabled = false;
            ModelViewport.IsRotationEnabled = false;
            ModelViewport.IsZoomEnabled = false;
            ModelViewport.ShowViewCube = false;
            ModelViewport.Camera = new OrthographicCamera(new Point3D(0, 0, 0), Axis.Z, Axis.Y, orthoSize.Width);

            DirectionalLight.Transform = new RotateTransform3D(new AxisAngleRotation3D(Axis.Y, 160));

            var paperBalloon = new Balloon3D(_modelImporter) {
                Y = 9,
            };
            ModelViewport.Children.Add(paperBalloon);
            paperBalloons.Add(paperBalloon);

            var hand = new Hand3D(_modelImporter) {
                Y = -7.5,
            };
            ModelViewport.Children.Add(hand);
            hand3D.Add(hand);

            // Kinect 接続用タスク
            Task.Run(() => {
                try {
                    InitKinect();
                    _kinectCaptureTask = StartKinectCapture();
                    Dispatcher.Invoke(new Action(() => {
                        MessageText.Visibility = Visibility.Hidden;
                    }));
                    MouseMove -= Window_MouseMove;
                    mouse.X = -100;
                    mouse.Y = -100;
                } catch (Exception) {
                }
            });
            
            StartAnimation();
        }

        /// <summary>
        /// Kinect が接続されてトラッキング可能になるまで永遠に待つ
        /// </summary>
        private void InitKinect() {
            while (_kinect == null && _loop) {
                try {
                    _kinect = Device.Open();
                } catch (Exception) {
                    Thread.Sleep(3000);
                }
            }

            if (_kinect == null) {
                return;
            }

            var deviceConfig = new DeviceConfiguration {
                ColorResolution = ColorResolution.Off,
                DepthMode = DepthMode.NFOV_2x2Binned,
            };
            _kinect.StartCameras(deviceConfig);

            var calibration = _kinect.GetCalibration(deviceConfig.DepthMode, deviceConfig.ColorResolution);
            var trackerConfig = new TrackerConfiguration {
                ProcessingMode = TrackerProcessingMode.Gpu,       //GPUがない場合はCpuを指定
                SensorOrientation = SensorOrientation.Default
            };
            _bodyTracker = Tracker.Create(calibration, trackerConfig);
        }

        /// <summary>
        /// 本格的に Kinect トラッキングをスタート
        /// </summary>
        /// <returns>トラッキングタスク</returns>
        private Task StartKinectCapture() {
            return Task.Run(async () => {
                while (_loop) {
                    if (_kinect == null || _bodyTracker == null) {
                        continue;
                    }

                    using (Capture capture = await Task.Run(() => _kinect.GetCapture()).ConfigureAwait(true)) {
                        _bodyTracker.EnqueueCapture(capture);
                    }

                    using (TrackerFrame frame = _bodyTracker.PopResult(TimeSpan.Zero, false)) {
                        if (frame != null) {
                            lock (this) {
                                if (_trackingCount != frame.NumberOfBodies) {
                                    _trackingCount = frame.NumberOfBodies;
                                    hand3D.Clear();
                                    for (int i = 0; i < _trackingCount << 1; ++i) {
                                        hand3D.Add(new Hand3D(_modelImporter));
                                    }
                                }

                                for (int i = 0; i < _trackingCount; ++i) {
                                    var skeleton = frame.GetBodySkeleton((uint)i);
                                    int handIndex = i << 1;

                                    {
                                        var joint = skeleton.GetJoint(JointId.HandLeft);
                                        hand3D[handIndex].X = MathHelper.Lerp(joint.Position.X / orthoSize.Width, orthoSize.Width * 0.5f, orthoSize.Width);
                                        hand3D[handIndex].Y = MathHelper.Lerp(joint.Position.Y / orthoSize.Height, orthoSize.Height * 0.5f, orthoSize.Height);
                                    }

                                    {
                                        var joint = skeleton.GetJoint(JointId.HandRight);
                                        hand3D[handIndex + 1].X = MathHelper.Lerp(joint.Position.X / orthoSize.Width, orthoSize.Width * 0.5f, orthoSize.Width);
                                        hand3D[handIndex + 1].Y = MathHelper.Lerp(joint.Position.Y / orthoSize.Height, orthoSize.Height * 0.5f, orthoSize.Height);
                                    }
                                }
                            }
                        }
                    }
                }
                _kinect?.StopCameras();
                _kinect?.Dispose();
                _bodyTracker?.Shutdown();
                _bodyTracker?.Dispose();
            });
        }

        /// <summary>
        /// 本格的にアニメーションをスタート
        /// </summary>
        /// <returns>アニメーションタスク</returns>
        private Task StartAnimation() {
            int animationTickMillis = (int)(1000f / ANIMATION_REFRESH_RATE);

            return Task.Run(() => {
                while (_loop) {
                    long now = TimeUtils.CurrentTimeMillis();
                    //if (now - _lastBalloonSpawnTime > SPAWN_TIME_INTERVAL && Random.Shared.NextDouble() > 0.85) {
                    //    _lastBalloonSpawnTime = now;
                    //    double randomX = Random.Shared.NextDouble() * canvasSize.Width;
                    //    double maxX = canvasSize.Width - BALLOON_SIZE;
                    //    double spawnX = Math.Min(maxX, Math.Max(BALLOON_SIZE, randomX));
                    //    balloons.Add(new Balloon(spawnX, 0, BALLOON_SIZE));
                    //}

                    //foreach (var balloon in balloons) {
                    //    balloon.Update(now - _lastAnimationTime);
                    //    if (balloon.Y > canvasSize.Height + balloon.Size || balloon.Y < -balloon.Size) {
                    //        balloon.Dead = true;
                    //    }
                    //}
                    //balloons.RemoveAll(b => b.Dead);

                    Dispatcher.Invoke(() => {
                        foreach (var balloon in paperBalloons) {
                            foreach (var hand in hand3D) {
                                if (balloon.IsColide(hand)) {
                                    balloon.Velocity = 1.2;
                                    balloon.RotationAngleRate = Random.Shared.NextDouble() * 20;
                                    balloon.RotationAxis = new Vector3D(
                                        Random.Shared.NextDouble(),
                                        Random.Shared.NextDouble(),
                                        Random.Shared.NextDouble()
                                    );
                                    balloon.RotationAxis.Normalize();
                                }
                            }
                        }

                        foreach (var balloon in paperBalloons) {
                            balloon.Tick(now - _lastAnimationTime);
                        }

                        paperBalloons.RemoveAll(b => b.Dead);
                    });
                    _lastAnimationTime = now;
                    Thread.Sleep(animationTickMillis);
                }
            });
        }

        private void Window_Closing(object? sender, System.ComponentModel.CancelEventArgs e) {
            _loop = false;
            // Kinect トラッキングが終わって Dispose されるまで待つ
            _kinectCaptureTask?.Wait();
        }

        private void Window_MouseMove(object sender, MouseEventArgs e) {
            var point = e.GetPosition(this);
            mouse.X = point.X;
            mouse.Y = point.Y;
        }
    }
}