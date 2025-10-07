using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static BalloonGame.Settings;

namespace BalloonGame {
    /// <summary>
    /// 設定をここに集める
    /// </summary>
    public class Settings {
        /// <summary>
        /// 風船の大きさ
        /// </summary>
        [TypeConverter(typeof(EnumDescriptionTypeConverter))]  // TypeConverterでDescriptionを拾う
        [Description("風船の大きさ")]
        public enum BalloonSize {
            [Description("小さめ")]
            Small,

            [Description("通常")]
            Medium,

            [Description("大きめ")]
            Large,
        }

        /// <summary>
        /// 落ちるスピード
        /// </summary>
        [TypeConverter(typeof(EnumDescriptionTypeConverter))]
        [Description("落ちるスピード")]
        public enum Gravity {
            [Description("遅い")]
            Weak,

            [Description("通常")]
            Medium,

            [Description("早い")]
            Strong,
        }
    }

    /// <summary>
    /// <para>初期設定値を格納して、変更された値を保存しておくViewModel</para>
    /// <para>設定が増えたらこっちも増やそう</para>
    /// </summary>
    public class SettingsViewModel {
        public BalloonSize BalloonSize { get; set; } = BalloonSize.Medium;
        public Gravity Gravity { get; set; } = Gravity.Medium;
    }
}