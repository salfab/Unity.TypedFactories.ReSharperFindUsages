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
    using JetBrains.ReSharper.Psi.Search;
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
            var typeDeclaration = context.GetSelectedTreeNode<ICSharpTypeDeclaration>();

            if (typeDeclaration != null)
            {
                ISearchDomain searchDomain = SearchDomainFactory.Instance.CreateSearchDomain(typeDeclaration.GetPsiModule().GetSolution(), false);
                var references = typeDeclaration.GetPsiServices()
                                                .Finder.FindReferences(
                                                    typeDeclaration.DeclaredElement,
                                                    searchDomain,
                                                    NullProgressIndicator.Instance);
                var referenceExpression = references.Select(o => o.GetTreeNode().GetContainingNode<IReferenceExpression>()).SingleOrDefault(o => o != null);
                if (referenceExpression != null)
                {
                    var creatingFactory = ((IReferenceExpression)referenceExpression.FirstChild.FirstChild).TypeArgumentList.TypeArguments[0];

                    // Find all the calls to IxxxFactory.Create() 
                    ICollection<IDeclaredElement> createMethods = ((Interface)creatingFactory.GetScalarType().Resolve().DeclaredElement).Methods.Where(
                            o => typeDeclaration.DeclaredElement.IsDescendantOf(o.ReturnType.GetScalarType().GetTypeElement())/*SuperTypes.Contains(o.ReturnType.GetScalarType()))  TODO: if the type of the returned object is not the direct parent, then it won't work  - Alternative :  o.ShortName == "Create"*/)
                                                                                              .Select(
                                                                                                  o =>
                                                                                                  o.GetDeclarations()
                                                                                                   .First()
                                                                                                   .DeclaredElement)
                                                                                              .ToList();

                    return createMethods;
                    //var referencesToCreate = creatingFactory.GetPsiServices()
                    //                                        .Finder.FindReferences(
                    //                                            createMethods, searchDomain, NullProgressIndicator.Instance);

                    //foreach (var reference in referencesToCreate)
                    //{
                    //    declaredElements.Add(referencesToCreate.First().Resolve().DeclaredElement);
                    //}
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
