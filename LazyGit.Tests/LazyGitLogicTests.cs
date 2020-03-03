using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using LazyGit.Common.Enums;
using LazyGit.Common.Pocos;
using LazyGit.Logic;
using LazyGit.Logic.Interfaces;
using LazyGit.Services.Interfaces;
using Moq;
using SharpBucket.V2.Pocos;
using Xunit;

namespace LazyGit.Tests
{
	public class LazyGitLogicTests
	{
		private readonly Mock<IBitbucketService> _bitbucketService;
		private readonly Mock<IJiraService> _jiraService;
		private readonly Mock<IEmailService> _emailService;
		private readonly Mock<IGitReader> _gitReader;
		private readonly Mock<IGitManager> _gitManager;

		private readonly ILazyGitLogic _lazyGitLogic;

		public LazyGitLogicTests()
		{
			_bitbucketService = new Mock<IBitbucketService>();
			_jiraService = new Mock<IJiraService>();
			_emailService = new Mock<IEmailService>();
			_gitReader = new Mock<IGitReader>();
			_gitManager = new Mock<IGitManager>();

			_lazyGitLogic = new LazyGitLogic(_jiraService.Object, _bitbucketService.Object, _gitReader.Object,
				_gitManager.Object, _emailService.Object);
		}

		[Fact]
		public async Task GetJiraTickets_ReturnTickets()
		{
			_jiraService.Setup(j => j.GetReadyToMergeJiraTicketsAsync())
				.ReturnsAsync(GetJiraIssueResponse);

			var result = await _lazyGitLogic.GetJiraTickets();

			Assert.Equal(3, result.Issues.Count());
		}

		[Fact]
		public async Task GetJiraTickets_ReturnEmptyIssueList()
		{
			_jiraService.Setup(j => j.GetReadyToMergeJiraTicketsAsync())
				.ReturnsAsync(new JiraIssueResponse
				{
					Issues = new List<JiraTicket>()
				});

			var result = await _lazyGitLogic.GetJiraTickets();

			Assert.Empty(result.Issues);
		}

		[Fact]
		public async Task GetPullRequestInfo_ReturnTicketInfoList()
		{
			var mockResponse = GetJiraIssueResponse();
			var issueKeys = mockResponse.Issues.Select(m => m.Key);

			_bitbucketService.Setup(b => b.GetPullRequestInfoForIssueKeys(It.IsAny<IEnumerable<string>>()))
				.ReturnsAsync(GetTicketInformation);

			var result = await _lazyGitLogic.GetPullRequestInfoForJiraTickets(issueKeys);

			Assert.Equal(3, result.Count());
		}

		[Fact]
		public async Task GetPullRequestInfo_ReturnEmptyList()
		{
			var mockResponse = GetJiraIssueResponse();
			var issueKeys = mockResponse.Issues.Select(m => m.Key);

			_bitbucketService.Setup(b => b.GetPullRequestInfoForIssueKeys(It.IsAny<IEnumerable<string>>()))
				.ReturnsAsync(new List<TicketInformation>());

			var result = await _lazyGitLogic.GetPullRequestInfoForJiraTickets(issueKeys);

			Assert.Empty(result);
		}

		[Fact]
		public void RebaseTicket_FailedToFindPullRequest()
		{
			_bitbucketService.Setup(b => b.GetPullRequestById(It.IsAny<int>()))
				.Returns(new PullRequest());

			var result = _lazyGitLogic.RebaseTicket(GetTicketInformation().First());

			_gitManager.Verify(gm => gm.RebaseBranch(It.IsAny<string>(), It.IsAny<string>()), Times.Never);

			Assert.Equal(RebaseStatus.FailedToFindPullRequest, result.Status);
		}

		[Fact]
		public void RebaseTicket_ReturnMergeConflict()
		{
			_bitbucketService.Setup(b => b.GetPullRequestById(It.IsAny<int>()))
				.Returns(GetPullRequest);
			_gitReader.Setup(gr => gr.IsRepositoryDirty()).Returns(false);
			_gitManager.Setup(gm => gm.RebaseBranch(It.IsAny<string>(), It.IsAny<string>()))
				.Returns(MergeConflictRebaseResult);

			var result = _lazyGitLogic.RebaseTicket(GetTicketInformation().First());

			_gitManager.Verify(gm => gm.StashPendingChanges(), Times.Never);
			_gitManager.Verify(gm => gm.PushBranch(It.IsAny<string>()), Times.Never);

			Assert.Equal(RebaseStatus.Conflicts, result.Status);
		}

