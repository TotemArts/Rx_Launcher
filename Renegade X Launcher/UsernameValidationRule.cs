using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;

namespace LauncherTwo
{
    class UsernameValidationRule : ValidationRule
    {
        public override ValidationResult Validate(object value, CultureInfo cultureInfo)
        {
            if (((string)value).Contains('\"') ||
                ((string)value).Contains('#'))
                return new ValidationResult(false, "Your name may not contain any of the following characters: \" #");

            return new ValidationResult(true, null);
        }
    }
}
