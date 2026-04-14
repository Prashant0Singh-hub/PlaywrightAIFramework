# Fix Instructions — Final Session

## What was wrong (all 12 errors)

### Errors 1-9: Generated_20260413_174427.cs — `Page` does not exist
Claude generated code using `Page` directly, but BaseTest exposes `Browser.Page`.
**Fix:** Delete the bad generated file. The new AITestGenerator system prompt now
explicitly instructs Claude to use `Browser.Page` everywhere.

### Error 10: FlipkartPage.cs(79) — `SaveSessionAsync` takes 2 args
Old FlipkartPage.cs was calling `SaveSessionAsync(context, path)` but the new
BrowserFactory.SaveSessionAsync() takes 0 args (it uses internal state).
**Fix:** FlipkartPage now calls `_browser.SaveSessionAsync()` with no arguments.

### Error 11-12: FlipkartTests.cs — `AITestGenerator(IPage)` constructor + `GenerateAsync`
FlipkartTests was using old API: `new AITestGenerator(page, prompt)` + `.GenerateAsync()`.
New API: `new AITestGenerator(page)` + `.GenerateTestsAsync(prompt)`.
**Fix:** FlipkartTests updated to use the correct constructor and method name.

---

## Files to Replace

| File | Location in project |
|------|---------------------|
| `Core/AITestGenerator.cs` | Replace existing |
| `Core/BrowserFactory.cs` | Replace existing |
| `Pages/FlipkartPage.cs` | Replace existing |
| `Tests/FlipkartTests.cs` | Replace existing |

## Step 1 — Delete the bad generated file

```cmd
del "Tests\Generated\Generated_20260413_174427.cs"
```

Also delete any other generated files that may have the same problem:

```cmd
del "Tests\Generated\*.cs"
```

## Step 2 — Replace the 4 files from the zip

## Step 3 — Build

```cmd
dotnet build
```
Expected: **0 errors, 0 warnings**

## Step 4 — Run

### Login (one time only):
```cmd
dotnet test --filter "Name=TC01_Login_OTP_Once_ThenRemember"
```

### Generate + Execute tests from a prompt:
```cmd
$env:PLAYWRIGHT_PROMPT = "Search for campus shoes for men, click on HURRICANE Running Shoes, click Buy at 696"
dotnet test --filter "Name=TC02_Prompt_To_TestScenarios"
```

---

## Sample Prompts

```powershell
# Campus shoes buy flow
$env:PLAYWRIGHT_PROMPT = "Search for campus shoes for men, click on HURRICANE Running Shoes, click Buy at 696"

# Samsung mobile
$env:PLAYWRIGHT_PROMPT = "Search for Samsung Galaxy mobile, click the first product, click Buy Now"

# Nike shoes with size
$env:PLAYWRIGHT_PROMPT = "Search for Nike running shoes for men, click the first product, select size 9, click Buy Now"

# Laptop
$env:PLAYWRIGHT_PROMPT = "Search for HP laptop, click the first result, click Buy Now"

# boAt headphones
$env:PLAYWRIGHT_PROMPT = "Search for boAt headphones, click the first product, click Buy Now"
```

Run any prompt:
```cmd
dotnet test --filter "Name=TC02_Prompt_To_TestScenarios"
```

## Session Notes

- Session saved to: `Session/flipkart_storage.json`
- To re-login: `del "Session\flipkart_storage.json"` then run TC01 again
- Session persists across ALL runs — login popup never shows again
