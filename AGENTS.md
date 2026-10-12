# Project maintenance

- The workspace root is the source of truth. Runtime code lives in `src/`; automated checks live in `tests/`.
- Every update must run `Build.ps1`, the existing `--self-test`, and `VerifyUI.ps1` when the UI changes. Inspect affected screenshots.
- `Build.ps1` synchronizes approved source files and documentation into `publish-export/upload-ready` and refreshes the installation ZIP. Run `Sync-Publish.ps1` again after any documentation-only edits.
- Preserve `publish-export/upload-ready/.git` and unrelated files. If that checkout has changes differing from the source being synchronized, inspect them before overwriting. Synchronization does not commit or push automatically.
- Never publish local task data, summaries, backups, `qa-data`, test output, editor settings, or personal screenshots. Only the four synthetic screenshots explicitly listed in `Sync-Publish.ps1` may be published.
- Use `UpdateInstalled.ps1` when the user requests updates to the installed app; preserve user data and verify that the new process starts.
- Keep these maintenance instructions in the publish checkout too, so later updates follow the same workflow.
