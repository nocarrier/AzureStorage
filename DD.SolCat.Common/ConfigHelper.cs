 using System;
using System.Configuration;
using System.Globalization;

namespace DD.SolCat.Common
{
    public static class ConfigHelper
    {
        /// <summary>
        /// Gets configuration value for the specified key (null if key does not exist)
        /// </summary>
        /// <param name="key">Configuration key</param>
        /// <returns></returns>
        public static string GetConfigValue(
            string key
            )
        {
            return ConfigurationManager.AppSettings.Get(key);
        }

        /// <summary>
        /// Gets configuration value for the specified key (0 if key does not exist)
        /// </summary>
        /// <param name="key">Configuration key</param>
        /// <returns></returns>
        public static int GetConfigIntValue(
            string key
            )
        {
            string value = GetConfigValue(key);
            int intValue;

            int.TryParse(value,
                NumberStyles.Integer,
                CultureInfo.InvariantCulture,
                out intValue);

            return intValue;
        }

        /// <summary>
        /// Gets configuration value for the specified key (uses default value if key does not exist)
        /// </summary>
        /// <param name="key">Configuration key</param>
        /// <param name="defaultValue">Default value if key does not exist</param>
        /// <returns></returns>
        public static string GetConfigValue(
            string key,
            string defaultValue
            )
        {
            string value = GetConfigValue(key);

            if (value == null)
            {
                value = defaultValue;
            }

            return value;
        }

        /// <summary>
        /// Gets configuration value for the specified key (uses default value if key does not exist)
        /// </summary>
        /// <param name="key">Configuration key</param>
        /// <param name="defaultValue">Default value if key does not exist</param>
        /// <returns></returns>
        public static int GetConfigIntValue(
            string key,
            int defaultValue
            )
        {
            string value = GetConfigValue(key);
            int intValue;

            if (!int.TryParse(value,
                NumberStyles.Integer,
                CultureInfo.InvariantCulture,
                out intValue))
            {
                intValue = defaultValue;
            }

            return intValue;
        }

        private const string m_yesString = "yes";

        /// <summary>
        /// Gets configuration bool value for the specified key, 
        /// if the key does not exit, or empty, defaultValue is returned.
        /// otherwise if the value is true/yes, it return true,
        /// </summary>
        /// <param name="key"></param>
        /// <param name="defaultValue"></param>
        /// <returns></returns>
        public static bool GetConfigBoolValue(
            string key,
            bool defaultValue
            )
        {
            string value = GetConfigValue(key);
            value = (value == null) ? string.Empty : value.Trim();
            if (string.IsNullOrEmpty(value))
            {
                return defaultValue;
            }
            if (value.Equals(bool.TrueString, StringComparison.OrdinalIgnoreCase)
                || value.Equals(m_yesString, StringComparison.OrdinalIgnoreCase)
                )
            {
                return true;
            }
            return false;

        }
    }
}
