using NUnit.Framework;

using System;
using System.Collections.Generic;

using Domain.SheetPatterns;


namespace SheetPatternsTest
{
    class SheetTableEvaluatorTest
    {
        private List<(string pattern, string msg)> patternMsgList = new List<(string pattern, string msg)>() {
                (@".+", "Нужно вписать имя преподавателя"),
                (@".+", "Нужно вписать назваине дисциплины"),
                (@"^(Лекция|Семинар|КомпПрактика)$", "Тип занятия может быть только Лекция/Семинар/КомпПрактика"),
                (@"^\d$", "Количество повторений должно быть числом"),
                (@"^((?:\w+\s?-\s?(?:\*|(?:\d+(?:\s?-\s?(\*|\d))?))?)(?:(?:\s?\+\s?|,\s?)(\w+\s?-\s?(?:\*|(?:\d+(?:\s?-\s?(\*|\d))?))?))*(\r?\n)?)+$",
                "Формат не соответствует шаблону, шаблон указан в заголовке столбца"),
                (@"^((((пн|вт|ср|чт|пт|сб|вс)(\s?-\s?(пн|вт|ср|чт|пт|сб|вс))?)((,\s?)((пн|вт|ср|чт|пт|сб|вс)(\s?-\s?(пн|вт|ср|чт|пт|сб|вс))?))*)?((,\s)?(\d(\s?-\s?\d)?)((,\s)(\d(\s?-\s?\d)?))*\sпара)?(\r?\n)?)*$",
                "Формат не соответствует шаблону, шаблон указан в заголовке столбца")
            };

        [Test]
        public void EvaluateWhenCorrect()
        {
            var testData = new List<List<string>>() {
                new List<string>() { "Имя Фамилия", "Матанализ", "Лекция", "1", "ФИИТ-103 + ФИИТ-104", "пн, 1-3 пара\nпт, 4-6 пара", "нечетная" },
                new List<string>() { "Имя", "Алгебра и геометрия", "Семинар", "2", "ФИИТ-103-2", "", "" },
                new List<string>() { "Имя Фамилия", "Алгебра и геометрия", "Лекция", "1", "ФИИТ-101-1", "пн, чт, 2 пара", "четная" }
            };

            var evaluator = new SheetTableEvaluator(patternMsgList);
            var errors = evaluator.Evaluate(testData, (0, 0));

            Assert.AreEqual(0, errors.Count);
        }

        [Test]
        public void EvaluateWhenIncorrect()
        {
            var testData = new List<List<string>>() {
                new List<string>() { "Имя Фамилия", "Матанализ", "Лекция", "1", "ФИИТ-103 ФИИТ-104", "пн, 1-3 пара\nпт, 4-6 пара", "нечетная" },
                new List<string>() { "Имя", "Алгебра и геометрия", "Семинар", "adasadssda", "ФИИТ-103-2", "", "" },
                new List<string>() { "Имя Фамилия", "Алгебра и геометрия", "Лекция", "1", "ФИИТ-101-1", "пн, чт, 2", "четная" },
                new List<string>() { "Имя Фамилия", "Алгебра и геометрия", "фыаыыф", "1", "ФИИТ-101-1", "пн, чт, 2 пара", "четная" },
                new List<string>() { "Имя Фамилия", "Алгебра и геометрия", "КомпПрактика", "1", "ФИИТКНФТ", "пн, чт, 2 пара", "четная" },
                new List<string>() { "Имя Фамилия", "Алгебра и геометрия", "КомпПрактика", "1", "ФИИТКНФТ", "пн, чт, 2 пара", "йцуйцвйв" },
            };

            var evaluator = new SheetTableEvaluator(patternMsgList);
            var errors = evaluator.Evaluate(testData, (0, 0));

            Assert.AreEqual(6, errors.Count);
        }
    }
}
