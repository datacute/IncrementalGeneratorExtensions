using System;
using System.Linq;
using Xunit;

namespace Datacute.IncrementalGeneratorExtensions.Tests
{
    public class GeneratorStageDescriptionsTests
    {
        /// <summary>
        /// Asserts that every value defined in <see cref="GeneratorStage"/> has a corresponding
        /// entry in <see cref="GeneratorStageDescriptions.GeneratorStageNameMap"/>.
        /// Catches the drift bug where a new stage is added to the enum but the description
        /// dictionary is not updated.
        /// </summary>
        [Fact]
        public void GeneratorStageNameMap_ContainsEntryForEveryGeneratorStageValue()
        {
            var allStages = (GeneratorStage[])Enum.GetValues(typeof(GeneratorStage));

            var missingStages = allStages
                .Where(stage => !GeneratorStageDescriptions.GeneratorStageNameMap.ContainsKey((int)stage))
                .ToArray();

            Assert.Empty(missingStages);
        }

        /// <summary>
        /// Asserts that every key in <see cref="GeneratorStageDescriptions.GeneratorStageNameMap"/>
        /// corresponds to a defined <see cref="GeneratorStage"/> value.
        /// Catches stale entries left behind after a stage is removed.
        /// </summary>
        [Fact]
        public void GeneratorStageNameMap_ContainsNoEntriesForUndefinedGeneratorStageValues()
        {
            var definedValues = ((GeneratorStage[])Enum.GetValues(typeof(GeneratorStage)))
                .Select(s => (int)s)
                .ToArray();

            var extraKeys = GeneratorStageDescriptions.GeneratorStageNameMap.Keys
                .Where(key => Array.IndexOf(definedValues, key) < 0)
                .ToArray();

            Assert.Empty(extraKeys);
        }
    }
}

