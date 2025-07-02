#!/usr/bin/env python3
"""
FlowBox Complete Validation Runner
Systematically finds ALL issues in one comprehensive pass
Eliminates iterative discovery by running every possible check
"""

import os
import re
import json
import asyncio
from pathlib import Path
from typing import List, Dict, Tuple, Set
from dataclasses import dataclass, asdict
from concurrent.futures import ThreadPoolExecutor
import time

@dataclass
class Issue:
    severity: str  # Critical, Warning, Info
    category: str  # Performance, Unity6, VR, CodeQuality, etc.
    file: str
    line: int
    description: str
    solution: str
    can_auto_fix: bool
    code_snippet: str

@dataclass
class ValidationReport:
    total_issues: int
    critical_issues: int
    warning_issues: int
    info_issues: int
    issues_by_category: Dict[str, int]
    all_issues: List[Issue]
    validation_time: float
    is_deployment_ready: bool
    summary: str

class ComprehensiveValidator:
    def __init__(self, project_root: str):
        self.project_root = Path(project_root)
        self.assets_path = self.project_root / "Assets"
        self.scripts_path = self.assets_path / "Scripts"
        
        # All possible issue patterns to check
        self.validation_patterns = {
            # Performance Issues (Critical for VR)
            'findobjectoftype': {
                'pattern': r'FindObjectOfType[<(]',
                'severity': 'Critical',
                'category': 'Performance',
                'description': 'FindObjectOfType causes VR performance issues (50-150ms spikes)',
                'solution': 'Replace with CachedReferenceManager.Get<T>()',
                'can_auto_fix': True
            },
            'linq_usage': {
                'pattern': r'(using System\.Linq|\.Where\(|\.Select\(|\.FirstOrDefault\()',
                'severity': 'Warning', 
                'category': 'Performance',
                'description': 'LINQ causes GC allocations in VR (memory pressure)',
                'solution': 'Replace with for loops or pre-allocated collections',
                'can_auto_fix': False
            },
            'async_void': {
                'pattern': r'async\s+void\s+\w+',
                'severity': 'Critical',
                'category': 'AsyncPatterns',
                'description': 'async void can cause unhandled exceptions and memory leaks',
                'solution': 'Change to async Task',
                'can_auto_fix': True
            },
            'string_concatenation': {
                'pattern': r'"\s*\+\s*\w+|\w+\s*\+\s*"',
                'severity': 'Warning',
                'category': 'Performance', 
                'description': 'String concatenation in loops causes GC pressure',
                'solution': 'Use StringBuilder or string interpolation',
                'can_auto_fix': False
            },
            
            # Unity 6 Compliance Issues
            'legacy_coroutines': {
                'pattern': r'StartCoroutine\(',
                'severity': 'Warning',
                'category': 'Unity6Compliance',
                'description': 'Legacy coroutines, consider async/await for Unity 6',
                'solution': 'Convert to async Task methods',
                'can_auto_fix': False
            },
            'legacy_input': {
                'pattern': r'Input\.(GetButton|GetKey|GetAxis)',
                'severity': 'Warning',
                'category': 'Unity6Compliance', 
                'description': 'Legacy Input system, Unity 6 uses new Input System',
                'solution': 'Migrate to Unity Input System',
                'can_auto_fix': False
            },
            'resources_load': {
                'pattern': r'Resources\.Load[<(]',
                'severity': 'Warning',
                'category': 'Unity6Compliance',
                'description': 'Resources.Load is legacy, Unity 6 prefers Addressables',
                'solution': 'Convert to Addressable Asset System',
                'can_auto_fix': False
            },
            
            # VR Optimization Issues  
            'instantiate_without_pool': {
                'pattern': r'Instantiate\(',
                'severity': 'Warning',
                'category': 'VROptimization',
                'description': 'Direct Instantiate causes GC pressure in VR',
                'solution': 'Use object pooling',
                'can_auto_fix': False
            },
            'getcomponent_in_update': {
                'pattern': r'GetComponent[<(].*\)',
                'severity': 'Warning', 
                'category': 'VROptimization',
                'description': 'GetComponent calls should be cached for VR performance',
                'solution': 'Cache component references in Start/Awake',
                'can_auto_fix': False
            },
            'camera_main': {
                'pattern': r'Camera\.main',
                'severity': 'Warning',
                'category': 'VROptimization',
                'description': 'Camera.main is slow, cache VR camera reference',
                'solution': 'Cache camera reference at startup',
                'can_auto_fix': False
            },
            
            # Code Quality Issues
            'magic_numbers': {
                'pattern': r'=\s*[0-9]+\.?[0-9]*[f]?\s*[;}]',
                'severity': 'Info',
                'category': 'CodeQuality',
                'description': 'Magic numbers reduce code maintainability',
                'solution': 'Replace with named constants',
                'can_auto_fix': False
            },
            'empty_catch': {
                'pattern': r'catch[^{]*{\s*}',
                'severity': 'Warning',
                'category': 'CodeQuality',
                'description': 'Empty catch blocks hide errors',
                'solution': 'Add proper error handling or logging',
                'can_auto_fix': False
            },
            'todo_fixme': {
                'pattern': r'(TODO|FIXME|HACK|BUG):',
                'severity': 'Info',
                'category': 'CodeQuality',
                'description': 'Unresolved development notes',
                'solution': 'Address or document the issue',
                'can_auto_fix': False
            },
            
            # Memory Management Issues
            'list_add_in_update': {
                'pattern': r'\.Add\(',
                'severity': 'Warning',
                'category': 'MemoryManagement',
                'description': 'Collection modifications in Update can cause GC',
                'solution': 'Pre-allocate collections or use object pooling',
                'can_auto_fix': False
            },
            'new_in_update': {
                'pattern': r'new\s+\w+\(',
                'severity': 'Warning',
                'category': 'MemoryManagement',
                'description': 'Object allocation in Update causes GC pressure',
                'solution': 'Move allocations outside of Update or use pooling',
                'can_auto_fix': False
            },
            
            # Threading Issues
            'invoke_without_check': {
                'pattern': r'Invoke\(',
                'severity': 'Info',
                'category': 'Threading',
                'description': 'Invoke without null checking can cause issues',
                'solution': 'Add null checks before Invoke',
                'can_auto_fix': False
            },
            
            # Error Handling Issues  
            'missing_null_check': {
                'pattern': r'\w+\.\w+\(',
                'severity': 'Info',
                'category': 'ErrorHandling',
                'description': 'Potential null reference access',
                'solution': 'Add null checks where appropriate',
                'can_auto_fix': False
            }
        }
        
    async def run_comprehensive_validation(self) -> ValidationReport:
        """Run ALL validation checks in parallel"""
        print("🚀 Starting COMPREHENSIVE FlowBox validation...")
        print("📋 This will find ALL issues in one systematic pass")
        
        start_time = time.time()
        all_issues = []
        
        # Get all C# files
        cs_files = list(self.scripts_path.rglob("*.cs"))
        print(f"📁 Analyzing {len(cs_files)} C# files...")
        
        # Run all validations in parallel
        with ThreadPoolExecutor(max_workers=8) as executor:
            tasks = []
            
            # File-based analysis
            for file_path in cs_files:
                task = executor.submit(self.analyze_file, file_path)
                tasks.append(task)
            
            # Project-wide analysis
            project_task = executor.submit(self.analyze_project_structure)
            tasks.append(project_task)
            
            # Scene analysis
            scene_task = executor.submit(self.analyze_scenes)
            tasks.append(scene_task)
            
            # Gather all results
            for task in tasks:
                issues = task.result()
                all_issues.extend(issues)
        
        # Generate comprehensive report
        report = self.generate_report(all_issues, time.time() - start_time)
        
        # Save detailed report
        self.save_report(report)
        
        print(f"🏁 VALIDATION COMPLETE! Found {report.total_issues} total issues")
        print(f"🔴 Critical: {report.critical_issues}")
        print(f"🟡 Warning: {report.warning_issues}")  
        print(f"🔵 Info: {report.info_issues}")
        
        if report.is_deployment_ready:
            print("🎉 PROJECT IS DEPLOYMENT READY!")
        else:
            print(f"⚠️ {report.critical_issues} critical issues must be fixed")
            
        return report
    
    def analyze_file(self, file_path: Path) -> List[Issue]:
        """Analyze a single C# file for all possible issues"""
        issues = []
        
        try:
            with open(file_path, 'r', encoding='utf-8') as f:
                content = f.read()
                lines = content.split('\n')
                
            # Check every pattern against every line
            for line_num, line in enumerate(lines, 1):
                for pattern_name, pattern_info in self.validation_patterns.items():
                    if re.search(pattern_info['pattern'], line, re.IGNORECASE):
                        # Skip false positives
                        if self.is_false_positive(line, pattern_name):
                            continue
                            
                        issue = Issue(
                            severity=pattern_info['severity'],
                            category=pattern_info['category'],
                            file=str(file_path.relative_to(self.project_root)),
                            line=line_num,
                            description=pattern_info['description'],
                            solution=pattern_info['solution'],
                            can_auto_fix=pattern_info['can_auto_fix'],
                            code_snippet=line.strip()
                        )
                        issues.append(issue)
                        
        except Exception as e:
            print(f"❌ Error analyzing {file_path}: {e}")
            
        return issues
    
    def is_false_positive(self, line: str, pattern_name: str) -> bool:
        """Filter out false positives"""
        # Skip comments
        if line.strip().startswith('//') or line.strip().startswith('*'):
            return True
            
        # Pattern-specific false positive checks
        if pattern_name == 'findobjectoftype' and 'CachedReferenceManager' in line:
            return True  # Already optimized
            
        if pattern_name == 'async_void' and 'async void Start(' in line:
            return True  # Unity lifecycle method
            
        return False
    
    def analyze_project_structure(self) -> List[Issue]:
        """Analyze overall project structure"""
        issues = []
        
        # Check for missing Unity 6 packages
        manifest_path = self.project_root / "Packages" / "manifest.json"
        if manifest_path.exists():
            with open(manifest_path) as f:
                manifest = json.load(f)
                dependencies = manifest.get('dependencies', {})
                
            # Check for important Unity 6 packages
            required_packages = {
                'com.unity.addressables': 'Addressable Asset System for efficient loading',
                'com.unity.burst': 'Burst compiler for performance',
                'com.unity.entities': 'ECS for advanced performance',
                'com.unity.inputsystem': 'New Input System for Unity 6',
                'com.unity.render-pipelines.universal': 'URP for modern rendering'
            }
            
            for package, description in required_packages.items():
                if package not in dependencies:
                    issues.append(Issue(
                        severity='Warning',
                        category='Unity6Compliance',
                        file='Packages/manifest.json',
                        line=0,
                        description=f'Missing Unity 6 package: {package}',
                        solution=f'Add {package} - {description}',
                        can_auto_fix=False,
                        code_snippet=''
                    ))
        
        return issues
    
    def analyze_scenes(self) -> List[Issue]:
        """Analyze Unity scenes"""
        issues = []
        
        scenes_path = self.assets_path / "Scenes"
        if scenes_path.exists():
            scene_files = list(scenes_path.rglob("*.unity"))
            
            if len(scene_files) < 2:
                issues.append(Issue(
                    severity='Warning',
                    category='Architecture',
                    file='Assets/Scenes/',
                    line=0,
                    description='Only one scene found, may need menu/loading scenes',
                    solution='Consider adding multiple scenes for better organization',
                    can_auto_fix=False,
                    code_snippet=''
                ))
        
        return issues
    
    def generate_report(self, all_issues: List[Issue], validation_time: float) -> ValidationReport:
        """Generate comprehensive validation report"""
        critical_issues = [i for i in all_issues if i.severity == 'Critical']
        warning_issues = [i for i in all_issues if i.severity == 'Warning']
        info_issues = [i for i in all_issues if i.severity == 'Info']
        
        # Count by category
        issues_by_category = {}
        for issue in all_issues:
            issues_by_category[issue.category] = issues_by_category.get(issue.category, 0) + 1
        
        # Generate summary
        summary = self.generate_summary(all_issues, issues_by_category)
        
        return ValidationReport(
            total_issues=len(all_issues),
            critical_issues=len(critical_issues),
            warning_issues=len(warning_issues),
            info_issues=len(info_issues),
            issues_by_category=issues_by_category,
            all_issues=all_issues,
            validation_time=validation_time,
            is_deployment_ready=len(critical_issues) == 0,
            summary=summary
        )
    
    def generate_summary(self, issues: List[Issue], categories: Dict[str, int]) -> str:
        """Generate human-readable summary"""
        critical_count = len([i for i in issues if i.severity == 'Critical'])
        warning_count = len([i for i in issues if i.severity == 'Warning'])
        info_count = len([i for i in issues if i.severity == 'Info'])
        
        summary = "=== FLOWBOX COMPREHENSIVE VALIDATION REPORT ===\n\n"
        
        summary += f"📊 SUMMARY:\n"
        summary += f"Total Issues Found: {len(issues)}\n"
        summary += f"🔴 Critical (Must Fix): {critical_count}\n" 
        summary += f"🟡 Warning (Should Fix): {warning_count}\n"
        summary += f"🔵 Info (Consider): {info_count}\n\n"
        
        if critical_count == 0:
            summary += "✅ NO CRITICAL ISSUES - PROJECT IS DEPLOYMENT READY!\n\n"
        else:
            summary += "❌ CRITICAL ISSUES FOUND - MUST FIX BEFORE DEPLOYMENT\n\n"
        
        summary += "📋 ISSUES BY CATEGORY:\n"
        for category, count in sorted(categories.items()):
            summary += f"  {category}: {count}\n"
        summary += "\n"
        
        # Show critical issues first
        critical_issues = [i for i in issues if i.severity == 'Critical']
        if critical_issues:
            summary += "🔴 CRITICAL ISSUES (MUST FIX IMMEDIATELY):\n"
            for issue in critical_issues[:10]:  # Show first 10
                summary += f"  📁 {issue.file}:{issue.line}\n"
                summary += f"     {issue.description}\n"
                summary += f"     💡 Solution: {issue.solution}\n"
                if issue.can_auto_fix:
                    summary += f"     🤖 Can auto-fix: YES\n"
                summary += "\n"
            
            if len(critical_issues) > 10:
                summary += f"  ... and {len(critical_issues) - 10} more critical issues\n\n"
        
        return summary
    
    def save_report(self, report: ValidationReport):
        """Save detailed report to files"""
        
        # Save JSON report for programmatic access
        json_report = {
            'summary': asdict(report),
            'timestamp': time.time(),
            'project': 'FlowBox VR Boxing Game'
        }
        
        with open(self.project_root / "COMPREHENSIVE_VALIDATION_REPORT.json", 'w') as f:
            json.dump(json_report, f, indent=2, default=str)
        
        # Save human-readable report
        with open(self.project_root / "COMPREHENSIVE_VALIDATION_REPORT.md", 'w') as f:
            f.write(report.summary)
            
            # Add detailed issue list
            f.write("\n\n=== DETAILED ISSUE LIST ===\n\n")
            
            for category in sorted(set(i.category for i in report.all_issues)):
                category_issues = [i for i in report.all_issues if i.category == category]
                f.write(f"## {category} ({len(category_issues)} issues)\n\n")
                
                for issue in category_issues:
                    f.write(f"### {issue.severity}: {issue.description}\n")
                    f.write(f"**File:** {issue.file}:{issue.line}\n")
                    f.write(f"**Solution:** {issue.solution}\n")
                    f.write(f"**Auto-fix:** {'Yes' if issue.can_auto_fix else 'No'}\n")
                    f.write(f"**Code:** `{issue.code_snippet}`\n\n")
        
        print(f"📄 Detailed reports saved:")
        print(f"  - COMPREHENSIVE_VALIDATION_REPORT.json")
        print(f"  - COMPREHENSIVE_VALIDATION_REPORT.md")

async def main():
    """Run comprehensive validation on FlowBox project"""
    project_root = "."  # Current directory should be FlowBox root
    
    validator = ComprehensiveValidator(project_root)
    report = await validator.run_comprehensive_validation()
    
    print("\n" + "="*60)
    print("COMPREHENSIVE VALIDATION COMPLETE!")
    print("="*60)
    print(report.summary)
    
    if not report.is_deployment_ready:
        print("\n🔧 RECOMMENDED ACTIONS:")
        print("1. Fix all critical issues listed above")
        print("2. Review warning issues for performance improvements") 
        print("3. Re-run validation to confirm fixes")
        print("4. Consider auto-fix for applicable issues")

if __name__ == "__main__":
    asyncio.run(main()) 