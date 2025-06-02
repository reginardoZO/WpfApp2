using Microsoft.VisualStudio.TestTools.UnitTesting;
using WpfApp2; // Assuming this is the namespace for Cable, ConduitSize, ConduitCalculator
using System.Collections.Generic;
using System.Linq; // For .Contains on strings etc.
using System; // For Math.PI

namespace WpfApp2.Tests
{
    [TestClass]
    public class ConduitCalculatorTests
    {
        private ConduitCalculator calculator;

        [TestInitialize]
        public void Setup()
        {
            calculator = new ConduitCalculator();
        }

        [TestMethod]
        public void Test_SingleStandardCable_CorrectConduit_ShouldSelectMediumPVC()
        {
            // Arrange
            var selectedCables = new List<Cable> { new Cable("C1", "Cable 1", 10, false, 0) }; // OD=10
            // Area = PI * (10/2)^2 = PI * 25 = 78.5398
            // Fill = 53%, Required Usable Area = 78.5398
            // Conduit Area Needed = 78.5398 / 0.53 = 148.188
            var allConduits = new List<ConduitSize> {
                new ConduitSize("PVC-Small", "PVC", 140),      // Usable = 140 * 0.53 = 74.2 (Too small)
                new ConduitSize("PVC-Medium", "PVC", 150),     // Usable = 150 * 0.53 = 79.5 (Suitable)
                new ConduitSize("PVC-Large", "PVC", 200),      // Usable = 200 * 0.53 = 106
                new ConduitSize("EMT-Medium", "EMT", 150)      // Different type
            };
            string selectedConduitType = "PVC";

            // Act
            var result = calculator.CalculateConduit(selectedCables, allConduits, selectedConduitType);

            // Assert
            Assert.AreEqual("PVC-Medium", result.ConduitName, "Incorrect conduit selected.");
            Assert.IsTrue(result.CalculationSteps.Contains("1 cable: Using 53% fill capacity."), "Fill percentage log not found or incorrect.");
            Assert.IsTrue(result.CalculationSteps.Contains("Selected Conduit: PVC-Medium"), "Selected conduit log entry not found.");
            Assert.IsTrue(result.CalculationSteps.Contains($"Total calculated cable bundle area: {78.5398:F4}"), "Total cable area log incorrect.");
        }

        [TestMethod]
        public void Test_TwoStandardCables_CorrectConduit_ShouldSelectSmallPVC()
        {
            // Arrange
            var selectedCables = new List<Cable> {
                new Cable("C1", "Cable 1", 8, false, 0), // Area = PI*(4^2) = 50.2655
                new Cable("C2", "Cable 2", 6, false, 0)  // Area = PI*(3^2) = 28.2743
            };
            // Total Area = 50.2655 + 28.2743 = 78.5398
            // Fill = 31%, Required Usable Area = 78.5398
            // Conduit Area Needed = 78.5398 / 0.31 = 253.354
            var allConduits = new List<ConduitSize> {
                new ConduitSize("PVC-TooSmall", "PVC", 250), // Usable = 250 * 0.31 = 77.5 (Too small)
                new ConduitSize("PVC-Small", "PVC", 260),    // Usable = 260 * 0.31 = 80.6 (Suitable)
                new ConduitSize("PVC-Medium", "PVC", 300),
                new ConduitSize("EMT-Small", "EMT", 260)
            };
            string selectedConduitType = "PVC";

            // Act
            var result = calculator.CalculateConduit(selectedCables, allConduits, selectedConduitType);

            // Assert
            Assert.AreEqual("PVC-Small", result.ConduitName);
            Assert.IsTrue(result.CalculationSteps.Contains("2 cables: Using 31% fill capacity."));
            Assert.IsTrue(result.CalculationSteps.Contains("Selected Conduit: PVC-Small"));
            Assert.IsTrue(result.CalculationSteps.Contains($"Total calculated cable bundle area: {78.5398:F4}"));
        }

