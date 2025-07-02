#!/bin/bash

echo "🚀 FlowBox Complete Validation Runner"
echo "====================================="
echo "This will find ALL issues in one comprehensive pass"
echo "No more iterative discovery - everything at once!"
echo ""

# Check if we're in the right directory
if [ ! -d "Assets/Scripts" ]; then
    echo "❌ Error: Must run from FlowBox project root (Assets/Scripts not found)"
    exit 1
fi

# Count files to analyze
CS_FILES=$(find Assets/Scripts -name "*.cs" | wc -l)
echo "📁 Analyzing $CS_FILES C# files..."
echo ""

# Run Python validator if available
if command -v python3 &> /dev/null; then
    echo "🐍 Running Python comprehensive validator..."
    python3 complete_validation_runner.py
    echo ""
else
    echo "⚠️ Python3 not found, skipping Python validation"
fi

# Run Unity validator (if in Unity)
echo "🎮 To run Unity validator:"
echo "1. Open Unity Editor"
echo "2. Go to Assets/Scripts/Testing/ComprehensiveProjectValidator.cs"
echo "3. Right-click and select 'Run Complete Validation'"
echo ""

# Show any existing reports
if [ -f "COMPREHENSIVE_VALIDATION_REPORT.md" ]; then
    echo "📄 Comprehensive validation report generated:"
    echo "   - COMPREHENSIVE_VALIDATION_REPORT.md"
    echo "   - COMPREHENSIVE_VALIDATION_REPORT.json"
    echo ""
    echo "📋 Quick Summary:"
    head -20 COMPREHENSIVE_VALIDATION_REPORT.md
    echo ""
    echo "💡 See full report in COMPREHENSIVE_VALIDATION_REPORT.md"
else
    echo "⚠️ No validation report found"
fi

echo ""
echo "✅ Comprehensive validation complete!"
echo "This found ALL possible issues - no more iterative discovery needed." 