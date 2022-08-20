using Newtonsoft.Json;

#pragma warning disable CS8618

namespace Domain2.Probabilities;

[JsonObject]
public class StudentChoicesModel
{ 
    public ulong PersonalNumber { get; set; }
    
    [JsonProperty("fullname")]
    public string FullName { get; set; }
    
    [JsonProperty("group")]
    public string GroupId { get; set; }
    
    public List<Guid> MupIds { get; set; }
}

[JsonObject]
public class StudentsDistribution
{
    public List<StudentChoicesModel> Students { get; set; }
    public Dictionary<Guid, string> MupIdToMupName { get; set; }
}