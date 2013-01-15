using System.Collections.Generic;
using System.Linq;
using JetBrains.ReSharper.Daemon;
using JetBrains.ReSharper.Feature.Services.Bulbs;
using JetBrains.ReSharper.Intentions.Extensibility;
using JetBrains.ReSharper.Intentions.Extensibility.Menu;
using JetBrains.Util;

namespace Unity.TypedFactories.ReSharperFindUsages
{
    using JetBrains.ReSharper.Intentions.CSharp.QuickFixes;
    using JetBrains.ReSharper.Psi.CSharp.Tree;

    [QuickFix]
    public sealed class ReplaceNewWithTypedFactory : IQuickFix
    {
        private readonly IContextActionDataProvider provider;

        private readonly List<IBulbAction> _items;

        private string createdClassName;

        private IObjectCreationExpression creationExpression;

        /// <summary>
        /// For languages other than C# any inheritor of <see cref="IContextActionDataProvider"/> can 
        /// be injected in this constructor.
        /// </summary>
        public ReplaceNewWithTypedFactory(IContextActionDataProvider provider)
        {
            this.provider = provider;
            _items = new List<IBulbAction>();
            var description = string.Format("Replace instantiation with an injection of I{0}Factory.", this.createdClassName);
            IBulbAction bulbAction = new CreateNewFactoryFromClass(description);
            _items.Add(bulbAction);
            // fill the 'items' list with any bulb item you want to show
        }

        public bool IsAvailable(IUserDataHolder cache)
        {
            this.creationExpression = this.provider.GetSelectedElement<IObjectCreationExpression>(true, true);
            createdClassName = creationExpression.TypeName.ShortName;
            return creationExpression != null;
        }

        public void CreateBulbItems(BulbMenu menu, Severity severity)
        {
            menu.ArrangeQuickFixes(_items.Select(_ => Pair.Of(_, severity)));
        }
    }
}
