#!/bin/bash
# Pre-commit hook for SaveStateReborn
# This script runs before each commit to ensure code quality

set -e

RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
CYAN='\033[0;36m'
NC='\033[0m' # No Color

echo -e "${CYAN}🔍 Running pre-commit checks...${NC}"
echo ""

HAS_ERRORS=0

# Check 1: No 'return null' in public APIs
echo -n -e "✓ Check 1: No 'return null' in public APIs..."
NULL_RETURNS=$(grep -r "return null;" src --include="*.cs" | grep -v "Test.cs" | grep -v "private" | head -5 || true)
if [ -n "$NULL_RETURNS" ]; then
    echo -e " ${RED}FAILED${NC}"
    echo -e "${YELLOW}  Found 'return null' statements that may violate Result pattern:${NC}"
    echo "$NULL_RETURNS" | head -5
    HAS_ERRORS=1
else
    echo -e " ${GREEN}PASSED${NC}"
fi

# Check 2: No DateTime.Now usage
echo -n -e "✓ Check 2: No DateTime.Now usage..."
DATETIME_NOW=$(grep -r "DateTime\.\(Now\|UtcNow\)" src --include="*.cs" | grep -v "Test.cs" | grep -v "TimeProvider" | head -5 || true)
if [ -n "$DATETIME_NOW" ]; then
    echo -e " ${RED}FAILED${NC}"
    echo -e "${YELLOW}  Found DateTime.Now/DateTime.UtcNow usage (use ITimeProvider instead):${NC}"
    echo "$DATETIME_NOW" | head -5
    HAS_ERRORS=1
else
    echo -e " ${GREEN}PASSED${NC}"
fi

# Check 3: Build with 0 warnings
echo -n -e "✓ Check 3: Build with 0 warnings..."
if dotnet build SaveStateReborn.Core.sln --warnaserror --verbosity minimal > /dev/null 2>&1; then
    echo -e " ${GREEN}PASSED${NC}"
else
    echo -e " ${RED}FAILED${NC}"
    echo -e "${YELLOW}  Build failed with warnings treated as errors${NC}"
    HAS_ERRORS=1
fi

# Check 4: Architecture tests
echo -n -e "✓ Check 4: Architecture tests..."
if dotnet test tests/SaveState.Infrastructure.Tests --filter "FullyQualifiedName~ArchitectureTests" --verbosity minimal > /dev/null 2>&1; then
    echo -e " ${GREEN}PASSED${NC}"
else
    echo -e " ${RED}FAILED${NC}"
    HAS_ERRORS=1
fi

echo ""
if [ $HAS_ERRORS -eq 1 ]; then
    echo -e "${RED}❌ Pre-commit checks FAILED${NC}"
    echo -e "${YELLOW}Please fix the issues above before committing.${NC}"
    exit 1
else
    echo -e "${GREEN}✅ All pre-commit checks passed!${NC}"
    exit 0
fi
