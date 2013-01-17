using System.Collections.Generic;
using JetBrains.ActionManagement;
using JetBrains.Application.DataContext;
using JetBrains.ReSharper.Feature.Services.ContextNavigation;
using JetBrains.ReSharper.Features.Finding.NavigateFromHere;

namespace Unity.TypedFactories.ReSharperFindUsages
{
    using System;
    using System.Collections.ObjectModel;
    using System.Linq;

    using JetBrains.Application.Progress;
    using JetBrains.ReSharper.Daemon;
    using JetBrains.ReSharper.Feature.Services.ContextNavigation.ContextSearches;
    using JetBrains.ReSharper.Feature.Services.ContextNavigation.ContextSearches.BaseSearches;
    using JetBrains.ReSharper.Feature.Services.Search;
    using JetBrains.ReSharper.Feature.Services.Search.SearchRequests;
    using JetBrains.ReSharper.Features.Common.Occurences.ExecutionHosting;
    using JetBrains.ReSharper.Psi;
    using JetBrains.ReSharper.Feature.Services.ContextNavigation.Util;
    using JetBrains.ReSharper.Psi.CSharp.Tree;
    using JetBrains.ReSharper.Psi.ExtensionsAPI.Caches2;
    using JetBrains.ReSharper.Psi.Impl.Resolve;
    using JetBrains.ReSharper.Psi.Search;
    using JetBrains.ReSharper.Refactorings.ClassFromParameters.Util;
    using JetBrains.Util;

    [FeaturePart]
    public class FindUsagesContext : FindUsagesContextSearch
    {

        protected override SearchUsagesRequest CreateSearchRequest(IDataContext dataContext, IDeclaredElement declaredElement, IDeclaredElement initialTarget)
        {
            // do not call the base implementation : we are going to override it completely, by mimicking most of its behavior. see comments below.

            SearchUsagesRequest specialUsagesRequest = this.TryFindSpecialUsagesRequest(dataContext);
            if (specialUsagesRequest != null)
                return specialUsagesRequest;

            var elementsToSearch = new Collection<IDeclaredElement> { declaredElement };

            // sole addition to the base method implementation
            var additionalElementsToSearch = this.GetAdditionalElementsToSearch(dataContext);
            elementsToSearch.AddRange(additionalElementsToSearch);

            const SearchPattern SearchPattern = SearchPattern.FIND_USAGES | SearchPattern.FIND_IMPLEMENTORS_USAGES | SearchPattern.FIND_LATEBOUND_REFERENCES;
            var searchDomain = SearchDomainContextUtil.GetSearchDomainContext(dataContext).GetDefaultDomain().SearchDomain;
            var declaredElements = new[] { initialTarget };

            var searchUsagesRequest = (SearchUsagesRequest)new SearchDeclaredElementUsagesRequest(elementsToSearch, SearchPattern, searchDomain, declaredElements);
            return searchUsagesRequest;
        }     

        protected ICollection<IDeclaredElement> GetAdditionalElementsToSearch(IDataContext context)
        {
            var selectedConstructorDeclaration = context.GetSelectedTreeNode<IConstructorDeclaration>();

            if (selectedConstructorDeclaration != null)
            {
                var selectedTypeDeclaration = selectedConstructorDeclaration.GetContainingTypeDeclaration();

                if (selectedTypeDeclaration != null)
                {
                    ISearchDomain searchDomain = SearchDomainFactory.Instance.CreateSearchDomain(selectedTypeDeclaration.GetPsiModule().GetSolution(), false);
                    var referencesToSelectedType = selectedTypeDeclaration
                        .GetPsiServices()
                        .Finder
                        .FindReferences(selectedTypeDeclaration.DeclaredElement, searchDomain, NullProgressIndicator.Instance);
                    
                    var referencesToSelectedTypeExpressions = referencesToSelectedType
                        .Select(o => o.GetTreeNode().GetContainingNode<IReferenceExpression>())
                        .Where(o => o != null);

                    var createMethods = new Collection<IDeclaredElement>();

                    foreach (var referenceExpression in referencesToSelectedTypeExpressions)
                    {
                        var creatingFactory = ((IReferenceExpression)referenceExpression.FirstChild.FirstChild)
                            .TypeArgumentList
                            .TypeArguments[0];

                        // Find all the calls to IxxxFactory.Create() 
                        var factoryInterface = ((Interface)creatingFactory.GetScalarType().Resolve().DeclaredElement);
                        // FIXME : we are only handling the methods of the interface, not the inherited methods !
                        var createMethodsForReference =
                            factoryInterface.Methods.Where(
                                o =>
                                    {
                                        var substitution = creatingFactory.GetScalarType().GetSubstitution();
                                        var isFactoryGeneric = substitution.Domain.Any();

                                        // todo : we still have to make sure we only keep the typeParameters which are compatible with the selected .ctor.
                                        //var typeParameters = substitution.Domain.Select(typeParameter => substitution[typeParameter]).Where(x => x.IsImplicitlyConvertibleTo(selectedTypeDeclaration.));                                        
                                        IEnumerable<IType> typeParameters = substitution.Domain.Select(typeParameter => substitution[typeParameter]).Where(x => x != null);                                        
                                        /* SuperTypes.Contains(o.ReturnType.GetScalarType()))  TODO: if the type of the returned object is not the direct parent, then it won't work  - Alternative :  o.ShortName == "Create"*/
                                        var typeReturnedByMethodOfFactory = o.ReturnType.GetScalarType().GetTypeElement();
                                        var isDescendant = selectedTypeDeclaration.DeclaredElement.IsDescendantOf(typeReturnedByMethodOfFactory);
                                        if (isFactoryGeneric)
                                        {
                                            // if it is not a direct descendant, maybe we have a generic factory interface : let's check if one of the type arguments do match the output value of a method.
                                            isDescendant = selectedTypeDeclaration.SuperTypes.Cast<IType>().Union(typeParameters).Any();
                                        }
                                        return isDescendant;
                                    })
                                    .Select(o => o.GetDeclarations().First().DeclaredElement);                        
                            createMethods.AddRange(createMethodsForReference);                                              
                    }
                    return createMethods;
                }
            }
            return new IDeclaredElement[0];
        }
    }

    //[ContextNavigationProvider]
    //public class MyContextNavigationProvider : IContextSearch
    //{
    //    public Action GetSearchesExecution(IDataContext dataContext, INavigationExecutionHost host)
    //    {
    //        throw new NotImplementedException();
    //    }
    //}

}
