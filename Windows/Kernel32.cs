﻿using System;
using System.ComponentModel;
using System.IO;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using System.Text;
using JetBrains.Annotations;
using Microsoft.Win32.SafeHandles;
using FileTime = System.Runtime.InteropServices.ComTypes.FILETIME;

// ReSharper disable InconsistentNaming

namespace AcTools.Windows {
    public static class Kernel32 {
        public static readonly IntPtr InvalidHandleValue = new IntPtr(-1);

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
        public struct Win32FindData {
            public uint FileAttributes;
            public FileTime CreationTime;
            public FileTime LastAccessTime;
            public FileTime LastWriteTime;
            public uint FileSizeHigh;
            public uint FileSizeLow;
            public uint Reserved0;
            public uint Reserved1;

            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 260)]
            public string FileName;

            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 14)]
            public string AlternateFileName;
        }

        public static long GetFileSizeOnDisk(FileInfo info) {
            var result = GetDiskFreeSpaceW(info.Directory?.Root.FullName ?? "", out var sectorsPerCluster, out var bytesPerSector, out uint dummy, out dummy);
            if (result == 0) throw new Win32Exception();
            var clusterSize = sectorsPerCluster * bytesPerSector;
            var losize = GetCompressedFileSizeW(info.FullName, out var hosize);
            var size = ((long)hosize << 32) | losize;
            return (size + clusterSize - 1) / clusterSize * clusterSize;
        }

        public static long GetFileSizeOnDisk(FileInfo info, out bool isCompressed) {
            var result = GetDiskFreeSpaceW(info.Directory?.Root.FullName ?? "", out var sectorsPerCluster, out var bytesPerSector, out uint dummy, out dummy);
            if (result == 0) throw new Win32Exception();
            var clusterSize = sectorsPerCluster * bytesPerSector;
            var losize = GetCompressedFileSizeW(info.FullName, out var hosize);
            var size = ((long)hosize << 32) | losize;
            isCompressed = info.Length == size;
            return (size + clusterSize - 1) / clusterSize * clusterSize;
        }

        public static long GetFileSizeOnDisk(string file) {
            return GetFileSizeOnDisk(new FileInfo(file));
        }

        [DllImport("kernel32.dll")]
        public static extern uint GetCompressedFileSizeW([In, MarshalAs(UnmanagedType.LPWStr)] string lpFileName,
                [Out, MarshalAs(UnmanagedType.U4)] out uint lpFileSizeHigh);

        [DllImport("kernel32.dll", SetLastError = true, PreserveSig = true)]
        public static extern int GetDiskFreeSpaceW([In, MarshalAs(UnmanagedType.LPWStr)] string lpRootPathName,
                out uint lpSectorsPerCluster, out uint lpBytesPerSector, out uint lpNumberOfFreeClusters,
                out uint lpTotalNumberOfClusters);

        [DllImport("kernel32.dll", CharSet = CharSet.Auto)]
        public static extern IntPtr FindFirstFile(string lpFileName, out Win32FindData lpFindFileData);

        [DllImport("kernel32.dll", CharSet = CharSet.Auto)]
        public static extern bool FindNextFile(IntPtr hFindFile, out Win32FindData lpFindFileData);

        [StructLayout(LayoutKind.Sequential)]
        public struct ByHandleFileInformation {
            public uint FileAttributes;
            public FileTime CreationTime;
            public FileTime LastAccessTime;
            public FileTime LastWriteTime;
            public uint VolumeSerialNumber;
            public uint FileSizeHigh;
            public uint FileSizeLow;
            public uint NumberOfLinks;
            public uint FileIndexHigh;
            public uint FileIndexLow;
        }

        [DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Auto)]
        public static extern SafeFileHandle CreateFile(
                string lpFileName,
                [MarshalAs(UnmanagedType.U4)] System.IO.FileAccess dwDesiredAccess,
                [MarshalAs(UnmanagedType.U4)] System.IO.FileShare dwShareMode,
                IntPtr lpSecurityAttributes,
                [MarshalAs(UnmanagedType.U4)] FileMode dwCreationDisposition,
                [MarshalAs(UnmanagedType.U4)] System.IO.FileAttributes dwFlagsAndAttributes,
                IntPtr hTemplateFile);

        [DllImport("kernel32.dll", SetLastError = true)]
        public static extern bool GetFileInformationByHandle(SafeFileHandle handle, out ByHandleFileInformation lpFileInformation);

        [DllImport("kernel32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool CloseHandle(SafeHandle hObject);

        [DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
        public static extern IntPtr FindFirstFileNameW(string lpFileName, uint dwFlags, ref uint stringLength, StringBuilder fileName);

        [DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
        public static extern bool FindNextFileNameW(IntPtr hFindStream, ref uint stringLength, StringBuilder fileName);

        [DllImport("kernel32.dll", SetLastError = true)]
        public static extern bool FindClose(IntPtr fFindHandle);

        [DllImport("kernel32.dll")]
        public static extern bool GetVolumePathName(string lpszFileName,
                [Out] StringBuilder lpszVolumePathName, uint cchBufferLength);

        [DllImport("kernel32.dll")]
        public static extern IntPtr LoadLibrary(string lpFileName);

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
        public class MemoryStatusEx {
            public uint Length;
            public uint MemoryLoad;
            public ulong Total;
            public ulong Available;
            public ulong TotalPageFile;
            public ulong AvailablePageFile;
            public ulong TotalVirtual;
            public ulong AvailableVirtual;
            public ulong AvailableExtendedVirtual;

            public MemoryStatusEx() {
                Length = (uint)Marshal.SizeOf(typeof(MemoryStatusEx));
            }
        }

        private static void AddDllDirectoryFallback(string directory) {
            Environment.SetEnvironmentVariable("PATH", Environment.GetEnvironmentVariable("PATH") + ";" + directory);
        }

        public static void AddDllDirectory(string directory) {
            AddDllDirectoryFallback(directory);

            /*try {
                AddDllDirectoryInner(directory);
            } catch (Exception e) {
                AcToolsLogging.Write(e.Message);
                AddDllDirectoryFallback(directory);
            }*/
        }

        /*[DllImport("kernel32", EntryPoint = "AddDllDirectory", CharSet = CharSet.Unicode)]
        private static extern int AddDllDirectoryInner(string directory);*/

        [DllImport("kernel32", CharSet = CharSet.Unicode, SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool DeleteFile(string name);

        [Flags]
        public enum FileAccess : uint {
            GenericRead = 0x80000000,
            GenericWrite = 0x40000000,
            GenericExecute = 0x20000000,
            GenericAll = 0x10000000
        }

        [Flags]
        public enum FileShare : uint {
            None = 0x00000000,
            Read = 0x00000001,
            Write = 0x00000002,
            Delete = 0x00000004
        }

        public enum CreationDisposition : uint {
            New = 1,
            CreateAlways = 2,
            OpenExisting = 3,
            OpenAlways = 4,
            TruncateExisting = 5
        }

        [Flags]
        public enum FileAttributes : uint {
            Normal = 0x00000080
        }

        [DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Auto)]
        public static extern SafeFileHandle CreateFile(
                string lpFileName,
                FileAccess dwDesiredAccess,
                FileShare dwShareMode,
                IntPtr lpSecurityAttributes,
                CreationDisposition dwCreationDisposition,
                FileAttributes dwFlagsAndAttributes,
                IntPtr hTemplateFile);

        [DllImport("kernel32.dll", EntryPoint = "RtlMoveMemory")]
        public static extern unsafe void CopyMemory(byte* dst, byte* src, long size);

        [return: MarshalAs(UnmanagedType.Bool)]
        [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        public static extern bool GlobalMemoryStatusEx([In, Out] MemoryStatusEx lpBuffer);

        [DllImport("Kernel32.dll", CharSet = CharSet.Unicode)]
        public static extern bool CreateHardLink(string lpFileName, string lpExistingFileName, IntPtr lpSecurityAttributes);

        [DllImport("kernel32.dll")]
        public static extern bool CreateSymbolicLink(string lpSymlinkFileName, string lpTargetFileName, SymbolicLink dwFlags);

        public enum SymbolicLink {
            File = 0,
            Directory = 1
        }

        [DllImport("kernel32.dll")]
        public static extern uint GetCurrentThreadId();

        [DllImport("kernel32.dll")]
        public static extern IntPtr GetModuleHandle(string name);

        #region INI Files
        [DllImport("kernel32.dll")]
        public static extern long WritePrivateProfileString(string section, string key, string val, string filePath);

        [DllImport("kernel32.dll")]
        public static extern long WritePrivateProfileSection(string section, string val, string filePath);

        [DllImport("kernel32.dll")]
        public static extern int GetPrivateProfileString(string section, string key, string def, StringBuilder retVal, int size, string filePath);
        #endregion

        #region Another Processes Memory
        [Flags]
        public enum ProcessAccessFlags : uint {
            All = 0x001F0FFF,
            Terminate = 0x00000001,
            CreateThread = 0x00000002,
            VMOperation = 0x00000008,
            VMRead = 0x00000010,
            VMWrite = 0x00000020,
            DupHandle = 0x00000040,
            SetInformation = 0x00000200,
            QueryInformation = 0x00000400,
            QueryLimitedInformation = 0x00001000,
            Synchronize = 0x00100000
        }

        [DllImport("kernel32.dll")]
        public static extern IntPtr OpenProcess(ProcessAccessFlags dwDesiredAccess, [MarshalAs(UnmanagedType.Bool)] bool bInheritHandle, int dwProcessId);

        public const int STILL_ACTIVE = 0x00000103;

        [DllImport("kernel32.dll")]
        public static extern bool GetExitCodeProcess(IntPtr processHandle, out int exitCode);

        [DllImport("kernel32.dll", SetLastError = true)]
        public static extern bool WriteProcessMemory(IntPtr hProcess, IntPtr lpBaseAddress, byte[] lpBuffer, uint nSize, out int lpNumberOfBytesWritten);

        [DllImport("kernel32.dll")]
        public static extern int CloseHandle(IntPtr hProcess);

        [DllImport("kernel32.dll")]
        [ResourceExposure(ResourceScope.Process)]
        public static extern int GetCurrentProcessId();

        [DllImport("kernel32.dll")]
        [ResourceExposure(ResourceScope.Process)]
        public static extern IntPtr GetCurrentProcess();

        [DllImport("kernel32.dll", CharSet = CharSet.Ansi, SetLastError = true, BestFitMapping = false)]
        [ResourceExposure(ResourceScope.Machine)]
        public static extern bool DuplicateHandle(HandleRef hSourceProcessHandle, IntPtr hSourceHandle, HandleRef hTargetProcess,
                out SafeWaitHandle targetHandle, int dwDesiredAccess, bool bInheritHandle, int dwOptions);

        public const int DUPLICATE_CLOSE_SOURCE = 1;
        public const int DUPLICATE_SAME_ACCESS = 2;
        #endregion

        [DllImport("kernel32.dll", SetLastError = true)]
        public static extern bool SetDllDirectory(string lpPathName);

        [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        public static extern int GetDllDirectory(int nBufferLength, StringBuilder lpPathName);

        [CanBeNull]
        public static string GetDllDirectory() {
            var sb = new StringBuilder(500);
            return GetDllDirectory(sb.Capacity, sb) == 0 ? null : sb.ToString();
        }
    }
}