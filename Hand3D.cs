using HelixToolkit.Wpf;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media.Media3D;

namespace BalloonGame {
    class Hand3D : Model3DBase {
        public Hand3D(ModelImporter importer) : base(importer, "assets\\hand.obj") {
        }
    }
}
