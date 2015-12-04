﻿using Prism.Common;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Xamarin.Forms;

namespace Prism.Navigation
{
    /// <summary>
    /// Provides page based navigation for ViewModels.
    /// </summary>
    public abstract class PageNavigationService : INavigationService, IPageAware
    {
        private Page _page;
        Page IPageAware.Page
        {
            get { return _page; }
            set { _page = value; }
        }

        /// <summary>
        /// Navigates to the most recent entry in the back navigation history by popping the calling Page off the navigation stack.
        /// </summary>
        /// <param name="useModalNavigation">If <c>true</c> uses PopModalAsync, if <c>false</c> uses PopAsync</param>
        /// <param name="animated">If <c>true</c> the transition is animated, if <c>false</c> there is no animation on transition.</param>
        public async void GoBack(NavigationParameters parameters = null, bool? useModalNavigation = null, bool animated = true)
        {
            var page = GetCurrentPage();
            var navParameters = GetSegmentParameters(null, parameters);

            if (!CanNavigate(page, navParameters))
                return;

            bool useModalForDoPop = UseModalNavigation(page, useModalNavigation);
            Page previousPage = GetPreviousPage(page, useModalForDoPop);

            OnNavigatedFrom(page, navParameters);

            var poppedPage = await DoPop(page.Navigation, useModalForDoPop, animated);

            if (poppedPage != null)
                OnNavigatedTo(previousPage, navParameters);
        }

        /// <summary>
        /// Initiates navigation to the target specified by the <typeparamref name="T"/>.
        /// </summary>
        /// <typeparam name="T">The type which will be used to identify the name of the navigation target.</typeparam>
        /// <param name="parameters">The navigation parameters</param>
        /// <param name="useModalNavigation">If <c>true</c> uses PopModalAsync, if <c>false</c> uses PopAsync</param>
        /// <param name="animated">If <c>true</c> the transition is animated, if <c>false</c> there is no animation on transition.</param>
        public void Navigate<T>(NavigationParameters parameters = null, bool? useModalNavigation = null, bool animated = true)
        {
            Navigate(typeof(T).FullName, parameters, useModalNavigation, animated);
        }

        /// <summary>
        /// Initiates navigation to the target specified by the <paramref name="name"/>.
        /// </summary>
        /// <param name="name">The name of the target to navigate to.</param>
        /// <param name="parameters">The navigation parameters</param>
        /// <param name="useModalNavigation">If <c>true</c> uses PopModalAsync, if <c>false</c> uses PopAsync</param>
        /// <param name="animated">If <c>true</c> the transition is animated, if <c>false</c> there is no animation on transition.</param>
        public void Navigate(string name, NavigationParameters parameters = null, bool? useModalNavigation = null, bool animated = true)
        {
            Navigate(new Uri(name, UriKind.RelativeOrAbsolute), parameters, useModalNavigation, animated);
        }

        /// <summary>
        /// Initiates navigation to the target specified by the <paramref name="uri"/>.
        /// </summary>
        /// <param name="uri">The Uri to navigate to</param>
        /// <param name="parameters">The navigation parameters</param>
        /// <param name="useModalNavigation">If <c>true</c> uses PopModalAsync, if <c>false</c> uses PopAsync</param>
        /// <param name="animated">If <c>true</c> the transition is animated, if <c>false</c> there is no animation on transition.</param>
        /// <remarks>Navigation parameters can be provided in the Uri and by using the <paramref name="parameters"/>.</remarks>
        /// <example>
        /// Navigate(new Uri("MainPage?id=3&name=brian", UriKind.RelativeSource), parameters);
        /// </example>
        public void Navigate(Uri uri, NavigationParameters parameters = null, bool? useModalNavigation = null, bool animated = true)
        {
            var navigationSegments = UriParsingHelper.GetUriSegments(uri);
            var isDeepLink = navigationSegments.Count > 1;

            if (uri.IsAbsoluteUri)
                ProcessNavigationForAbsoulteUri(navigationSegments, parameters, useModalNavigation, isDeepLink ? false : animated);
            else
                ProcessNavigation(GetCurrentPage(), navigationSegments, parameters, useModalNavigation, isDeepLink ? false : animated);
        }

