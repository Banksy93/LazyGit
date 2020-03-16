using System.Collections.Generic;

namespace LazyGit.Common.Pocos
{
	public class JiraIssueResponse
	{
		public IEnumerable<JiraTicket> Issues { get; set; }
	}

	public class JiraTicket
	{
		public string Key { get; set; }
		public Fields Fields { get; set; }
	}

	public class Fields
	{
		/// <summary>
		/// Pull request custom field
		/// </summary>
		public string CustomField_11100 { get; set; }
		public string Summary { get; set; }
		public IssueType IssueType { get; set; }
	}

	public class IssueType
	{
		public bool SubTask { get; set; }
	}
}
