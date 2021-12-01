﻿using System.ComponentModel;
using JetBrains.Annotations;

namespace AcTools.DataFile {
    [Localizable(false)]
    public static class DataWrapperExtension {
        [NotNull]
        public static IniFile GetIniFile([NotNull] this IDataReadWrapper data, [NotNull] string name) {
            return data.GetFile<IniFile>(name);
        }

        [NotNull]
        public static LutDataFile GetLutFile([NotNull] this IDataReadWrapper data, [NotNull] string name) {
            return data.GetFile<LutDataFile>(name);
        }

        [NotNull]
        public static RtoDataFile GetRtoFile([NotNull] this IDataReadWrapper data, [NotNull] string name) {
            return data.GetFile<RtoDataFile>(name);
        }

        [NotNull]
        public static RawDataFile GetRawFile([NotNull] this IDataReadWrapper data, [NotNull] string name) {
            return data.GetFile<RawDataFile>(name);
        }
    }
}