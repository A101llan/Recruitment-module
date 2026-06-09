using System;
using System.Globalization;
using System.Text.RegularExpressions;

namespace System.ComponentModel.DataAnnotations
{
    /// <summary>
    /// .NET 4.0 polyfill for EmailAddressAttribute (added in .NET 4.5).
    /// </summary>
    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Field | AttributeTargets.Parameter, AllowMultiple = false)]
    public sealed class EmailAddressAttribute : DataTypeAttribute
    {
        private static readonly Regex EmailPattern = new Regex(
            @"^[a-zA-Z0-9._%+\-]+@[a-zA-Z0-9.\-]+\.[a-zA-Z]{2,}$",
            RegexOptions.Compiled | RegexOptions.CultureInvariant);

        public EmailAddressAttribute()
            : base(DataType.EmailAddress)
        {
            ErrorMessage = "The {0} field is not a valid e-mail address.";
        }

        public override bool IsValid(object value)
        {
            if (value == null)
            {
                return true;
            }

            var text = Convert.ToString(value, CultureInfo.CurrentCulture);
            if (string.IsNullOrEmpty(text))
            {
                return true;
            }

            return EmailPattern.IsMatch(text);
        }
    }

    /// <summary>
    /// .NET 4.0 polyfill for CompareAttribute (added in .NET 4.5).
    /// </summary>
    [AttributeUsage(AttributeTargets.Property, AllowMultiple = false)]
    public sealed class CompareAttribute : ValidationAttribute
    {
        public CompareAttribute(string otherProperty)
        {
            if (string.IsNullOrEmpty(otherProperty))
            {
                throw new ArgumentNullException("otherProperty");
            }

            OtherProperty = otherProperty;
        }

        public string OtherProperty { get; private set; }

        protected override ValidationResult IsValid(object value, ValidationContext validationContext)
        {
            if (validationContext == null)
            {
                throw new ArgumentNullException("validationContext");
            }

            var otherPropertyInfo = validationContext.ObjectType.GetProperty(OtherProperty);
            if (otherPropertyInfo == null)
            {
                return new ValidationResult(string.Format(CultureInfo.CurrentCulture, "Could not find a property named {0}.", OtherProperty));
            }

            var otherValue = otherPropertyInfo.GetValue(validationContext.ObjectInstance, null);
            if (object.Equals(value, otherValue))
            {
                return ValidationResult.Success;
            }

            return new ValidationResult(FormatErrorMessage(validationContext.DisplayName));
        }
    }
}
