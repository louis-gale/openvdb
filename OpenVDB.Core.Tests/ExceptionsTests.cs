// Copyright Contributors to the OpenVDB Project
// SPDX-License-Identifier: Apache-2.0

using NUnit.Framework;
using System;
using OpenVDB; // Namespace for OpenVDBException and derived types

namespace OpenVDB.Core.Tests
{
    [TestFixture]
    public class ExceptionsTests
    {
        [Test]
        public void OpenVDBException_CanBeThrownAndCaught()
        {
            Assert.Throws<OpenVDBException>(() => throw new OpenVDBException("Test OpenVDBException"));
        }

        [Test]
        public void OpenVDBException_WithMessage_StoresMessage()
        {
            string message = "Custom OpenVDB message";
            try
            {
                throw new OpenVDBException(message);
            }
            catch (OpenVDBException ex)
            {
                Assert.AreEqual(message, ex.Message);
            }
        }

        [Test]
        public void OpenVDBException_WithInnerException_StoresInnerException()
        {
            string message = "Outer exception";
            var innerEx = new Exception("Inner exception");
            try
            {
                throw new OpenVDBException(message, innerEx);
            }
            catch (OpenVDBException ex)
            {
                Assert.AreEqual(message, ex.Message);
                Assert.AreSame(innerEx, ex.InnerException);
            }
        }

        // Test a few derived exception types
        [Test]
        public void ArithmeticErrorException_CanBeThrownAndCaught()
        {
            Assert.Throws<ArithmeticErrorException>(() => throw new ArithmeticErrorException("Test ArithmeticError"));
        }

        [Test]
        public void IndexErrorException_CanBeThrownAndCaught()
        {
            // This now derives from System.IndexOutOfRangeException
            Assert.Throws<IndexErrorException>(() => throw new IndexErrorException("Test IndexError"));
            Assert.Throws<System.IndexOutOfRangeException>(() => throw new IndexErrorException("Test System.IndexOutOfRangeException"));
        }

        [Test]
        public void IoErrorException_CanBeThrownAndCaught()
        {
            // This now derives from System.IO.IOException
            Assert.Throws<IoErrorException>(() => throw new IoErrorException("Test IoError"));
            Assert.Throws<System.IO.IOException>(() => throw new IoErrorException("Test System.IO.IOException"));
        }

        [Test]
        public void KeyErrorException_CanBeThrownAndCaught()
        {
            // This now derives from System.Collections.Generic.KeyNotFoundException
            Assert.Throws<KeyErrorException>(() => throw new KeyErrorException("Test KeyError"));
            Assert.Throws<System.Collections.Generic.KeyNotFoundException>(() => throw new KeyErrorException("Test System.Collections.Generic.KeyNotFoundException"));
        }

        [Test]
        public void NotImplementedErrorException_CanBeThrownAndCaught()
        {
            // This now derives from System.NotImplementedException
            Assert.Throws<NotImplementedErrorException>(() => throw new NotImplementedErrorException("Test NotImplementedError"));
            Assert.Throws<System.NotImplementedException>(() => throw new NotImplementedErrorException("Test System.NotImplementedException"));
        }

        [Test]
        public void ValueErrorException_CanBeThrownAndCaught()
        {
            // This now derives from System.ArgumentException
            Assert.Throws<ValueErrorException>(() => throw new ValueErrorException("Test ValueError"));
            Assert.Throws<System.ArgumentException>(() => throw new ValueErrorException("Test System.ArgumentException"));
        }

        [Test]
        public void ValueErrorException_WithParamName_StoresParamName()
        {
            string message = "Invalid parameter value";
            string paramName = "testParam";
            try
            {
                throw new ValueErrorException(message, paramName);
            }
            catch (ValueErrorException ex)
            {
                Assert.AreEqual(message, ex.Message.Split(new[] { Environment.NewLine }, StringSplitOptions.None)[0]); // Message can include param name
                Assert.AreEqual(paramName, ex.ParamName);
            }
             catch (ArgumentException ex) // Catch base if specific type isn't caught first
            {
                Assert.AreEqual(message, ex.Message.Split(new[] { Environment.NewLine }, StringSplitOptions.None)[0]);
                Assert.AreEqual(paramName, ex.ParamName);
            }
        }
    }
}
