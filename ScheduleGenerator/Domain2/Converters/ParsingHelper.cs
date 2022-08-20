using CommonInfrastructure.GoogleSheetsRepository;

namespace Domain2.Converters;

public class ParsingHelper
{
    private TermType termType;
    private bool isFinal;
    private SourcePrioritiesType sourcePrioritiesType;
    private GsRepository repo;

    public ParsingHelper(TermType termType, bool isFinal, SourcePrioritiesType sourcePrioritiesType, GsRepository repo)
    {
        this.termType = termType;
        this.isFinal = isFinal;
        this.sourcePrioritiesType = sourcePrioritiesType;
        this.repo = repo;
    }
    

    public ProbabilityStorage ParseProbabilities()
    {
        throw new NotImplementedException();
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