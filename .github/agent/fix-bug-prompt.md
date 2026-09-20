You are the on-call engineer for this repository. A bug was reported in Azure DevOps and your
job is to fix it and open a pull request.

## The bug

The work item is saved at `.github/agent/workitem.json`. Read it first. The title, repro steps
(`Microsoft.VSTS.TCM.ReproSteps`, HTML) and the stack trace in it tell you which endpoint failed,
the exception type, and the file and line that threw. The work item id is in the `id` field.

## What to do

1. Read `CLAUDE.md` for the build, test and PR conventions.
2. Reproduce the failure with a failing unit test in `tests/Dashboard.Tests` before changing any
   production code. Name the test after the behaviour, not the bug number.
3. Fix the root cause in `src/`, not the symptom. Prefer the smallest change that makes the
   endpoint return a sensible result for the input in the report (an empty result, a 404, a
   clamped value) over swallowing exceptions. Do not remove or weaken the Chaos scenarios.
4. Run `dotnet build Dashboard.slnx -c Release` and `dotnet test Dashboard.slnx -c Release`.
   Everything must pass.
5. Create a branch named `fix/ab-<work item id>-<short-slug>`, commit with a Conventional Commit
   message that starts with `fix:` and ends with `AB#<id>`, push it, and open a pull request with
   `gh pr create`. The PR title must start with `fix:` (it decides the version bump) and the body
   must contain the line `Fixes AB#<id>` so Azure Boards links and resolves the work item, plus a
   short explanation of the root cause and the test you added.
6. Post the PR URL back to the work item:
   `scripts/ado-update-workitem.sh <id> --comment "Fix proposed: <pr url>"`.

If you cannot find the root cause or the fix would require a design decision, do not guess: open no
PR, and instead run `scripts/ado-update-workitem.sh <id> --comment "<what you found and why you stopped>" --state New`.
