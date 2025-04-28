using System.Net.Http.Headers;
using System.Windows;
using System.Windows.Media;

namespace BalloonGame {
    /// <summary>
    /// 風船を表現するクラス
    /// </summary>
    internal class Balloon : CircleBase {
        private const double VELOCITY_CLIP_VALUE = 0.15;

        private bool dead = false;
        private double velocity = 0;

        public bool Dead {
            get { return dead; }
            set { dead = value; }
        }

        public Balloon(double x, double y, int size) : base(x, y, size) {
        }

        public void Draw(DrawingContext context) {
            Ellipse(context, Brushes.Navy);
            double r = Size * 0.85;
            Arc(context, new Pen(Brushes.White, 2), r, 281, 329);
            Arc(context, new Pen(Brushes.White, 2), r, 337, 346);
        }

        /// <summary>
        /// 自分自身の座標と速度を更新
        /// </summary>
        /// <param name="tickDelta">前回アニメーション実行時間からの経過時間 [ミリ秒]</param>
        public void Update(long tickDelta) {
            double distance = tickDelta * velocity;
            velocity = Math.Min(VELOCITY_CLIP_VALUE, velocity + tickDelta * 0.00025);
            this.Y += distance;
        }

        public void NotifyCollide() {
                this.velocity = -VELOCITY_CLIP_VALUE * 2;
        }
    }
}
