---
name: debug-flow
description: A systematic debugging workflow based on Karpathy guidelines and System.IO logging.
---

# Debug Flow Skill

Use this skill when a bug is persistent and needs structured investigation.

## Workflow Steps

1. **Hypothesis Generation**:
    - List 3-5 specific hypotheses for why the issue is occurring.
    - Reference @[/karpathy-guidelines] to ensure simplicity and surgical changes.

2. **Instrument with System.IO Logs**:
    - Add `System.IO.File.AppendAllText` logs to the relevant script.
    - Log state, method calls, and critical variables to a file (e.g., `BulletDebugLog.txt`).
    - **Crucial**: Include timestamps and unique identifiers if multiple instances are involved.

3. **User Playtest**:
    - Ask the user to play the game and perform the action that triggers the issue.

4. **Observation Collection**:
    - Wait for the user to report if the issue occurred or was solved.

5. **Log Analysis & Resolution**:
    - Read the `System.IO` log file using `view_file`.
    - Compare log data against hypotheses.
    - Implement a surgical fix based on the findings.

6. **Iterate**:
    - Repeat until the success criteria (defined in @[/karpathy-guidelines]) are met.

7. **Cleanup**:
    - Once the issue is resolved or the user confirms the fix, **you MUST remove all added `System.IO.File.AppendAllText` logs** from the codebase.
    - Delete any temporary log files (e.g., `BulletDebugLog.txt`) generated during the process.
