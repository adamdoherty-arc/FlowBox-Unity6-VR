#!/bin/bash
echo "🔄 Restoring FlowBox from auto-fix backup..."
echo "Backup location: AutoFix_Backups/20250702_003752"

if [ ! -d "AutoFix_Backups/20250702_003752" ]; then
    echo "❌ Backup directory not found: AutoFix_Backups/20250702_003752"
    exit 1
fi

echo "⚠️ This will overwrite current files with backup. Continue? (y/N)"
read -n 1 -r
echo
if [[ $REPLY =~ ^[Yy]$ ]]; then
    echo "🔄 Restoring Assets/Scripts..."
    rm -rf Assets/Scripts
    cp -r "AutoFix_Backups/20250702_003752/Scripts" Assets/Scripts
    echo "✅ Restore complete!"
else
    echo "❌ Restore cancelled"
fi
