﻿using System.Collections.Generic;
using NUnit.Framework;
using static Infrastructure.SheetPatterns.HeaderPatternCreator;
using static Infrastructure.SheetConstants;

namespace Testing.SheetPatternsTest
{
    [TestFixture]
    internal class HeaderPatternCreatorTest
    {
        [SetUp]
        [TearDown]
        public void SetUp()
        {
            TestRepository.ClearSheet(SheetName);
        }

        [Test]
        public void SetUpHeadersOnClearSheet()
        {
            TestRepository.SetUpSheetInfo();
            TestRepository.ClearCellRange(SheetName, 0, 0, 10, 10);

            var headers = new List<string>
            {
                "Преподавател", "Предмет", "Тип занятия", "Количество повторений каждого занятия",
                "Приоритеты групп, в которых назначать занятия", "Время", "Четность"
            };

            var comments = new List<string>
            {
                "Имя преподавателя",
                "Название предмета (например Матанализ)",
                "Лекция/Семинар/КомпПрактика",
                "Количество подряд идущих занятий с той же группой",
                @"через + объединяются группы в один поток. Через запятую те группы, в которые можно назначать. В разных строках можно задавать предпочтения - чем ниже, тем менее предпочтительно.

Например:

ФИИТ-101, ФИИТ-102
ФИИТ-103

означает, что хочу вести в 101 или 102, если не получится, то 103 тоже подойдет. 104 не предлагать.",
                @"варианты в строчках, по уменьшению желаемости. Список дней недели, список номеров пар.
Например:
пн-чт, 1-3 пара
пт, 3-4 пара

означает, что желательно пару не в пятницу поставить в диапазон 1-3. Если не получится, то поставить в пятницу 3 или 4.",
                "четная/нечетная (можно не указывать)"
            };

            SetUpHeaders(TestRepository, SheetName, 5, 1, headers, comments);

            var actualHeaders = TestRepository.ReadCellRange(SheetName, 5, 1, 5, 8)![0]!;

            Assert.AreEqual(headers.Count, actualHeaders.Count);
            for (var i = 0; i < headers.Count; i++) Assert.AreEqual(headers[i], actualHeaders[i]);
        }
    }
}