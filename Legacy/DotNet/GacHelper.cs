using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;

namespace Inedo.BuildMasterExtensions.WindowsSdk.DotNet
{
    #region Fusion Interfaces
    [ComImport]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    [Guid("e707dcde-d1cd-11d2-bab9-00c04f8eceae")]
    internal interface IAssemblyCache
    {
        [PreserveSig]
        int UninstallAssembly(
                            int flags,
                            [MarshalAs(UnmanagedType.LPWStr)]
                            string assemblyName,
                            InstallReference refData,
                            out AssemblyCacheUninstallDisposition disposition);

        [PreserveSig]
        int QueryAssemblyInfo(
                            int flags,
                            [MarshalAs(UnmanagedType.LPWStr)]
                            string assemblyName,
                            ref AssemblyInfo assemblyInfo);
        [PreserveSig]
        int Reserved(
                            int flags,
                            IntPtr pvReserved,
                            out Object ppAsmItem,
                            [MarshalAs(UnmanagedType.LPWStr)]
                            string assemblyName);
        [PreserveSig]
        int Reserved(out Object ppAsmScavenger);

        [PreserveSig]
        int InstallAssembly(
                            int flags,
                            [MarshalAs(UnmanagedType.LPWStr)]
                            string assemblyFilePath,
                            InstallReference refData);
    }// IAssemblyCache

    [ComImport]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    [Guid("CD193BC0-B4BC-11d2-9833-00C04FC31D2E")]
    internal interface IAssemblyName
    {
        [PreserveSig]
        int SetProperty(
                int PropertyId,
                IntPtr pvProperty,
                int cbProperty);

        [PreserveSig]
        int GetProperty(
                int PropertyId,
                IntPtr pvProperty,
                ref int pcbProperty);

        [PreserveSig]
        int Finalize();

        [PreserveSig]
        int GetDisplayName(
                StringBuilder pDisplayName,
                ref int pccDisplayName,
                int displayFlags);

        [PreserveSig]
        int Reserved(ref Guid guid,
            Object obj1,
            Object obj2,
            string string1,
            Int64 llFlags,
            IntPtr pvReserved,
            int cbReserved,
            out IntPtr ppv);

        [PreserveSig]
        int GetName(
                ref int pccBuffer,
                StringBuilder pwzName);

        [PreserveSig]
        int GetVersion(
                out int versionHi,
                out int versionLow);
        [PreserveSig]
        int IsEqual(
                IAssemblyName pAsmName,
                int cmpFlags);

        [PreserveSig]
        int Clone(out IAssemblyName pAsmName);
    }// IAssemblyName

    [ComImport]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    [Guid("21b8916c-f28e-11d2-a473-00c04f8ef448")]
    internal interface IAssemblyEnum
    {
        [PreserveSig]
        int GetNextAssembly(
                IntPtr pvReserved,
                out IAssemblyName ppName,
                int flags);
        [PreserveSig]
        int Reset();
        [PreserveSig]
        int Clone(out IAssemblyEnum ppEnum);
    }// IAssemblyEnum

    [ComImport]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    [Guid("582dac66-e678-449f-aba6-6faaec8a9394")]
    internal interface IInstallReferenceItem
    {
        // A pointer to a FUSION_INSTALL_REFERENCE structure. 
        // The memory is allocated by the GetReference method and is freed when 
        // IInstallReferenceItem is released. Callers must not hold a reference to this 
        // deployablesBuffer after the IInstallReferenceItem object is released. 
        // This uses the InstallReferenceOutput object to avoid allocation 
        // issues with the interop layer. 
        // This cannot be marshaled directly - must use IntPtr 
        [PreserveSig]
        int GetReference(
                out IntPtr pRefData,
                int flags,
                IntPtr pvReserced);
    }// IInstallReferenceItem

    [ComImport]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    [Guid("56b1a988-7c0c-4aa2-8639-c3eb5a90226f")]
    internal interface IInstallReferenceEnum
    {
        [PreserveSig]
        int GetNextInstallReferenceItem(
                out IInstallReferenceItem ppRefItem,
                int flags,
                IntPtr pvReserced);
    }// IInstallReferenceEnum
    #endregion

    #region Fusion Enums
    internal enum AssemblyCommitFlags
    {
        Default = 1,
        Force = 2
    }// enum AssemblyCommitFlags

    internal enum AssemblyCacheUninstallDisposition
    {
        Unknown = 0,
        Uninstalled = 1,
        StillInUse = 2,
        AlreadyUninstalled = 3,
        DeletePending = 4,
        HasInstallReference = 5,
        ReferenceNotFound = 6
    }

    [Flags]
    internal enum AssemblyCacheFlags
    {
        GAC = 2,
    }

