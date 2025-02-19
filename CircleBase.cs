using System.Windows;
using System.Windows.Media;

namespace BalloonGame {
    /// <summary>
    /// 円を表現する抽象クラス
    /// </summary>
    internal abstract class CircleBase {
        private double x;
        private double y;
        private readonly int size;

        protected CircleBase(double x, double y, int size) {
            this.x = x;
            this.y = y;
            this.size = size;
        }

        public double X {
            get { return x; }
            set { x = value; }
        }

        public double Y {
            get { return y; }
            set { y = value; }
        }

        public int Size { get { return size; } }

        /// <summary>
        /// 渡された DrawingContext に Brush で円を描画
        /// </summary>
        /// <param name="context">描画コンテキスト</param>
        /// <param name="fillColor">描画色</param>
        public void Draw(DrawingContext context, Brush fillColor) {
            context.DrawEllipse(fillColor, null, new Point(x, y), size, size);
        }

        /// <summary>
        /// 渡された CircleBase に対してユークリッド距離で衝突判定
        /// </summary>
        /// <param name="other">ターゲット</param>
        /// <returns>衝突したら true</returns>
        public bool IsCollide(CircleBase other) {
            if (other == this) {
                return false;
            }

            double x_distance = Math.Abs(this.x - other.x);
            double y_distance = Math.Abs(this.y - other.y);
            double euclid_distance = Math.Sqrt(x_distance * x_distance + y_distance * y_distance);
            return euclid_distance <= this.size + other.size;
        }
    }
}
