using System.Collections.Generic;
using Timberborn.BottomBarSystem;
using Timberborn.ToolButtonSystem;
using Timberborn.ToolSystem;

namespace Timberborn.Splicetails {

    // Adds serum application mark/unmark tools to the existing "TreeCutting" group
    // so they appear alongside the vanilla tree cutting area tools.
    public class TreeMutationAreaButton : IBottomBarElementsProvider {

        private static readonly string ToolGroupId = "TreeCutting";
        private static readonly string SelectionImageKey = "TreeCuttingAreaSelectionTool";
        private static readonly string UnselectionImageKey = "CancelToolIcon";

        private readonly TreeMutationAreaSelectionTool _selectionTool;
        private readonly TreeMutationAreaUnselectionTool _unselectionTool;
        private readonly ToolButtonFactory _toolButtonFactory;
        private readonly ToolGroupButtonFactory _toolGroupButtonFactory;
        private readonly ToolGroupService _toolGroupService;

        public TreeMutationAreaButton(TreeMutationAreaSelectionTool selectionTool,
                                      TreeMutationAreaUnselectionTool unselectionTool,
                                      ToolButtonFactory toolButtonFactory,
                                      ToolGroupButtonFactory toolGroupButtonFactory,
                                      ToolGroupService toolGroupService) {
            _selectionTool = selectionTool;
            _unselectionTool = unselectionTool;
            _toolButtonFactory = toolButtonFactory;
            _toolGroupButtonFactory = toolGroupButtonFactory;
            _toolGroupService = toolGroupService;
        }

        public IEnumerable<BottomBarElement> GetElements() {
            var toolGroup = _toolGroupService.GetGroup(ToolGroupId);
            var groupButton = _toolGroupButtonFactory.CreateBlue(toolGroup);
            _toolButtonFactory.Create(_selectionTool, SelectionImageKey, groupButton.ToolButtonsElement);
            _toolButtonFactory.Create(_unselectionTool, UnselectionImageKey, groupButton.ToolButtonsElement);
            yield return BottomBarElement.CreateMultiLevel(groupButton.Root, groupButton.ToolButtonsElement);
        }
    }
}
