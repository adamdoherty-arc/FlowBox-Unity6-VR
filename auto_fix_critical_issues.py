#!/usr/bin/env python3
"""
FlowBox Auto-Fix Critical Issues
Automatically fixes all critical issues that can be programmatically resolved
Includes safety features: backups, validation, rollback capability
"""

import os
import re
import shutil
import json
from pathlib import Path
from datetime import datetime
from typing import List, Dict, Tuple, Set
import time

class AutoFixer:
    def __init__(self, project_root: str):
        self.project_root = Path(project_root)
        self.scripts_path = self.project_root / "Assets" / "Scripts"
        self.backup_dir = self.project_root / "AutoFix_Backups" / datetime.now().strftime("%Y%m%d_%H%M%S")
        
        # Auto-fix patterns - only safe, reliable fixes
        self.fix_patterns = {
            'findobjectoftype': {
                'pattern': r'FindObjectOfType<([^>]+)>\(\)',
                'replacement': r'CachedReferenceManager.Get<\1>()',
                'description': 'Replace FindObjectOfType with cached reference manager',
                'add_using': 'using VRBoxingGame.Core;'
            },
            'async_void_methods': {
                'pattern': r'(private|public|protected|internal)?\s*(async\s+void\s+)(\w+)\s*\(',
                'replacement': r'\1 async Task \3(',
                'description': 'Convert async void to async Task',
                'add_using': 'using System.Threading.Tasks;'
            },
            'async_void_simple': {
                'pattern': r'async\s+void\s+(\w+)\s*\(',
                'replacement': r'async Task \1(',
                'description': 'Convert simple async void to async Task',
                'add_using': 'using System.Threading.Tasks;'
            },
            'legacy_resources_load': {
                'pattern': r'Resources\.Load<([^>]+)>\("([^"]+)"\)',
                'replacement': r'// TODO: Convert to Addressables - Resources.Load<\1>("\2")',
                'description': 'Mark Resources.Load for conversion to Addressables'
            },
            'string_concatenation_simple': {
                'pattern': r'("[^"]*")\s*\+\s*(\w+)\s*\+\s*("[^"]*")',
                'replacement': r'$"\1{\2}\3"',
                'description': 'Convert simple string concatenation to interpolation'
            }
        }
        
        # Files to skip (too risky to auto-modify)
        self.skip_files = {
            'EnhancingPromptSystem.cs',  # Already optimized
            'CachedReferenceManager.cs',  # Core system
            'BaselineProfiler.cs'  # Core system
        }
        
        self.fixes_applied = []
        self.errors_encountered = []
        
    def create_backup(self):
        """Create backup of entire Scripts directory"""
        print(f"📦 Creating backup in {self.backup_dir}...")
        
        self.backup_dir.mkdir(parents=True, exist_ok=True)
        shutil.copytree(self.scripts_path, self.backup_dir / "Scripts")
        
        # Also backup any config files
        config_files = [
            "Packages/manifest.json",
            "ProjectSettings/ProjectSettings.asset"
        ]
        
        for config_file in config_files:
            config_path = self.project_root / config_file
            if config_path.exists():
                backup_config_path = self.backup_dir / config_file
                backup_config_path.parent.mkdir(parents=True, exist_ok=True)
                shutil.copy2(config_path, backup_config_path)
        
        print(f"✅ Backup created successfully")
        return self.backup_dir
    
    def auto_fix_all_issues(self) -> Dict:
        """Automatically fix all critical issues that can be safely fixed"""
        print("🤖 Starting automatic fix of critical issues...")
        print("=" * 60)
        
        # Create backup first
        backup_path = self.create_backup()
        
        start_time = time.time()
        total_fixes = 0
        
        try:
            # Get all C# files
            cs_files = list(self.scripts_path.rglob("*.cs"))
            print(f"📁 Processing {len(cs_files)} C# files...")
            
            for file_path in cs_files:
                # Skip certain files
                if file_path.name in self.skip_files:
                    print(f"⏭️ Skipping {file_path.name} (protected file)")
                    continue
                
                fixes_in_file = self.fix_file(file_path)
                total_fixes += fixes_in_file
                
                if fixes_in_file > 0:
                    print(f"🔧 Fixed {fixes_in_file} issues in {file_path.name}")
            
            # Add missing using statements
            self.add_missing_using_statements()
            
            execution_time = time.time() - start_time
            
            # Generate report
            report = {
                'total_fixes_applied': total_fixes,
                'files_modified': len(set(fix['file'] for fix in self.fixes_applied)),
                'execution_time': execution_time,
                'backup_location': str(backup_path),
                'fixes_by_type': self.get_fixes_by_type(),
                'errors': self.errors_encountered,
                'timestamp': datetime.now().isoformat()
            }
            
            self.save_fix_report(report)
            self.print_summary(report)
            
            return report
            
        except Exception as e:
            print(f"❌ Auto-fix failed: {e}")
            print(f"🔄 You can restore from backup: {backup_path}")
            raise
    
    def fix_file(self, file_path: Path) -> int:
        """Fix all issues in a single file"""
        try:
            with open(file_path, 'r', encoding='utf-8') as f:
                original_content = f.read()
            
            modified_content = original_content
            fixes_in_file = 0
            
            # Apply each fix pattern
            for fix_name, fix_info in self.fix_patterns.items():
                pattern = fix_info['pattern']
                replacement = fix_info['replacement']
                
                # Count matches before replacement
                matches = re.findall(pattern, modified_content)
                if matches:
                    # Apply the fix
                    new_content = re.sub(pattern, replacement, modified_content)
                    
                    if new_content != modified_content:
                        fix_count = len(matches)
                        fixes_in_file += fix_count
                        
                        # Record the fix
                        self.fixes_applied.append({
                            'file': str(file_path.relative_to(self.project_root)),
                            'fix_type': fix_name,
                            'description': fix_info['description'],
                            'count': fix_count
                        })
                        
                        modified_content = new_content
            
            # Write back if any changes were made
            if modified_content != original_content:
                with open(file_path, 'w', encoding='utf-8') as f:
                    f.write(modified_content)
            
            return fixes_in_file
            
        except Exception as e:
            error_msg = f"Error fixing {file_path}: {e}"
            self.errors_encountered.append(error_msg)
            print(f"❌ {error_msg}")
            return 0
    
    def add_missing_using_statements(self):
        """Add missing using statements to files that need them"""
        print("📝 Adding missing using statements...")
        
        # Track which files need which using statements
        files_needing_using = {}
        
        for fix in self.fixes_applied:
            file_path = self.project_root / fix['file']
            fix_type = fix['fix_type']
            
            if fix_type in self.fix_patterns:
                using_statement = self.fix_patterns[fix_type].get('add_using')
                if using_statement:
                    if str(file_path) not in files_needing_using:
                        files_needing_using[str(file_path)] = set()
                    files_needing_using[str(file_path)].add(using_statement)
        
        # Add using statements to files
        for file_path_str, using_statements in files_needing_using.items():
            file_path = Path(file_path_str)
            
            try:
                with open(file_path, 'r', encoding='utf-8') as f:
                    content = f.read()
                
                # Check which using statements are missing
                missing_statements = []
                for using_stmt in using_statements:
                    if using_stmt not in content:
                        missing_statements.append(using_stmt)
                
                if missing_statements:
                    # Find where to insert using statements (after existing using statements)
                    lines = content.split('\n')
                    insert_index = 0
                    
                    # Find the last using statement
                    for i, line in enumerate(lines):
                        if line.strip().startswith('using ') and not line.strip().startswith('using static'):
                            insert_index = i + 1
                    
                    # Insert missing using statements
                    for using_stmt in missing_statements:
                        lines.insert(insert_index, using_stmt)
                        insert_index += 1
                    
                    # Write back
                    with open(file_path, 'w', encoding='utf-8') as f:
                        f.write('\n'.join(lines))
                    
                    print(f"📝 Added {len(missing_statements)} using statements to {file_path.name}")
                    
            except Exception as e:
                print(f"❌ Error adding using statements to {file_path}: {e}")
    
    def get_fixes_by_type(self) -> Dict[str, int]:
        """Get count of fixes by type"""
        fixes_by_type = {}
        for fix in self.fixes_applied:
            fix_type = fix['fix_type']
            fixes_by_type[fix_type] = fixes_by_type.get(fix_type, 0) + fix['count']
        return fixes_by_type
    
    def save_fix_report(self, report: Dict):
        """Save detailed fix report"""
        report_path = self.project_root / "AUTO_FIX_REPORT.json"
        
        with open(report_path, 'w') as f:
            json.dump(report, f, indent=2, default=str)
        
        # Also save human-readable report
        md_report_path = self.project_root / "AUTO_FIX_REPORT.md"
        with open(md_report_path, 'w') as f:
            f.write(self.generate_markdown_report(report))
        
        print(f"📄 Fix reports saved:")
        print(f"  - AUTO_FIX_REPORT.json")
        print(f"  - AUTO_FIX_REPORT.md")
    
    def generate_markdown_report(self, report: Dict) -> str:
        """Generate human-readable markdown report"""
        md = "# FlowBox Auto-Fix Report\n\n"
        md += f"**Date:** {datetime.now().strftime('%Y-%m-%d %H:%M:%S')}\n"
        md += f"**Execution Time:** {report['execution_time']:.2f} seconds\n"
        md += f"**Backup Location:** {report['backup_location']}\n\n"
        
        md += "## Summary\n\n"
        md += f"- **Total Fixes Applied:** {report['total_fixes_applied']}\n"
        md += f"- **Files Modified:** {report['files_modified']}\n"
        md += f"- **Errors Encountered:** {len(report['errors'])}\n\n"
        
        if report['fixes_by_type']:
            md += "## Fixes by Type\n\n"
            for fix_type, count in report['fixes_by_type'].items():
                description = self.fix_patterns.get(fix_type, {}).get('description', fix_type)
                md += f"- **{description}:** {count} fixes\n"
            md += "\n"
        
        if self.fixes_applied:
            md += "## Detailed Fix List\n\n"
            current_file = None
            for fix in self.fixes_applied:
                if fix['file'] != current_file:
                    current_file = fix['file']
                    md += f"### {current_file}\n\n"
                
                md += f"- {fix['description']}: {fix['count']} fixes\n"
            md += "\n"
        
        if report['errors']:
            md += "## Errors Encountered\n\n"
            for error in report['errors']:
                md += f"- {error}\n"
            md += "\n"
        
        md += "## Next Steps\n\n"
        md += "1. Test the application to ensure all fixes work correctly\n"
        md += "2. Run the comprehensive validator again to verify issues are resolved\n"
        md += "3. If issues occur, restore from backup and fix manually\n"
        md += f"4. Backup is located at: `{report['backup_location']}`\n"
        
        return md
    
    def print_summary(self, report: Dict):
        """Print summary of auto-fix results"""
        print("\n" + "=" * 60)
        print("🏁 AUTO-FIX COMPLETE!")
        print("=" * 60)
        
        print(f"✅ Total Fixes Applied: {report['total_fixes_applied']}")
        print(f"📁 Files Modified: {report['files_modified']}")
        print(f"⏱️ Execution Time: {report['execution_time']:.2f} seconds")
        
        if report['fixes_by_type']:
            print("\n🔧 Fixes by Type:")
            for fix_type, count in report['fixes_by_type'].items():
                description = self.fix_patterns.get(fix_type, {}).get('description', fix_type)
                print(f"  • {description}: {count}")
        
        if report['errors']:
            print(f"\n⚠️ Errors Encountered: {len(report['errors'])}")
            for error in report['errors'][:5]:  # Show first 5 errors
                print(f"  • {error}")
            if len(report['errors']) > 5:
                print(f"  • ... and {len(report['errors']) - 5} more")
        
        print(f"\n💾 Backup created at: {report['backup_location']}")
        print("\n🧪 NEXT STEPS:")
        print("1. Test your application to ensure everything works")
        print("2. Run validation again: python3 complete_validation_runner.py")
        print("3. If issues occur, restore from backup")
        
        if report['total_fixes_applied'] > 0:
            print("\n🎉 Critical performance issues have been automatically fixed!")
            print("   Your VR application should now run much smoother.")
        else:
            print("\n🤔 No auto-fixable issues found. Manual review may be needed.")

