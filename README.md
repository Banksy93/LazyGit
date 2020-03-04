# LazyGit

LazyGit is a tool designed to automate the process of rebasing and merging the pull request of any Jira tickets for a specific release that have been code reviewed, tested and are ready to be merged into a release branch.

It uses 3rd party nuget packages for [Bitbucket](https://github.com/MitjaBezensek/SharpBucket "SharpBucket") and
[Git](https://github.com/libgit2/libgit2sharp "libgit2sharp") and utilizes the latest version of Jira's API for retrieving and updating tickets.

# Process
* GET Jira tickets with a specific fix version that are ready to be merged into a release or master branch.
* If tickets are found, get their key e.g. TT-123 and attempt to find pull request information for a branch containing that key.
* Loop through the pull requests found and attempt to rebase them on top of their target branch.
* If the rebase was successful, merge the pull request, if not, log out the ticket.
* Successfully rebased tickets will then have their Jira ticket updated with a comment (defined in appsettings.json) and their status updated (transition ID defined in appsettings.json).
* If email is configured, an email report will be sent out detailing the merged tickets, tickets with merge conflicts and tickets that faced other issues.
* Inidividual emails will be sent to the author's of pull requests which encountered merge conflicts asking them to manually rebase the branch

# Assumptions
* The release branch contains the *release version* the pull request is targeting e.g. Release/1.1.1
* The source branch of the Jira tickets contain the ticket key e.g. TT-123.
* Credentials provided for Bitbucket are that of users who have merge capabilities into release / master branches.

*LazyGit will be updated to handle more configuration on Jira tickets and Branch naming conventions.*

# Configuration

For now, credentials and other configurations are set in *appsettings.json* and these are used throughout the program. They are split into sections such as *Jira, Bitbucket and Git*.

* Jira Configuration
```json
"Url": The base URL for api calls, e.g. https://test-company.atlassian.net/rest/api,
"AuthorizationHeader": "",
"FixVersion": Fix version to target in Jira, e.g. 1.1.1,
"TicketStatus": The status of the tickets to pull back from the API, e.g. 'Ready to Merge',
"Project": The project Key in Jira, e.g. TEST,
"TicketUrl": Base URL for individual tickets, e.g. https://test-company.atlassian.net/browse/TT-123,
"Username": "",
"Password": "",
"Comment": The comment to put on a successfully merged and transitioned ticket, default already set.,
"TransitionId": TransitionId to use when updating a ticket via the API.
```
* Bitbucket Configuration
```json
"Username": ,
"RepoSlug": The repo slug to be used in with SharpBucket, e.g. TestRepo,
"Secret": "",
"ClientId": "",
"PullRequestUrl": "https://bitbucket.org/user/repoSlug/pull-requests/"
```

* Git Configuration
```json
"RepositoryPath": Path to the local Git repository, e.g. C:\\Git\\Test\\Project,
"Username": "",
"Password": "",
"Email": ""
```

* Email Configuration 
```json
"Server": the server used for the smtp client,
"Account": "",
"Password": "",
"Port": port number,
"Recipients": semi-colon delimited list of emails to send report to, e.g. john@test.com;bob@test.com,
"FromEmail": email address of the sender,
"EmailDomain": email domain for merge conflict tickets, e.g. @test.com
```

* Serilog

Optional configuration if using elastic search logging for Serilog.
```json
"NodeUri": The URI to use when logging to elastic search
```
