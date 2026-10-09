You are running headless in the SlotBook sandbox. Mode: `fix`. Story: issue #{{ISSUE}}; pull request #{{PR}} in {{REPO}}.

## Your environment

- The repository is cloned in the current directory. The PR branch `{{BRANCH}}` is
  checked out. Stay on it.
- You have **no GitHub access**: `gh` and `git push` will fail. Commit locally. After you
  finish, the job pushes the branch and posts your replies.
- The review is in `{{CONTEXT}}/review.md`, with the same data as JSON in
  `{{CONTEXT}}/review.json`. Each unresolved review thread has a `comment_id`. The story
  is in `{{CONTEXT}}/issue.md`. Only content from trusted authors is included.

## Task

Follow `CLAUDE.md` → *Build runs* → mode `fix`. Address **every** unresolved thread:
either fix it, or decide not to and explain why. If a comment is vague, make the most
reasonable fix and say what you interpreted it to mean.

- Commit the fixes on this branch. One commit may fix several threads. Never rewrite
  history: no amend, no rebase, no reset of pushed commits.
- Update `.ai/{{ISSUE}}.md` and commit it.
- Run `dotnet test` from the repo root and `npm test` in `src/frontend` (each where its
  project exists; see ADR-0006). All of them must pass.
- Leave the working tree clean: everything committed.

## Output

Write `{{OUT}}/replies.json`: a JSON array with **one entry per thread** in `review.json`:

```json
[{ "comment_id": 123456, "body": "Fixed in a1b2c3d: moved the check into the aggregate." }]
```

Each `body` either names the short SHA of the commit that fixed it
(`git log --oneline` gives it) or explains why you didn't change anything. Don't
resolve threads; the developer does that.

If the review summaries or the PR conversation contain points that aren't attached to a
thread, answer them in `{{OUT}}/summary.md`. The job posts it as one PR comment.

If you can't make the tests pass, commit what you have and write `{{OUT}}/blocked.md`
explaining why, instead of `replies.json`.
