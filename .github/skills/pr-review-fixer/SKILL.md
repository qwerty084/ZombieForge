---
name: pr-review-fixer
description: >-
  Fetches GitHub Copilot PR review comments on the current branch's pull request,
  evaluates each comment for correctness and relevance to ZombieForge conventions,
  fixes valid issues in the codebase, and dismisses/resolves incorrect or irrelevant ones via the GitHub API.
  Use when asked to "fix Copilot review comments", "review the PR review", "triage review comments",
  or "dismiss wrong review comments". Supports explicit PR number input (for example: "check Copilot review on PR #12").
allowed-tools: shell
---

# PR Review Fixer

This skill triages GitHub Copilot's automated PR review for the ZombieForge repository.
It validates each comment against the project's conventions, fixes genuine issues, and dismisses noise.

---

## Step 1 — Identify the pull request

If the user already provided a PR number (for example: `PR #12`), use it directly:

```powershell
$PR = 12
```

Otherwise, run `gh pr view --json number,headRefName,url` to find the PR for the current branch.
If you are not on a feature branch or no PR exists, ask the user to specify a PR number and pass it explicitly.

Store the PR number as `$PR` for use in subsequent commands.

---

## Step 2 — Fetch Copilot review comments

Run the helper script from this skill's base directory:

```powershell
.\get-review-comments.ps1 -PrNumber $PR
```

This outputs:
- **COPILOT INLINE REVIEW COMMENTS** — line-level comments from the Copilot bot, with:
  - `comment_id` — REST API comment ID
  - `thread_node_id` — `PRRT_*` GraphQL node ID; pass directly to `resolveReviewThread`
  - `pull_request_review_id` — needed to dismiss the whole review
  - `file` and `line` — where the comment points
  - `body` — Copilot's suggestion text
  - `diff_hunk` — the surrounding diff context
- **COPILOT OVERALL REVIEWS** — high-level APPROVED / CHANGES_REQUESTED / COMMENTED reviews with `review_id`

If the script yields no results, also try fetching via the GitHub MCP `get_review_comments` tool for PR `$PR` in repo `qwerty084/ZombieForge`.

---

## Step 3 — Evaluate each comment

For every top-level inline comment, evaluate its validity **before** acting. Read the full file at
the referenced path and line range using your file-reading tools so you have full context beyond the diff hunk.

### A comment is VALID (fix it) if ALL of the following hold:
- The suggestion is technically correct — it would not introduce a bug or regression.
- The suggestion improves the code in a meaningful way (correctness, clarity, safety).
- It does not contradict ZombieForge's established conventions (see below).

### A comment is INVALID (dismiss it) if ANY of the following hold:
- It suggests `{Binding}` instead of `x:Bind` (project always uses `x:Bind`).
- It suggests putting UI-framework types (Brushes, Controls, etc.) in a Service or Model.
- It suggests bypassing `DispatcherQueue.TryEnqueue` for UI updates from background threads.
- It suggests creating an `ILoggerFactory` outside of `App.LoggerFactory.CreateLogger<T>()`.
- It suggests string interpolation inside `_logger.Log*()` calls (use structured logging instead).
- It questions or recommends changing hardcoded memory addresses (they are fixed game constants).
- It recommends moving game-specific logic (memory offsets, process names) out of `Services/Games/`.
- It recommends adding `{Binding}` mode or classic MVVM patterns that this project explicitly avoids.
- It is a generic style suggestion that conflicts with the project's actual patterns (e.g., different naming).
- It is based on a misunderstanding of the codebase (e.g., calls something an anti-pattern when it is intentional).
- It is a duplicate of another comment already being fixed.

### When UNCERTAIN:
Stop and ask the user whether to apply or dismiss the comment, showing them the comment body and the
relevant code lines.

---

## Step 4 — Fix valid comments

For each VALID comment:
1. Apply the suggested change (or your own equivalent fix) to the file.
2. Verify the fix is correct and consistent with surrounding code.
3. Resolve the review thread (see Step 5A) to mark it as addressed.

After all fixes, stage and commit:

```powershell
git add -A
git commit -m "fix: address Copilot PR review comments

Co-authored-by: Copilot <223556219+Copilot@users.noreply.github.com>"
```

---

## Step 5 — Dismiss / resolve invalid comments

### 5A — Resolve an individual comment thread (preferred for inline comments)

Use the GraphQL `resolveReviewThread` mutation with the `thread_node_id` (`PRRT_*`) from the script output — **not** the `comment_id` or `PRRC_*` comment node:

```powershell
$threadId = "<thread_node_id from script output>"  # e.g. PRRT_kwDOR6XAlc577tAT

gh api graphql -f query='
  mutation($id: ID!) {
    resolveReviewThread(input: { threadId: $id }) {
      thread { id isResolved }
    }
  }' -f id="$threadId"
```

Do this for every INVALID inline comment thread.

### 5B — Dismiss an entire CHANGES_REQUESTED review

If an overall review has `state == "CHANGES_REQUESTED"` and all its inline comments are either
fixed or dismissed as invalid, dismiss the review:

```powershell
$reviewId     = "<review_id from script output>"
$dismissMsg   = "Review dismissed: comments were addressed or were not applicable to this project."

gh api -X PUT "repos/qwerty084/ZombieForge/pulls/$PR/reviews/$reviewId/dismissals" `
  -f message="$dismissMsg"
```

Note: Only `CHANGES_REQUESTED` reviews support the dismissals endpoint.
`COMMENTED` reviews do not need to be dismissed — resolving their threads is sufficient.

---

## Step 6 — Summarize results

After completing all actions, output a clear summary:

```
PR #<n> — Review triage complete
  ✅ Fixed  : <list of files/lines fixed>
  ❌ Dismissed: <list of comments dismissed with one-line reason each>
  ❓ Skipped  : <any uncertain comments left for user decision>
```

Then push the branch if any fixes were committed:

```powershell
git push
```
