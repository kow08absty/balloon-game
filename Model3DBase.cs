using HelixToolkit.Wpf;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media.Imaging;
using System.Windows.Media.Media3D;

namespace BalloonGame {
    class Model3DBase : ModelVisual3D {
        private double x;
        private double y;

        public double X {
            get => x;
            set {
                x = value;
                GetTransform(out TranslateTransform3D translate, out AxisAngleRotation3D rotation);
                translate.OffsetX = x;
                SetTransform(translate, rotation);
            }
        }

        public double Y {
            get => y;
            set {
                y = value;
                GetTransform(out TranslateTransform3D translate, out AxisAngleRotation3D rotation);
                translate.OffsetY = y;
                SetTransform(translate, rotation);
            }
        }

        public Model3DBase(ModelImporter importer, string path) {
            Content = importer.Load(path);
        }

        public void GetTransform(out TranslateTransform3D translate, out AxisAngleRotation3D rotation) {
            var currentTranslate = new TranslateTransform3D();
            var currentRotation = new AxisAngleRotation3D();

            if (Transform is MatrixTransform3D matrix) {
                currentTranslate = new TranslateTransform3D(matrix.Matrix.OffsetX, matrix.Matrix.OffsetY, matrix.Matrix.OffsetZ);
            } else if (Transform is Transform3DGroup group) {
                currentTranslate = (TranslateTransform3D)group.Children.First(c => c is TranslateTransform3D);
                currentRotation = (AxisAngleRotation3D)((RotateTransform3D)group.Children.First(c => c is RotateTransform3D)).Rotation;
            }

            translate = currentTranslate;
            rotation = currentRotation;
        }

        public void SetTransform(TranslateTransform3D translate, AxisAngleRotation3D rotation) {
            var group = new Transform3DGroup();
            var rotate = new RotateTransform3D(rotation, translate.OffsetX, translate.OffsetY, translate.OffsetZ);
            group.Children.Add(translate);
            group.Children.Add(rotate);
            Transform = group;
        }

        public bool IsColide(Model3DBase that) {
            var thisBounds = ToWorldBounds(this.Content.Bounds, this.Transform);
            var thatBounds = ToWorldBounds(that.Content.Bounds, that.Transform);
            return thisBounds.IntersectsWith(thatBounds);
        }

        private static Rect3D ToWorldBounds(Rect3D target, Transform3D transform) {
            var points = new Point3D[8];

            // 8つの頂点を取得
            points[0] = new Point3D(target.X, target.Y, target.Z);
            points[1] = new Point3D(target.X + target.SizeX, target.Y, target.Z);
            points[2] = new Point3D(target.X, target.Y + target.SizeY, target.Z);
            points[3] = new Point3D(target.X, target.Y, target.Z + target.SizeZ);
            points[4] = new Point3D(target.X + target.SizeX, target.Y + target.SizeY, target.Z);
            points[5] = new Point3D(target.X + target.SizeX, target.Y, target.Z + target.SizeZ);
            points[6] = new Point3D(target.X, target.Y + target.SizeY, target.Z + target.SizeZ);
            points[7] = new Point3D(target.X + target.SizeX, target.Y + target.SizeY, target.Z + target.SizeZ);

            // 変換を適用
            for (int i = 0; i < points.Length; ++i) {
                points[i] = transform.Transform(points[i]);
            }

            double minX = points.Min(p => p.X);
            double minY = points.Min(p => p.Y);
            double minZ = points.Min(p => p.Z);
            double maxX = points.Max(p => p.X);
            double maxY = points.Max(p => p.Y);
            double maxZ = points.Max(p => p.Z);

            return new Rect3D(minX, minY, minZ, maxX - minX, maxY - minY, maxZ - minZ);
        }
    }
}