    internal enum CreateAssemblyNameObjectFlags
    {
        CANOF_DEFAULT = 0,
        CANOF_PARSE_DISPLAY_NAME = 1,
    }

    [Flags]
    internal enum AssemblyNameDisplayFlags
    {
        VERSION = 0x01,
        CULTURE = 0x02,
        PUBLIC_KEY_TOKEN = 0x04,
        PROCESSORARCHITECTURE = 0x20,
        RETARGETABLE = 0x80,
        // This enum will change in the future to include
        // more attributes.
        ALL = VERSION
                                    | CULTURE
                                    | PUBLIC_KEY_TOKEN
                                    | PROCESSORARCHITECTURE
                                    | RETARGETABLE
    }
    #endregion

    [StructLayout(LayoutKind.Sequential)]
    internal sealed class InstallReference
    {
        public InstallReference(Guid guid, string id, string data)
        {
            cbSize = (int)(2 * IntPtr.Size + 16 + (id.Length + data.Length) * 2);
            flags = 0;
            // quiet compiler warning 
            if (flags == 0) { }
            guidScheme = guid;
            identifier = id;
            description = data;
        }

        public Guid GuidScheme
        {
            get { return guidScheme; }
        }

        public string Identifier
        {
            get { return identifier; }
        }

        public string Description
        {
            get { return description; }
        }

        int cbSize;
        int flags;
        Guid guidScheme;
        [MarshalAs(UnmanagedType.LPWStr)]
        string identifier;
        [MarshalAs(UnmanagedType.LPWStr)]
        string description;
    }

    [StructLayout(LayoutKind.Sequential)]
    internal struct AssemblyInfo
    {
        public int cbAssemblyInfo; // size of this structure for future expansion
        public int assemblyFlags;
        public long assemblySizeInKB;
        [MarshalAs(UnmanagedType.LPWStr)]
        public string currentAssemblyPath;
        public int cchBuf; // size of path buf.
    }

    [ComVisible(false)]
    internal static class InstallReferenceGuid
    {
        public static bool IsValidGuidScheme(Guid guid)
        {
            return (guid.Equals(UninstallSubkeyGuid) ||
                    guid.Equals(FilePathGuid) ||
                    guid.Equals(OpaqueGuid) ||
                    guid.Equals(Guid.Empty));
        }

        public readonly static Guid UninstallSubkeyGuid = new Guid("8cedc215-ac4b-488b-93c0-a50a49cb2fb8");
        public readonly static Guid FilePathGuid = new Guid("b02f9d65-fb77-4f7a-afa5-b391309f11c9");
        public readonly static Guid OpaqueGuid = new Guid("2ec93463-b0c3-45e1-8364-327e96aea856");

        // These GUID's cannot be used for installing into the GAC.
        public readonly static Guid MsiGuid = new Guid("25df0fc1-7f97-4070-add7-4b13bbfd7cb8");
        public readonly static Guid OsInstallGuid = new Guid("d16d444c-56d8-11d5-882d-0080c847b195");
    }

    internal static class AssemblyCache
    {
        /// <summary>
        /// Installs an assembly to the GAC.
        /// </summary>
        /// <param name="assemblyPath">Path to assembly to install.</param>
        /// <param name="reference">Describes the application which is installing the assembly.</param>
        /// <param name="flags">Additional assembly commit flags.</param>
        public static void InstallAssembly(string assemblyPath, InstallReference reference, AssemblyCommitFlags flags)
        {
            if (reference != null)
            {
                if (!InstallReferenceGuid.IsValidGuidScheme(reference.GuidScheme))
                    throw new ArgumentException("Invalid reference guid.", "guid");
            }

            IAssemblyCache ac = null;

            int hr = 0;

            hr = Fusion.CreateAssemblyCache(out ac, 0);
            if (hr >= 0)
            {
                hr = ac.InstallAssembly((int)flags, assemblyPath, reference);
            }

            if (hr < 0)
            {
                Marshal.ThrowExceptionForHR(hr);
            }
        }

        /// <summary>
        /// Uninstalls an assembly from the GAC.
        /// </summary>
        /// <param name="assemblyName">Fully specified assembly name.</param>
        /// <param name="reference">Application which is uninstalling the assembly.</param>
        /// <returns>Resulting assembly disposition.</returns>
        /// <remarks>
        /// The assemblyName parameter has to be a fully specified name. 
        /// A.k.a, for v1.0/v1.1 assemblies, it should be "name, Version=xx, Culture=xx, PublicKeyToken=xx".
        /// For v2.0 assemblies, it should be "name, Version=xx, Culture=xx, PublicKeyToken=xx, ProcessorArchitecture=xx".
        /// If assemblyName is not fully specified, a random matching assembly will be uninstalled. 
        /// </remarks>
        public static AssemblyCacheUninstallDisposition UninstallAssembly(string assemblyName, InstallReference reference)
        {
            var dispResult = AssemblyCacheUninstallDisposition.Uninstalled;
            if (reference != null)
            {
                if (!InstallReferenceGuid.IsValidGuidScheme(reference.GuidScheme))
                    throw new ArgumentException("Invalid reference guid.", "guid");
            }

            IAssemblyCache ac = null;

            int hr = Fusion.CreateAssemblyCache(out ac, 0);
            if (hr >= 0)
            {
                hr = ac.UninstallAssembly(0, assemblyName, reference, out dispResult);
            }

            if (hr < 0)
            {
                Marshal.ThrowExceptionForHR(hr);
            }

            return dispResult;
        }

