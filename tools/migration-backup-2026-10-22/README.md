# Migration backup execution records

These are task-specific scripts used for the2026-10-22 laptop migration, retained so the next Codex can inspect/reproduce the backup and its checks. They contain historical C:/D: paths, source lists and a release ID; do not run them blindly on a new machine. Several initial approaches were superseded; the final archive index and FINAL-BACKUP-STATUS.json on D: identify authoritative results.

For normal restoration use skills/rungproof-playtest-handoff/SKILL.md and its portable restore scripts. Copy verification and archive-building scripts use Python3.14; the restore scripts use Windows PowerShell. The temporary PyYAML validation dependency was stored on D:, not vendored here. The GitHub helper retrieves an existing mnwinter login into its process environment; no credential is embedded in these files and it does not switch the global account.

The primary complete D: folder copy is independent of public archive deduplication. Raw private session state, application userdata and the chat transcript are local-only. See public archive scan/redaction records for the exact public scope. All temporary content blobs represent already verified source files; the final archive includes the complete path/hash map.
