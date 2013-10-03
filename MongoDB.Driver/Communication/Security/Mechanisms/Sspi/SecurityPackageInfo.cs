using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;

namespace MongoDB.Driver.Communication.Security.Mechanisms.Sspi
{
    [StructLayout(LayoutKind.Sequential)]
    public struct SecurityPackageInfo
    {
        public uint Capabilities;
        public ushort Version;
        public ushort RpcIdentifier;
        public uint MaxTokenSize;
        [MarshalAs(UnmanagedType.LPWStr)]
        public string Name;
        [MarshalAs(UnmanagedType.LPWStr)]
        public string Comment;
    }
}