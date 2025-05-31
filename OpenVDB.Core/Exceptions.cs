// Copyright Contributors to the OpenVDB Project
// SPDX-License-Identifier: Apache-2.0

using System;
using System.IO; // For IOException
using System.Collections.Generic; // For KeyNotFoundException

namespace OpenVDB
{
    /// <summary>
    /// Base class for OpenVDB-specific exceptions.
    /// </summary>
    [Serializable]
    public class OpenVDBException : Exception
    {
        public OpenVDBException() { }
        public OpenVDBException(string message) : base(message) { }
        public OpenVDBException(string message, Exception inner) : base(message, inner) { }
        protected OpenVDBException(
          System.Runtime.Serialization.SerializationInfo info,
          System.Runtime.Serialization.StreamingContext context) : base(info, context) { }
    }

    [Serializable]
    public class ArithmeticErrorException : OpenVDBException
    {
        public ArithmeticErrorException() : base("Error in arithmetic operation.") { }
        public ArithmeticErrorException(string message) : base(message) { }
        public ArithmeticErrorException(string message, Exception inner) : base(message, inner) { }
        protected ArithmeticErrorException(
          System.Runtime.Serialization.SerializationInfo info,
          System.Runtime.Serialization.StreamingContext context) : base(info, context) { }
    }

    [Serializable]
    public class IndexErrorException : IndexOutOfRangeException // System.IndexOutOfRangeException is a good match
    {
        public IndexErrorException() : base("Index out of range.") { }
        public IndexErrorException(string message) : base(message) { }
        public IndexErrorException(string message, Exception inner) : base(message, inner) { }
        protected IndexErrorException(
          System.Runtime.Serialization.SerializationInfo info,
          System.Runtime.Serialization.StreamingContext context) : base(info, context) { }
    }

    [Serializable]
    public class IoErrorException : IOException // System.IO.IOException is a good match
    {
        public IoErrorException() : base("I/O error occurred.") { }
        public IoErrorException(string message) : base(message) { }
        public IoErrorException(string message, Exception inner) : base(message, inner) { }
        protected IoErrorException(
          System.Runtime.Serialization.SerializationInfo info,
          System.Runtime.Serialization.StreamingContext context) : base(info, context) { }
    }

    [Serializable]
    public class KeyErrorException : KeyNotFoundException // System.Collections.Generic.KeyNotFoundException is a good match
    {
        public KeyErrorException() : base("Key not found.") { }
        public KeyErrorException(string message) : base(message) { }
        public KeyErrorException(string message, Exception inner) : base(message, inner) { }
        protected KeyErrorException(
          System.Runtime.Serialization.SerializationInfo info,
          System.Runtime.Serialization.StreamingContext context) : base(info, context) { }
    }

    [Serializable]
    public class LookupErrorException : OpenVDBException
    {
        public LookupErrorException() : base("Lookup error.") { }
        public LookupErrorException(string message) : base(message) { }
        public LookupErrorException(string message, Exception inner) : base(message, inner) { }
        protected LookupErrorException(
          System.Runtime.Serialization.SerializationInfo info,
          System.Runtime.Serialization.StreamingContext context) : base(info, context) { }
    }

    [Serializable]
    public class NotImplementedErrorException : NotImplementedException // System.NotImplementedException is a direct match
    {
        public NotImplementedErrorException() : base("Functionality not implemented.") { }
        public NotImplementedErrorException(string message) : base(message) { }
        public NotImplementedErrorException(string message, Exception inner) : base(message, inner) { }
        protected NotImplementedErrorException(
          System.Runtime.Serialization.SerializationInfo info,
          System.Runtime.Serialization.StreamingContext context) : base(info, context) { }
    }

    [Serializable]
    public class ReferenceErrorException : OpenVDBException
    {
        public ReferenceErrorException() : base("Reference error.") { }
        public ReferenceErrorException(string message) : base(message) { }
        public ReferenceErrorException(string message, Exception inner) : base(message, inner) { }
        protected ReferenceErrorException(
          System.Runtime.Serialization.SerializationInfo info,
          System.Runtime.Serialization.StreamingContext context) : base(info, context) { }
    }

    [Serializable]
    public class RuntimeErrorException : OpenVDBException
    {
        public RuntimeErrorException() : base("Runtime error occurred.") { }
        public RuntimeErrorException(string message) : base(message) { }
        public RuntimeErrorException(string message, Exception inner) : base(message, inner) { }
        protected RuntimeErrorException(
          System.Runtime.Serialization.SerializationInfo info,
          System.Runtime.Serialization.StreamingContext context) : base(info, context) { }
    }

    [Serializable]
    public class TypeErrorException : InvalidCastException // System.InvalidCastException can be a good match for type errors
    {
        public TypeErrorException() : base("Type error occurred.") { }
        public TypeErrorException(string message) : base(message) { }
        public TypeErrorException(string message, Exception inner) : base(message, inner) { }
        protected TypeErrorException(
          System.Runtime.Serialization.SerializationInfo info,
          System.Runtime.Serialization.StreamingContext context) : base(info, context) { }
    }

    [Serializable]
    public class ValueErrorException : ArgumentException // System.ArgumentException is a good match for invalid values
    {
        public ValueErrorException() : base("Invalid value.") { }
        public ValueErrorException(string message) : base(message) { }
        public ValueErrorException(string message, Exception inner) : base(message, inner) { }
        // Consider adding constructors with paramName if it's often used for specific arguments
        public ValueErrorException(string message, string paramName) : base(message, paramName) { }
        public ValueErrorException(string message, string paramName, Exception inner) : base(message, paramName, inner) { }
        protected ValueErrorException(
          System.Runtime.Serialization.SerializationInfo info,
          System.Runtime.Serialization.StreamingContext context) : base(info, context) { }
    }
}
