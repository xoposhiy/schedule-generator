using CommonDomain;
using CommonInfrastructure;
using CommonInfrastructure.GoogleSheetsRepository;
using static Domain2.Constants;

// ReSharper disable MemberCanBePrivate.Global

namespace Domain2;

public static class SheetToProbabilityConverter
{
    private const int StartColumn = 2;
    private static int DisciplinesCount;
    private static int RightBorder;
    private static readonly Dictionary<int, Discipline> IndexToDiscipline = new();

    public static void SetDisciplinesCount(int count)
    {
        DisciplinesCount = count;
        RightBorder = DisciplinesCount * 3 + StartColumn;
    }

    public static void ReadPriorities(GsRepository repo, ProbabilityStorage probabilityStorage,
        IEnumerable<Meeting2> meetings, string meetingsSheetName)
    {
        probabilityStorage.FillDisciplineToMaxGroups(meetings);

        var disciplines = meetings.Select(e => e.Discipline).ToHashSet();
        var prioritiesDataRaw = SheetTableReader.ReadRowsFromSheet(repo, meetingsSheetName,
            0, 0, RightBorder);
        var header = prioritiesDataRaw[0];
        for (var i = StartColumn; i < header.Count; i += 3)
            IndexToDiscipline[i] = disciplines.First(d => d.Name == header[i]);

        foreach (var row in prioritiesDataRaw.Skip(2))
        {
            var student = row[0];
            switch (student)
            {
                case "":
                {
                    break;
                }
                default:
                {
                    for (var i = StartColumn; i < RightBorder; i += 3)
                    {
                        var priority = ParseInt(row[i], UnselectedPriority);
                        // int.TryParse(row[i+1], out var priorityLk);
                        probabilityStorage.AddSubjectForStudent(student, IndexToDiscipline[i], priority);
                    }

                    break;
                }
            }
        }
    }

    public static ProbabilityStorage ReadProbabilities(GsRepository repo, string meetingsSheetName)
    {
        var probabilityStorage = new ProbabilityStorage();
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