		[Fact]
		public void RebaseTicket_ReturnSuccess()
		{
			_bitbucketService.Setup(b => b.GetPullRequestById(It.IsAny<int>()))
				.Returns(GetPullRequest);
			_gitReader.Setup(gr => gr.IsRepositoryDirty()).Returns(true);
			_gitManager.Setup(gm => gm.RebaseBranch(It.IsAny<string>(), It.IsAny<string>()))
				.Returns(SuccessfulRebaseResult);

			var result = _lazyGitLogic.RebaseTicket(GetTicketInformation().First());

			_gitManager.Verify(gm => gm.StashPendingChanges(), Times.Once);
			_gitManager.Verify(gm => gm.PushBranch(It.IsAny<string>()), Times.Once);

			Assert.Equal(RebaseStatus.Success, result.Status);
		}

		[Fact]
		public void MergePullRequest_ReturnTrue()
		{
			var result = _lazyGitLogic.MergePullRequest(555);

			Assert.True(result);
		}

		[Fact]
		public void MergePullRequest_ThrowException_ReturnFalse()
		{
			_bitbucketService.Setup(b => b.MergePullRequest(It.IsAny<int>()))
				.Throws(new Exception("Error on attempt to merge pull request."));

			var result = _lazyGitLogic.MergePullRequest(123);

			_gitReader.Verify(gr => gr.FetchLatest(), Times.Never);
			_bitbucketService.Verify(b => b.MergePullRequest(It.IsAny<int>()), Times.Once());

			Assert.False(result);
		}

		[Fact]
		public void MergePullRequest_ThrowException_ReAttempt_ReturnTrue()
		{
			_bitbucketService.SetupSequence(b => b.MergePullRequest(It.IsAny<int>()))
				.Throws(new Exception("Unable to fast forward, target branch has changed."))
				.Pass();

			var result = _lazyGitLogic.MergePullRequest(4321);

			_gitReader.Verify(gr => gr.FetchLatest(), Times.Once);
			_bitbucketService.Verify(b => b.MergePullRequest(It.IsAny<int>()), Times.Exactly(2));

			Assert.True(result);
		}

		[Fact]
		public void MergePullRequest_ThrowException_ReAttempt_ReturnFalse()
		{
			_bitbucketService.SetupSequence(b => b.MergePullRequest(It.IsAny<int>()))
				.Throws(new Exception("Unable to fast forward, target branch has changed."))
				.Throws(new Exception("Something went wrong."));

			var result = _lazyGitLogic.MergePullRequest(125);

			_gitReader.Verify(gr => gr.FetchLatest(), Times.Once);
			_bitbucketService.Verify(b => b.MergePullRequest(It.IsAny<int>()), Times.Exactly(2));

			Assert.False(result);
		}

		[Fact]
		public async Task UpdateJiraTicket_ReturnTrue()
		{
			_jiraService.Setup(j => j.UpdateJiraTicketAsync(It.IsAny<string>()))
				.ReturnsAsync(true);

			var result = await _lazyGitLogic.UpdateJiraTicket("TT-123");

			Assert.True(result);
		}

		[Fact]
		public async Task UpdateJiraTicket_ReturnFalse()
		{
			_jiraService.Setup(j => j.UpdateJiraTicketAsync(It.IsAny<string>()))
				.ReturnsAsync(false);

			var result = await _lazyGitLogic.UpdateJiraTicket("TT-321");

			Assert.False(result);
		}

		[Fact]
		public void DeleteBranch_ReturnTrue()
		{
			_gitManager.Setup(gm => gm.DeleteBranch(It.IsAny<string>()))
				.Returns(true);

			var result = _lazyGitLogic.DeleteBranch("TestBranch-12");

			Assert.True(result);
		}

		[Fact]
		public void DeleteBranch_ReturnFalse()
		{
			_gitManager.Setup(gm => gm.DeleteBranch(It.IsAny<string>()))
				.Returns(false);

			var result = _lazyGitLogic.DeleteBranch("TestBranch-01");

			Assert.False(result);
		}

		[Fact]
		public void ProcessEmails_DoesNotCall_SendMergeConflictEmails()
		{
			// Set ticket status' to Success to ensure MergeConflict emails aren't sent
			var tickets = GetTicketInformation();
			tickets = tickets.Select(t =>
			{
				t.RebaseStatus = RebaseStatus.Success;
				return t;
			});

			_emailService.Setup(e => e.SendEmailReport(It.IsAny<IEnumerable<TicketInformation>>()));

			_lazyGitLogic.ProcessEmails(tickets.ToList());

			_emailService.Verify(e => e.SendEmailReport(It.IsAny<IEnumerable<TicketInformation>>()),
				Times.Once);
			_emailService.Verify(e => e.SendMergeConflictEmails(It.IsAny<IEnumerable<TicketInformation>>()),
				Times.Never);
		}

