# AGENTS.md

Always respond in Chinese.

---

## Table of Contents

- [Skills Reference](#skills-reference)
- [Development Workflow](#development-workflow)
- [Testing](#testing)
- [Editor Scripts](#editor-scripts)
- [Preferences](#preferences)
- [Troubleshooting](#troubleshooting)

---

## Skills Reference

Invoke the appropriate skill before starting each workflow:

| Workflow | Skill |
|----------|-------|
| unityMCP operations | `unity-mcp-orchestrator` |
| Unity Test Framework | `unity-test-framework` |

---

## Development Workflow

### Unity Operations

- **Check MCP results**: Verify both `message` and `success` fields from MCP calls
- **Retry on failure**: Adjust parameters based on returned `message` and retry
- **Batch operations**: Use `batch_execute` to reduce MCP call times
- **Editor scripts**: Place under `Assets/Editor/`, delete after one-time use
- **Tooltips**: Add `[Tooltip]` attributes to editor-configurable variables
- **Console monitoring**: Check Unity Console regularly for logs and compile errors
- **Play mode**: Ensure editor is not in Play mode before executing editor scripts
- **Save scenes**: Save scenes promptly after modifications finished.

### Version Control

- Use git for version control
- Add proper `.gitignore` before `git init`

### Plan Mode Orchestration

- Non-trivial tasks (3+ steps or architecture decisions) must enter plan mode
- If things go off track, stop and replan—don't push through
- If bugs or frustration accumulate, stop and replan
- Write detailed specs as input to reduce ambiguity
- Use numbered steps

### Task Management

1. Plan first: write plan to `tasks/todo.md` with checkboxes
2. Validate plan before starting implementation
3. Implement incrementally: check off each step as completed
4. Explain changes: provide high-level summary for each step
5. Document results: add review section in `tasks/todo.md`
6. Record lessons: update `tasks/lessons.md` after corrections

### Subagent Strategy

- Use subagents heavily to keep the main context window clean
- Outsource research, exploration, and parallel analysis to subagents
- Reduce cognitive load and separate concerns via subagents
- One task per subagent, focused execution

### Code Quality Principles

**Self-Improvement Loop:**

- After user correction: update `tasks/lessons.md` to record patterns
- Write rules for yourself to prevent the same mistakes
- Iterate improvements until error rate drops
- Verify improvements each session, apply to relevant projects

**Verification Defaults:**

- Never mark a task complete until you've proven it works
- Compare main branch to your changes when relevant
- Always be ready to push to production, verify
- Ask yourself "Would a senior engineer approve this?"
- Do assertions, logs, test suite corrections

**Elegant Code:**

- For non-trivial changes: pause and ask "Is there a more elegant way?"
- If a fix feels like a patch: "Now that I know everything, implement an elegant solution"
- Skip this step for simple obvious fixes, don't over-engineer
- Challenge your own work before committing

**Core Principles:**

- Simplicity first: each change should be as simple as possible
- Don't be lazy: find root causes, no band-aid fixes
- Minimal impact: changes should only involve what's necessary

**Autonomous Bug Fixing:**

- When you find a bug: fix it directly, don't ask for help
- Point out logs, errors, failing tests, then resolve
- User doesn't need to switch context
- Proactively fix failing CI tests

---

## Testing

### Test-Driven Development (TDD)

- Follow the **Red-Green-Refactor** cycle
- Run tests through `run_tests` in `unityMCP`
- After tests pass, refactor via code-review

### Running Tests

**Step 1: Start test run**

```json
{ "tool": "run_tests", "params": { "mode": "EditMode" } }
```

**Step 2: Get results** (use `job_id` from step 1)

```json
{ "tool": "get_test_job", "params": { "job_id": "<job_id>", "wait_timeout": 60, "include_failed_tests": true } }
```

---

## Editor Scripts

### When to Use

Use editor scripts (`Assets/Editor/`) to automate complex setups:
- Scene object references: `GameObject.Find()`
- Prefab references: `AssetDatabase.LoadAssetAtPath<T>()`

### Execution Flow

1. Add `[MenuItem("YourMenuPath")]` attribute to a static method
2. Refresh Unity: `mcp_unityMCP_refresh_unity`
3. Execute: `mcp_unityMCP_execute_menu_item`
4. Delete one-time scripts after successful execution

---

## Preferences

| Category | Preference |
|----------|------------|
| Input System | Use legacy input system |
| Git | Do not use `git worktree` |
| GameObjects | Design size and materials carefully |

---

## Troubleshooting

### Common Issues

| Issue | Solution |
|-------|----------|
| Compilation errors with unity-test-framework | Call the `unity-test-framework` skill first to configure asmdef correctly |