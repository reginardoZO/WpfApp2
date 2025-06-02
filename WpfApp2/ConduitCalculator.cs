using System;
using System.Collections.Generic;
using System.Linq; // Added for .Any()
using System.Text;
using System.Threading.Tasks; // This was in the original file, keeping it for now.

namespace WpfApp2
{
    public class ConduitCalculatorResult
    {
        public string ConduitName { get; set; }
        public string CalculationSteps { get; set; }

        public ConduitCalculatorResult(string conduitName, string calculationSteps)
        {
            ConduitName = conduitName;
            CalculationSteps = calculationSteps;
        }
    }

    public class ConduitCalculator
    {
        public ConduitCalculatorResult CalculateConduit(List<Cable> selectedCables, List<ConduitSize> allConduits, string selectedConduitType)
        {
            StringBuilder stepsLog = new StringBuilder();
            double totalCableArea = 0.0;
            int effectiveCableCount = 0;

            stepsLog.AppendLine("Starting conduit calculation...");
            
            if (selectedCables == null || !selectedCables.Any())
            {
                stepsLog.AppendLine("No cables selected for calculation.");
                return new ConduitCalculatorResult("Error - No Cables", stepsLog.ToString());
            }

            if (allConduits == null) // Added null check for allConduits
            {
                stepsLog.AppendLine("Error: Conduit list not provided.");
                return new ConduitCalculatorResult("Error - No Conduits", stepsLog.ToString());
            }
            
            stepsLog.AppendLine("Calculating total cable area:");
            stepsLog.AppendLine("====================================");

            foreach (var cable in selectedCables)
            {
                stepsLog.AppendLine($"Processing cable: {cable.Name} (ID: {cable.ID})");
                double currentCableArea;

                if (cable.IsTriplex)
                {
                    // Area = PI * r^2 = PI * (D/2)^2
                    double areaPhaseConductor = Math.PI * Math.Pow(cable.OD / 2, 2);
                    double areaPhase = 3 * areaPhaseConductor;
                    double areaGround = Math.PI * Math.Pow(cable.GroundOD / 2, 2);
                    currentCableArea = areaPhase + areaGround;
                    effectiveCableCount += 4; // 3 phase conductors + 1 ground conductor

                    stepsLog.AppendLine("- Type: Triplex");
                    stepsLog.AppendLine($"  - Phase OD: {cable.OD}, Area per phase conductor: {areaPhaseConductor:F4}");
                    stepsLog.AppendLine($"  - Total Phase Area (3 cond.): {areaPhase:F4}");
                    stepsLog.AppendLine($"  - Ground OD: {cable.GroundOD}, Ground Area: {areaGround:F4}");
                    stepsLog.AppendLine($"  - Combined Area for this Triplex: {currentCableArea:F4}");
                    stepsLog.AppendLine($"  - Effective cables for fill rule: 4 (3 phase + 1 ground)");
                }
                else // Single or multicore cable treated as a single conductor for overall area
                {
                    currentCableArea = Math.PI * Math.Pow(cable.OD / 2, 2);
                    effectiveCableCount += 1;

                    stepsLog.AppendLine("- Type: Standard/Multicore");
                    stepsLog.AppendLine($"  - OD: {cable.OD}");
                    stepsLog.AppendLine($"  - Calculated Area: {currentCableArea:F4}");
                    stepsLog.AppendLine($"  - Effective cables for fill rule: 1");
                }
                totalCableArea += currentCableArea;
                stepsLog.AppendLine("------------------------------------");
            }

            stepsLog.AppendLine("\n====================================");
            stepsLog.AppendLine($"Total effective cable count: {effectiveCableCount}");
            stepsLog.AppendLine($"Total calculated cable bundle area: {totalCableArea:F4} (units squared)");
            stepsLog.AppendLine("====================================");

            // Determine Fill Percentage
            double fillPercentage = 0.0;
            stepsLog.AppendLine($"\nDetermining conduit fill percentage based on {effectiveCableCount} effective cable(s):");
            if (effectiveCableCount == 1)
            {
                fillPercentage = 0.53; // 53%
                stepsLog.AppendLine($"- 1 cable: Using {fillPercentage * 100}% fill capacity.");
            }
            else if (effectiveCableCount == 2)
            {
                fillPercentage = 0.31; // 31%
                stepsLog.AppendLine($"- 2 cables: Using {fillPercentage * 100}% fill capacity.");
            }
            else // effectiveCableCount > 2 (or 0, but covered by initial check)
            {
                fillPercentage = 0.40; // 40%
                stepsLog.AppendLine($"- {effectiveCableCount} cables (>2): Using {fillPercentage * 100}% fill capacity.");
            }

            // Filter and Sort Conduits
            stepsLog.AppendLine($"\nFiltering and sorting available conduits of type '{selectedConduitType}':");
            List<ConduitSize> suitableConduits = allConduits
                .Where(c => string.Equals(c.Type, selectedConduitType, StringComparison.OrdinalIgnoreCase))
                .OrderBy(c => c.AreaIN)
                .ToList();

            if (!suitableConduits.Any())
            {
                stepsLog.AppendLine($"No conduits found for type '{selectedConduitType}'. Unable to perform calculation.");
                return new ConduitCalculatorResult("Error - No Matching Conduits", stepsLog.ToString());
            }
            stepsLog.AppendLine($"Found {suitableConduits.Count} conduits of type '{selectedConduitType}' and ordered by area.");

            // Iterate and Select Conduit
            stepsLog.AppendLine($"\nChecking filtered conduits against total cable area ({totalCableArea:F4}):");
            string selectedConduitName = "Overflow";

            foreach (var conduit in suitableConduits)
            {
                double usableArea = conduit.AreaIN * fillPercentage;
                stepsLog.AppendLine($"- Checking Conduit: {conduit.Name} (Type: {conduit.Type}, Total Internal Area: {conduit.AreaIN:F4})");
                stepsLog.AppendLine($"  - Usable Area at {fillPercentage * 100}% fill: {usableArea:F4}");

                if (usableArea >= totalCableArea)
                {
                    selectedConduitName = conduit.Name;
                    stepsLog.AppendLine($"  - SUITABLE: Usable area {usableArea:F4} >= Total cable area {totalCableArea:F4}.");
                    stepsLog.AppendLine($"  >>> Selected Conduit: {selectedConduitName} <<<");
                    break; 
                }
                else
                {
                    stepsLog.AppendLine($"  - NOT SUITABLE: Usable area {usableArea:F4} < Total cable area {totalCableArea:F4}.");
                }
                stepsLog.AppendLine("------------------------------------");
            }

            if (selectedConduitName == "Overflow")
            {
                stepsLog.AppendLine("\n====================================");
                stepsLog.AppendLine("RESULT: No suitable conduit found with sufficient capacity. All conduits checked are too small.");
                stepsLog.AppendLine("Consider selecting a different conduit type or reducing the number/size of cables.");
                stepsLog.AppendLine("====================================");
            }
            else
            {
                stepsLog.AppendLine("\n====================================");
                stepsLog.AppendLine($"FINAL RESULT: Selected Conduit is {selectedConduitName}");
                stepsLog.AppendLine("====================================");
            }
            
            return new ConduitCalculatorResult(selectedConduitName, stepsLog.ToString());
        }
    }
}
