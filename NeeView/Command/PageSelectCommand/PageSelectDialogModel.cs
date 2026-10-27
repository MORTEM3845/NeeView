using CommunityToolkit.Mvvm.ComponentModel;
using NeeView.Properties;
using System;
using System.Globalization;
using System.Text;

namespace NeeView
{
    /// <summary>
    /// 「指定ページに移動」コマンド用
    /// </summary>
    public class PageSelectDialogModel : ObservableObject
    {
        private int _value;

        public PageSelectDialogModel(int value, int min, int max)
        {
            Value = value;
            Min = min;
            Max = max;
        }

        public int Value
        {
            get { return _value; }
            set { SetProperty(ref _value, value); }
        }

        public int Min { get; set; }
        public int Max { get; set; }

        public static bool TryParseValue(string? text, out int value)
        {
            value = 0;
            if (string.IsNullOrEmpty(text))
            {
                return false;
            }

            var normalized = new StringBuilder(text.Length);
            for (var index = 0; index < text.Length; index++)
            {
                var digit = CharUnicodeInfo.GetDecimalDigitValue(text, index);
                if (digit >= 0)
                {
                    normalized.Append((char)('0' + digit));
                }
                else
                {
                    normalized.Append(text[index]);
                }
            }
            return int.TryParse(normalized.ToString(), NumberStyles.Integer, CultureInfo.InvariantCulture, out value);
        }

        public string Caption => TextResources.GetString("JumpPageCommand");

        public string Label => string.Format(CultureInfo.InvariantCulture, TextResources.GetString("Notice.JumpPageLabel"), Min, Max);

        public void AddValue(int delta)
        {
            if (delta < 0 && Value > Min)
            {
                Value = Math.Max(Value + delta, Min);
            }
            else if (delta > 0 && Value < Max)
            {
                Value = Math.Min(Value + delta, Max);
            }
        }
    }
}
