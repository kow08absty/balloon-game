using System.Windows.Media;

namespace BalloonGame {
    internal class Hand : CircleBase {
        public Hand(double x, double y, int size) : base(x, y, size) {
        }

        public void Draw(DrawingContext context) {
            Ellipse(context, Brushes.Orange);
        }
    }
}
