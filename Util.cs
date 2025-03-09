using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media.Media3D;

namespace BalloonGame {
    public class MathHelper {
        public static float Lerp(float alpha, float start, float end) {
            return start + alpha * (end - start);
        }

        public static double Lerp(double alpha, double start, double end) {
            return start + alpha * (end - start);
        }
    }

    public class TimeUtils {
        private static readonly DateTime Jan1st1970 = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);

        public static long CurrentTimeMillis() {
            return (long)(DateTime.UtcNow - Jan1st1970).TotalMilliseconds;
        }
    }

    class Axis {
        public static Vector3D X = new Vector3D(1, 0, 0);
        public static Vector3D Y = new Vector3D(0, 1, 0);
        public static Vector3D Z = new Vector3D(0, 0, 1);
    }
}