        [TestMethod]
        public void Test_ThreeStandardCables_CorrectConduit_ShouldSelectLargePVC()
        {
            // Arrange
            var selectedCables = new List<Cable> {
                new Cable("C1", "C1", 5, false, 0), // Area = PI*(2.5^2) = 19.6350
                new Cable("C2", "C2", 5, false, 0), // Area = 19.6350
                new Cable("C3", "C3", 5, false, 0)  // Area = 19.6350
            };
            // Total Area = 3 * 19.6350 = 58.9050
            // Fill = 40%, Required Usable Area = 58.9050
            // Conduit Area Needed = 58.9050 / 0.40 = 147.2625
             var allConduits = new List<ConduitSize> {
                new ConduitSize("PVC-Small", "PVC", 140),   // Usable = 140 * 0.40 = 56 (Too small)
                new ConduitSize("PVC-Medium", "PVC", 147),  // Usable = 147 * 0.40 = 58.8 (Too small, due to precision)
                new ConduitSize("PVC-Large", "PVC", 148),   // Usable = 148 * 0.40 = 59.2 (Suitable)
                new ConduitSize("PVC-XLarge", "PVC", 200)
            };
            string selectedConduitType = "PVC";

            // Act
            var result = calculator.CalculateConduit(selectedCables, allConduits, selectedConduitType);

            // Assert
            Assert.AreEqual("PVC-Large", result.ConduitName);
            Assert.IsTrue(result.CalculationSteps.Contains("3 cables (>2): Using 40% fill capacity."));
            Assert.IsTrue(result.CalculationSteps.Contains("Selected Conduit: PVC-Large"));
            Assert.IsTrue(result.CalculationSteps.Contains($"Total calculated cable bundle area: {58.9050:F4}"));
        }

        [TestMethod]
        public void Test_SingleTriplexCable_CorrectConduit_ShouldUse40PercentFill()
        {
            // Arrange
            // Phase OD=10 (Area/conductor = PI*25 = 78.5398), GroundOD=5 (Area = PI*6.25 = 19.6350)
            // Total Phase Area = 3 * 78.5398 = 235.6194
            // Total Cable Area = 235.6194 + 19.6350 = 255.2544
            var selectedCables = new List<Cable> { new Cable("TR1", "Triplex 1", 10, true, 5) };
            // Effective cables = 4, Fill = 40%
            // Required Usable Area = 255.2544
            // Conduit Area Needed = 255.2544 / 0.40 = 638.136
            var allConduits = new List<ConduitSize> {
                new ConduitSize("PVC-Small", "PVC", 630),   // Usable = 630 * 0.40 = 252 (Too small)
                new ConduitSize("PVC-Medium", "PVC", 638),  // Usable = 638 * 0.40 = 255.2 (Too small, precision)
                new ConduitSize("PVC-Large", "PVC", 639),   // Usable = 639 * 0.40 = 255.6 (Suitable)
            };
            string selectedConduitType = "PVC";

            // Act
            var result = calculator.CalculateConduit(selectedCables, allConduits, selectedConduitType);

            // Assert
            Assert.AreEqual("PVC-Large", result.ConduitName);
            Assert.IsTrue(result.CalculationSteps.Contains("4 cables (>2): Using 40% fill capacity."), "Fill rule for 4 effective cables not logged correctly.");
            Assert.IsTrue(result.CalculationSteps.Contains("- Type: Triplex"), "Triplex cable type not logged.");
            Assert.IsTrue(result.CalculationSteps.Contains("Effective cables for fill rule: 4"), "Triplex effective cable count not logged.");
            Assert.IsTrue(result.CalculationSteps.Contains("Selected Conduit: PVC-Large"));
            Assert.IsTrue(result.CalculationSteps.Contains($"Total calculated cable bundle area: {255.2544:F4}"));
        }

        [TestMethod]
        public void Test_Overflow_WhenNoSufficientConduitExists()
        {
            // Arrange
            var selectedCables = new List<Cable> { new Cable("C1", "Cable 1", 20, false, 0) }; // Area = PI * (10^2) = 314.159
            // Fill = 53%, Required Usable Area = 314.159
            // Conduit Area Needed = 314.159 / 0.53 = 592.75
            var allConduits = new List<ConduitSize> {
                new ConduitSize("PVC-Small", "PVC", 100),
                new ConduitSize("PVC-Medium", "PVC", 500) // Usable = 500 * 0.53 = 265 (Too small)
            };
            string selectedConduitType = "PVC";

            // Act
            var result = calculator.CalculateConduit(selectedCables, allConduits, selectedConduitType);

            // Assert
            Assert.AreEqual("Overflow", result.ConduitName);
            Assert.IsTrue(result.CalculationSteps.Contains("RESULT: No suitable conduit found with sufficient capacity."), "Overflow message not found.");
        }

        [TestMethod]
        public void Test_Error_WhenNoConduitsOfSelectedType()
        {
            // Arrange
            var selectedCables = new List<Cable> { new Cable("C1", "Cable 1", 10, false, 0) };
            var allConduits = new List<ConduitSize> {
                new ConduitSize("PVC-Medium", "PVC", 150)
            };
            string selectedConduitType = "EMT"; // This type does not exist in allConduits

            // Act
            var result = calculator.CalculateConduit(selectedCables, allConduits, selectedConduitType);

            // Assert
            Assert.AreEqual("Error - No Matching Conduits", result.ConduitName, "ConduitName should indicate no matching conduits error.");
            Assert.IsTrue(result.CalculationSteps.Contains($"No conduits found for type '{selectedConduitType}'"), "Log message for no matching conduit type not found.");
        }

