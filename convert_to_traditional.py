#!/usr/bin/env python3
"""
Rider IDE 外部工具脚本：将简体中文语言文件转换为繁体中文

配置步骤：
1. 确保项目根目录有 .venv 虚拟环境并已安装 opencc-python-reimplemented
   如果还没有，运行: python3 -m venv .venv && source .venv/bin/activate && pip install opencc-python-reimplemented

2. 在 Rider 中打开 Settings/Preferences -> Tools -> External Tools
3. 点击 + 添加新工具：
   - Name: Convert to Traditional Chinese
   - Program: $ProjectFileDir$/.venv/bin/python3 (Mac/Linux)
     或 $ProjectFileDir$\\.venv\\Scripts\\python.exe (Windows)
   - Arguments: $ProjectFileDir$/convert_to_traditional.py
   - Working directory: $ProjectFileDir$
4. 可选：在 Keymap 中为该工具设置快捷键

使用方法：
- 通过 Tools -> External Tools -> Convert to Traditional Chinese 运行
- 或使用设置的快捷键

特性：
- 自动检测并使用项目虚拟环境中的 opencc
- 保留 JSON 格式和缩进
- 递归处理嵌套结构
- 转换后显示统计信息
"""

import json
import os
import sys

def check_opencc():
    """检查 opencc 是否可用"""
    try:
        import opencc
        return opencc
    except ImportError:
        print("错误: 找不到 opencc 模块")
        print("请确保已创建虚拟环境并安装依赖:")
        print("  python3 -m venv .venv")
        print(r"  source .venv/bin/activate  (Windows: .venv\Scripts\activate)")
        print("  pip install opencc-python-reimplemented")
        sys.exit(1)

def convert_simplified_to_traditional(input_file, output_file):
    """将简体中文JSON转换为繁体中文"""
    opencc = check_opencc()
    
    # 创建转换器（简体转台湾繁体，使用常用词汇）
    converter = opencc.OpenCC('s2tw')  # s2tw: 简体转台湾繁体
    
    # 读取简体中文文件
    with open(input_file, 'r', encoding='utf-8') as f:
        data = json.load(f)
    
    # 递归转换函数
    def convert_value(value):
        if isinstance(value, str):
            return converter.convert(value)
        elif isinstance(value, dict):
            return {k: convert_value(v) for k, v in value.items()}
        elif isinstance(value, list):
            return [convert_value(item) for item in value]
        else:
            return value
    
    # 转换所有值
    converted_data = convert_value(data)
    
    # 写入繁体中文文件
    with open(output_file, 'w', encoding='utf-8') as f:
        json.dump(converted_data, f, ensure_ascii=False, indent=2)
    
    print(f"转换完成！")
    print(f"输入文件: {input_file}")
    print(f"输出文件: {output_file}")
    
    # 统计信息
    def count_strings(obj, count=0):
        if isinstance(obj, str):
            return count + 1
        elif isinstance(obj, dict):
            return sum(count_strings(v) for v in obj.values())
        elif isinstance(obj, list):
            return sum(count_strings(item) for item in obj)
        return count
    
    print(f"转换了 {count_strings(converted_data)} 个字符串")

def main():
    # 脚本所在目录
    script_dir = os.path.dirname(os.path.abspath(__file__))
    
    # 语言文件目录
    lang_dir = os.path.join(script_dir, "Resources", "lang")
    
    # 输入输出文件路径
    input_file = os.path.join(lang_dir, "ChineseSimplified.json")
    output_file = os.path.join(lang_dir, "ChineseTraditional.json")
    
    # 检查输入文件是否存在
    if not os.path.exists(input_file):
        print(f"错误: 找不到输入文件 {input_file}")
        sys.exit(1)
    
    # 执行转换
    convert_simplified_to_traditional(input_file, output_file)

if __name__ == "__main__":
    main()
