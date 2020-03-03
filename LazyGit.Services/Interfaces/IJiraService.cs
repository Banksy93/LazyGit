using System.Threading.Tasks;
using LazyGit.Common.Pocos;

namespace LazyGit.Services.Interfaces
{
	public interface IJiraService
	{
		/// <summary>
		/// Get a list of Jira tickets for a release which have a status of ready to merge
		/// If includeRelease is set to false, search for all tickets ready to be merged in case some don't have a release number
		/// </summary>
		/// <returns></returns>
		Task<JiraIssueResponse> GetReadyToMergeJiraTicketsAsync();

		/// <summary>
		/// Update the status to 'Done' and add a comment to the Jira ticket once it is processed
		/// </summary>
		/// <param name="issueKey"></param>
		/// <returns></returns>
		Task<bool> UpdateJiraTicketAsync(string issueKey);
	}
}