        [TestMethod]
        public void Test_Error_WhenSelectedCablesListIsEmpty()
        {
            // Arrange
            var selectedCables = new List<Cable>();
            var allConduits = new List<ConduitSize> { new ConduitSize("PVC-Medium", "PVC", 150) };
            string selectedConduitType = "PVC";

            // Act
            var result = calculator.CalculateConduit(selectedCables, allConduits, selectedConduitType);

            // Assert
            Assert.AreEqual("Error - No Cables", result.ConduitName);
            Assert.IsTrue(result.CalculationSteps.Contains("No cables selected for calculation."));
        }

        [TestMethod]
        public void Test_Error_WhenSelectedCablesListIsNull()
        {
            // Arrange
            List<Cable> selectedCables = null;
            var allConduits = new List<ConduitSize> { new ConduitSize("PVC-Medium", "PVC", 150) };
            string selectedConduitType = "PVC";

            // Act
            var result = calculator.CalculateConduit(selectedCables, allConduits, selectedConduitType);

            // Assert
            Assert.AreEqual("Error - No Cables", result.ConduitName);
            Assert.IsTrue(result.CalculationSteps.Contains("No cables selected for calculation."));
        }

        [TestMethod]
        public void Test_Error_WhenAllConduitsListIsNull()
        {
            // Arrange
            var selectedCables = new List<Cable> { new Cable("C1", "Cable 1", 10, false, 0) };
            List<ConduitSize> allConduits = null;
            string selectedConduitType = "PVC";

            // Act
            var result = calculator.CalculateConduit(selectedCables, allConduits, selectedConduitType);

            // Assert
            Assert.AreEqual("Error - No Conduits", result.ConduitName); // Corrected expected error name
            Assert.IsTrue(result.CalculationSteps.Contains("Error: Conduit list not provided."));
        }

        [TestMethod]
        public void Test_MixedCableTypes_TriplexAndStandard_CorrectFillAndSelection()
        {
            // Arrange
            var selectedCables = new List<Cable> {
                new Cable("TR1", "Triplex 1", 8, true, 4), // PhaseOD=8 (Area/cond = PI*16=50.2655), GroundOD=4 (Area=PI*4=12.5664)
                                                          // Total Triplex Area = 3*50.2655 + 12.5664 = 150.7965 + 12.5664 = 163.3629. Effective cables = 4
                new Cable("C1", "Standard 1", 6, false, 0) // Area = PI*9 = 28.2743. Effective cables = 1
            };
            // Total Cable Area = 163.3629 + 28.2743 = 191.6372
            // Total Effective Cables = 4 + 1 = 5
            // Fill = 40% (since 5 > 2)
            // Required Usable Area = 191.6372
            // Conduit Area Needed = 191.6372 / 0.40 = 479.093
            var allConduits = new List<ConduitSize> {
                new ConduitSize("PVC-Small", "PVC", 470),   // Usable = 470 * 0.40 = 188 (Too small)
                new ConduitSize("PVC-Medium", "PVC", 479),  // Usable = 479 * 0.40 = 191.6 (Too small, precision)
                new ConduitSize("PVC-Large", "PVC", 480),   // Usable = 480 * 0.40 = 192 (Suitable)
            };
            string selectedConduitType = "PVC";

            // Act
            var result = calculator.CalculateConduit(selectedCables, allConduits, selectedConduitType);

            // Assert
            Assert.AreEqual("PVC-Large", result.ConduitName, "Incorrect conduit selected for mixed cables.");
            Assert.IsTrue(result.CalculationSteps.Contains("5 cables (>2): Using 40% fill capacity."), "Fill percentage log for 5 cables incorrect.");
            Assert.IsTrue(result.CalculationSteps.Contains("Processing cable: Triplex 1"), "Triplex cable processing log not found.");
            Assert.IsTrue(result.CalculationSteps.Contains("Processing cable: Standard 1"), "Standard cable processing log not found.");
            Assert.IsTrue(result.CalculationSteps.Contains($"Total calculated cable bundle area: {191.6372:F4}"), "Total cable area for mixed cables incorrect.");
            Assert.IsTrue(result.CalculationSteps.Contains("Selected Conduit: PVC-Large"), "Selected conduit log entry not found.");
        }
    }
}
