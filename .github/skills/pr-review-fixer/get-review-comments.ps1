# get-review-comments.ps1
# Fetches all Copilot bot review comments and overall reviews for a pull request.
# Usage: .\get-review-comments.ps1 [-PrNumber <number>] [-Repo <owner/repo>]
# If -PrNumber is omitted, the script auto-detects the PR from the current branch.
# You can also provide PR_NUMBER in the environment to force a specific PR.

param(
    [Alias("PR", "PullRequest", "PullRequestNumber")]
    [int]$PrNumber = 0,
    [string]$Repo = "qwerty084/ZombieForge"
)

if ($PrNumber -eq 0 -and -not [string]::IsNullOrWhiteSpace($env:PR_NUMBER)) {
    $parsedPr = 0
    if (-not [int]::TryParse($env:PR_NUMBER, [ref]$parsedPr) -or $parsedPr -le 0) {
        Write-Error "Invalid PR_NUMBER value '$($env:PR_NUMBER)'. It must be a positive integer."
        exit 1
    }
    $PrNumber = $parsedPr
    Write-Host "Using PR #$PrNumber from PR_NUMBER environment variable" -ForegroundColor Cyan
}

if ($PrNumber -eq 0) {
    $prJson = gh pr view --json number 2>$null
    if ($LASTEXITCODE -ne 0) {
        Write-Error "No PR found for the current branch. Specify -PrNumber explicitly."
        exit 1
    }
    $PrNumber = ($prJson | ConvertFrom-Json).number
    Write-Host "Auto-detected PR #$PrNumber" -ForegroundColor Cyan
}

if ($PrNumber -le 0) {
    Write-Error "Invalid PR number '$PrNumber'. It must be a positive integer."
    exit 1
}

# Inline review comments (attached to specific lines/diffs)
$allComments = gh api "repos/$Repo/pulls/$PrNumber/comments" --paginate | ConvertFrom-Json

# Overall PR reviews (APPROVED / CHANGES_REQUESTED / COMMENTED)
$allReviews = gh api "repos/$Repo/pulls/$PrNumber/reviews" --paginate | ConvertFrom-Json

# Fetch review thread node IDs via GraphQL.
# REST comments have PRRC_* node_ids (comment nodes); resolveReviewThread requires
# PRRT_* thread node IDs. Build a map from comment databaseId -> thread node_id.
$repoOwner = ($Repo -split '/')[0]
$repoName  = ($Repo -split '/')[1]
$threadGql = @"
query {
  repository(owner: "$repoOwner", name: "$repoName") {
    pullRequest(number: $PrNumber) {
      reviewThreads(first: 100) {
        nodes {
          id
          isResolved
          comments(first: 1) { nodes { databaseId } }
        }
      }
    }
  }
}
"@
$threadData = gh api graphql -f query="$threadGql" | ConvertFrom-Json
$threadMap  = @{}
foreach ($t in $threadData.data.repository.pullRequest.reviewThreads.nodes) {
    $dbId = $t.comments.nodes[0].databaseId
    $threadMap[$dbId] = $t.id   # PRRT_* node — pass directly to resolveReviewThread
}

# Filter to Copilot bot only
$copilotComments = $allComments | Where-Object {
    $_.user.type -eq "Bot" -and $_.user.login -match "copilot"
}

$copilotReviews = $allReviews | Where-Object {
    $_.user.type -eq "Bot" -and $_.user.login -match "copilot"
}

Write-Host "`n=== COPILOT INLINE REVIEW COMMENTS ===" -ForegroundColor Yellow
Write-Host "Total: $($copilotComments.Count) (showing top-level only below)" -ForegroundColor DarkYellow
Write-Host ""

# Only emit top-level comments (not replies) — replies share a thread
foreach ($c in ($copilotComments | Where-Object { -not $_.in_reply_to_id })) {
    $line = if ($c.line) { $c.line } else { $c.original_line }
    [PSCustomObject]@{
        comment_id             = $c.id
        thread_node_id         = $threadMap[$c.id]    # PRRT_* — use this for resolveReviewThread
        pull_request_review_id = $c.pull_request_review_id  # needed to dismiss the whole review
        file                   = $c.path
        line                   = $line
        side                   = $c.side              # LEFT or RIGHT
        body                   = $c.body
        diff_hunk              = $c.diff_hunk
        html_url               = $c.html_url
    } | ConvertTo-Json -Depth 5
    Write-Host "---"
}

Write-Host "`n=== COPILOT OVERALL REVIEWS ===" -ForegroundColor Yellow
Write-Host "Total: $($copilotReviews.Count)" -ForegroundColor DarkYellow

foreach ($r in $copilotReviews) {
    [PSCustomObject]@{
        review_id = $r.id
        state     = $r.state    # APPROVED | CHANGES_REQUESTED | COMMENTED
        body      = $r.body
        html_url  = $r.html_url
    } | ConvertTo-Json -Depth 3
    Write-Host "---"
}