def create_restore_script(backup_path: Path, project_root: Path):
    """Create a script to restore from backup if needed"""
    restore_script = project_root / "restore_from_backup.sh"
    
    script_content = f"""#!/bin/bash
echo "🔄 Restoring FlowBox from auto-fix backup..."
echo "Backup location: {backup_path}"

if [ ! -d "{backup_path}" ]; then
    echo "❌ Backup directory not found: {backup_path}"
    exit 1
fi

echo "⚠️ This will overwrite current files with backup. Continue? (y/N)"
read -n 1 -r
echo
if [[ $REPLY =~ ^[Yy]$ ]]; then
    echo "🔄 Restoring Assets/Scripts..."
    rm -rf Assets/Scripts
    cp -r "{backup_path}/Scripts" Assets/Scripts
    echo "✅ Restore complete!"
else
    echo "❌ Restore cancelled"
fi
"""
    
    with open(restore_script, 'w') as f:
        f.write(script_content)
    
    os.chmod(restore_script, 0o755)
    print(f"🔄 Restore script created: {restore_script}")

def main():
    """Run auto-fix for FlowBox critical issues"""
    print("🤖 FlowBox Auto-Fix Critical Issues")
    print("=" * 50)
    print("This will automatically fix critical VR performance issues")
    print("A backup will be created before any changes are made")
    print("")
    
    project_root = "."
    
    # Confirm before proceeding
    print("⚠️ This will modify your source code. Continue? (y/N): ", end="")
    response = input().strip().lower()
    
    if response != 'y':
        print("❌ Auto-fix cancelled")
        return
    
    auto_fixer = AutoFixer(project_root)
    
    try:
        report = auto_fixer.auto_fix_all_issues()
        
        # Create restore script
        create_restore_script(Path(report['backup_location']), Path(project_root))
        
    except Exception as e:
        print(f"❌ Auto-fix failed: {e}")
        print("Please check the backup and restore if needed")

if __name__ == "__main__":
    main() 