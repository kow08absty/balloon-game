using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace BalloonGame {
    internal class Hand /*: CircleBase*/ {
        private static readonly BitmapImage image = new BitmapImage(new Uri(@"assets/hand.png", UriKind.Relative));

        private double x;
        private double y;
        private readonly Size size;

        public double X {
            get { return x; }
            set { x = value; }
        }

        public double Y {
            get { return y; }
            set { y = value; }
        }

        public Size Size { get { return size; } }

        public Hand(double x, double y) {
            this.x = x;
            this.y = y;
            this.size = new Size(image.PixelWidth, image.PixelHeight);
        }

        public void Draw(DrawingContext context) {
            DrawingUtil.DrawImage(context, x, y, image);
            //Ellipse(context, Brushes.Orange);
        }
    }
}