        public static string QueryAssemblyInfo(string assemblyName)
        {
            if (assemblyName == null)
            {
                throw new ArgumentException("Invalid name", "assemblyName");
            }

            AssemblyInfo aInfo = new AssemblyInfo();

            aInfo.cchBuf = 1024;
            // Get a string with the desired length
            aInfo.currentAssemblyPath = new string('\0', aInfo.cchBuf);

            IAssemblyCache ac = null;
            int hr = Fusion.CreateAssemblyCache(out ac, 0);
            if (hr >= 0)
            {
                hr = ac.QueryAssemblyInfo(0, assemblyName, ref aInfo);
            }
            if (hr < 0)
            {
                Marshal.ThrowExceptionForHR(hr);
            }

            return aInfo.currentAssemblyPath;
        }

        public static IEnumerable<string> GetInstalledAssemblies()
        {
            return new AssemblyCacheEnum(null);
        }
        public static IEnumerable<string> GetInstalledAssemblies(string assemblyName)
        {
            return new AssemblyCacheEnum(assemblyName);
        }

        [ComVisible(false)]
        private sealed class AssemblyCacheEnum : IEnumerable<string>
        {
            // null means enumerate all the assemblies
            public AssemblyCacheEnum(string assemblyName)
            {
                IAssemblyName fusionName = null;
                int hr = 0;

                if (assemblyName != null)
                {
                    hr = Fusion.CreateAssemblyNameObject(
                            out fusionName,
                            assemblyName,
                            CreateAssemblyNameObjectFlags.CANOF_PARSE_DISPLAY_NAME,
                            IntPtr.Zero);
                }

                if (hr >= 0)
                {
                    hr = Fusion.CreateAssemblyEnum(
                            out m_AssemblyEnum,
                            IntPtr.Zero,
                            fusionName,
                            AssemblyCacheFlags.GAC,
                            IntPtr.Zero);
                }

                if (hr < 0)
                {
                    Marshal.ThrowExceptionForHR(hr);
                }
            }

            private string GetNextAssembly()
            {
                int hr = 0;
                IAssemblyName fusionName = null;

                if (done)
                {
                    return null;
                }

                // Now get next IAssemblyName from m_AssemblyEnum
                hr = m_AssemblyEnum.GetNextAssembly((IntPtr)0, out fusionName, 0);

                if (hr < 0)
                {
                    Marshal.ThrowExceptionForHR(hr);
                }

                if (fusionName != null)
                {
                    return GetFullName(fusionName);
                }
                else
                {
                    done = true;
                    return null;
                }
            }

            private string GetFullName(IAssemblyName fusionAsmName)
            {
                StringBuilder sDisplayName = new StringBuilder(1024);
                int iLen = 1024;

                int hr = fusionAsmName.GetDisplayName(sDisplayName, ref iLen, (int)AssemblyNameDisplayFlags.ALL);
                if (hr < 0)
                {
                    Marshal.ThrowExceptionForHR(hr);
                }

                return sDisplayName.ToString();
            }

            private IAssemblyEnum m_AssemblyEnum = null;
            private bool done;

            public IEnumerator<string> GetEnumerator()
            {
                string s;
                while ((s = GetNextAssembly()) != null)
                {
                    yield return s;
                }
            }

            System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
            {
                return GetEnumerator();
            }
        }
    }

    #region P/Invoke
    internal static class Fusion
    {
        [DllImport("fusion.dll")]
        internal static extern int CreateAssemblyEnum(
                out IAssemblyEnum ppEnum,
                IntPtr pUnkReserved,
                IAssemblyName pName,
                AssemblyCacheFlags flags,
                IntPtr pvReserved);

        [DllImport("fusion.dll")]
        internal static extern int CreateAssemblyNameObject(
                out IAssemblyName ppAssemblyNameObj,
                [MarshalAs(UnmanagedType.LPWStr)]
                string szAssemblyName,
                CreateAssemblyNameObjectFlags flags,
                IntPtr pvReserved);

        [DllImport("fusion.dll")]
        internal static extern int CreateAssemblyCache(
                out IAssemblyCache ppAsmCache,
                int reserved);
    }
    #endregion
}
