using System.Windows.Media;

namespace BalloonGame {
    /// <summary>
    /// 風船を表現するクラス
    /// </summary>
    internal class Balloon : CircleBase {
        private bool dead = false;
        private bool collided = false;

        public bool Dead {
            get { return dead; }
            set { dead = value; }
        }

        public bool Collided {
            get { return collided; }
            set { collided = value; }
        }

        public Balloon(double x, double y, int size) : base(x, y, size) {
        }

        public void Draw(DrawingContext context) {
            Draw(context, Brushes.Navy);
        }

        /// <summary>
        /// 自分自身の座標を更新
        /// </summary>
        /// <param name="tickDelta">前回アニメーション実行時間からの経過時間 [ミリ秒]</param>
        public void Update(long tickDelta) {
            double distance = tickDelta * 0.15;
            this.Y += (!this.Collided) ? distance : -distance;
        }
    }
}
