// Copyright Contributors to the OpenVDB Project
// SPDX-License-Identifier: Apache-2.0

namespace OpenVDB
{
    /// <summary>
    /// Contains platform-specific definitions and utilities.
    /// Many C++ macros from Platform.h do not have direct equivalents in C#
    /// or are handled by the .NET runtime and C# compiler (e.g., inlining, deprecation).
    /// </summary>
    public static class Platform
    {
        // OPENVDB_HAS_CXX11 / OPENVDB_HAS_CXX20 - C# language version is determined by the project target.
        // SIMD intrinsics - C# has System.Numerics.Vectors for SIMD.
        // NOMINMAX - Not applicable in C#.
        // OPENVDB_DLL / OPENVDB_STATICLIB - Handled by .NET assembly mechanisms.
        // OPENVDB_UBSAN_SUPPRESS - Not directly applicable.
        // OPENVDB_LIKELY / OPENVDB_UNLIKELY - JIT optimization, no direct C# equivalent.
        // OPENVDB_FORCE_INLINE - JIT optimization, System.Runtime.CompilerServices.MethodImpl(MethodImplOptions.AggressiveInlining).
        // Warning suppression macros (OPENVDB_NO_UNREACHABLE_CODE_WARNING, etc.) - C# uses #pragma warning disable/restore.
        // Deprecation macros (OPENVDB_DEPRECATED) - C# uses [System.ObsoleteAttribute].
        // OPENVDB_EXPORT / OPENVDB_IMPORT / OPENVDB_API - Handled by C# accessibility modifiers (public, internal, etc.).
        // Thread-safe static warnings - C# has different mechanisms for thread safety.

        // Example of a constant that might be defined, if any were directly translatable.
        // public const int SomePlatformConstant = 42;

        /// <summary>
        /// Marks a feature as deprecated.
        /// </summary>
        public class ObsoleteAttribute : System.ObsoleteAttribute
        {
            public ObsoleteAttribute() : base() { }
            public ObsoleteAttribute(string message) : base(message) { }
            public ObsoleteAttribute(string message, bool error) : base(message, error) { }
        }
    }
}