		[Fact]
		public void ProcessEmails_DoesCall_SendMergeConflictEmails()
		{
			// Set ticket status' to Conflicts to ensure MergeConflict emails are sent
			var tickets = GetTicketInformation();
			tickets = tickets.Select(t =>
			{
				t.RebaseStatus = RebaseStatus.Conflicts;
				return t;
			});

			_emailService.Setup(e => e.SendEmailReport(It.IsAny<IEnumerable<TicketInformation>>()));

			_lazyGitLogic.ProcessEmails(tickets.ToList());

			_emailService.Verify(e => e.SendEmailReport(It.IsAny<IEnumerable<TicketInformation>>()),
				Times.Once);
			_emailService.Verify(e => e.SendMergeConflictEmails(It.IsAny<IEnumerable<TicketInformation>>()),
				Times.Exactly(3));
		}

		#region Model Setup

		/// <summary>
		/// Mock Jira Issue Response
		/// </summary>
		/// <returns></returns>
		private static JiraIssueResponse GetJiraIssueResponse()
		{
			return new JiraIssueResponse
			{
				Issues = new List<JiraTicket>
				{
					new JiraTicket
					{
						Fields = new Fields
						{
							Summary = "This is a test ticket with a pull request.",
							CustomField_11100 = "Pull request for test ticket."
						},
						Key = "TT-123"
					},
					new JiraTicket
					{
						Fields = new Fields
						{
							Summary = "This is another test ticket with a pull request.",
							CustomField_11100 = "Pull request for test ticket number two."
						},
						Key = "TT-321"
					},
					new JiraTicket
					{
						Fields = new Fields
						{
							Summary = "This is a test ticket without a pull request."
						},
						Key = "TT-213"
					}
				}
			};
		}

		/// <summary>
		/// Mock ticket information list
		/// </summary>
		/// <returns></returns>
		private static IEnumerable<TicketInformation> GetTicketInformation()
		{
			return new List<TicketInformation>
			{
				new TicketInformation
				{
					JiraKey = "JT-123",
					Summary = "Jira Key summary",
					PullRequestId = 1234,
					Author = new PullRequestAuthor
					{
						Name = "John Bloggs",
						Email = "john.bloggs@testdomain.com"
					},
					PullRequestUrl = "https://lazygit.bitbucket.test.com/1234"
				},
				new TicketInformation
				{
					JiraKey = "JT-321",
					Summary = "Second Jira Key summary",
					PullRequestId = 5534,
					Author = new PullRequestAuthor
					{
						Name = "John Doe",
						Email = "john.doe@testdomain.com"
					},
					PullRequestUrl = "https://lazygit.bitbucket.test.com/5534"
				},
				new TicketInformation
				{
					JiraKey = "JT-555",
					Summary = "Another test object summary",
					PullRequestId = 5555,
					Author = new PullRequestAuthor
					{
						Name = "Bob Smit",
						Email = "bob.smith@testdomain.com"
					},
					PullRequestUrl = "https://lazygit.bitbucket.test.com/5555"
				}
			};
		}

		/// <summary>
		/// Mock PullRequest model
		/// </summary>
		/// <returns></returns>
		private static PullRequest GetPullRequest()
		{
			return new PullRequest
			{
				source = new Source
				{
					branch = new SharpBucket.V2.Pocos.Branch
					{
						name = "TestBranch1"
					}
				},
				author = new User
				{
					display_name = "John Smith"
				},
				id = 555,
				description = "A test pull request",
				destination = new Source
				{
					branch = new SharpBucket.V2.Pocos.Branch
					{
						name = "master"
					}
				}
			};
		}

		/// <summary>
		/// Mock successful rebase result
		/// </summary>
		/// <returns></returns>
		private static RebaseResult SuccessfulRebaseResult()
		{
			return new RebaseResult
			{
				SourceBranch = "TestBranch",
				Status = RebaseStatus.Success
			};
		}

		/// <summary>
		/// Mock conflict rebase result
		/// </summary>
		/// <returns></returns>
		private static RebaseResult MergeConflictRebaseResult()
		{
			return new RebaseResult
			{
				SourceBranch = "TestBranch2",
				Status = RebaseStatus.Conflicts
			};
		}

		#endregion
	}
}
