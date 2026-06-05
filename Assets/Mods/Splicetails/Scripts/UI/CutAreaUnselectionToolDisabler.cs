using Timberborn.ToolSystem;

namespace Timberborn.Splicetails {

    // Hides the vanilla "unmark cutting area" button. The combined
    // TreeMutationAreaUnselectionTool already removes both cutting and serum marks,
    // so the vanilla-only button is redundant.
    public class CutAreaUnselectionToolDisabler : IToolDisabler {

        // Resolved via reflection so we don't need to reference the internal type directly.
        private static readonly System.Type VanillaType = System.Type.GetType(
            "Timberborn.ForestryUI.TreeCuttingAreaUnselectionTool, Timberborn.ForestryUI");

        public bool IsEnabled(ITool tool) => VanillaType == null || tool.GetType() != VanillaType;
    }
}
