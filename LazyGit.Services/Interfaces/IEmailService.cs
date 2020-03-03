using System.Collections.Generic;
using LazyGit.Common.Pocos;

namespace LazyGit.Services.Interfaces
{
	public interface IEmailService
	{
		/// <summary>
		/// Send an email report containing the successfully merged and failed to merge Jira tickets and their URLs
		/// </summary>
		/// <param name="tickets"></param>
		/// <returns></returns>
		void SendEmailReport(IEnumerable<TicketInformation> tickets);

		/// <summary>
		/// Send emails to user's who are required to manually rebase branches
		/// </summary>
		/// <param name="ticket"></param>
		void SendMergeConflictEmails(IEnumerable<TicketInformation> ticket);
	}
}
