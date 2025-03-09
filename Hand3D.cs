using HelixToolkit.Wpf;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media.Media3D;

namespace BalloonGame {
    class Hand3D : ModelVisual3D {
        public Hand3D(ModelImporter importer) {
            Content = importer.Load("assets\\hand.obj");
            Transform = new TranslateTransform3D(0, -6.5, 0);
        }
    }
}
