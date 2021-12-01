using System.Globalization;
using System.Linq;
using System.Text;
using AcTools.Utils.Helpers;
using AcTools.Utils.Physics;

namespace AcTools.DataFile {
    public class LutDataFile : DataFileBase {
        public LutDataFile(string filename) : base(filename) {}
        public LutDataFile() {}

        public LutDataFile(Lut values) {
            _lut = values;
        }

        private Lut _lut;
        public Lut Values => _lut ?? (_lut = new Lut());

        protected override void ParseString(string data) {
            Clear();

            var started = -1;
            var key = double.NaN;
            var malformed = -1;
            var line = 1;

            for (var i = 0; i < data.Length; i++) {
                switch (data[i]) {
                    case '\r':
                        if (i + 1 < data.Length) {
                            var next = data[i + 1];
                            if (next == '\n' || next == '\r') continue;
                        }

                        //AcToolsLogging.Write($@"Unexpected “\r” at {line}");
                        malformed = line;
                        break;

                    case '|':
                        if (started != -1) {
                            if (!double.IsNaN(key) || !FlexibleParser.TryParseDouble(data.Substring(started, i - started), out key)) {
                                if (malformed == -1) {
                                    //AcToolsLogging.Write(!double.IsNaN(key)
                                    //        ? $@"Key already defined! But then, there is a second one, at {line}"
                                    //        : $@"Failed to parse key “{data.Substring(started, i - started)}” at {line}");
                                    malformed = line;
                                }

                                SkipLine(data, ref i, ref line);
                                key = double.NaN;
                            }
                            started = -1;
                        }
                        break;

                    case '\n':
                        Finish(Values, data, i, line, ref key, ref started, ref malformed);
                        line++;
                        break;

                    case '/':
                        if (i + 1 < data.Length && data[i + 1] == '/') goto case ';';
                        goto default;

                    case ';':
                        Finish(Values, data, i, line, ref key, ref started, ref malformed);
                        SkipLine(data, ref i, ref line);
                        break;

                    default:
                        if (started == -1) started = i;
                        break;
                }
            }

            Finish(Values, data, data.Length, line, ref key, ref started, ref malformed);

            if (malformed != -1) {
                ErrorsCatcher?.Catch(this, malformed);
            }
        }

        private static void SkipLine(string data, ref int index, ref int line) {
            do { index++; } while (index < data.Length && data[index] != '\n');
            line++;
        }

        private static void Finish(Lut values, string data, int index, int line, ref double key, ref int started, ref int malformed) {
            if (started != -1) {
                if (double.IsNaN(key)) {
                    if (malformed == -1) {
                       // AcToolsLogging.Write($@"Key is NaN at {line}");
                        malformed = line;
                    }
                } else {
                    if (FlexibleParser.TryParseDouble(data.Substring(started, index - started), out var value)) {
                        values.Add(new LutPoint(key, value));
                    } else {
                        if (malformed == -1) {
                           // AcToolsLogging.Write($@"Failed to parse key “{data.Substring(started, index - started)}” at {line}");
                            malformed = line;
                        }
                    }
                    key = double.NaN;
                }
                started = -1;
            } else if (!double.IsNaN(key)) {
                key = double.NaN;
            }
        }

        public override void Clear() {
            Values.Clear();
        }

        public override string Stringify() {
            return Stringify(true);
        }

        public string Stringify(bool ordered) {
            var sb = new StringBuilder(Values.Count * 4);

            if (ordered) {
                foreach (var pair in Values.OrderBy(x => x.X)) {
                    sb.Append(pair.X.ToString(CultureInfo.InvariantCulture)).Append("|")
                      .Append(pair.Y.ToString(CultureInfo.InvariantCulture)).Append("\n");
                }
            } else {
                foreach (var pair in Values) {
                    sb.Append(pair.X.ToString(CultureInfo.InvariantCulture)).Append("|")
                      .Append(pair.Y.ToString(CultureInfo.InvariantCulture)).Append("\n");
                }
            }

            return sb.ToString();
        }

        public bool IsEmptyOrDamaged() {
            return Values.Count == 0;
        }
    }
}