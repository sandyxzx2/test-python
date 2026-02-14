#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""
Soul自动化脚本 - 修复版
适配1600x900分辨率
"""

import subprocess
import time
import random
import xml.etree.ElementTree as ET
import os
import re

# --- 配置区 ---
PACKAGE_NAME = "cn.soulapp.android"
DUMP_PATH = "/sdcard/view.xml"
LOCAL_XML = "view.xml"
SCREEN_WIDTH = 1600
SCREEN_HEIGHT = 900

# 需要过滤的无用文本（键盘按键、控制字符等）
FILTER_KEYWORDS = [
    'ESC', 'HOME', 'END', 'PGUP', 'PGDN', 'CTRL', 'ALT', 'SHIFT',
    'TAB', 'ENTER', 'SPACE', 'BACKSPACE', 'DELETE', 'INSERT',
    'F1', 'F2', 'F3', 'F4', 'F5', 'F6', 'F7', 'F8', 'F9', 'F10', 'F11', 'F12'
]

def check_adb_connection():
    """检查ADB连接是否正常"""
    try:
        result = subprocess.run(
            ["adb", "devices"],
            capture_output=True,
            text=True,
            timeout=5
        )
        if result.returncode == 0:
            # 检查是否有设备连接
            lines = result.stdout.strip().split('\n')
            devices = [line for line in lines if 'device' in line and 'List' not in line]
            if devices:
                print(f"  ✓ ADB连接正常，找到 {len(devices)} 个设备")
                return True
            else:
                print("  ⚠️  ADB未找到连接的设备")
                return False
        return False
    except Exception as e:
        print(f"  ❌ 检查ADB连接失败: {e}")
        return False

def adb_shell(command, timeout=10, show_error=True):
    """执行ADB命令，带错误处理"""
    try:
        # 使用列表方式传递命令，避免shell解析问题
        cmd = ["adb", "shell"] + command.split()
        result = subprocess.run(
            cmd,
            capture_output=True,
            text=True,
            timeout=timeout
        )
        
        # 即使返回码不为0，也检查输出（某些命令可能返回非0但仍有输出）
        if result.returncode != 0:
            # 忽略ADB daemon启动信息（这是正常的）
            if "daemon" in result.stderr.lower() and "started" in result.stderr.lower():
                # daemon启动成功，继续执行
                pass
            elif show_error and result.stderr.strip():
                # 只显示真正的错误，不显示daemon启动信息
                error_msg = result.stderr.strip()
                if "daemon not running" not in error_msg.lower():
                    print(f"  ⚠️  命令执行失败: {command}")
                    if error_msg:
                        print(f"  错误: {error_msg}")
        
        # 返回输出（即使有错误，也可能有部分输出）
        return result.stdout if result.stdout else None
        
    except subprocess.TimeoutExpired:
        if show_error:
            print(f"  ⚠️  命令超时: {command}")
        return None
    except Exception as e:
        if show_error:
            print(f"  ❌ 执行命令出错: {e}")
        return None

def check_app_running():
    """检查Soul应用是否在前台运行"""
    # 方法1: 使用dumpsys获取当前焦点窗口（不使用grep）
    result = adb_shell("dumpsys window windows")
    if result:
        # 在Python中搜索，而不是使用grep
        for line in result.split('\n'):
            if 'mCurrentFocus' in line or 'mFocusedApp' in line:
                if PACKAGE_NAME in line:
                    return True
    
    # 方法2: 使用dumpsys activity获取当前活动
    result2 = adb_shell("dumpsys activity activities | grep mResumedActivity")
    if result2 and PACKAGE_NAME in result2:
        return True
    
    # 方法3: 使用am命令检查
    result3 = adb_shell("dumpsys activity top | grep ACTIVITY")
    if result3 and PACKAGE_NAME in result3:
        return True
    
    return False

def launch_soul_app():
    """启动Soul应用"""
    print("-> 检查Soul应用状态...")
    
    # 先检查ADB连接
    if not check_adb_connection():
        print("  ❌ ADB连接异常，请检查:")
        print("    1. 设备是否已连接")
        print("    2. 是否已启用USB调试")
        print("    3. 运行 'adb devices' 查看设备状态")
        return False
    
    # 检查应用是否已在运行
    if check_app_running():
        print("  ✓ Soul应用已在运行")
        return True
    
    print("-> 正在启动Soul应用...")
    # 使用monkey启动应用（不显示错误，因为monkey可能输出到stderr）
    result = adb_shell(f"monkey -p {PACKAGE_NAME} -c android.intent.category.LAUNCHER 1", show_error=False)
    
    # 等待应用启动
    print("  ⏱️  等待应用启动...")
    time.sleep(3)
    
    # 再次检查应用状态
    if check_app_running():
        print("  ✓ Soul应用启动成功")
        return True
    else:
        # 尝试备用启动方法
        print("  -> 尝试备用启动方法...")
        result2 = adb_shell(f"am start -n {PACKAGE_NAME}/.MainActivity", show_error=False)
        time.sleep(2)
        if check_app_running():
            print("  ✓ 使用备用方法启动成功")
            return True
        else:
            print("  ⚠️  应用可能未成功启动，但继续尝试...")
            # 即使检查失败，也继续（可能是检查方法的问题）
            return True

def get_screen_size():
    """获取屏幕尺寸"""
    result = adb_shell("wm size")
    if result:
        # 解析格式: Physical size: 1600x900
        match = re.search(r'(\d+)x(\d+)', result)
        if match:
            width = int(match.group(1))
            height = int(match.group(2))
            print(f"  ✓ 屏幕尺寸: {width}x{height}")
            return width, height
    print(f"  ⚠️  无法获取屏幕尺寸，使用默认值: {SCREEN_WIDTH}x{SCREEN_HEIGHT}")
    return SCREEN_WIDTH, SCREEN_HEIGHT

def random_sleep(min_s=2, max_s=5):
    """模拟人类思考时间"""
    sleep_time = random.uniform(min_s, max_s)
    print(f"  ⏱️  等待 {sleep_time:.1f} 秒...")
    time.sleep(sleep_time)

def random_tap(x1, y1, x2, y2):
    """在指定范围内随机点击，防止被识别为机器人"""
    x = random.randint(x1, x2)
    y = random.randint(y1, y2)
    print(f"  🖱️  点击坐标: ({x}, {y})")
    adb_shell(f"input tap {x} {y}")
    time.sleep(0.5)  # 等待点击生效

def click_planet_button(width, height):
    """点击左下角的'星球'按钮返回主页"""
    # 对于1600x900，星球按钮在底部导航栏最左侧
    # 底部导航栏大约在屏幕底部10%的位置
    # 星球按钮在左侧约10-15%宽度位置
    planet_x = int(width * 0.12)  # 左侧约12%位置
    planet_y = int(height * 0.95)  # 底部约95%位置
    
    print(f"  🪐 点击'星球'按钮返回主页: ({planet_x}, {planet_y})")
    adb_shell(f"input tap {planet_x} {planet_y}")
    time.sleep(1)  # 等待页面切换
    return True

def go_to_homepage(width, height):
    """返回主页 - 尝试多种方法"""
    print("-> 正在返回主页...")
    
    # 方法1: 先尝试返回键
    print("  [方法1] 尝试使用返回键...")
    adb_shell("input keyevent 4")  # 返回键
    time.sleep(1)
    
    # 方法2: 如果返回键无效，点击星球按钮
    print("  [方法2] 点击'星球'按钮...")
    click_planet_button(width, height)
    
    # 等待页面加载
    time.sleep(2)
    print("  ✓ 已尝试返回主页")

def get_screen_xml():
    """获取当前页面的XML结构"""
    print("  -> 正在获取页面结构...")
    result = adb_shell(f"uiautomator dump {DUMP_PATH}")
    if result is None:
        print("  ❌ 无法导出UI结构")
        return None
    
    # 拉取XML文件
    pull_result = subprocess.run(
        ["adb", "pull", DUMP_PATH, LOCAL_XML], 
        capture_output=True,
        timeout=10
    )
    
    if pull_result.returncode != 0:
        print("  ❌ 无法拉取XML文件")
        return None
    
    if os.path.exists(LOCAL_XML):
        try:
            tree = ET.parse(LOCAL_XML)
            print("  ✓ 成功获取页面结构")
            return tree
        except Exception as e:
            print(f"  ❌ 解析XML失败: {e}")
            return None
    return None

def is_valid_text(text):
    """判断文本是否有效（过滤无用信息）"""
    if not text or len(text.strip()) < 2:
        return False
    
    # 过滤单个字符
    if len(text.strip()) == 1:
        return False
    
    # 过滤键盘按键和控制字符
    text_upper = text.upper().strip()
    if text_upper in FILTER_KEYWORDS:
        return False
    
    # 过滤纯数字（可能是ID）
    if text.strip().isdigit() and len(text.strip()) > 3:
        return False
    
    # 过滤常见的UI元素文本
    ui_elements = ['确定', '取消', '返回', '关闭', '下一步', '完成']
    if text.strip() in ui_elements:
        return False
    
    return True

def extract_user_info():
    """解析个人资料页信息 - 改进版"""
    print("-> 正在提取用户信息...")
    tree = get_screen_xml()
    if not tree:
        print("  ❌ 无法获取页面结构")
        return {}
    
    root = tree.getroot()
    user_info = {
        'name': None,
        'age': None,
        'location': None,
        'tags': [],
        'description': None,
        'other_info': []
    }
    
    valid_texts = []
    
    # 查找所有具有text属性的节点
    for node in root.iter():
        text = node.get('text')
        resource_id = node.get('resource-id', '')
        class_name = node.get('class', '')
        
        if text and is_valid_text(text):
            # 根据resource-id和文本内容判断信息类型
            text_clean = text.strip()
            
            # 过滤掉明显不是用户信息的内容
            if any(keyword in text_clean.upper() for keyword in FILTER_KEYWORDS):
                continue
            
            # 尝试识别信息类型
            if 'name' in resource_id.lower() or 'nickname' in resource_id.lower():
                if not user_info['name']:
                    user_info['name'] = text_clean
            elif 'age' in resource_id.lower() or '岁' in text_clean:
                if not user_info['age']:
                    user_info['age'] = text_clean
            elif 'location' in resource_id.lower() or '地址' in resource_id.lower():
                if not user_info['location']:
                    user_info['location'] = text_clean
            elif 'tag' in resource_id.lower() or '标签' in resource_id.lower():
                user_info['tags'].append(text_clean)
            elif 'description' in resource_id.lower() or '简介' in resource_id.lower():
                if not user_info['description']:
                    user_info['description'] = text_clean
            else:
                # 保存其他可能有用的信息
                if len(text_clean) > 2 and text_clean not in valid_texts:
                    valid_texts.append(text_clean)
    
    # 打印提取到的信息
    print("\n  📋 提取到的用户信息:")
    if user_info['name']:
        print(f"    姓名: {user_info['name']}")
    if user_info['age']:
        print(f"    年龄: {user_info['age']}")
    if user_info['location']:
        print(f"    位置: {user_info['location']}")
    if user_info['tags']:
        print(f"    标签: {', '.join(user_info['tags'][:5])}")  # 只显示前5个
    if user_info['description']:
        print(f"    简介: {user_info['description'][:50]}...")  # 只显示前50字符
    
    # 显示其他有效文本（限制数量）
    if valid_texts:
        print(f"\n  📝 其他信息 (前10条):")
        for i, text in enumerate(valid_texts[:10], 1):
            if len(text) > 50:
                text = text[:50] + "..."
            print(f"    {i}. {text}")
    
    if not any([user_info['name'], user_info['age'], user_info['location'], valid_texts]):
        print("  ⚠️  未提取到有效用户信息，可能不在用户资料页面")
    
    return user_info

def soul_logic():
    """主逻辑控制 - 改进版"""
    print("=" * 50)
    print("=== Soul 自动化脚本启动 ===")
    print("=" * 50)
    
    # 检查并启动应用
    print("\n[初始化] 检查环境...")
    if not launch_soul_app():
        print("\n❌ 无法启动Soul应用，请检查:")
        print("  1. 应用是否已安装")
        print("  2. ADB连接是否正常 (运行: adb devices)")
        print("  3. 是否已启用USB调试或无线调试")
        print("\n提示: 如果使用Termux，可能需要:")
        print("  - 启用无线调试: adb tcpip 5555")
        print("  - 连接设备: adb connect 127.0.0.1:5555")
        return
    
    print("\n[初始化] 环境检查完成，开始自动化流程...")
    
    # 获取屏幕尺寸
    width, height = get_screen_size()
    
    # 等待应用完全加载
    print("-> 等待应用加载...")
    time.sleep(2)
    
    max_iterations = 5  # 限制循环次数
    iteration = 0
    
    try:
        while iteration < max_iterations:
            iteration += 1
            print(f"\n{'='*50}")
            print(f"第 {iteration} 轮操作")
            print(f"{'='*50}")
            
            # 1. 模拟随机浏览广场
            print("\n[步骤1] 模拟随机浏览...")
            swipe_y1 = int(height * 0.7)
            swipe_y2 = int(height * 0.3)
            swipe_x = width // 2
            adb_shell(f"input swipe {swipe_x} {swipe_y1} {swipe_x} {swipe_y2} 500")
            random_sleep(2, 4)
            
            # 2. 确保在主页，然后点击"灵魂匹配"按钮
            print("\n[步骤2] 确保在主页...")
            # 先点击星球按钮确保在主页
            click_planet_button(width, height)
            time.sleep(1)
            
            print("-> 尝试发起灵魂匹配...")
            # 根据图片，"灵魂匹配"卡片在屏幕左侧，大约在中间偏上位置
            # 对于1600x900，"开始匹配"按钮大约在卡片中间
            match_positions = [
                (int(width * 0.25), int(height * 0.35)),  # 左侧卡片，"开始匹配"按钮位置
                (int(width * 0.25), int(height * 0.4)),     # 稍微下移
                (int(width * 0.3), int(height * 0.35)),   # 稍微右移
            ]
            
            for pos_x, pos_y in match_positions:
                print(f"  尝试点击'开始匹配'按钮: ({pos_x}, {pos_y})")
                adb_shell(f"input tap {pos_x} {pos_y}")
                time.sleep(2)
            
            random_sleep(3, 6)  # 等待匹配结果
            
            # 3. 尝试点击头像或进入用户主页
            print("\n[步骤3] 尝试查看用户资料...")
            # 点击屏幕中间区域（可能是头像或用户卡片）
            tap_x = width // 2
            tap_y = height // 2
            print(f"  点击屏幕中心: ({tap_x}, {tap_y})")
            adb_shell(f"input tap {tap_x} {tap_y}")
            random_sleep(2, 3)
            
            # 4. 提取用户信息
            print("\n[步骤4] 提取用户信息...")
            user_info = extract_user_info()
            
            # 5. 滑动查看更多资料
            print("\n[步骤5] 滑动查看更多资料...")
            adb_shell(f"input swipe {width//2} {int(height*0.7)} {width//2} {int(height*0.3)} 800")
            random_sleep(2, 3)
            extract_user_info()
            
            # 6. 返回主页面
            print("\n[步骤6] 返回主页面...")
            go_to_homepage(width, height)
            random_sleep(2, 4)
            
            print(f"\n✓ 第 {iteration} 轮操作完成")
            
    except KeyboardInterrupt:
        print("\n\n⚠️  脚本被用户中断")
    except Exception as e:
        print(f"\n❌ 发生错误: {e}")
        import traceback
        traceback.print_exc()
    finally:
        # 清理临时文件
        if os.path.exists(LOCAL_XML):
            try:
                os.remove(LOCAL_XML)
                print("\n✓ 已清理临时文件")
            except:
                pass
        print("\n=== 脚本结束 ===")

if __name__ == "__main__":
    soul_logic()
