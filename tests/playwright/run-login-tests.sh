#!/bin/bash
# Script to run login/register page tests with screenshots
# This demonstrates the fix for the Raspberry Pi login crash issue

echo "=========================================="
echo "Login/Register Page Test with Screenshots"
echo "=========================================="
echo ""
echo "This script runs the Playwright tests that verify:"
echo "1. Login page loads without crashing"
echo "2. Register page loads without crashing"
echo "3. Screenshots are captured for documentation"
echo ""

# Check if the web app is running
if ! curl -s http://localhost:5274 > /dev/null; then
    echo "❌ Web application is not running on http://localhost:5274"
    echo ""
    echo "Please start the web application first:"
    echo "  cd ../../src/Privatekonomi.Web"
    echo "  dotnet run"
    echo ""
    exit 1
fi

echo "✅ Web application is running"
echo ""

# Install dependencies if needed
if [ ! -d "node_modules" ]; then
    echo "📦 Installing npm dependencies..."
    npm install
    echo ""
fi

# Install Playwright browsers if needed
if [ ! -d "$HOME/.cache/ms-playwright/chromium-1194" ]; then
    echo "🌐 Installing Playwright browsers..."
    npx playwright install chromium
    echo ""
fi

# Create screenshots directory if it doesn't exist
mkdir -p screenshots

echo "🧪 Running Playwright tests..."
echo ""
echo "Tests will:"
echo "  - Navigate to /Account/Login"
echo "  - Verify the page loads without crashing"
echo "  - Verify form elements are present"
echo "  - Take screenshot: screenshots/login-page.png"
echo ""
echo "  - Navigate to /Account/Register"
echo "  - Verify the page loads without crashing"
echo "  - Verify form elements are present"
echo "  - Take screenshot: screenshots/register-page.png"
echo ""

# Run the tests
npx playwright test login.spec.ts register.spec.ts

TEST_RESULT=$?

echo ""
echo "=========================================="
if [ $TEST_RESULT -eq 0 ]; then
    echo "✅ All tests passed!"
    echo ""
    echo "Screenshots saved:"
    echo "  - screenshots/login-page.png"
    echo "  - screenshots/register-page.png"
    echo ""
    echo "These screenshots demonstrate that the login/register pages"
    echo "now load correctly on Raspberry Pi in production mode."
else
    echo "❌ Some tests failed"
    echo ""
    echo "Check the test output above for details."
fi
echo "=========================================="

exit $TEST_RESULT
