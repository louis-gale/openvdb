// Copyright Contributors to the OpenVDB Project
// SPDX-License-Identifier: Apache-2.0

using System;
using OpenVDB.Core.Tree; // For grid type registration if done here
using OpenVDB.Core.Math.Maps; // For map type registration if done here
using OpenVDB.Core.Metadata; // For metadata type registration if done here

namespace OpenVDB.Core
{
    /// <summary>
    /// Global initialization and finalization for the OpenVDB library.
    /// </summary>
    public static class OpenVDB
    {
        private static bool _isInitialized = false;

        /// <summary>
        /// Initializes the OpenVDB library. This can include registering standard grid types,
        /// map types, metadata types, and setting up global configurations (like thread counts in C++).
        /// For the C# port, this is currently a placeholder for such future initializations.
        /// It should be called once before using most OpenVDB functionalities.
        /// </summary>
        public static void Initialize()
        {
            if (_isInitialized) return;

            // Placeholder for future initializations, e.g.:
            // - Registering standard grid types with GridBase.RegisterGrid()
            //   (Example: FloatGrid.Register(), Vec3SGrid.Register(), etc.)
            // - Registering standard map types with a future MapFactory/Registry.
            // - Registering standard metadata types with a future MetadataRegistry.
            // - Setting default number of threads for parallel operations (if applicable).

            // Example (if grid registration was manual and not in static ctors):
            // if (!FloatGrid.IsGridRegistered()) FloatGrid.RegisterGrid();
            // ... other grid types ...
            
            Console.WriteLine("OpenVDB.Core Initialized (placeholder)");
            _isInitialized = true;
        }

        /// <summary>
        /// Uninitializes the OpenVDB library, cleaning up any global resources.
        /// For the C# port, this is currently a placeholder.
        /// </summary>
        public static void Uninitialize()
        {
            if (!_isInitialized) return;

            // Placeholder for future uninitializations, e.g.:
            // - Clearing type registries
            // GridBase.ClearRegistry();
            // MapFactory.ClearRegistry(); // If exists
            // MetadataRegistry.ClearRegistry(); // If exists
            
            Console.WriteLine("OpenVDB.Core Uninitialized (placeholder)");
            _isInitialized = false;
        }

        public static bool IsInitialized => _isInitialized;
    }
}
