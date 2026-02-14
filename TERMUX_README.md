# Termux 运行脚本指南

## 问题解决

### 错误：`env: 'python\r': No such file or directory`

这个错误有两个原因：
1. **换行符问题**：文件使用了Windows换行符（CRLF），需要转换为Unix换行符（LF）
2. **Python命令**：Termux中通常只有 `python3`，没有 `python`

## 解决方案

### 方法1: 使用修复后的脚本（推荐）

我已经创建了 `auto_input_termux.py`，这个版本：
- 使用 `python3` 作为解释器
- 使用Unix换行符
- 使用adb命令而不是uiautomator2（更适合Termux环境）

**运行步骤：**

1. **安装依赖**（首次使用）：
   ```bash
   python3 auto_input_termux.py install
   ```
   或者手动安装：
   ```bash
   pkg install android-tools
   ```

2. **运行脚本**：
   ```bash
   python3 auto_input_termux.py
   ```

### 方法2: 修复原文件

如果你想继续使用 `auto_input.py`，需要：

1. **转换换行符**（在Termux中）：
   ```bash
   # 安装dos2unix工具
   pkg install dos2unix
   
   # 转换文件
   dos2unix auto_input.py
   ```

2. **使用python3运行**：
   ```bash
   python3 auto_input.py
   ```

   或者直接运行（如果文件权限已设置）：
   ```bash
   chmod +x auto_input.py
   ./auto_input.py
   ```

### 方法3: 直接使用python3运行（最简单）

即使文件有换行符问题，也可以直接指定解释器：

```bash
python3 auto_input.py
```

这样会忽略shebang行，直接使用python3运行。

## Termux环境说明

### 为什么原脚本可能不工作？

1. **uiautomator2限制**：
   - `uiautomator2` 主要用于从**电脑控制手机**
   - 在Termux中（手机内部）运行，需要特殊配置
   - 可能需要root权限或无线调试

2. **推荐方案**：
   - 使用 `auto_input_termux.py`（基于adb命令）
   - 或者使用Android的AccessibilityService
   - 或者使用Tasker等自动化工具

### 安装Python包

如果需要在Termux中安装Python包：

```bash
# 安装pip
pkg install python

# 安装包
pip install 包名
```

## 快速测试

测试脚本是否能正常运行：

```bash
# 测试Python环境
python3 --version

# 测试adb
adb version

# 测试设备连接
adb devices
```

## 注意事项

1. **权限**：某些操作可能需要root权限
2. **中文输入**：adb的 `input text` 命令对中文支持有限，可能需要其他方法
3. **设备连接**：在Termux中，设备就是本机，可能需要启用无线调试：
   ```bash
   # 启用无线调试（需要root或开发者选项）
   adb tcpip 5555
   adb connect 127.0.0.1:5555
   ```

## 如果还是不行

如果所有方法都失败，可以尝试：

1. **手动输入测试**：
   ```bash
   # 先手动点击输入框，然后运行
   adb shell input text "你好，美女"
   ```

2. **查看错误信息**：
   ```bash
   python3 auto_input_termux.py 2>&1 | tee error.log
   ```

3. **检查系统信息**：
   ```bash
   uname -a
   python3 --version
   adb version
   ```
