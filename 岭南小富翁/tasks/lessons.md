# Lessons

## 编码：Edit/Write 会破坏含中文 .cs 文件的 UTF-8 BOM
- 本环境 Edit/Write 工具写文件时按系统默认 GBK 编码、且不保留 BOM。
- 对原本 UTF-8 BOM 的中文 .cs 做 Edit 后：BOM(U+FEFF) 无法被 GBK 编码 → 被替换成字面量 `?`(0x3F)，整文件变成 GBK 无 BOM。
- 若再用 cp936 解码并转回 UTF-8 BOM，会得到 `EF BB BF 3F ...`（BOM + 多余的 `?` + using），导致 Unity 编译报 `CS1031: Type expected` at (1,1)。
- 修复：删掉 BOM 后那个多余的 `3F` 字节；或转换前先 `TrimStart('\uFEFF','?')`。
- 流程：凡用 Edit/Write 改过含中文的 .cs，改完后必须用字节级脚本复验 `首字节==EF BB BF`、其后紧跟正文、StrictUTF8=True；发现 `EF BB BF 3F` 立即去掉那个 3F。

## 修复中文 .cs 的可靠方法（已验证）
- 实验确认：Write/Edit 工具写中文 = GBK 无 BOM（字节 B2 E2...）；但通过 Shell 内联命令传入的中文字面量，PowerShell 收到的是正确 Unicode，可用 `[System.IO.File]::WriteAllText(path, text, (New-Object System.Text.UTF8Encoding($true)))` 写出正确 UTF-8 BOM。
- 故改中文 .cs 一律走 PowerShell，不用 Edit/Write：
  1. `ReadAllLines(path, [Text.Encoding]::UTF8)` 读行（自动去 BOM）；
  2. 按行号 `lines[n-1].Contains(old)` 做锚点校验，再 `.Replace(old,new)`（保留缩进与上下文，old/new 用 PowerShell 单引号串避免 `$ { }` 被解释）；
  3. 探测原 EOL 与末尾换行，`[string]::Join(eol,lines)` 重组后 `WriteAllText` 写 `UTF8Encoding($true)`；
  4. 任一行 MISS 则整文件放弃写入；写完字节级复验 BOM + StrictUTF8，并 `git diff --stat` 确认无整文件行尾抖动（应为 N 增 N 删）。