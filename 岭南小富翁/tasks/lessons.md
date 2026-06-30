# Lessons

## 编码：Edit/Write 会破坏含中文 .cs 文件的 UTF-8 BOM
- 本环境 Edit/Write 工具写文件时按系统默认 GBK 编码、且不保留 BOM。
- 对原本 UTF-8 BOM 的中文 .cs 做 Edit 后：BOM(U+FEFF) 无法被 GBK 编码 → 被替换成字面量 `?`(0x3F)，整文件变成 GBK 无 BOM。
- 若再用 cp936 解码并转回 UTF-8 BOM，会得到 `EF BB BF 3F ...`（BOM + 多余的 `?` + using），导致 Unity 编译报 `CS1031: Type expected` at (1,1)。
- 修复：删掉 BOM 后那个多余的 `3F` 字节；或转换前先 `TrimStart('\uFEFF','?')`。
- 流程：凡用 Edit/Write 改过含中文的 .cs，改完后必须用字节级脚本复验 `首字节==EF BB BF`、其后紧跟正文、StrictUTF8=True；发现 `EF BB BF 3F` 立即去掉那个 3F。
