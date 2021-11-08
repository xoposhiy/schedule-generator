﻿using System;
using System.Linq;
using Domain;
using Domain.Enums;
using NUnit.Framework;

namespace Testing.DomainTests
{
    [TestFixture]
    public class DomainExtensionsTests
    {
        [Test]
        public void GetWeekTypesThrowsWhenOddOrEvenTest()
        {
            Assert.Throws(Is.InstanceOf<ArgumentException>(), () => GetWeekTypesTest(WeekType.OddOrEven, 0));
        }

        [TestCase(WeekType.Odd, 1)]
        [TestCase(WeekType.Even, 1)]
        [TestCase(WeekType.All, 2)]
        public void GetWeekTypesTest(WeekType weekType, int partsCount)
        {
            var actualCount = weekType.GetWeekTypes().Count();
            Assert.AreEqual(partsCount, actualCount);
        }
    }
}