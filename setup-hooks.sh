#!/bin/bash
# Setup script to install git hooks for local development safeguards

set -e

# Check if we're in the repo root
if [ ! -d ".git" ]; then
  echo "Error: Please run this script from the repository root"
  exit 1
fi

echo "Setting up git hooks..."

# Configure git to use .githooks directory
git config core.hooksPath .githooks

# Make hooks executable
chmod +x .githooks/*

echo "✓ Git hooks installed successfully!"
echo ""
echo "The following hooks are now active:"
echo "  - pre-commit: Runs linting and code analysis before each commit"
echo ""
echo "To skip hooks (not recommended), use: git commit --no-verify"
