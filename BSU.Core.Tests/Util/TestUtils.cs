using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BSU.Core.Model;
using BSU.Core.Services;
using BSU.CoreCommon.Hashes;
using Moq;
using Xunit;

namespace BSU.Core.Tests.Util
{
    internal static class TestUtils
    {
        public static void SetupHashes<T1>(this Mock<T1> mock, params IModHash[] hashes)
            where T1 : class, IHashCollection =>
            SetupHashes(mock, hashes.ToDictionary(h => h.GetType(), h => (IModHash?)h));

        public static void SetupHashes<T1>(this Mock<T1> mock, Dictionary<Type, IModHash?> hashes) where T1 : class, IHashCollection
        {
            foreach (var (type, hash) in hashes)
            {
                if (hash != null)
                    mock.Setup(o => o.GetHash(type)).Returns(Task.FromResult(hash));
                else
                    mock.Setup(o => o.GetHash(type)).Returns(new TaskCompletionSource<IModHash>().Task);
            }

            mock.Setup(o => o.GetSupportedHashTypes()).Returns(hashes.Keys.ToList());
        }

        public static IModelStorageMod StorageModFromAction(ModActionEnum action, int repoModMatch = 1, int repoModVersion = 1)
        {
            var mock = new Mock<IModelStorageMod>(MockBehavior.Strict);

            var (state, match, version) = action switch
            {
                ModActionEnum.Use => (StorageModStateEnum.Created, repoModMatch, repoModVersion),
                ModActionEnum.Update => (StorageModStateEnum.Created, repoModMatch, repoModVersion - 1),
                ModActionEnum.ContinueUpdate => (StorageModStateEnum.CreatedWithUpdateTarget, repoModMatch, repoModVersion),
                ModActionEnum.Await => (StorageModStateEnum.Updating, repoModMatch, repoModVersion),
                ModActionEnum.AbortAndUpdate => (StorageModStateEnum.CreatedWithUpdateTarget, repoModMatch, repoModVersion - 1),
                ModActionEnum.Unusable => (StorageModStateEnum.Created, repoModMatch - 1, repoModVersion),
                ModActionEnum.AbortActiveAndUpdate => (StorageModStateEnum.Updating, repoModMatch, repoModVersion - 1),
                ModActionEnum.Loading => (StorageModStateEnum.Loading, repoModMatch, repoModVersion),
                ModActionEnum.UnusableSteam => (StorageModStateEnum.Created, repoModMatch, repoModVersion - 1),
                _ => throw new ArgumentOutOfRangeException(nameof(action), action, null)
            };
            var canWrite = action != ModActionEnum.UnusableSteam;

            mock.Setup(m => m.GetState()).Returns(state);
            mock.SetupHashes(new TestMatchHash(match), new TestVersionHash(version));
            mock.Setup(m => m.CanWrite).Returns(canWrite);

            var checkObj = new Mock<IModelRepositoryMod>(MockBehavior.Strict);
            checkObj.SetupHashes(new TestMatchHash(repoModMatch), new TestVersionHash(repoModVersion));
            checkObj.Setup(m => m.State).Returns(LoadingState.Loaded);
            Assert.Equal(action, new ModActionService().GetModAction(checkObj.Object, mock.Object));

            return mock.Object;
        }
    }
}
