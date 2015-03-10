﻿namespace Our.Umbraco.Ditto
{
    using System;
    using System.Collections.Generic;
    using System.ComponentModel;
    using System.Globalization;
    using System.Linq;

    /// <summary>
    /// Provides a unified way of converting objects to an <see cref="Enum"/>.
    /// </summary>
    /// <typeparam name="TEnum">
    /// The <see cref="Type"/> to convert to.
    /// </typeparam>
    public class EnumConverter<TEnum> : TypeConverter where TEnum : struct, IConvertible
    {
        /// <summary>
        /// Returns whether this converter can convert an object of the given type to the type of this converter,
        /// using the specified context.
        /// </summary>
        /// <param name="context">
        /// An <see cref="T:System.ComponentModel.ITypeDescriptorContext" /> that provides a format context.
        /// </param>
        /// <param name="sourceType">
        /// A <see cref="T:System.Type" /> that represents the type you want to convert from.
        /// </param>
        /// <returns>
        /// true if this converter can perform the conversion; otherwise, false.
        /// </returns>
        public override bool CanConvertFrom(ITypeDescriptorContext context, Type sourceType)
        {
            if (typeof(TEnum).IsEnum &&
                (sourceType == typeof(string)
                || sourceType == typeof(int)
                || sourceType.IsEnum
                || (sourceType.IsEnumerableType() && sourceType.GenericTypeArguments[0] == typeof(string))))
            {
                return true;
            }

            return base.CanConvertFrom(context, sourceType);
        }

        /// <summary>
        /// Converts the given object to the type of this converter, using the specified context and culture information.
        /// </summary>
        /// <param name="context">An <see cref="T:System.ComponentModel.ITypeDescriptorContext" /> that provides a format context.</param>
        /// <param name="culture">The <see cref="T:System.Globalization.CultureInfo" /> to use as the current culture.</param>
        /// <param name="value">The <see cref="T:System.Object" /> to convert.</param>
        /// <returns>
        /// An <see cref="T:System.Object" /> that represents the converted value.
        /// </returns>
        public override object ConvertFrom(ITypeDescriptorContext context, CultureInfo culture, object value)
        {
            if (value == null)
            {
                return default(TEnum);
            }

            if (value is string)
            {
                TEnum fallback;
                Enum.TryParse(value.ToString(), true, out fallback);
                return fallback;
            }

            if (value is int)
            {
                // Should handle most cases.
                if (Enum.IsDefined(typeof(TEnum), value))
                {
                    return (TEnum)value;
                }
            }

            if (value.GetType().IsEnum)
            {
                // This should work for most cases where enums base type is int.
                return Enum.ToObject(typeof(TEnum), Convert.ToInt64(value, culture));
            }

            if (value.GetType().IsEnumerableType())
            {
                long convertedValue = 0;
                Type type = typeof(TEnum);
                List<string> enumerable = ((IEnumerable<string>)value).ToList();

                if (enumerable.Any())
                {
                    foreach (string v in enumerable)
                    {
                        TEnum fallback;
                        Enum.TryParse(v, true, out fallback);

                        // OR assignment. Stolen from ComponentModel EnumConverter.
                        convertedValue |= Convert.ToInt64(fallback, culture);
                    }

                    return Enum.ToObject(type, convertedValue);
                }

                return default(TEnum);
            }

            return base.ConvertFrom(context, culture, value);
        }
    }
}
