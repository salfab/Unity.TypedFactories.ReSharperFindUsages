namespace Unity.TypedFactories.ReSharperFindUsages
{
    using JetBrains.ProjectModel;
    using JetBrains.ReSharper.Intentions.Extensibility;
    using JetBrains.TextControl;

    public class CreateNewFactoryFromClass : IBulbAction
    {
        public CreateNewFactoryFromClass(string description)
        {
            Text = description;
        }
        public void Execute(ISolution solution, ITextControl textControl)
        {
            throw new System.NotImplementedException();
        }

        public string Text { get; private set; }
    }
}