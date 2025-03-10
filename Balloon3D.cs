using HelixToolkit.Wpf;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media.Media3D;

namespace BalloonGame {
    class Balloon3D : Model3DBase {
        private double rotationAngleRate = 60;
        private Vector3D rotationAxis = Axis.Z;
        private double velocity = -0.01;
        private double gravity = -0.0098;
        private bool dead = false;
        private double clipVelocity = -0.3;

        /// <summary>
        /// 回転速度
        /// </summary>
        public double RotationAngleRate {
            get => rotationAngleRate;
            set => rotationAngleRate = value;
        }

        /// <summary>
        /// 回転軸
        /// </summary>
        public Vector3D RotationAxis {
            get => rotationAxis;
            set => rotationAxis = value;
        }

        /// <summary>
        /// 移動速度
        /// </summary>
        public double Velocity {
            get => velocity;
            set => velocity = value;
        }

        /// <summary>
        /// 破裂判定
        /// </summary>
        public bool Dead {
            get => dead;
            set => dead = value;
        }

        public Balloon3D(ModelImporter importer) : base(importer, "assets\\balloon.obj") {
        }

        /// <summary>
        /// 風船の位置と回転を更新
        /// </summary>
        /// <param name="tickDelta">前回 tick からの差分[ms]</param>
        public void Tick(long tickDelta) {
            double delta = tickDelta * 0.01;

            GetTransform(out TranslateTransform3D translate, out AxisAngleRotation3D rotation);
            translate.OffsetY += delta * Velocity;
            rotation.Axis = RotationAxis;
            rotation.Angle += delta * RotationAngleRate;
            rotation.Angle %= 360;
            SetTransform(translate, rotation);

            Velocity = Math.Max(Velocity + gravity, clipVelocity);
        }
    }
}
