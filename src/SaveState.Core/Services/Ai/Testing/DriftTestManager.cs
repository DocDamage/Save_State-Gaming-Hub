using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;

namespace SaveState.Core.Services.Ai.Testing
{
    public class DriftTestManager
    {
        private readonly IAiTestHarness _testHarness;
        private readonly string _testCasesPath;
        private List<TestCase> _goldenMasterTests = new();

        public DriftTestManager(IAiTestHarness testHarness, string testCasesPath = "golden_master_tests.json")
        {
            _testHarness = testHarness;
            _testCasesPath = testCasesPath;
        }

        public async Task LoadTestCasesAsync()
        {
            if (File.Exists(_testCasesPath))
            {
                var json = await File.ReadAllTextAsync(_testCasesPath);
                _goldenMasterTests = JsonSerializer.Deserialize<List<TestCase>>(json) ?? new List<TestCase>();
            }
            else
            {
                // Initialize with defaults if missing
                _goldenMasterTests = new List<TestCase>
                {
                    new TestCase { TestId = "G-001", Input = "Who are you?", ExpectedOutput = "I am the SaveState AI assistant.", SimilarityThreshold = 0.5 },
                    new TestCase { TestId = "G-002", Input = "What is the capital of France?", ExpectedOutput = "Paris.", SimilarityThreshold = 0.8 }
                };
                await SaveTestCasesAsync();
            }
        }

        public async Task SaveTestCasesAsync()
        {
            var json = JsonSerializer.Serialize(_goldenMasterTests, new JsonSerializerOptions { WriteIndented = true });
            await File.WriteAllTextAsync(_testCasesPath, json);
        }

        public void AddTestCase(string input, string expectedOutput, double threshold = 0.8)
        {
            _goldenMasterTests.Add(new TestCase
            {
                TestId = Guid.NewGuid().ToString(),
                Input = input,
                ExpectedOutput = expectedOutput,
                SimilarityThreshold = threshold
            });
        }

        public async Task<RegressionTestResult> RunDriftCheckAsync()
        {
            if (_goldenMasterTests.Count == 0) await LoadTestCasesAsync();
            
            return await _testHarness.RunRegressionTestsAsync(_goldenMasterTests);
        }

        public async Task UpdateGoldenMasterAsync(string testId, string newExpectedOutput)
        {
            var test = _goldenMasterTests.FirstOrDefault(t => t.TestId == testId);
            if (test != null)
            {
                test.ExpectedOutput = newExpectedOutput;
                await SaveTestCasesAsync();
            }
        }
    }
}
