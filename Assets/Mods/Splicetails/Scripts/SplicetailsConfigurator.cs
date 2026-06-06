using Bindito.Core;
using Timberborn.BlockSystem;
using Timberborn.BottomBarSystem;
using Timberborn.Forestry;
using Timberborn.LaborSystem;
using Timberborn.TemplateInstantiation;
using Timberborn.ToolSystem;
using Timberborn.WorkSystem;

namespace Timberborn.Splicetails {

    [Context("Game")]
    public class SplicetailsConfigurator : Configurator {

        private class BottomBarModuleProvider : IProvider<BottomBarModule> {
            private readonly TreeMutationAreaButton _button;
            public BottomBarModuleProvider(TreeMutationAreaButton button) { _button = button; }
            public BottomBarModule Get() {
                var builder = new BottomBarModule.Builder();
                builder.AddLeftSectionElement(_button, 25);
                return builder.Build();
            }
        }

        protected override void Configure() {
            Bind<SplicetailsInitializer>().AsSingleton();

            Bind<TreeMutationArea>().AsSingleton();
            Bind<TreeMutationAreaVisualizer>().AsSingleton();
            Bind<Mutatable>().AsTransient();
            Bind<MutatableReacher>().AsTransient();
            Bind<SerumApplicatorWorkplaceBehavior>().AsTransient();
            Bind<SerumDeliveryBehavior>().AsTransient();
            Bind<UndergroundLodgeExcavator>().AsTransient();
            MultiBind<IBlockObjectValidator>().To<UndergroundLodgeExclusionValidator>().AsSingleton();

            Bind<TreeMutationAreaSelectionTool>().AsSingleton();
            Bind<TreeMutationAreaUnselectionTool>().AsSingleton();
            Bind<TreeMutationAreaButton>().AsSingleton();
            MultiBind<IToolDisabler>().To<CutAreaUnselectionToolDisabler>().AsSingleton();

            MultiBind<TemplateModule>().ToProvider(ProvideTemplateModule).AsSingleton();
            MultiBind<BottomBarModule>().ToProvider<BottomBarModuleProvider>().AsSingleton();
        }

        private static TemplateModule ProvideTemplateModule() {
            var builder = new TemplateModule.Builder();

            builder.AddDecorator<TreeComponentSpec, Mutatable>();
            builder.AddDecorator<Mutatable, MutatableReacher>();

            builder.AddDecorator<UndergroundLodgeSpec, UndergroundLodgeExcavator>();

            builder.AddDecorator<SerumApplicatorSpec, SerumApplicatorWorkplaceBehavior>();
            builder.AddDecorator<SerumApplicatorSpec, LaborWorkplaceBehavior>();
            builder.AddDecorator<WorkerSpec, SerumDeliveryBehavior>();
            builder.AddDecorator<SerumApplicatorSpec, WaitInsideIdlyWorkplaceBehavior>();

            return builder.Build();
        }
    }
}
