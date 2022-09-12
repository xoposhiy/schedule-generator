using CommonDomain;
using CommonInfrastructure.GoogleSheetsRepository;
using Domain2.Probabilities;
using Newtonsoft.Json;

namespace Domain2.Converters;

public class ParsingHelper
{
    private string termType;
    private SourcePrioritiesType sourcePrioritiesType;
    private GsRepository repo;

    public ParsingHelper(TermType termType, SourcePrioritiesType sourcePrioritiesType, GsRepository repo)
    {
        this.termType = EnumHelper.GetTermString(termType);
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
        return SheetToProbabilityConverter.ReadProbabilities(repo, probabilitiesSource);
    }

    public (List<(string Student, Discipline Discipline, int priority)> priorities,
        StudentsDistribution? studentsDistribution) ReadPriorities(
        Dictionary<string, Discipline> disciplines)
    {
        var prioritiesSource = $"Приоритеты ({termType})";
        
        List<(string Student, Discipline Discipline, int priority)> priorities;
        StudentsDistribution? studentsDistribution = null;

        switch (sourcePrioritiesType)
        {
            case SourcePrioritiesType.GoogleSheet:
                priorities = SheetToProbabilityConverter
                    .ReadPriorities(repo, disciplines.Values.ToHashSet(), prioritiesSource).ToList();
                break;
            case SourcePrioritiesType.JsonFinal:
                var content = File.ReadAllText(Constants.DistributionJsonPath);
                studentsDistribution = JsonConvert.DeserializeObject<StudentsDistribution>(content);
                priorities = FillPrioritiesFromJson(studentsDistribution, disciplines);
                break;
            case SourcePrioritiesType.JsonLk:
                var jsonContent = File.ReadAllText(Constants.PrioritiesJsonPath);
                studentsDistribution = null;
                var lkPriorities = JsonConvert.DeserializeObject<StudentsPriorities>(jsonContent);
                priorities = FillPrioritiesFromLkPriorities(lkPriorities, disciplines);
                break;
            default:
                throw new ArgumentException("Not supported source type");
        }

        return (priorities, studentsDistribution);
    }

    private List<(string Student, Discipline Discipline, int priority)> FillPrioritiesFromLkPriorities(StudentsPriorities lkPriorities, Dictionary<string, Discipline> disciplines)
    {
        return lkPriorities.StudentPriorities
            .SelectMany(lk => lk.MupPriorities
                .Select(p => (lk.Name, disciplines[lkPriorities.MupIdToMupName[p.MupId]], p.Priority)))
            .ToList();
    }

    private static List<(string Student, Discipline Discipline, int priority)> FillPrioritiesFromJson(StudentsDistribution? studentsDistribution, 
        Dictionary<string, Discipline> disciplines)
    {
        List<(string Student, Discipline Discipline, int priority)> priorities;
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

public class StudentsPriorities
{
    public StudentPriorities[] StudentPriorities { get; set; }
    public Dictionary<string, string> MupIdToMupName { get; set; }
}

public class StudentPriorities
{
    public string Name { get; set; }
    public string PersonalNumber { get; set; }
    public string GroupName { get; set; }
    public List<MupPriority> MupPriorities { get; set; }
}

public class MupPriority
{
    public string MupId { get; set; }
    public int Priority { get; set; }
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