using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace BalloonGame {
    /// <summary>
    /// 描画ユーティリティクラス
    /// </summary>
    internal abstract class DrawingUtil {


        public static void DrawImage(DrawingContext drawingContext, double x, double y, BitmapImage target) {
            Point halved = new Point(target.PixelWidth / 2, target.PixelHeight / 2);
            drawingContext.DrawImage(target, new Rect(x - halved.X, y - halved.Y, target.PixelWidth, target.PixelHeight));
        }

        /// <summary>
        /// 渡された DrawingContext に Brush で円を描画
        /// </summary>
        /// <param name="context">描画コンテキスト</param>
        /// <param name="fillColor">描画色</param>
        public static void Ellipse(DrawingContext context, double x, double y, double size, Brush fillColor) {
            context.DrawEllipse(fillColor, null, new Point(x, y), size, size);
        }

        public static void Arc(DrawingContext context, double x, double y, Pen pen, double r, double startDegrees, double endDegrees, SweepDirection direction = SweepDirection.Clockwise) {
            var arcPathGeo = ArcGeometry(new Point(x, y), r, startDegrees, endDegrees, direction);
            context.DrawGeometry(null, pen, arcPathGeo);
        }

        protected static PathGeometry ArcGeometry(Point center, double distance, double startDegrees, double stopDegrees, SweepDirection direction) {
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
    }
}
