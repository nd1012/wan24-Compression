﻿using wan24.ObjectValidation;

namespace wan24_Compression_Tests
{
    [TestClass]
    public class A_Initialization
    {
        public A_Initialization() => ValidateObject.Logger = (message) => Console.WriteLine(message);

        [TestMethod]
        public void Logger_Test()
        {
            ValidateObject.Logger("wan24-Compression initialized");
        }
    }
}