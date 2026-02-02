namespace GithubActionsAutoscaler.Configuration;

public class RepositoryFilterOptions
{
	public string AllowlistPrefix { get; set; } = "";
	public string[] Allowlist { get; set; } = [];
	public bool IsAllowlistExactMatch { get; set; } = true;
	public string DenylistPrefix { get; set; } = "";
	public string[] Denylist { get; set; } = [];
	public bool IsDenylistExactMatch { get; set; }
}
