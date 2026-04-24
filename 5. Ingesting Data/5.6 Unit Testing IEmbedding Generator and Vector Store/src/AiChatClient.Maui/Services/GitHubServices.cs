using Octokit;

namespace AiChatClient.Maui;

public class GitHubServices(GitHubClient client)
{
	readonly GitHubClient _client = client;

	public string GetBrandonMinnicksUserName() => "TheCodeTraveler";

	public async Task<string> GetUserBio(string userName)
	{
		var user = await _client.User.Get(userName);
		return user.Bio;
	}

	public async Task<int> GetRepositoryCount(string userName)
	{
		var user = await _client.User.Get(userName);
		return user.PublicRepos;
	}
}