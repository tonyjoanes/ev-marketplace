# Mutation Testing Guide

## What is Mutation Testing?

Mutation testing validates the **quality** of your tests by introducing small bugs (mutations) into your source code and checking if your tests catch them.

### How It Works

1. **Mutate**: Stryker modifies your code (e.g., changes `>` to `>=`)
2. **Test**: Runs your test suite against the mutated code
3. **Score**:
   - ✅ **Killed**: Test failed (mutation caught) - Good!
   - ❌ **Survived**: Test passed (mutation not caught) - Test gap!
   - ⏱️ **Timeout**: Test took too long
   - 🔄 **No Coverage**: No tests cover this code

### Mutation Score

```
Mutation Score = (Killed / (Killed + Survived)) × 100%
```

**Target Scores:**
- 🥇 **80%+** - Excellent test quality
- 🥈 **60-80%** - Good test quality
- 🥉 **50-60%** - Acceptable (needs improvement)
- ❌ **<50%** - Poor test quality (build will break)

## Setup

### 1. Install Stryker.NET

```bash
# Restore local tools (including Stryker)
dotnet tool restore

# Or install globally
dotnet tool install -g dotnet-stryker
```

### 2. Verify Installation

```bash
dotnet stryker --version
```

Expected output: `Version: 4.3.0`

## Running Mutation Tests

### Basic Run

```bash
# From repository root
dotnet stryker
```

This will:
- Mutate code in `src/EvMarketplace.Domain/` and `src/EvMarketplace.Infrastructure/Services/`
- Run tests from `tests/EvMarketplace.Tests/`
- Generate an HTML report in `StrykerOutput/`

### View Results

```bash
# Open the HTML report (macOS)
open StrykerOutput/reports/mutation-report.html

# Linux
xdg-open StrykerOutput/reports/mutation-report.html

# Windows
start StrykerOutput/reports/mutation-report.html
```

### Advanced Options

```bash
# Run on specific project
dotnet stryker --project src/EvMarketplace.Domain/EvMarketplace.Domain.csproj

# Only mutate specific files
dotnet stryker --mutate "**/*Seller.cs"

# Run with different mutation level
dotnet stryker --mutation-level basic     # Fewer mutations, faster
dotnet stryker --mutation-level standard  # Default
dotnet stryker --mutation-level complete  # All mutations, slower

# Set custom thresholds
dotnet stryker --threshold-high 90 --threshold-low 70 --threshold-break 60

# Generate only specific reports
dotnet stryker --reporter html --reporter json

# Increase timeout for slow tests
dotnet stryker --timeout-ms 120000

# Use more CPU cores
dotnet stryker --concurrency 8
```

## Configuration

We've configured Stryker in `stryker-config.json`:

```json
{
  "mutate": [
    "src/EvMarketplace.Domain/**/*.cs",
    "src/EvMarketplace.Infrastructure/Services/**/*.cs"
  ],
  "thresholds": {
    "high": 80,    // Green if ≥80%
    "low": 60,     // Yellow if 60-79%
    "break": 50    // Red/fail if <50%
  },
  "mutation-level": "complete",
  "timeout-ms": 60000,
  "concurrency": 4
}
```

## Understanding Mutations

### Common Mutations

| Type | Original | Mutated | Example |
|------|----------|---------|---------|
| **Arithmetic** | `+` | `-` | `price + tax` → `price - tax` |
| **Relational** | `>` | `>=`, `<`, `<=` | `x > 0` → `x >= 0` |
| **Logical** | `&&` | `\|\|` | `a && b` → `a \|\| b` |
| **Equality** | `==` | `!=` | `status == Active` → `status != Active` |
| **Boolean** | `true` | `false` | `return true` → `return false` |
| **Negation** | `!x` | `x` | `!isValid` → `isValid` |
| **Assignment** | `=` | Remove | `x = 5` → `` |
| **Return** | `return x` | `return default(T)` | `return 10` → `return 0` |

### Example: Seller.CanAddListing()

**Original Code:**
```csharp
public bool CanAddListing() =>
    SubscriptionPlan.Plans.GetPlan(CurrentSubscriptionTier)
        .Match(
            Some: plan => plan.MaxListings == -1 || CurrentListingCount < plan.MaxListings,
            None: () => false
        );
```

**Possible Mutations:**
1. ❌ `<` → `<=` (boundary mutation)
   - If `MaxListings = 10` and `CurrentListingCount = 10`
   - Original returns `false` (correct)
   - Mutated returns `true` (incorrect)
   - **Should be killed by**: `CanAddListing_ProTierWithTenListings_ReturnsFalse()`

