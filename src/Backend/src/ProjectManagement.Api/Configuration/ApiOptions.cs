namespace ProjectManagement.Api.Configuration;

/// <summary>
/// Configuration options for the API.
/// </summary>
public class ApiOptions
{
    public const string SectionName = "Api";
    public string Title { get; set; } = "Project Management API";
    public string Version { get; set; } = "v1";
    public bool EnableSwagger { get; set; } = true;
}
