using Timberborn.ToolSystem;

namespace Timberborn.Splicetails {

    // Hides the vanilla "unmark cutting area" button. The combined
    // TreeMutationAreaUnselectionTool already removes both cutting and serum marks,
    // so the vanilla-only button is redundant.
    public class CutAreaUnselectionToolDisabler : IToolDisabler {

        public bool IsEnabled(ITool tool) =>
            tool.GetType().Name != "TreeCuttingAreaUnselectionTool";
    }
}
