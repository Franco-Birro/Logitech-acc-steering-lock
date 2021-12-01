using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using AcTools.Utils.Helpers;
using AcTools.Utils.Physics;
using JetBrains.Annotations;

namespace AcTools.DataFile {
    public class IniFileSection : Dictionary<string, string> {
        [CanBeNull]
        private readonly IDataReadWrapper _wrapper;

        public IniFileSection([CanBeNull] IDataReadWrapper wrapper, IniFileSection original) : base(original) {
            _wrapper = wrapper;
            Commentary = original.Commentary;
            Commentaries = original.Commentaries == null ? null : new Dictionary<string, string>(original.Commentaries);
        }

        public IniFileSection([CanBeNull] IDataReadWrapper wrapper) {
            _wrapper = wrapper;
        }

        public IniFileSection Clone() {
            return new IniFileSection(_wrapper, this);
        }

        [CanBeNull]
        public string Commentary { get; private set; }

        [CanBeNull]
        public Dictionary<string, string> Commentaries { get; private set; }

        public void SetCommentary([CanBeNull, LocalizationRequired(false)] string value) {
            Commentary = value;
        }

        public void SetCommentary([NotNull, LocalizationRequired(false)] string key, [CanBeNull, LocalizationRequired(false)] string value) {
            if (value == null) {
                RemoveCommentary(key);
                return;
            }

            if (Commentaries == null) Commentaries = new Dictionary<string, string>(1);
            Commentaries[key] = value;
        }

        public void RemoveCommentary([NotNull, LocalizationRequired(false)] string key) {
            if (Commentaries == null) return;
            Commentaries.Remove(key);
            if (Commentaries.Count == 0) {
                Commentaries = null;
            }
        }

        public new dynamic this[[NotNull, LocalizationRequired(false)] string key] {
            get => ContainsKey(key) ? base[key] : null;
            set {
                if (ReferenceEquals(value, IniFile.Nothing)) {
                    Remove(key);
                } else if (value == null) {
                    Set(key, (string)null);
                } else {
                    Set(key, value);
                }
            }
        }

        internal void SetDirect(string key, string value) {
            base[key] = value;
        }

        [CanBeNull, Pure, Localizable(false)]
        public string GetPossiblyEmpty([NotNull, LocalizationRequired(false)] string key) {
            return ContainsKey(key) ? base[key] : null;
        }

        [CanBeNull, Pure, Localizable(false)]
        public string GetNonEmpty([NotNull, LocalizationRequired(false)] string key) {
            if (!ContainsKey(key)) return null;
            var value = base[key];
            return string.IsNullOrWhiteSpace(value) ? null : value;
        }

        [NotNull, Pure]
        public string GetNonEmpty([NotNull, LocalizationRequired(false)] string key, [NotNull, Localizable(false)] string defaultValue) {
            return GetNonEmpty(key) ?? defaultValue;
        }

        [Pure]
        public bool GetBool([NotNull, LocalizationRequired(false)] string key, bool defaultValue) {
            if (!ContainsKey(key)) return defaultValue;
            var value = GetPossiblyEmpty(key);
            return value == "1";
        }

