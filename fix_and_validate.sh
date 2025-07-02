#!/bin/bash

echo "🚀 FlowBox Complete Fix & Validation Pipeline"
echo "============================================="
echo "1. Run comprehensive validation (find all issues)"
echo "2. Auto-fix critical issues" 
echo "3. Re-run validation (verify fixes)"
echo ""

# Check if we're in the right directory
if [ ! -d "Assets/Scripts" ]; then
    echo "❌ Error: Must run from FlowBox project root (Assets/Scripts not found)"
    exit 1
fi

echo "📊 STEP 1: Initial comprehensive validation"
echo "----------------------------------------"
python3 complete_validation_runner.py > initial_validation.log 2>&1

# Show summary of initial issues
echo "📋 Initial Issues Found:"
if [ -f "COMPREHENSIVE_VALIDATION_REPORT.md" ]; then
    grep -E "🔴|🟡|🔵" COMPREHENSIVE_VALIDATION_REPORT.md | head -5
else
    echo "⚠️ No validation report found"
fi

echo ""
echo "🤖 STEP 2: Auto-fixing critical issues"
echo "------------------------------------"
python3 auto_fix_critical_issues.py

echo ""
echo "🔍 STEP 3: Re-validation after fixes"
echo "----------------------------------"
python3 complete_validation_runner.py > post_fix_validation.log 2>&1

# Show comparison
echo ""
echo "📊 BEFORE vs AFTER COMPARISON:"
echo "=============================="

if [ -f "initial_validation.log" ] && [ -f "post_fix_validation.log" ]; then
    echo "📈 Initial Issues:"
    grep "🔴 Critical:" initial_validation.log | tail -1
    grep "🟡 Warning:" initial_validation.log | tail -1
    grep "🔵 Info:" initial_validation.log | tail -1
    
    echo ""
    echo "📈 After Auto-Fix:"
    grep "🔴 Critical:" post_fix_validation.log | tail -1
    grep "🟡 Warning:" post_fix_validation.log | tail -1  
    grep "🔵 Info:" post_fix_validation.log | tail -1
    
    echo ""
    
    # Calculate improvement
    INITIAL_CRITICAL=$(grep "🔴 Critical:" initial_validation.log | tail -1 | grep -o '[0-9]\+')
    FINAL_CRITICAL=$(grep "🔴 Critical:" post_fix_validation.log | tail -1 | grep -o '[0-9]\+')
    
    if [ ! -z "$INITIAL_CRITICAL" ] && [ ! -z "$FINAL_CRITICAL" ]; then
        IMPROVEMENT=$((INITIAL_CRITICAL - FINAL_CRITICAL))
        echo "🎯 CRITICAL ISSUES FIXED: $IMPROVEMENT"
        
        if [ "$FINAL_CRITICAL" -eq 0 ]; then
            echo "🎉 ALL CRITICAL ISSUES RESOLVED - PROJECT IS DEPLOYMENT READY!"
        else
            echo "⚠️ $FINAL_CRITICAL critical issues remain (manual fix needed)"
        fi
    fi
else
    echo "⚠️ Could not compare results - log files missing"
fi

echo ""
echo "📄 Generated Reports:"
echo "===================="
echo "✅ COMPREHENSIVE_VALIDATION_REPORT.md (Latest validation)"
echo "✅ AUTO_FIX_REPORT.md (Fix details)"
echo "✅ initial_validation.log (Before fixes)"  
echo "✅ post_fix_validation.log (After fixes)"

if [ -f "restore_from_backup.sh" ]; then
    echo "🔄 restore_from_backup.sh (Emergency restore)"
fi

echo ""
echo "🎯 RECOMMENDED NEXT STEPS:"
echo "========================="
echo "1. Review the fix report: AUTO_FIX_REPORT.md"
echo "2. Test your VR application in Unity"
echo "3. Check latest validation: COMPREHENSIVE_VALIDATION_REPORT.md"
echo "4. If issues occur, run: ./restore_from_backup.sh"
echo ""
echo "✅ Complete fix & validation pipeline finished!" 