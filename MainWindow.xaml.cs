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
using System.Diagnostics;

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
        /// 風船が床に落ちた回数
        /// </summary>
        private long _deadCount = 0;
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
        const int HAND_SIZE = 80;
        /// <summary>
        /// 風船の大きさ
        /// </summary>
        const int BALLOON_SIZE = 80;
        /// <summary>
        /// リフレッシュレート [フレーム/秒]
        /// </summary>
        const int ANIMATIOM_REFRESH_RATE = 60;

        /// <summary>
        /// Body Tracking で検出可能なおおよその解像度
        /// </summary>
        private static readonly System.Windows.Rect trackingSize = new System.Windows.Rect(-1024, -300, 1024, 1366);
        private static readonly System.Windows.Size normalizedTrackingSize = new System.Windows.Size(Math.Abs(trackingSize.Width - trackingSize.X), Math.Abs(trackingSize.Height - trackingSize.Y));

        /// <summary>
        /// 描画ターゲットの大きさ
        /// </summary>
        private System.Windows.Size canvasSize = new System.Windows.Size(1920, 1080);

        private static readonly Hand mouse = new Hand(-HAND_SIZE, -HAND_SIZE);
        private static readonly List<Hand> hands = new List<Hand>();
        private static readonly List<Balloon> balloons = new List<Balloon>();

        public MainWindow() {
            InitializeComponent();

            Closing += Window_Closing;
            SizeChanged += Window_SizeChanged;
            StateChanged += MainWindow_StateChanged;
            Loaded += Window_Loaded;
            MouseMove += Window_MouseMove;

            StartAnimation();

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
                                    hands.Clear();
                                    Dispatcher.Invoke(() => {
                                        for (int i = 0; i < _trackingCount << 1; ++i)
                                        {
                                            hands.Add(new Hand(0, 0));
                                        }
                                    });
                                }

                                for (int i = 0; i < _trackingCount; ++i) {
                                    var skeleton = frame.GetBodySkeleton((uint)i);
                                    int handIndex = i << 1;

                                    {
                                        var joint = skeleton.GetJoint(JointId.HandLeft);
                                        hands[handIndex].X = MathHelper.Lerp((trackingSize.Width + joint.Position.X) / normalizedTrackingSize.Width, canvasSize.Width, 0);
                                        hands[handIndex].Y = MathHelper.Lerp((trackingSize.Height + joint.Position.Y) / normalizedTrackingSize.Height, 0, canvasSize.Height);
                                    }

                                    {
                                        var joint = skeleton.GetJoint(JointId.HandRight);
                                        hands[handIndex + 1].X = MathHelper.Lerp((trackingSize.Width + joint.Position.X) / normalizedTrackingSize.Width, canvasSize.Width, 0);
                                        hands[handIndex + 1].Y = MathHelper.Lerp((trackingSize.Height + joint.Position.Y) / normalizedTrackingSize.Height, 0, canvasSize.Height);
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
            int animationTickMillis = (int)(1000f / ANIMATIOM_REFRESH_RATE);

            return Task.Run(() => {
                while (_loop) {
                    long now = TimeUtils.CurrentTimeMillis();

                    foreach (var balloon in balloons) {
                        hands.ForEach(hand => {
                            if (balloon.IsCollide(hand)) {
                                balloon.NotifyCollide();
                            }
                        });
                        if (balloon.IsCollide(mouse)) {
                            balloon.NotifyCollide();
                        }

                        balloon.Update(now - _lastAnimationTime);
                        if (balloon.Y > canvasSize.Height + balloon.Size.Height) {
                            balloon.Dead = true;
                        }
                    }
                    _deadCount += balloons.RemoveAll(b => b.Dead);
                    if (balloons.Count == 0) {
                        SpawnNewBalloon();
                    }

                    Dispatcher.Invoke(() => {
                        if (canvasSize.Height == 0 || canvasSize.Width == 0) {
                            UpdateCanvasSize();
                            return;
                        }

                        this.DeadCount.Text = String.Format("ボールが落ちた回数: {0}", _deadCount);

                        //this.Coordinate.Text = String.Join(", ", hands.Select(hand => String.Format("X: {0:#.###}, Y: {1:#.###}", hand.X, hand.Y)));

                        var renderTarget = new RenderTargetBitmap((int)canvasSize.Width, (int)canvasSize.Height, 96, 96, PixelFormats.Pbgra32);
                        var visual = new DrawingVisual();

                        lock (this) {
                            using (var context = visual.RenderOpen()) {
                                foreach (var ball in balloons) {
                                    ball.Draw(context);
                                }

                                mouse.Draw(context);

                                foreach (var hand in hands) {
                                    hand.Draw(context);
                                }
                            }
                        }

                        renderTarget.Render(visual);
                        MainCanvas.Source = renderTarget;
                    });
                    _lastAnimationTime = now;
                    Thread.Sleep(animationTickMillis);
                }
            });
        }

        private void SpawnNewBalloon(double x = 400, double y = 0) {
            Dispatcher.Invoke(() => {
                balloons.Add(new Balloon(x, y));
            });
        }

        private void UpdateCanvasSize() {
            canvasSize.Width = DesiredSize.Width;
            canvasSize.Height = DesiredSize.Height;
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

        private void Window_SizeChanged(object sender, SizeChangedEventArgs e) {
            UpdateCanvasSize();
        }

        private void Window_Loaded(object? sender, EventArgs e) {
            UpdateCanvasSize();
        }

        private void MainWindow_StateChanged(object? sender, EventArgs e)
        {
            Task.Run(() => {
                Thread.Sleep(200);
                Dispatcher.Invoke(() =>
                {
                    UpdateCanvasSize();
                });
            });
        }
    }
}