        [Pure]
        public bool? GetBoolNullable([NotNull, LocalizationRequired(false)] string key) {
            if (!ContainsKey(key)) return null;
            var value = GetPossiblyEmpty(key);
            return value == "1" || string.Equals(value, "true", StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(value, "yes", StringComparison.OrdinalIgnoreCase) || string.Equals(value, "y", StringComparison.OrdinalIgnoreCase);
        }

        [NotNull, Pure]
        public string[] GetStrings([NotNull, LocalizationRequired(false)] string key, char delimiter = ',') {
            return GetPossiblyEmpty(key)?.Split(delimiter).Select(x => x.Trim()).Where(x => x.Length > 0).ToArray() ?? new string[0];
        }

        [NotNull, Pure]
        private double[] GetVector([NotNull, LocalizationRequired(false)] string key, int size) {
            var result = new double[size];
            var line = GetNonEmpty(key);
            if (line == null) return result;

            var s = line.Split(',');
            if (s.Length != size) return result;

            for (var i = 0; i < result.Length; i++) {
                FlexibleParser.TryParseDouble(s[i], out result[i]);
            }

            return result;
        }

        [NotNull, Pure]
        private float[] GetVectorF([NotNull, LocalizationRequired(false)] string key, int size) {
            var result = new float[size];
            var line = GetNonEmpty(key);
            if (line == null) return result;

            var s = line.Split(',');
            if (s.Length != size) return result;

            for (var i = 0; i < result.Length; i++) {
                if (FlexibleParser.TryParseDouble(s[i], out double f)) {
                    result[i] = (float)f;
                }
            }

            return result;
        }

        [NotNull, Pure]
        public double[] GetVector2([NotNull, LocalizationRequired(false)] string key) {
            return GetVector(key, 2);
        }

        [NotNull, Pure]
        public float[] GetVector2F([NotNull, LocalizationRequired(false)] string key) {
            return GetVectorF(key, 2);
        }

        [NotNull, Pure]
        public double[] GetVector3([NotNull, LocalizationRequired(false)] string key) {
            return GetVector(key, 3);
        }

        [NotNull, Pure]
        public float[] GetVector3F([NotNull, LocalizationRequired(false)] string key) {
            return GetVectorF(key, 3);
        }

        [NotNull, Pure]
        public double[] GetVector4([NotNull, LocalizationRequired(false)] string key) {
            return GetVector(key, 4);
        }

        [NotNull, Pure]
        public float[] GetVector4F([NotNull, LocalizationRequired(false)] string key) {
            return GetVectorF(key, 4);
        }

        [Pure]
        public double? GetDoubleNullable([NotNull, LocalizationRequired(false)] string key) {
            return FlexibleParser.TryParseDouble(GetPossiblyEmpty(key));
        }

        [Pure]
        public double GetDouble([NotNull, LocalizationRequired(false)] string key, double defaultValue) {
            return FlexibleParser.ParseDouble(GetPossiblyEmpty(key), defaultValue);
        }

        [Pure]
        public float? GetFloatNullable([NotNull, LocalizationRequired(false)] string key) {
            return (float?)FlexibleParser.TryParseDouble(GetPossiblyEmpty(key));
        }

        [Pure]
        public float GetFloat([NotNull, LocalizationRequired(false)] string key, float defaultValue) {
            return (float)FlexibleParser.ParseDouble(GetPossiblyEmpty(key), defaultValue);
        }

        [Pure]
        public int? GetIntNullable([NotNull, LocalizationRequired(false)] string key) {
            return FlexibleParser.TryParseInt(GetPossiblyEmpty(key));
        }

        [Pure]
        public int GetInt([NotNull, LocalizationRequired(false)] string key, int defaultValue) {
            return FlexibleParser.TryParseInt(GetPossiblyEmpty(key)) ?? defaultValue;
        }

        [Pure]
        public long? GetLongNullable([NotNull, LocalizationRequired(false)] string key) {
            return FlexibleParser.TryParseLong(GetPossiblyEmpty(key));
        }

        [Pure]
        public long GetLong([NotNull, LocalizationRequired(false)] string key, long defaultValue) {
            return FlexibleParser.TryParseLong(GetPossiblyEmpty(key)) ?? defaultValue;
        }

        [Pure, CanBeNull]
        public Lut GetLut([NotNull, LocalizationRequired(false)] string key) {
            var value = GetNonEmpty(key);
            if (value == null) return null;
            if (!Lut.IsInlineValue(value)) {
                if (_wrapper != null) return _wrapper.GetLutFile(value).Values;
               // AcToolsLogging.Write("Non-inline LUT found, but DataWrapper is not set!");
            }

            return Lut.FromValue(value);
        }

        [Pure, NotNull]
        public Lut GetLut([NotNull, LocalizationRequired(false)] string key, [NotNull] string fallbackName) {
            var value = GetNonEmpty(key);
            if (string.IsNullOrEmpty(value)) {
                return _wrapper?.GetLutFile(fallbackName).Values ?? new Lut();
            }

            if (!Lut.IsInlineValue(value)) {
                if (_wrapper != null) return _wrapper.GetLutFile(value).Values;
              //  AcToolsLogging.Write("Non-inline LUT found, but DataWrapper is not set!");
            }

            return Lut.FromValue(value);
        }

        /// <summary>
        /// Just in case, this method can parse entries from something like “OVERSTEER_FACTOR” to “OversteerFactor”.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="key"></param>
        /// <param name="ignoreCase"></param>
        /// <returns></returns>
        [Pure]
        public T? GetEnumNullable<T>([NotNull, LocalizationRequired(false)] string key, bool ignoreCase = true) where T : struct, IConvertible {
            if (!typeof(T).IsEnum) {
                throw new ArgumentException("T must be an enumerated type");
            }

            var s = GetNonEmpty(key);
            if (s == null) return null;

            return Enum.TryParse(GetPossiblyEmpty(key), ignoreCase, out T result) ||
                    s.Contains('_') && Enum.TryParse(s.Replace("_", ""), ignoreCase, out result) ? result : (T?)null;
        }

        /// <summary>
        /// Just in case, this method can parse entries from something like “OVERSTEER_FACTOR” to “OversteerFactor”.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="key"></param>
        /// <param name="ignoreCase"></param>
        /// <returns></returns>
        [Pure]
        public T GetEnum<T>([NotNull, LocalizationRequired(false)] string key, T defaultValue, bool ignoreCase = true) where T : struct, IConvertible {
            if (!typeof(T).IsEnum) {
                throw new ArgumentException("T must be an enumerated type");
            }

            var s = GetNonEmpty(key);
            if (s == null) return defaultValue;

            return Enum.TryParse(s, ignoreCase, out T result) ||
                    s.Contains('_') && Enum.TryParse(s.Replace("_", ""), ignoreCase, out result) ? result : defaultValue;
        }

        [Pure]
        public T GetIntEnum<T>([NotNull, LocalizationRequired(false)] string key, T defaultValue) where T : struct, IConvertible {
            if (!typeof(T).IsEnum) {
                throw new ArgumentException("T must be an enumerated type");
            }

            var i = GetInt(key, Convert.ToInt32(defaultValue));
            return Enum.IsDefined(typeof(T), i) ? (T)Enum.ToObject(typeof(T), i) : defaultValue;
        }

        private static void Set([NotNull, LocalizationRequired(false)] string key, object value) {
            throw new Exception($"Type is not supported: {value?.GetType().ToString() ?? "null"} (key: “{key}”)");
        }

        public void Set([NotNull, LocalizationRequired(false)] string key, [LocalizationRequired(false)] string value) {
            if (value == null) return;
            base[key] = value;
        }

        public void SetOrRemove([NotNull, LocalizationRequired(false)] string key, [LocalizationRequired(false), CanBeNull] string value) {
            if (value != null) {
                base[key] = value;
            } else if (ContainsKey(key)){
                Remove(key);
            }
        }

        public void SetOrRemove([NotNull, LocalizationRequired(false)] string key, [LocalizationRequired(false)] bool value) {
            if (value) {
                base[key] = "1";
            } else if (ContainsKey(key)){
                Remove(key);
            }
        }

        public void Set<T>([NotNull, LocalizationRequired(false)] string key, IEnumerable<T> value, char delimiter = ',') {
            if (value == null) return;
            Set(key, value.Select(x => x.ToInvariantString()).JoinToString(delimiter));
        }

        public void SetId([NotNull, LocalizationRequired(false)] string key, string value) {
            if (value == null) return;
            base[key] = value.ToLowerInvariant();
        }

        public void Set([NotNull, LocalizationRequired(false)] string key, Enum value) {
            base[key] = Convert.ToDouble(value).ToString(CultureInfo.InvariantCulture);
        }

        public void SetIntEnum([NotNull, LocalizationRequired(false)] string key, Enum value) {
            base[key] = Convert.ToInt32(value).ToString(CultureInfo.InvariantCulture);
        }

        public void Set([NotNull, LocalizationRequired(false)] string key, int value) {
            base[key] = value.ToString(CultureInfo.InvariantCulture);
        }

        public void Set([NotNull, LocalizationRequired(false)] string key, int? value) {
            if (!value.HasValue) return;
            base[key] = value.Value.ToString(CultureInfo.InvariantCulture);
        }

        public void Set([NotNull, LocalizationRequired(false)] string key, long value) {
            base[key] = value.ToString(CultureInfo.InvariantCulture);
        }

        public void Set([NotNull, LocalizationRequired(false)] string key, long? value) {
            if (!value.HasValue) return;
            base[key] = value.Value.ToString(CultureInfo.InvariantCulture);
        }

        public void Set([NotNull, LocalizationRequired(false)] string key, [CanBeNull] Lut value) {
            if (value == null) return;
            base[key] = value.ToString();
        }

        private static string DoubleToString(double d) {
            return d.ToString(CultureInfo.InvariantCulture);
            // return s.IndexOf('.') == -1 ? $"{s}.0" : s;
        }

        public void Set([NotNull, LocalizationRequired(false)] string key, double value) {
            base[key] = DoubleToString(value);
        }

        public void Set([NotNull, LocalizationRequired(false)] string key, double? value) {
            if (!value.HasValue) return;
            base[key] = DoubleToString(value.Value);
        }

        public void Set([NotNull, LocalizationRequired(false)] string key, double value, [Localizable(false)] string format) {
            base[key] = value.ToString(format, CultureInfo.InvariantCulture);
        }

        public void Set([NotNull, LocalizationRequired(false)] string key, double? value, string format) {
            if (!value.HasValue) return;
            base[key] = value.Value.ToString(format, CultureInfo.InvariantCulture);
        }

        public void Set([NotNull, LocalizationRequired(false)] string key, bool value) {
            base[key] = value ? "1" : "0";
        }

        public void Set([NotNull, LocalizationRequired(false)] string key, bool? value) {
            if (!value.HasValue) return;
            base[key] = value.Value ? "1" : "0";
        }
    }
}