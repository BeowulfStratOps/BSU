using System;
using System.Collections.Generic;
using BSU.Core.Hashes;
using BSU.Core.Model;
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
                    var repoMatchHash = TestUtils.GetMatchHash(repoMatch);
                    var repoVersionHash = TestUtils.GetVersionHash(repoVersion);
                    possibleRepoStates.Add(new RepositoryModState(repoMatchHash, repoVersionHash));
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
                            foreach (var versionHashRequested in new[] {false, true})
                            {
                                var storageMatchHash = TestUtils.GetMatchHash(storageMatch);
                                var storageVersionHash = TestUtils.GetVersionHash(storageVersion);
                                var storageUpdateTarget = TestUtils.GetUpdateTarget(updateTarget);
                                var storageJobTarget = TestUtils.GetUpdateTarget(jobTarget);
                                try
                                {
                                    possibleStorageModStates.Add(new StorageModState(storageMatchHash, storageVersionHash,
                                        storageUpdateTarget, storageJobTarget, versionHashRequested));
                                }
                                catch (InvalidOperationException e)
                                {
                                    // invalid state.
                                }
                            }
                        }
                    }
                }
            }

            return possibleStorageModStates;
        }
        
        [Fact]
        public void NoExceptions()
        {
            var count = 0;

            var possibleStorageStates = GetPossibleStorageStates();
            
            foreach (var repoState in GetPossibleRepoStates())
            {
                foreach (var storageState in possibleStorageStates)
                {
                    try
                    {
                        var (match, _) = CoreCalculation.IsMatch(repoState, storageState);
                        if (!match) continue;
                        CoreCalculation.CalculateAction(repoState, storageState, true);
                        CoreCalculation.CalculateAction(repoState, storageState, false);
                    }
                    catch (Exception e)
                    {
                        count++;
                    }
                }
            }
            
            Assert.Equal(0, count);
        }
    }
}