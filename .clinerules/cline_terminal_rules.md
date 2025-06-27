# Cline Terminal Usage Rules

> [!WARNING]
> **CRITICAL: PATH FORMATTING**
> All file paths in commands MUST use either **forward slashes (`/`)** or **properly escaped backslashes (`\\\\`)**. Using single backslashes (`\`) will cause command execution to fail.

---

## MANDATORY PRE-COMMAND CHECK
Before **ANY** `execute_command` with paths:
- [ ] Are all paths using forward slashes (`/`)?
- [ ] OR, if using backslashes, are they all properly escaped (`\\\\`)?
- [ ] Is it confirmed that **NO single backslashes (`\`)** are present in any path?

---

This document outlines important rules for terminal command usage when working with Cline.

## Rule 1: Ampersand Symbol Handling

**DO NOT** use HTML entity escaping for the ampersand symbol in terminal commands.

❌ **Incorrect:**
```bash
command1 & command2
```

✅ **Correct:**
```bash
command1 & command2
```

The ampersand symbol (`&`) should be used directly in terminal commands without any escaping.

## Rule 2: Path Separators

**Prefer forward slashes** (`/`) for paths when possible, as they are more universally compatible.

### **WINDOWS PATH EXAMPLES**
| Description | Command | Status |
| :--- | :--- | :--- |
| Single Backslash | `cd C:\Users\Name` | ❌ **WRONG** |
| Forward Slash | `cd C:/Users/Name` | ✅ **CORRECT** |
| Escaped Backslash | `cd C:\\Users\\Name` | ✅ **CORRECT** |


### Path Guidelines Summary

1.  **Use forward slashes (`/`)** whenever possible for cross-platform compatibility.
2.  Git-bash, which is often used on Windows, fully supports forward slash paths.
3.  When backslashes are **absolutely necessary**, they must be escaped as `\\`.
4.  This ensures proper path handling across different terminal environments.

## Best Practices

-   Test commands in your target environment to ensure compatibility.
-   Consider the shell environment (cmd, PowerShell, git-bash, etc.) when choosing path formats.
-   Forward slashes generally provide the best cross-platform compatibility.
