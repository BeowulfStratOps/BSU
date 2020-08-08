using System;
using System.Collections.Generic;
using System.Linq;
using BSU.Core.Hashes;
using BSU.Core.Model;
using BSU.Core.Tests.Mocks;
using Xunit;

namespace BSU.Core.Tests
{
    public class CoreCalculationsTest
    {
        private static List<RepositoryModState> GetPossibleRepoStates()
        {
            var possibleRepoStates = new List<RepositoryModState>();
            foreach (var repoMatch in new[] {"a", "b", null})
            {
                foreach (var repoVersion in new[] {"1", "2", "3", "4", null})
                {
                    foreach (var error in new[] {null, new TestException()})
                    {
                        var repoMatchHash = TestUtils.GetMatchHash(repoMatch);
                        var repoVersionHash = TestUtils.GetVersionHash(repoVersion);
                        try
                        {
                            possibleRepoStates.Add(new RepositoryModState(repoMatchHash, repoVersionHash, error));
                        }
                        catch (InvalidOperationException)
                        {
                            // Invalid state
                        }
                    }
                }
            }

            return possibleRepoStates;
        }

        private static List<StorageModState> GetPossibleStorageStates()
        {
            var possibleStorageModStates = new List<StorageModState>();

            foreach (var storageMatch in new[] {"a", "b", null})
            {
                foreach (var storageVersion in new[] {"1", "2", "3", "4", null})
                {
                    foreach (var jobTarget in new[] {"1", "2", "3", "4", null})
                    {
                        foreach (var updateTarget in new[] {"1", "2", "3", "4", null})
                        {
                            foreach (var state in Enum.GetValues(typeof(StorageModStateEnum)).Cast<StorageModStateEnum>())
                            {
                                foreach (var error in new[] {null, new TestException()})
                                {
                                    var storageMatchHash = TestUtils.GetMatchHash(storageMatch);
                                    var storageVersionHash = TestUtils.GetVersionHash(storageVersion);
                                    var storageUpdateTarget = TestUtils.GetUpdateTarget(updateTarget);
                                    var storageJobTarget = TestUtils.GetUpdateTarget(jobTarget);
                                    try
                                    {
                                        possibleStorageModStates.Add(new StorageModState(storageMatchHash,
                                            storageVersionHash, storageUpdateTarget, storageJobTarget, state, error));
                                    }
                                    catch (InvalidOperationException)
                                    {
                                        // invalid state
                                    }
                                }
                            }
                        }
                    }
                }
            }

            return possibleStorageModStates;
        }
        
        [Fact]
        public void NoExceptions() // TODO: generate test cases dynamically, instead of looping states?
        {
            var count = 0;

            var possibleStorageStates = GetPossibleStorageStates();
            
            foreach (var repoState in GetPossibleRepoStates())
            {
                foreach (var storageState in possibleStorageStates)
                {
                    var match = CoreCalculation.IsMatch(repoState, storageState);
                    if (match != CoreCalculation.ModMatch.Match) continue;
                    CoreCalculation.CalculateAction(repoState, storageState, true);
                    CoreCalculation.CalculateAction(repoState, storageState, false);
                }
            }
            
            Assert.Equal(0, count);
        }
    }
}