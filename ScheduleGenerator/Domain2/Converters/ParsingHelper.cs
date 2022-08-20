using CommonDomain;
using CommonInfrastructure.GoogleSheetsRepository;
using Domain2.Probabilities;
using Newtonsoft.Json;

namespace Domain2.Converters;

public class ParsingHelper
{
    private string termType;
    private bool isFinal;
    private SourcePrioritiesType sourcePrioritiesType;
    private GsRepository repo;

    public ParsingHelper(TermType termType, bool isFinal, SourcePrioritiesType sourcePrioritiesType, GsRepository repo)
    {
        this.termType = EnumHelper.GetTermString(termType);
        this.isFinal = isFinal;
        this.sourcePrioritiesType = sourcePrioritiesType;
        this.repo = repo;
    }

    public List<Meeting2> ReadMeetings(string meetingsSource)
    {
        return SheetToRequisitionConverter.ReadMeetings(repo, meetingsSource);
    }

    public ProbabilityStorage ReadProbabilities()
    {
        var probabilitiesSource = $"Вероятности ({termType})";
        return SheetToProbabilityConverter.ReadProbabilities(repo, probabilitiesSource, isFinal);
    }

    public List<(string Student, Discipline Discipline, int priority)> ReadPriorities(
        Dictionary<string, Discipline> disciplines)
    {
        var prioritiesSource = $"Приоритеты ({termType}";

        List<(string Student, Discipline Discipline, int priority)> priorities;
        switch (sourcePrioritiesType)
        {
            case SourcePrioritiesType.GoogleSheet:
                priorities = SheetToProbabilityConverter
                    .ReadPriorities(repo, disciplines.Values.ToHashSet(), prioritiesSource).ToList();
                break;
            case SourcePrioritiesType.JsonFinal:
                priorities = FillPrioritiesFromJson(disciplines);
                break;
            case SourcePrioritiesType.JsonLk:
                throw new NotImplementedException();
            default:
                throw new ArgumentException("Not supported source type");
        }

        return priorities;
    }

    private static List<(string Student, Discipline Discipline, int priority)> FillPrioritiesFromJson(Dictionary<string, Discipline> disciplines)
    {
        List<(string Student, Discipline Discipline, int priority)> priorities;
        var content = File.ReadAllText(Constants.DistributionJsonPath);
        var studentsDistribution = JsonConvert.DeserializeObject<StudentsDistribution>(content);
        priorities = new List<(string Student, Discipline Discipline, int priority)>();
        foreach (var studentChoices in studentsDistribution!.Students)
        {
            foreach (var mupId in studentChoices.MupIds)
            {
                var disciplineName = studentsDistribution.MupIdToMupName[mupId];
                if (disciplines.TryGetValue(disciplineName, out var discipline))
                    priorities.Add((studentChoices.FullName, discipline, 1));
                else
                    throw new ArgumentException($"Unknown discipline name: {disciplineName}");
            }
        }
        return priorities;
    }
}

public enum SourcePrioritiesType
{
    GoogleSheet = 0,
    JsonLk = 1,
    JsonFinal = 2
}

public enum TermType
{
    Autumn = 0,
    Spring = 1
}


public static class EnumHelper
{
    public static string GetTermString(TermType termType)
    {
        return termType switch
        {
            TermType.Autumn => "Осень",
            TermType.Spring => "Весна",
            _ => throw new FormatException($"Некорректный тип семестра: {termType}")
        };
    }
}