        void ProcessNavigation(Page currentPage, Queue<string> segments, NavigationParameters parameters, bool? useModalNavigation, bool animated)
        {
            if (segments.Count == 0)
                return;

            var nextSegment = segments.Dequeue();

            if (currentPage == null)
            {
                ProcessNavigationForRootPage(nextSegment, segments, parameters, useModalNavigation, animated);
                return;
            }

            if (currentPage is ContentPage)
            {
                ProcessNavigationForContentPage(currentPage, nextSegment, segments, parameters, useModalNavigation, animated);
            }
            else if (currentPage is NavigationPage)
            {
                ProcessNavigationForNavigationPage((NavigationPage)currentPage, nextSegment, segments, parameters, useModalNavigation, animated);
            }
            else if (currentPage is TabbedPage)
            {
                ProcessNavigationForTabbedPage((TabbedPage)currentPage, nextSegment, segments, parameters, useModalNavigation, animated);
            }
            else if (currentPage is CarouselPage)
            {
                ProcessNavigationForCarouselPage((CarouselPage)currentPage, nextSegment, segments, parameters, useModalNavigation, animated);
            }
            else if (currentPage is MasterDetailPage)
            {
                ProcessNavigationForMasterDetailPage((MasterDetailPage)currentPage, nextSegment, segments, parameters, useModalNavigation, animated);
            }
        }

        void ProcessNavigationForAbsoulteUri(Queue<string> segments, NavigationParameters parameters, bool? useModalNavigation, bool animated)
        {
            ProcessNavigation(null, segments, parameters, useModalNavigation, animated);
        }

        void ProcessNavigationForRootPage(string nextSegment, Queue<string> segments, NavigationParameters parameters, bool? useModalNavigation, bool animated)
        {
            var nextPage = CreatePageFromSegment(nextSegment);

            ProcessNavigation(nextPage, segments, parameters, useModalNavigation, animated);

            DoNavigateAction(null, nextSegment, nextPage, parameters, () =>
            {
                DoPush(null, nextPage, true, animated);
            });
        }

        void ProcessNavigationForContentPage(Page currentPage, string nextSegment, Queue<string> segments, NavigationParameters parameters, bool? useModalNavigation, bool animated)
        {
            var nextPage = CreatePageFromSegment(nextSegment);

            bool useModalForDoPush = UseModalNavigation(currentPage, useModalNavigation);

            ProcessNavigation(nextPage, segments, parameters, useModalForDoPush, animated);

            DoNavigateAction(currentPage, nextSegment, nextPage, parameters, () =>
            {
                DoPush(currentPage, nextPage, useModalForDoPush, animated);
            });
        }

        void ProcessNavigationForNavigationPage(NavigationPage currentPage, string nextSegment, Queue<string> segments, NavigationParameters parameters, bool? useModalNavigation, bool animated)
        {
            if (currentPage.Navigation.NavigationStack.Count == 0)
            {
                var newRoot = CreatePageFromSegment(nextSegment);
                ProcessNavigation(newRoot, segments, parameters, false, animated);
                DoNavigateAction(currentPage, nextSegment, newRoot, parameters, () =>
                {
                    DoPush(currentPage, newRoot, false, animated);
                });
                return;
            }

            var currentNavRoot = currentPage.Navigation.NavigationStack[0];
            var nextPageType = PageNavigationRegistry.GetPageType(UriParsingHelper.GetSegmentName(nextSegment));
            if (currentNavRoot.GetType() == nextPageType)
            {
                if (currentPage.Navigation.NavigationStack.Count > 1)
                    currentPage.Navigation.PopToRootAsync(false);

                ProcessNavigation(currentNavRoot, segments, parameters, false, animated);
                DoNavigateAction(currentPage, nextSegment, currentNavRoot, parameters);
                return;
            }
            else
            {
                currentPage.Navigation.PopToRootAsync(false);
                var newRoot = CreatePageFromSegment(nextSegment);
                ProcessNavigation(newRoot, segments, parameters, false, animated);

                DoNavigateAction(currentPage, nextSegment, newRoot, parameters, () =>
                {
                    DoPush(currentPage, newRoot, false, animated);
                    currentPage.Navigation.RemovePage(currentNavRoot);
                });
                return;
            }
        }

