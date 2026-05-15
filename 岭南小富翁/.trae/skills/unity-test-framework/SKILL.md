---
name: unity-test-framework
description: Use when creating or configuring Unity Test Framework tests, including asmdef setup, EditMode/PlayMode configuration, and resolving test assembly reference issues
---

# Unity Test Framework

## Overview

Unity Test Framework (UTF) requires specific assembly definition (asmdef) configuration to work correctly. Tests are organized into EditMode (Editor-only) and PlayMode (runtime) categories, each requiring different asmdef settings.

## When to Use

- Creating new test assemblies
- Tests cannot find main code classes (CS0246 errors)
- EditMode tests not appearing in Test Runner
- Compilation errors related to NUnit or TestRunner
- Moving tests between EditMode and PlayMode

## Quick Reference

### Test Type Configuration

| Setting | EditMode | PlayMode |
|---------|----------|----------|
| `includePlatforms` | `["Editor"]` | `[]` |
| `testAssembly` | `true` | `true` |
| `autoReferenced` | `false` | `false` |
| `overrideReferences` | `true` | `true` |

### Required References

All test assemblies need:
```json
"references": [
    "YourMainScripts",
    "UnityEngine.TestRunner",
    "UnityEditor.TestRunner"
],
"precompiledReferences": [
    "nunit.framework.dll"
]
```

## Core Pattern

### 1. Create Main Code Assembly

Create asmdef for your main scripts first:

**Assets/Scripts/MainScripts.asmdef**
```json
{
    "name": "MainScripts",
    "rootNamespace": "",
    "references": [],
    "includePlatforms": [],
    "excludePlatforms": [],
    "allowUnsafeCode": false,
    "overrideReferences": false,
    "precompiledReferences": [],
    "autoReferenced": true,
    "defineConstraints": [],
    "versionDefines": [],
    "noEngineReferences": false
}
```

### 2. EditMode Test Assembly

**Assets/Tests/EditMode/MyEditModeTests.asmdef**
```json
{
    "name": "MyEditModeTests",
    "rootNamespace": "",
    "references": [
        "MainScripts",
        "UnityEngine.TestRunner",
        "UnityEditor.TestRunner"
    ],
    "includePlatforms": [
        "Editor"
    ],
    "excludePlatforms": [],
    "allowUnsafeCode": false,
    "overrideReferences": true,
    "precompiledReferences": [
        "nunit.framework.dll"
    ],
    "autoReferenced": false,
    "defineConstraints": [],
    "versionDefines": [],
    "noEngineReferences": false,
    "testAssembly": true
}
```

### 3. PlayMode Test Assembly

**Assets/Tests/PlayMode/MyPlayModeTests.asmdef**
```json
{
    "name": "MyPlayModeTests",
    "rootNamespace": "",
    "references": [
        "MainScripts",
        "UnityEngine.TestRunner",
        "UnityEditor.TestRunner"
    ],
    "includePlatforms": [],
    "excludePlatforms": [],
    "allowUnsafeCode": false,
    "overrideReferences": true,
    "precompiledReferences": [
        "nunit.framework.dll"
    ],
    "autoReferenced": false,
    "defineConstraints": [],
    "versionDefines": [],
    "noEngineReferences": false,
    "testAssembly": true
}
```

## Project Structure

```
Assets/
├── Scripts/
│   ├── MainScripts.asmdef      # Main code assembly
│   └── (your folds and scripts)
└── Tests/
    ├── EditMode/
    │   ├── MyEditModeTests.asmdef
    │   └── MyEditModeTests.cs
    └── PlayMode/
        ├── MyPlayModeTests.asmdef
        └── MyPlayModeTests.cs
```

## Common Mistakes

### Mistake 1: Using Assembly-CSharp

**Wrong:** Tests reference `"Assembly-CSharp"`
```json
"references": ["Assembly-CSharp"]  // ❌ Don't use this
```

**Right:** Create dedicated asmdef for main scripts
```json
"references": ["MainScripts"]  // ✅ Reference your asmdef
```

### Mistake 2: Missing testAssembly Flag

**Wrong:** No `testAssembly` field
```json
{
    "name": "MyTests"
    // Missing testAssembly: true
}
```

**Right:** Explicitly mark as test assembly
```json
{
    "name": "MyTests",
    "testAssembly": true  // ✅ Required
}
```

### Mistake 3: Wrong Platform for EditMode

**Wrong:** EditMode with no platform restriction
```json
"includePlatforms": []  // ❌ EditMode needs "Editor"
```

**Right:** EditMode must include Editor platform
```json
"includePlatforms": ["Editor"]  // ✅ Required for EditMode
```

### Mistake 4: autoReferenced Not Set

**Wrong:** Using default autoReferenced
```json
"autoReferenced": true  // ❌ Should be false for tests
```

**Right:** Disable auto-reference to control dependencies
```json
"autoReferenced": false  // ✅ Required
```

## Troubleshooting

### Tests Not Appearing in Test Runner

1. Check `testAssembly: true` is set
2. For EditMode: verify `includePlatforms: ["Editor"]`
3. Delete Library/ScriptAssemblies/* and refresh
4. Reopen Test Runner window

### CS0246: Type Not Found

1. Main code needs its own asmdef (not Assembly-CSharp)
2. Tests reference that asmdef name
3. Both assemblies compiled successfully

### NUnit Not Found

Ensure all test asmdef files include:
```json
"overrideReferences": true,
"precompiledReferences": ["nunit.framework.dll"],
"autoReferenced": false
```

## Refresh After Changes

After modifying asmdef files:

```bash
# Force Unity to recompile
# In Unity: Assets → Reimport All
# Or delete assemblies and let Unity regenerate:
rm -rf Library/ScriptAssemblies/*.dll
rm -rf Library/ScriptAssemblies/*.pdb
```

Then in Unity: `Ctrl+R` or `Assets → Refresh`
