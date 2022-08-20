using CommonDomain;
using CommonInfrastructure;
using CommonInfrastructure.GoogleSheetsRepository;
using static Domain2.Constants;

namespace Domain2.Converters;

public static class SheetToProbabilityConverter
{
    private const int StartColumn = 2;
    private static int disciplinesCount;
    private static int rightBorder;

    public static void SetDisciplinesCount(int count)
    {
        disciplinesCount = count;
        rightBorder = disciplinesCount * 3 + StartColumn;
    }

    public static IEnumerable<(string Student, Discipline Discipline, int priority)> ReadPriorities(
        GsRepository repo,
        ISet<Discipline> disciplines,
        string meetingsSheetName)
    {
        var prioritiesDataRaw = SheetTableReader.ReadRowsFromSheet(repo, meetingsSheetName,
            0, 0, rightBorder);
        var header = prioritiesDataRaw[0];
        var indexToDiscipline = new Dictionary<int, Discipline>();
        for (var i = StartColumn; i < header.Count; i += 3)
            indexToDiscipline[i] = disciplines.First(d => d.Name == header[i]);

        foreach (var row in prioritiesDataRaw.Skip(2))
        {
            var student = row[0];
            if (string.IsNullOrEmpty(student)) continue;
            for (var i = StartColumn; i < rightBorder; i += 3)
            {
                var priority = ParseInt(row[i], UnselectedPriority);
                var officialPriority = ParseInt(row[i + 1], UnselectedPriority);
                yield return (student, indexToDiscipline[i], priority);
            }
        }
    }

    public static ProbabilityStorage ReadProbabilities(GsRepository repo, string meetingsSheetName, bool isFinal)
    {
        var probabilityStorage = new ProbabilityStorage(isFinal);
        var meetingsDataRaw = SheetTableReader.ReadRowsFromSheet(repo, meetingsSheetName, 0, 0, 10);
        foreach (var row in meetingsDataRaw.Skip(2))
        {
            int.TryParse(row[0], out var priority);
            probabilityStorage.PriorityWithEntranceToProbability[priority] = double.Parse(row[1]);
            probabilityStorage.PriorityToProbability[priority] = double.Parse(row[4]);
        }

        return probabilityStorage;
    }

    private static int ParseInt(string raw, int defaultValue)
    {
        return int.TryParse(raw, out var result) ? result : defaultValue;
    }
}