        void ProcessNavigationForTabbedPage(TabbedPage currentPage, string nextSegment, Queue<string> segments, NavigationParameters parameters, bool? useModalNavigation, bool animated)
        {
            var nextSegmentType = PageNavigationRegistry.GetPageType(UriParsingHelper.GetSegmentName(nextSegment));
            foreach (var child in currentPage.Children)
            {
                if (child.GetType() != nextSegmentType)
                    continue;

                ProcessNavigation(child, segments, parameters, useModalNavigation, animated);
                DoNavigateAction(null, nextSegment, child, parameters, () =>
                {
                    currentPage.CurrentPage = child;
                });
                return;
            }

            var nextPage = CreatePageFromSegment(nextSegment);
            ProcessNavigation(nextPage, segments, parameters, useModalNavigation, animated);
            DoNavigateAction(currentPage, nextSegment, nextPage, parameters, () =>
            {
                DoPush(currentPage, nextPage, true, animated);
            });
        }

        void ProcessNavigationForCarouselPage(CarouselPage currentPage, string nextSegment, Queue<string> segments, NavigationParameters parameters, bool? useModalNavigation, bool animated)
        {
            var nextSegmentType = PageNavigationRegistry.GetPageType(UriParsingHelper.GetSegmentName(nextSegment));
            foreach (var child in currentPage.Children)
            {
                if (child.GetType() != nextSegmentType)
                    continue;

                ProcessNavigation(child, segments, parameters, useModalNavigation, animated);
                DoNavigateAction(null, nextSegment, child, parameters, () =>
                {
                    currentPage.CurrentPage = child;
                });
                return;
            }


            var nextPage = CreatePageFromSegment(nextSegment);
            ProcessNavigation(nextPage, segments, parameters, useModalNavigation, animated);
            DoNavigateAction(currentPage, nextSegment, nextPage, parameters, () =>
            {
                DoPush(currentPage, nextPage, true, animated);
            });
        }

        void ProcessNavigationForMasterDetailPage(MasterDetailPage currentPage, string nextSegment, Queue<string> segments, NavigationParameters parameters, bool? useModalNavigation, bool animated)
        {
            var detail = currentPage.Detail;
            if (detail == null)
            {
                var newDetail = CreatePageFromSegment(nextSegment);
                ProcessNavigation(newDetail, segments, parameters, newDetail is NavigationPage ? false : true, animated);
                DoNavigateAction(null, nextSegment, newDetail, parameters, () =>
                {
                    currentPage.Detail = newDetail;
                    currentPage.IsPresented = false;
                });
                return;
            }

            var nextSegmentType = PageNavigationRegistry.GetPageType(UriParsingHelper.GetSegmentName(nextSegment));
            if (detail.GetType() == nextSegmentType)
            {
                ProcessNavigation(detail, segments, parameters, useModalNavigation, animated);
                DoNavigateAction(null, nextSegment, detail, parameters);
                return;
            }
            else
            {
                var newDetail = CreatePageFromSegment(nextSegment);
                ProcessNavigation(newDetail, segments, parameters, newDetail is NavigationPage ? false : true, animated);
                DoNavigateAction(detail, nextSegment, newDetail, parameters, () =>
                {
                    currentPage.Detail = newDetail;
                    currentPage.IsPresented = false;
                });
                return;
            }
        }

        static void DoNavigateAction(Page fromPage, string toSegment, Page toPage, NavigationParameters parameters, Action navigationAction = null)
        {
            var segmentPrameters = GetSegmentParameters(toSegment, parameters);

            if (!CanNavigate(fromPage, segmentPrameters))
                return;

            OnNavigatedFrom(fromPage, segmentPrameters);

            if (navigationAction != null)
                navigationAction();

            OnNavigatedTo(toPage, segmentPrameters);
        }

        protected abstract Page CreatePage(string segment);

        Page CreatePageFromSegment(string segment)
        {
            return CreatePage(UriParsingHelper.GetSegmentName(segment));
        }

        static bool HasNavigationPageParent(Page page)
        {
            return page?.Parent != null && page?.Parent is NavigationPage;
        }

        static bool UseModalNavigation(Page currentPage, bool? useModalNavigationDefault)
        {
            bool useModalNavigation = true;

            if (useModalNavigationDefault.HasValue)
                useModalNavigation = useModalNavigationDefault.Value;
            else
                useModalNavigation = !HasNavigationPageParent(currentPage);

            //TODO: think about using an interface instead to give the developer a hook to perform conditional logic to return the proper result

            return useModalNavigation;
        }

