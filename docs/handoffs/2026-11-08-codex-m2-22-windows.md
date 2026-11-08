**Work session handoff**

Expected look of this build and why: no new game UI. Once merged, Actions / CI / Run workflow on main builds a commit-named Windows zip; v-tags also attach that zip to Releases. The player scenario picker excludes development _stub.

## PRs reviewed this session

PRs 64 and 66 have green applicable CI jobs (Windows export correctly skipped for PRs). PR 67 was just submitted; checks not yet reported. No fix requests received.

## Changed

M2-22: workflow_dispatch with export restricted to main; retain all four prerequisite checks; artifact and archive names use the tag or seven-character SHA. Only export-windows has contents:write, used by the tag-only release attachment. A pushed tag without an existing release gets a release using --verify-tag (no tag push). Export packaging skips underscore-prefixed development scenario folders. ExportSmokeCheck now rejects those folders rather than requiring _stub, and still requires the real scenarios/rules and day-10 replay. Update PLAYTEST's build steps and only this item's Status cell. Stacked on PR 67 to preserve all new smoke-test CI entries.

## Why

Tag creation was blocked in prior sessions. An on-demand main build gives a build route without a tag or release, and a tag build is directly downloadable from Releases. Development data should not ship.

## Validated how

Local solution build, 161 Sim + 97 Harness tests, validate.py (3 folders), provenance.py and game C# build pass. YAML parsed with the expected three triggers, main-only dispatch export condition and four needs. actionlint 1.7.12 validates the workflow with no findings; diff checks pass. The existing export smoke check compiles with the reversed development-scenario assertion.

## Unresolved

No local Windows runner or PowerShell is installed; Windows export/release upload is not claimed as executed. Run the main dispatch once merged to verify the Windows artifact and packaged executable; a v-tag run verifies the release attachment. No credentials, tag, release or workflow execution was changed in this session. This is a validation limit, not a Nick-only access request. No FOR-NICK entry, merge, main push, force-push or shared-branch deletion.

## Next

M2-23 seeded-disruption UI and Python cross-reference validation; then the M2-11 human playtest gate. Stop new work at five open Codex PRs per the triggering request.

Follow-up fix pass: integrated main 2c57c1c through M2-21’s keep-both merge, resolving BACKLOG.md from the upstream file plus only M2-22’s Status. Preserved all M3 rows, main’s status line, merged M2 statuses and every CI smoke entry. Combined-tree validation: 193 Sim + 97 Harness tests, solution/game builds, scenario/provenance/conflict checks and actionlint. Windows execution remains pending the first merged main dispatch.
