using CommonDomain;
using CommonInfrastructure;
using CommonInfrastructure.GoogleSheetsRepository;

// ReSharper disable MemberCanBePrivate.Global

namespace Domain2;

public static class SheetToProbabilityConverter
{
    
    private const int StartColumn = 2;
    private const int DisciplinesCount = 23;
    private const int RightBorder = DisciplinesCount * 3 + StartColumn;
    private static readonly Dictionary<int, Discipline> IndexToDiscipline = new();

    public static void ReadPriorities(GsRepository repo, HashSet<Discipline> disciplines, string meetingsSheetName)
    {
        var prioritiesDataRaw = SheetTableReader.ReadRowsFromSheet(repo, meetingsSheetName,
            0, 0, RightBorder);
        var header = prioritiesDataRaw[0];
        for (int i = StartColumn; i < header.Count; i+=3)
        {
            IndexToDiscipline[i] = disciplines.First(d => d.Name == header[i]);
        }
        
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
                    for (int i = StartColumn; i < RightBorder; i+=3)
                    {
                        int.TryParse(row[i], out var priority);
                        // int.TryParse(row[i+1], out var priorityLk);
                        ProbabilityStorage.AddSubjectForStudent(student, IndexToDiscipline[i], priority);
                    }
                    break;
                }
            }
        }
    }

    public static void ReadProbabilities(GsRepository repo, string meetingsSheetName)
    {
        var meetingsDataRaw = SheetTableReader.ReadRowsFromSheet(repo, meetingsSheetName, 0, 0, 10);
        foreach (var row in meetingsDataRaw.Skip(2))
        {
            int.TryParse(row[0], out var priority);
            ProbabilityStorage.PriorityWithEntranceToProbability[priority] = double.Parse(row[1]);
            ProbabilityStorage.PriorityCommonToProbability[priority] = double.Parse(row[4]);
        }
    }
}