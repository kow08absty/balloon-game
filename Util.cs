using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Markup;

namespace BalloonGame {
    public class MathHelper {
        public static float Lerp(float alpha, float start, float end) {
            return start + alpha * (end - start);
        }

        public static double Lerp(double alpha, double start, double end) {
            return start + alpha * (end - start);
        }

        public static double Radian(double degree) {
            return Math.PI / 180.0 * degree;
        }
    }

    public class TimeUtils {
        private static readonly DateTime Jan1st1970 = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);

        public static long CurrentTimeMillis() {
            return (long)(DateTime.UtcNow - Jan1st1970).TotalMilliseconds;
        }
    }

    /// <summary>
    /// Enum -> Description
    /// </summary>
    public class EnumDescriptionTypeConverter : EnumConverter {
        public EnumDescriptionTypeConverter(Type type) : base(type) {
        }

        public override object? ConvertTo(ITypeDescriptorContext? context, CultureInfo? culture, object? value, Type destinationType) {
            if (destinationType == typeof(string)) {
                try {
                    if (value == null) {
                        throw new Exception();
                    }

                    var field = value.GetType().GetField(value.ToString() ?? throw new Exception()) ?? throw new Exception();
                    var attribute = field.GetCustomAttribute<DescriptionAttribute>(false);
                    return attribute == null ? value.ToString() : attribute.Description;
                } catch (Exception) { }
            }
            return base.ConvertTo(context, culture, value, destinationType);
        }
    }

    /// <summary>
    /// XAMLからEnum全部を取得するためのマークアップ拡張
    /// </summary>
    public class EnumBindingSourceExtension : MarkupExtension {
        private readonly Type _enumType;

        public EnumBindingSourceExtension(Type enumType) {
            if (!enumType.IsEnum) {
                throw new ArgumentException($"{enumType} is not enum.");
            }

            _enumType = enumType ?? throw new ArgumentNullException(nameof(enumType));
        }

        public override object ProvideValue(IServiceProvider serviceProvider) =>
            Enum.GetValues(_enumType);
    }

    /// <summary>
    /// XAMLからtypeof EnumのDescriptionを取得するためのマークアップ拡張
    /// </summary>
    public class EnumDescSourceExtension : MarkupExtension {
        private readonly Type _enumType;

        public EnumDescSourceExtension(Type enumType) {
            if (!enumType.IsEnum) {
                throw new ArgumentException($"{enumType} is not enum.");
            }

            _enumType = enumType ?? throw new ArgumentNullException(nameof(enumType));
        }

        public override object ProvideValue(IServiceProvider serviceProvider) {
            var attribute = _enumType.GetCustomAttribute<DescriptionAttribute>(false);
            return attribute?.Description ?? _enumType.Name;
        }
    }

}
