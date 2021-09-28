using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace Infrastructure.SheetPatterns
{
    public class SheetTableEvaluator
    {
        private List<(Regex regex, string msg)> regexMsgList;

        public SheetTableEvaluator(List<(string, string)> patternMsgDict)
        {
            regexMsgList = SetUpRegexes(patternMsgDict);
        }

        private List<(Regex, string)> SetUpRegexes(List<(string pattern, string msg)> patternMsgList)
        {
            var regexMsgDict = new List<(Regex, string)>();
            foreach (var patternMsg in patternMsgList)
            {
                var regex = new Regex(patternMsg.pattern);
                regexMsgDict.Add((regex, patternMsg.msg));
            }

            return regexMsgDict;
        }

        public List<((int, int), string)> Evaluate(List<List<string>> tableData, (int rowIndex, int colIndex) start)
        {
            var issues = new List<((int, int), string)>();
            for (var r = start.rowIndex; r < tableData.Count; r++)
            {
                var currentRow = tableData[r];
                var rowCount = currentRow.Count;
                for (var c = 0; c < rowCount; c++)
                    if (c < regexMsgList.Count)
                    {
                        var column = c + start.colIndex;
                        var cellData = currentRow[column];
                        if (!regexMsgList[c].regex.IsMatch(cellData)) issues.Add(((r, column), regexMsgList[c].msg));
                    }
            }

            return issues;
        }
    }
}