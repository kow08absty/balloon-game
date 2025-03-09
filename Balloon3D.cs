using HelixToolkit.Wpf;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media.Media3D;

namespace BalloonGame {
    class Balloon3D : ModelVisual3D {
        private double rotationAngleRate = 60;
        private Vector3D rotationAxis = Axis.Z;
        private double velocity = -0.01;

        public double RotationAngleRate {
            get => rotationAngleRate;
            set => rotationAngleRate = value;
        }

        public Vector3D RotationAxis {
            get => rotationAxis;
            set => rotationAxis = value;
        }

        public double Velocity {
            get => velocity;
            set => velocity = value;
        }

        public Balloon3D(ModelImporter importer) {
            Content = importer.Load("assets\\balloon.obj");
            Transform = new TranslateTransform3D(0, 8.2, 0);
        }

        public void Tick(long tickDelta) {
            double delta = tickDelta * 0.01;
            TranslateTransform3D translate = new TranslateTransform3D();
            AxisAngleRotation3D rotation = new AxisAngleRotation3D(RotationAxis, 0);

            if (Content.Transform is MatrixTransform3D matrix) {
                translate = new TranslateTransform3D(matrix.Matrix.OffsetX, matrix.Matrix.OffsetY, matrix.Matrix.OffsetZ);
            } else if (Content.Transform is Transform3DGroup group) {
                translate = (TranslateTransform3D)group.Children.First(c => c is TranslateTransform3D);
                rotation = (AxisAngleRotation3D)((RotateTransform3D)group.Children.First(c => c is RotateTransform3D)).Rotation;
            }
            translate.OffsetY += delta * Velocity;
            rotation.Axis = RotationAxis;
            rotation.Angle += delta * RotationAngleRate;

            {
                var group = new Transform3DGroup();
                var rotate = new RotateTransform3D(rotation, translate.OffsetX, translate.OffsetY, translate.OffsetZ);
                group.Children.Add(translate);
                group.Children.Add(rotate);
                Content.Transform = group;
            }
        }

        // TODO: 衝突判定を実装する
    }
}
