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
        const int ANIMATIOM_REFRESH_RATE = 60;

        /// <summary>
        /// 描画ターゲットの大きさ
        /// </summary>
        private System.Windows.Size canvasSize = new System.Windows.Size(1920, 1080);

        private static readonly Hand mouse = new Hand(-HAND_SIZE, -HAND_SIZE, HAND_SIZE);
        private static readonly List<Hand> hands = new List<Hand>();
        private static readonly List<Balloon> balloons = new List<Balloon>();

        public MainWindow() {
            InitializeComponent();

            Closing += Window_Closing;
            SizeChanged += Window_SizeChanged;
            Loaded += Window_Loaded;
            MouseMove += Window_MouseMove;

            StartAnimation();

            // Kinect 接続用タスク
            Task.Run(() => {
                InitKinect();
                _kinectCaptureTask = StartKinectCapture();
                Dispatcher.Invoke(new Action(() => {
                    MessageText.Visibility = Visibility.Hidden;
                }));
                MouseMove -= Window_MouseMove;
                mouse.X = -100;
                mouse.Y = -100;
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
                                    for (int i = 0; i < _trackingCount << 1; ++i) {
                                        hands.Add(new Hand(0, 0, HAND_SIZE));
                                    }
                                }

                                for (int i = 0; i < _trackingCount; ++i) {
                                    var skeleton = frame.GetBodySkeleton((uint)i);
                                    int handIndex = i << 1;

                                    {
                                        var joint = skeleton.GetJoint(JointId.HandLeft);
                                        hands[handIndex].X = MathHelper.Lerp(joint.Position.X / canvasSize.Width, canvasSize.Width * 0.5f, canvasSize.Width);
                                        hands[handIndex].Y = MathHelper.Lerp(joint.Position.Y / canvasSize.Height, canvasSize.Height * 0.5f, canvasSize.Height);
                                    }

                                    {
                                        var joint = skeleton.GetJoint(JointId.HandRight);
                                        hands[handIndex + 1].X = MathHelper.Lerp(joint.Position.X / canvasSize.Width, canvasSize.Width * 0.5f, canvasSize.Width);
                                        hands[handIndex + 1].Y = MathHelper.Lerp(joint.Position.Y / canvasSize.Height, canvasSize.Height * 0.5f, canvasSize.Height);
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
                    if (now - _lastBalloonSpawnTime > SPAWN_TIME_INTERVAL && Random.Shared.NextDouble() > 0.85) {
                        _lastBalloonSpawnTime = now;
                        double randomX = Random.Shared.NextDouble() * canvasSize.Width;
                        double maxX = canvasSize.Width - BALLOON_SIZE;
                        double spawnX = Math.Min(maxX, Math.Max(BALLOON_SIZE, randomX));
                        balloons.Add(new Balloon(spawnX, 0, BALLOON_SIZE));
                    }

                    balloons.FindAll(balloon => !balloon.Collided).ForEach(balloon => {
                        hands.ForEach(hand => {
                            balloon.Collided |= balloon.IsCollide(hand);
                        });
                        balloon.Collided |= balloon.IsCollide(mouse);
                    });

                    foreach (var balloon in balloons) {
                        balloon.Update(now - _lastAnimationTime);
                        if (balloon.Y > canvasSize.Height + balloon.Size || balloon.Y < -balloon.Size) {
                            balloon.Dead = true;
                        }
                    }
                    balloons.RemoveAll(b => b.Dead);

                    Dispatcher.Invoke(() => {
                        if (canvasSize.Height == 0 || canvasSize.Width == 0) {
                            UpdateCanvasSize();
                            return;
                        }

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
    }
}