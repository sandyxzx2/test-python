# Termux 访问 Download 文件夹指南

## 方法1: 使用存储设置（推荐）

### 第一步：设置存储权限
在Termux终端中输入：
```bash
termux-setup-storage
```

这个命令会：
- 请求存储权限
- 在Termux的home目录创建 `~/storage` 目录
- 创建指向手机存储的符号链接

### 第二步：进入Download文件夹
设置完成后，使用以下命令：
```bash
cd ~/storage/downloads
```

或者：
```bash
cd ~/storage/shared/Download
```

## 方法2: 直接访问（如果已授权）

如果已经设置了存储权限，可以直接：
```bash
cd /sdcard/Download
```

## 方法3: 查看当前可访问的目录

如果想查看所有可用的存储位置：
```bash
ls ~/storage/
```

## 常用命令

```bash
# 查看当前目录
pwd

# 列出Download文件夹内容
ls ~/storage/downloads

# 或者如果已经在Download文件夹中
ls

# 返回home目录
cd ~

# 查看文件详细信息
ls -lh ~/storage/downloads
```

## 注意事项

1. **首次使用**：必须先运行 `termux-setup-storage` 来授权访问存储
2. **权限问题**：如果提示权限不足，确保在手机上允许了Termux的存储权限
3. **路径差异**：不同安卓版本的路径可能略有不同，`~/storage/downloads` 是最通用的方式
