using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Mail;
using System.Text;
using LazyGit.Common.Enums;
using LazyGit.Common.Pocos;
using LazyGit.Services.Interfaces;
using Microsoft.Extensions.Options;
using Serilog;

namespace LazyGit.Services
{
	public class EmailService : IEmailService
	{
		private readonly string _server;
		private readonly string _account;
		private readonly string _password;
		private readonly string _port;
		private readonly string _recipients;
		private readonly string _fromEmail;

		public EmailService(IOptions<EmailConfig> emailConfig)
		{
			_server = emailConfig.Value.Server;
			_account = emailConfig.Value.Account;
			_password = emailConfig.Value.Password;
			_port = emailConfig.Value.Port;
			_recipients = emailConfig.Value.Recipients;
			_fromEmail = emailConfig.Value.FromEmail;
		}

		public void SendEmailReport(IEnumerable<TicketInformation> tickets)
		{
			if (!AreConfigValuesSet())
				return;

			try
			{
				var client = GetSmtpClient();
				tickets = tickets.ToList();

				var mail = new MailMessage
				{
					From = new MailAddress(_fromEmail),
					IsBodyHtml = true,
					Subject = $"LazyGit Report - {DateTime.Today:dd-MMMM-yy}",
					Body = GenerateReportTemplate(tickets.ToList())
				};

				var recipientsList = _recipients.Split(';').ToList();

				foreach (var recipient in recipientsList)
				{
					mail.To.Add(recipient);
				}

				client.Send(mail);

				Log.Debug("Successfully sent email report detailing the result of the following tickets: {TicketKeys}.",
					tickets.Select(t => t.JiraKey));
			}
			catch (SmtpException ex)
			{
				// Don't throw exception but do log it
				Log.Error(ex.Message);
			}
		}

		public void SendMergeConflictEmails(IEnumerable<TicketInformation> tickets)
		{
			if (!AreConfigValuesSet())
				return;

			try
			{
				var client = GetSmtpClient();

				tickets = tickets.ToList();
				var mail = new MailMessage
				{
					From = new MailAddress(_fromEmail),
					IsBodyHtml = true,
					Subject = "Please rebase the pull requests for the following ticket(s)",
					Body = GenerateMergeConflictTemplate(tickets)
				};

				var email = tickets.First().Author.Email;
				mail.To.Add(email);

				client.Send(mail);

				Log.Debug(
					"Successfully emailed: {Name} regarding the following tickets which require a manual rebase: {TicketKeys}.",
					tickets.First().Author.Name, tickets.Select(t => t.JiraKey));
			}
			catch (SmtpException ex)
			{
				// Don't throw exception but do log it
				Log.Error(ex.Message);
			}
		}

		/// <summary>
		/// If any of the config values aren't set don't send emails
		/// </summary>
		/// <returns></returns>
		private bool AreConfigValuesSet()
		{
			return !string.IsNullOrWhiteSpace(_server)
				&& !string.IsNullOrWhiteSpace(_account)
				&& !string.IsNullOrWhiteSpace(_password)
				&& !string.IsNullOrWhiteSpace(_port)
				&& !string.IsNullOrWhiteSpace(_recipients)
				&& !string.IsNullOrWhiteSpace(_fromEmail);
		}

		/// <summary>
		/// Generate email for each user who is required to manually rebase their pull request
		/// </summary>
		/// <param name="tickets"></param>
		/// <returns></returns>
		private static string GenerateMergeConflictTemplate(IEnumerable<TicketInformation> tickets)
		{
			var sb = new StringBuilder();
			sb.AppendLine("<p>LazyGit attempted to rebase the branches for the following pull requests but found there were merge conflicts. Please can you rebase them and resolve any conflicts.</p>");
			sb.AppendLine("<ul>");
			foreach (var ticket in tickets)
			{
				sb.AppendLine($"<li><a href=\"{ticket.PullRequestUrl}\">{ticket.JiraKey}</a> - {ticket.Summary}</li>");
			}
			sb.AppendLine("</ul>");

			return sb.ToString();
		}

		/// <summary>
		/// Get SMTP Client details
		/// </summary>
		/// <returns></returns>
		private SmtpClient GetSmtpClient()
		{
			int.TryParse(_port, out var portNo);

			return new SmtpClient(_server)
			{
				Credentials = new NetworkCredential(_account, _password),
				EnableSsl = true,
				Port = portNo
			};
		}

		/// <summary>
		/// Generate the email template to be sent out in the report
		/// </summary>
		/// <param name="tickets"></param>
		/// <returns></returns>
		private static string GenerateReportTemplate(IList<TicketInformation> tickets)
		{

			var mergedTickets = tickets.Where(t => t.RebaseStatus == RebaseStatus.Success)
				.ToList();
			var conflictTickets = tickets.Where(t => t.RebaseStatus == RebaseStatus.Conflicts)
				.ToList();
			var otherTickets = tickets.Where(t =>
				t.RebaseStatus != RebaseStatus.Success && t.RebaseStatus != RebaseStatus.Conflicts)
				.ToList();

			var sb = new StringBuilder();
			sb.AppendLine("<html>");
			sb.AppendLine("<h2>Merged tickets</h2>");
			if (mergedTickets.Any())
			{
				sb.AppendLine("<ul>");

				foreach (var mergedTicket in mergedTickets)
				{
					sb.AppendLine($"<li><a href=\"{mergedTicket.JiraUrl}\">{mergedTicket.JiraKey}</a> - {mergedTicket.Summary}</li>");
				}

				sb.AppendLine("</ul>");
			}
			else
			{
				sb.AppendLine("No tickets were successfully merged.");
			}

			sb.AppendLine("<h2>Tickets with merge conflicts</h2>");
			if (conflictTickets.Any())
			{
				sb.AppendLine("<ul>");

				foreach (var conflictTicket in conflictTickets)
				{
					sb.AppendLine($"<li><a href=\"{conflictTicket.JiraUrl}\">{conflictTicket.JiraKey}</a> - {conflictTicket.Summary} - Author: {conflictTicket.Author.Name}</li>");
				}

				sb.AppendLine("</ul>");
			}
			else
			{
				sb.AppendLine("No tickets have merge conflicts.");
			}

			if (otherTickets.Any())
			{
				sb.AppendLine("<h2>Other issues</h2>");
				sb.AppendLine("<ul>");

				foreach (var otherTicket in otherTickets)
				{
					var reason = GetFailureReason(otherTicket.RebaseStatus);

					sb.AppendLine($"<li><a href=\"{otherTicket.JiraUrl}\">{otherTicket.JiraKey}</a> - {otherTicket.Summary} - {reason} - Author: {otherTicket.Author.Name}</li>");
				}

				sb.AppendLine("</ul>");
			}

			sb.AppendLine("</html>");

			return sb.ToString();
		}

		/// <summary>
		/// Build string based on failure reason
		/// </summary>
		/// <param name="status"></param>
		/// <returns></returns>
		private static string GetFailureReason(RebaseStatus status)
		{
			switch (status)
			{
				case RebaseStatus.FailedToFindSourceBranch:
					return "Failed to find source branch for Jira reference.";
				case RebaseStatus.FailedToFindTargetBranch:
					return "Failed to find target branch for Jira reference.";
				default:
					return "Failed to rebase and merge pull request.";
			}
		}
	}
}