2. ❌ `false` → `true` (boolean mutation)
   - When plan not found
   - Original returns `false` (correct)
   - Mutated returns `true` (incorrect)
   - **Should be killed by**: Test covering invalid tier

3. ❌ `== -1` → `!= -1` (equality mutation)
   - Enterprise tier with unlimited listings
   - Original returns `true` for unlimited (correct)
   - Mutated returns `false` (incorrect)
   - **Should be killed by**: `CanAddListing_EnterpriseTierWithManyListings_ReturnsTrue()`

## Interpreting Results

### Good Test Quality (80%+ mutation score)

```
Mutation score: 85.4%

✅ 123 Killed
❌  21 Survived
⏱️   5 Timeout
🔄   2 No Coverage

Files with low scores:
- Seller.cs: 92% (8/87 mutations survived)
- Payment.cs: 78% (15/68 mutations survived)
```

**Action:** Review survived mutations, add tests for edge cases.

### Poor Test Quality (<60% mutation score)

```
Mutation score: 45.2%

✅  67 Killed
❌  81 Survived
⏱️   3 Timeout
🔄  15 No Coverage

Files with low scores:
- Subscription.cs: 38% (43/69 mutations survived)
- AuthService.cs: 52% (28/58 mutations survived)
```

**Action:**
1. Increase code coverage (15 mutations have no tests!)
2. Add tests for boundary conditions
3. Test error paths more thoroughly

## Expected Results for Our Tests

Given our comprehensive test suite (86 tests), we should expect:

### Domain Models (Expected: 85-95%)
- ✅ **Seller.cs**: High score (23 tests)
- ✅ **Subscription.cs**: High score (24 tests)
- ✅ **Payment.cs**: High score (20 tests)
- ✅ **ElectricVehicle.cs**: High score (9 tests)

### Services (Expected: 75-85%)
- ✅ **AuthService.cs**: Good score (19 tests)
- ⚠️ May have some survived mutations in JWT generation (complex code)

### Potential Weak Spots
- Edge cases in validation logic
- Option/Either handling in Match expressions
- Boundary conditions in numeric comparisons

## CI/CD Integration

### GitHub Actions

```yaml
- name: Run Mutation Tests
  run: dotnet stryker --reporter "cleartext" --reporter "json"

- name: Check Mutation Score
  run: |
    SCORE=$(jq '.thresholds.break' StrykerOutput/reports/mutation-report.json)
    if [ $SCORE -lt 50 ]; then
      echo "Mutation score below threshold!"
      exit 1
    fi
```

### Azure DevOps

```yaml
- task: DotNetCoreCLI@2
  displayName: 'Run Stryker Mutation Tests'
  inputs:
    command: 'custom'
    custom: 'stryker'
    arguments: '--reporter html --reporter json'

- task: PublishPipelineArtifact@1
  inputs:
    targetPath: 'StrykerOutput/reports'
    artifact: 'mutation-reports'
```

## Best Practices

1. **Run Regularly**: Weekly or before major releases
2. **Start Small**: Run on one module first, then expand
3. **Ignore Noisy Mutations**: Use `ignore-mutations` for string mutations if too many
4. **Set Realistic Thresholds**: Start at 50%, increase gradually
5. **Review Survived Mutations**: Each survivor is a potential bug
6. **Don't Chase 100%**: 85-90% is excellent, diminishing returns above that

## Troubleshooting

### Tests Take Too Long

```bash
# Increase timeout
dotnet stryker --timeout-ms 120000

# Reduce mutations
dotnet stryker --mutation-level basic

# Increase concurrency
dotnet stryker --concurrency 8
```

### Too Many String Mutations

```json
{
  "ignore-mutations": ["string"]
}
```

### Out of Memory

```bash
# Reduce concurrency
dotnet stryker --concurrency 2
```

## Learn More

- **Stryker.NET Docs**: https://stryker-mutator.io/docs/stryker-net/introduction
- **Mutation Testing Guide**: https://en.wikipedia.org/wiki/Mutation_testing
- **Best Practices**: https://stryker-mutator.io/docs/General/mutation-testing-elements

## Quick Start Checklist

- [ ] Install Stryker: `dotnet tool restore`
- [ ] Run first mutation test: `dotnet stryker`
- [ ] Review HTML report
- [ ] Identify survived mutations
- [ ] Add tests to kill survivors
- [ ] Re-run and improve score
- [ ] Set up CI/CD integration
- [ ] Celebrate when you hit 80%! 🎉
