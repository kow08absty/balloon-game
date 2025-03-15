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
        public void Ellipse(DrawingContext context, Brush fillColor) {
            context.DrawEllipse(fillColor, null, new Point(x, y), size, size);
        }

        public void Arc(DrawingContext context, Pen pen, double r, double startDegrees, double endDegrees, SweepDirection direction = SweepDirection.Clockwise) {
            var arcPathGeo = ArcGeometry(new Point(x, y), r, startDegrees, endDegrees, direction);
            context.DrawGeometry(null, pen, arcPathGeo);
        }

        protected PathGeometry ArcGeometry(Point center, double distance, double startDegrees, double stopDegrees, SweepDirection direction) {
            Point stop = MakePoint(stopDegrees, center, distance);//終点座標

            //IsLargeの判定、
            //開始角度から終了角度までが180度を超えていたらtrue、なければfalse
            double diffDegrees = (direction == SweepDirection.Clockwise) ? stopDegrees - startDegrees : startDegrees - stopDegrees;
            if (diffDegrees < 0) { diffDegrees += 360.0; }
            bool isLarge = (diffDegrees > 180) ? true : false;

            //ArcSegment作成
            var arc = new ArcSegment(stop, new Size(distance, distance), 0, isLarge, direction, true);

            //PathFigure作成
            var fig = new PathFigure();
            Point start = MakePoint(startDegrees, center, distance);//始点座標
            fig.StartPoint = start;//始点座標をスタート地点に
            fig.Segments.Add(arc);//ArcSegment追加

            //PathGeometry作成、PathFigure追加
            var pg = new PathGeometry();
            pg.Figures.Add(fig);
            return pg;
        }

        /// <summary>
        /// 距離と角度からその座標を返す
        /// </summary>
        /// <param name="degrees">360以上は359.99になる</param>
        /// <param name="center">中心点</param>
        /// <param name="distance">中心点からの距離</param>
        /// <returns></returns>
        private static Point MakePoint(double degrees, Point center, double distance) {
            if (degrees >= 360) { degrees = 359.99; }
            var rad = MathHelper.Radian(degrees);
            var cos = Math.Cos(rad);
            var sin = Math.Sin(rad);
            var x = center.X + cos * distance;
            var y = center.Y + sin * distance;
            return new Point(x, y);
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