        static Page GetPreviousPage(Page page, bool useModalNavigation)
        {
            Page previousPage = null;

            if (useModalNavigation)
            {
                int modalStackCount = page.Navigation.ModalStack.Count;
                int previousPageIndex = modalStackCount - 2;
                if (modalStackCount > 0 && previousPageIndex >= 0)
                {
                    previousPage = page.Navigation.ModalStack[previousPageIndex];
                }
            }
            else
            {
                int navStackCount = page.Navigation.NavigationStack.Count;
                int previousPageIndex = navStackCount - 2;
                if (navStackCount > 0 && previousPageIndex >= 0)
                {
                    previousPage = page.Navigation.NavigationStack[previousPageIndex];
                }

                if (previousPage == null)
                    previousPage = GetPreviousPage(page, true);
            }

            return previousPage;
        }

        async static void DoPush(Page currentPage, Page page, bool useModalNavigation, bool animated)
        {
            if (page == null)
                return;

            if (currentPage == null)
            {
                Application.Current.MainPage = page;
            }
            else
            {
                if (useModalNavigation)
                    await currentPage.Navigation.PushModalAsync(page, animated);
                else
                    await currentPage.Navigation.PushAsync(page, animated);
            }
        }

        async static Task<Page> DoPop(INavigation navigation, bool useModalNavigation, bool animated)
        {
            if (useModalNavigation)
                return await navigation.PopModalAsync(animated);
            else
                return await navigation.PopAsync(animated);
        }

        static bool CanNavigate(object page, NavigationParameters parameters)
        {
            var confirmNavigationItem = page as IConfirmNavigation;
            if (confirmNavigationItem != null)
                return confirmNavigationItem.CanNavigate(parameters);

            var bindableObject = page as BindableObject;
            if (bindableObject != null)
            {
                var confirmNavigationBindingContext = bindableObject.BindingContext as IConfirmNavigation;
                if (confirmNavigationBindingContext != null)
                    return confirmNavigationBindingContext.CanNavigate(parameters);
            }

            return true;
        }

        static void OnNavigatedFrom(object page, NavigationParameters parameters)
        {
            if (page != null)
                InvokeOnNavigationAwareElement(page, v => v.OnNavigatedFrom(parameters));
        }

        static void OnNavigatedTo(object page, NavigationParameters parameters, bool includeChild = false)
        {
            if (page != null)
                InvokeOnNavigationAwareElement(page, v => v.OnNavigatedTo(parameters));

            if (includeChild)
            {
                Page childPage = null;

                if (page is NavigationPage)
                    childPage = ((NavigationPage)page).CurrentPage;
                else if (page is TabbedPage)
                    childPage = ((TabbedPage)page).CurrentPage;
                if (page is CarouselPage)
                    childPage = ((CarouselPage)page).CurrentPage;
                else if (page is MasterDetailPage)
                    childPage = ((MasterDetailPage)page).Detail;

                if (childPage != null)
                    InvokeOnNavigationAwareElement(childPage, c => c.OnNavigatedTo(parameters));
            }
        }

        static void InvokeOnNavigationAwareElement(object item, Action<INavigationAware> invocation)
        {
            var navigationAwareItem = item as INavigationAware;
            if (navigationAwareItem != null)
                invocation(navigationAwareItem);

            var bindableObject = item as BindableObject;
            if (bindableObject != null)
            {
                var navigationAwareDataContext = bindableObject.BindingContext as INavigationAware;
                if (navigationAwareDataContext != null)
                    invocation(navigationAwareDataContext);
            }
        }

        static NavigationParameters GetSegmentParameters(string uriSegment, NavigationParameters parameters)
        {
            var navParameters = UriParsingHelper.GetSegmentParameters(uriSegment);

            if (parameters != null)
            {
                foreach (KeyValuePair<string, object> navigationParameter in parameters)
                {
                    navParameters.Add(navigationParameter.Key, navigationParameter.Value);
                }
            }

            return navParameters;
        }

        Page GetCurrentPage()
        {
            return _page != null ? _page : Application.Current.MainPage;
        }
    }
}
