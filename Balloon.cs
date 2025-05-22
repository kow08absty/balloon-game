using System.Net.Http.Headers;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace BalloonGame {
    /// <summary>
    /// 風船を表現するクラス
    /// </summary>
    internal class Balloon {
        private static readonly BitmapImage image = new BitmapImage(new Uri(@"assets/balloon.png", UriKind.Relative));

        private const double VELOCITY_CLIP_VALUE = 0.15;

        private double x;
        private double y;
        private readonly Size size;
        private bool dead = false;
        private double velocity = 0;

        public double X {
            get { return x; }
            set { x = value; }
        }

        public double Y {
            get { return y; }
            set { y = value; }
        }

        public Size Size { get { return size; } }

        public bool Dead {
            get { return dead; }
            set { dead = value; }
        }

        public Balloon(double x, double y) {
            this.x = x;
            this.y = y;
            this.size = new Size(image.PixelWidth, image.PixelHeight);
        }

        public void Draw(DrawingContext context) {
            DrawingUtil.DrawImage(context, x, y, image);
            //Ellipse(context, Brushes.Navy);
            //double r = Size * 0.85;
            //Arc(context, new Pen(Brushes.White, 2), r, 281, 329);
            //Arc(context, new Pen(Brushes.White, 2), r, 337, 346);
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
            this.velocity = -VELOCITY_CLIP_VALUE * 3.5;
        }

        /// <summary>
        /// 渡された Hand に対してユークリッド距離で衝突判定してた
        /// 
        /// >>> TODO: 衝突判定をちゃんと仕上げる
        /// 
        /// https://ftvoid.com/blog/post/300
        /// 
        /// </summary>
        /// <param name="hand">ターゲット</param>
        /// <returns>衝突したら true</returns>
        public bool IsCollide(Hand hand) {
            if (this.x > hand.X || this.x < hand.X + hand.Size.Width || this.y > hand.Y - this.Size.Width || this.y < hand.Y + this.Size.Height) {
                return true;
            }
            if (this.x > hand.X - this.Size.Width || this.x < hand.X + this.Size.Width || this.y > hand.Y || this.y < hand.Y + hand.Size.Height) {
                return true;
            }

            if (Math.Pow(hand.X - this.x, 2) + Math.Pow(hand.Y - this.y, 2) < Math.Pow(this.Size.Width, 2))
            {
                return true;
            }
            if (Math.Pow(hand.X + hand.Size.Width - this.x, 2) + Math.Pow(hand.Y - this.y, 2) < Math.Pow(this.Size.Width, 2))
            {
                return true;
            }
            if (Math.Pow(hand.X - this.x, 2) + Math.Pow(hand.Y + hand.Size.Height - this.y, 2) < Math.Pow(this.Size.Width, 2))
            {
                return true;
            }
            if (Math.Pow(hand.X + hand.Size.Width - this.x, 2) + Math.Pow(hand.Y + hand.Size.Height - this.y, 2) < Math.Pow(this.Size.Width, 2))
            {
                return true;
            }

            return false;
            //double x_distance = Math.Abs(this.x - hand.X);
            //double y_distance = Math.Abs(this.y - hand.Y);
            //double euclid_distance = Math.Sqrt(x_distance * x_distance + y_distance * y_distance);
            //return euclid_distance <= this.Size.X + hand.Size.X;
        }

    }
}
