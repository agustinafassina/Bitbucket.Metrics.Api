# API Response Examples

This folder contains JSON examples of the API responses for each endpoint.

## Files

### Metrics Endpoints

- **`commits-by-person-list.json`** - Example response for `GET /api/v1/Metrics/commits-by-person`
  - Type: `List<CommitsByPersonDto>`
  - Shows multiple people with their commits grouped together

- **`commits-by-person-single.json`** - Example response for `GET /api/v1/Metrics/commits-by-person/{email}`
  - Type: `CommitsByPersonDto`
  - Shows commits for a single person

### Bitbucket Endpoints

- **`commits-list.json`** - Example response for `GET /api/v1/Bitbucket/commits`
  - Type: `List<CommitDto>`
  - Shows a list of commits with full author information

- **`commit-single.json`** - Example response for `GET /api/v1/Bitbucket/commits/{commitHash}`
  - Type: `CommitDto`
  - Shows a single commit with full details

- **`repositories-list.json`** - Example response for `GET /api/v1/Bitbucket/repositories`
  - Type: `List<RepositoryDto>`
  - Shows a list of repositories in a workspace

### Supporting Types

- **`commit-summary.json`** - Example of `CommitSummaryDto`
  - Used within `CommitsByPersonDto.Commits`
  - Simplified commit information without author details

- **`author.json`** - Example of `AuthorDto`
  - Used within `CommitDto.Author`
  - Author information with name, email, userId, and displayName

## Usage

These examples can be used for:
- API documentation
- Testing and development
- Understanding the response structure
- Frontend development and mock data
