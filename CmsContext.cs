﻿// Copyright© 2015 Jeroen Stemerdink. All Rights Reserved.
// 
// Permission is hereby granted, free of charge, to any person
// obtaining a copy of this software and associated documentation
// files (the "Software"), to deal in the Software without
// restriction, including without limitation the rights to use,
// copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the
// Software is furnished to do so, subject to the following
// conditions:
// 
// The above copyright notice and this permission notice shall be
// included in all copies or substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
// EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES
// OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
// NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT
// HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY,
// WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING
// FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR
// OTHER DEALINGS IN THE SOFTWARE.

using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Web;

using EPiServer;
using EPiServer.BaseLibrary;
using EPiServer.Core;
using EPiServer.DataAbstraction;
using EPiServer.DataAccess;
using EPiServer.DataAnnotations;
using EPiServer.Framework.Localization;
using EPiServer.Security;
using EPiServer.ServiceLocation;
using EPiServer.Web;
using EPiServer.Web.Routing;

using FakeItEasy;

using Machine.Specifications.Annotations;

namespace EPi.Libraries.UnitTests.Base
{
    /// <summary>
    ///     The cms context.
    /// </summary>
    public class CmsContext
    {
        #region Fields

        /// <summary>
        ///     The accept headers
        /// </summary>
        private readonly string[] acceptHeaders = new string[1];

        /// <summary>
        ///     The child pages.
        /// </summary>
        private readonly Dictionary<ContentReference, List<IContent>> childPages =
            new Dictionary<ContentReference, List<IContent>>();

        /// <summary>
        ///     The form collection
        /// </summary>
        private readonly NameValueCollection formCollection = new NameValueCollection();

        /// <summary>
        ///     The page types.
        /// </summary>
        private readonly List<ContentType> pageTypes = new List<ContentType>();

        /// <summary>
        ///     The query string collection
        /// </summary>
        private readonly NameValueCollection queryStringCollection = new NameValueCollection();

        /// <summary>
        ///     The request header collection
        /// </summary>
        private readonly NameValueCollection requestHeaderCollection = new NameValueCollection();

        /// <summary>
        ///     The response header collection
        /// </summary>
        private readonly NameValueCollection responseHeaderCollection = new NameValueCollection();

        /// <summary>
        ///     The response output
        /// </summary>
        private readonly StringBuilder responseOutput = new StringBuilder();

        /// <summary>
        ///     The next page id.
        /// </summary>
        private int nextPageId = 5;

        /// <summary>
        ///     The next page type id
        /// </summary>
        private int nextPageTypeId = 1;

        #endregion

        #region Constructors and Destructors

        /// <summary>
        ///     Initializes a new instance of the <see cref="CmsContext" /> class.
        /// </summary>
        public CmsContext()
        {
            ContentReference.RootPage = new PageReference(1).CreateWritableClone();

            ////ContentReference.StartPage.CreateWritableClone().ID = 4;

            SiteDefinition.Current = A.Fake<SiteDefinition>();
            SiteDefinition.Current.StartPage = new PageReference(4);

            this.StartPageUrl = "/";

            Context.Current = A.Fake<IContext>();

            ServiceLocator.SetLocator(A.Fake<IServiceLocator>());

            this.ContentLoader = A.Fake<IContentLoader>();

            this.UrlResolver = A.Fake<UrlResolver>();

            this.TemplateResolver = A.Fake<TemplateResolver>();

            this.ContentRepository = A.Fake<IContentRepository>();

            this.ContentTypeRepository = A.Fake<IContentTypeRepository>();

            this.LanguageBranchRepository = A.Fake<ILanguageBranchRepository>();

            this.LoaderOptions = A.Fake<LoaderOptions>();

            this.LocalizationService = A.Fake<LocalizationService>();

            this.ProviderBasedLocalizationService = new ProviderBasedLocalizationService();

            A.CallTo(() => Context.Current.RequestTime).Returns(DateTime.Now);

            A.CallTo(() => ServiceLocator.Current.GetInstance<IContentRepository>()).Returns(this.ContentRepository);

            A.CallTo(() => ServiceLocator.Current.GetInstance<IContentTypeRepository>())
                .Returns(this.ContentTypeRepository);

            A.CallTo(() => ServiceLocator.Current.GetInstance<ILanguageBranchRepository>())
                .Returns(this.LanguageBranchRepository);

            A.CallTo(() => ServiceLocator.Current.GetInstance<TemplateResolver>()).Returns(this.TemplateResolver);

            A.CallTo(() => this.UrlResolver.GetUrl(ContentReference.StartPage)).Returns(this.StartPageUrl);

            A.CallTo(() => ServiceLocator.Current.GetInstance<ProviderBasedLocalizationService>())
                .Returns(this.ProviderBasedLocalizationService);

            A.CallTo(() => ServiceLocator.Current.GetInstance<LocalizationService>())
                .Returns(this.ProviderBasedLocalizationService);

            // Also fake the TryGetExistingInstance for the LocalizationService
            LocalizationService ignored;
            A.CallTo(() => ServiceLocator.Current.TryGetExistingInstance(out ignored))
                .Returns(true)
                .AssignsOutAndRefParameters(this.ProviderBasedLocalizationService);

            ILanguageBranchRepository languageBranchRepository = this.LanguageBranchRepository;

            if (languageBranchRepository != null)
            {
                languageBranchRepository.Save(new LanguageBranch(MasterLanguage));
                languageBranchRepository.Save(new LanguageBranch(SecondLanguage));

                A.CallTo(() => languageBranchRepository.ListEnabled())
                    .Returns(new[] { new LanguageBranch(MasterLanguage), new LanguageBranch(SecondLanguage) });
            }

            this.RegisterMocks();

            HttpContextBase = this.CreateFakeHttpContext();
        }

        #endregion

        #region Public Properties

        /// <summary>
        ///     Gets or sets the http context.
        /// </summary>
        [NotNull]
        public static HttpContextBase HttpContextBase { get; set; }

        /// <summary>
        ///     Gets or sets the HTTP request base.
        /// </summary>
        [NotNull]
        public static HttpRequestBase HttpRequestBase { get; set; }

        /// <summary>
        ///     Gets or sets the HTTP response base.
        /// </summary>
        [NotNull]
        public static HttpResponseBase HttpResponseBase { get; set; }

        /// <summary>
        ///     Gets or sets the content loader.
        /// </summary>
        [NotNull]
        public IContentLoader ContentLoader { get; set; }

        /// <summary>
        ///     Gets or sets the content repository.
        /// </summary>
        [NotNull]
        public IContentRepository ContentRepository { get; set; }

        /// <summary>
        ///     Gets or sets the content type repository.
        /// </summary>
        /// <value>
        ///     The content type repository.
        /// </value>
        [NotNull]
        public IContentTypeRepository ContentTypeRepository { get; set; }

        [NotNull]
        public ILanguageBranchRepository LanguageBranchRepository { get; set; }

        [NotNull]
        public LoaderOptions LoaderOptions { get; set; }

        /// <summary>
        ///     Gets or sets the localization service.
        /// </summary>
        /// <value>
        ///     The localization service.
        /// </value>
        [NotNull]
        public LocalizationService LocalizationService { get; set; }

        /// <summary>
        ///     Gets the master language.
        /// </summary>
        /// <value>
        ///     The master language.
        /// </value>
        [NotNull]
        public static CultureInfo MasterLanguage
        {
            get
            {
                return new CultureInfo("en-US");
            }
        }

        /// <summary>
        ///     Gets or sets the localization service.
        /// </summary>
        /// <value>
        ///     The localization service.
        /// </value>
        [NotNull]
        public ProviderBasedLocalizationService ProviderBasedLocalizationService { get; set; }

        /// <summary>
        ///     Gets the second language.
        /// </summary>
        /// <value>
        ///     The second language.
        /// </value>
        [NotNull]
        public static CultureInfo SecondLanguage
        {
            get
            {
                return new CultureInfo("nl");
            }
        }

        /// <summary>
        ///     Gets or sets the start page url.
        /// </summary>
        [NotNull]
        public string StartPageUrl { get; set; }

        /// <summary>
        ///     Gets or sets the template resolver.
        /// </summary>
        [NotNull]
        public TemplateResolver TemplateResolver { get; set; }

        /// <summary>
        ///     Gets or sets the url resolver.
        /// </summary>
        [NotNull]
        public UrlResolver UrlResolver { get; set; }

        #endregion

        #region Public Methods and Operators

        /// <summary>
        ///     Creates a page and sets the properties.
        /// </summary>
        /// <typeparam name="T">
        ///     The type of container to use.
        /// </typeparam>
        /// <param name="name">
        ///     The name.
        /// </param>
        /// <param name="parentLink">
        ///     The parent link.
        /// </param>
        /// <returns>
        ///     The <see cref="ContentReference" />.
        /// </returns>
        [NotNull]
        public T CreateContent<T>([NotNull] string name, [NotNull] ContentReference parentLink)
            where T : PageData, new()
        {
            // Get the ContentType for the page you want to create
            ContentType contentType = this.ContentTypeRepository.Load(typeof(T));

            T page = new T();

            page.Property["PageWorkStatus"] = new PropertyNumber((int)VersionStatus.Published);
            page.Property["PageStartPublish"] = new PropertyDate(DateTime.Now.AddDays(-1));
            page.Property["PageName"] = new PropertyString(name);

            page.Property["PageMasterLanguageBranch"] = new PropertyString(MasterLanguage.Name);
            page.Property["PageLanguageBranch"] = new PropertyString(MasterLanguage.Name);
            page.Language = MasterLanguage;

            page.Property["PageParentLink"] = new PropertyPageReference(parentLink);

            page.Property["PageLink"] = new PropertyPageReference();

            page.ContentLink = parentLink == ContentReference.RootPage
                                   ? ContentReference.StartPage
                                   : new PageReference(++this.nextPageId);

            page.ExistingLanguages = new List<CultureInfo> { MasterLanguage };

            // If no ContentType can be found, just create the ContentData without a ContentType
            if (contentType.ID > 0)
            {
                page.Property["PageTypeID"] = new PropertyNumber(contentType.ID);
                page.Property["PageTypeName"] = new PropertyString(contentType.Name);
                page.Property["ContentTypeID"] = new PropertyNumber(contentType.ID);
            }

            this.ContentRepository.Save(page, SaveAction.Publish);

            this.AddChild<T>(page.ParentLink, page);

            return page;
        }

        /// <summary>
        ///     Add a language version to a content item.
        /// </summary>
        /// <typeparam name="T">
        ///     The type of container to use.
        /// </typeparam>
        /// <param name="contentLink">
        ///     The content link.
        /// </param>
        /// <param name="language">
        ///     The language selector.
        /// </param>
        /// <returns>
        ///     The <see cref="ContentReference" />.
        /// </returns>
        /// <exception cref="EPiServer.Core.ContentNotFoundException">
        ///     Content not found.
        /// </exception>
        /// <exception cref="EPiServer.Core.EPiServerException">
        ///     Creating a copy of the item did not succeed.
        /// </exception>
        [NotNull]
        public T CreateLanguageVersionOfContent<T>(
            [NotNull] ContentReference contentLink,
            [NotNull] CultureInfo language) where T : PageData, new()
        {
            // Get the original page
            T page = this.ContentRepository.Get<T>(contentLink, CultureInfo.CurrentUICulture);

            if (page == null)
            {
                throw new ContentNotFoundException();
            }

            // Create a copy
            T languageVersion = page.CreateWritableClone() as T;

            if (languageVersion == null)
            {
                throw new EPiServerException("Creating a copy of the item did not succeed.");
            }

            // Set the language to the new version
            languageVersion.Language = language;

            // Set the contentlink to self, as it is a language version
            languageVersion.ContentLink = contentLink;

            // Add new language to the version
            languageVersion.ExistingLanguages =
                page.ExistingLanguages = new List<CultureInfo> { page.Language, language };

            this.AddChild<T>(page.ParentLink, languageVersion);

            return languageVersion;
        }

        /// <summary>
        ///     Creates the type of the page.
        /// </summary>
        /// <param name="pageType">
        ///     Type of the page.
        /// </param>
        public void CreatePageType([NotNull] Type pageType)
        {
            this.nextPageTypeId += 1;

            ContentTypeAttribute attribute =
                (ContentTypeAttribute)Attribute.GetCustomAttribute(pageType, typeof(ContentTypeAttribute));

            ContentType contentType = A.Fake<ContentType>();
            contentType.ID = this.nextPageTypeId;
            contentType.ModelType = pageType;
            contentType.Name = pageType.Name;
            contentType.GUID = attribute.GetGUID().GetValueOrDefault();
            contentType.Description = attribute.Description;
            contentType.DisplayName = attribute.DisplayName;
            contentType.IsAvailable = attribute.AvailableInEditMode;
            contentType.GroupName = "UnitTests";
            contentType.SortOrder = 100;
            contentType.ACL = new AccessControlList();

            this.pageTypes.Add(contentType);

            this.ContentTypeRepository.Save(contentType);

            A.CallTo(() => this.ContentTypeRepository.Load(pageType.Name))
                .Returns(this.pageTypes.FirstOrDefault(pt => pt.Name.Equals(pageType.Name)));

            A.CallTo(() => this.ContentTypeRepository.Load(pageType))
                .Returns(this.pageTypes.FirstOrDefault(pt => pt.Name.Equals(pageType.Name)));
        }

        /// <summary>
        ///     Gets the <see cref="PageReference" /> for the specified <see cref="PageData" />.
        /// </summary>
        /// <param name="pageData">
        ///     The <see cref="PageData" />.
        /// </param>
        /// <returns>
        ///     A <see cref="PageReference" />.
        /// </returns>
        [NotNull]
        public PageReference Page([NotNull] PageData pageData)
        {
            if (ContentReference.IsNullOrEmpty(pageData.ContentLink))
            {
                pageData.Property["PageLink"] = new PropertyPageReference();
                PageReference pageLink = new PageReference(++this.nextPageId);
                pageData.ContentLink = pageLink;
            }

            if (ContentReference.IsNullOrEmpty(pageData.ParentLink))
            {
                pageData.Property["PageParentLink"] = new PropertyPageReference();
                pageData.ParentLink = ContentReference.StartPage;
            }

            this.AddChild<PageData>(pageData.ParentLink, pageData);

            return pageData.ContentLink.ToPageReference();
        }

        /// <summary>
        ///     Updates the content.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="content">The content.</param>
        /// <param name="language">The language.</param>
       public void UpdateContent<T>([NotNull] T content, [NotNull] CultureInfo language) where T : PageData, new()
        {
            T[] items = this.childPages[content.ParentLink].OfType<T>().ToArray();
            int i = 0;
            foreach (T page in items)
            {
                if (page.ContentLink == content.ContentLink && page.Language.Equals(language))
                {
                    items[i] = content;
                    break;
                }

                i++;
            }
        }

        #endregion

        #region Methods

        /// <summary>
        ///     The add child.
        /// </summary>
        /// <param name="parentLink">
        ///     The parent link.
        /// </param>
        /// <param name="child">
        ///     The child.
        /// </param>
        /// <returns>
        ///     The <see cref="ContentReference" />.
        /// </returns>
        private void AddChild<T>([NotNull] ContentReference parentLink, [NotNull] IContent child) where T : PageData
        {
            if (!this.childPages.ContainsKey(parentLink))
            {
                this.childPages.Add(parentLink, new List<IContent>());
            }

            this.childPages[parentLink].Add(child);

            // Add some specific mock calls for EPiServer calls, getting the pagedata from the container.
            A.CallTo(() => this.ContentRepository.Get<T>(child.ContentLink))
                .Returns(this.childPages[parentLink].OfType<T>().First(p => p.ContentLink == child.ContentLink));

            T outValue;
            A.CallTo(() => this.ContentRepository.TryGet(child.ContentLink, out outValue))
                .Returns(true)
                .AssignsOutAndRefParameters(
                    this.childPages[parentLink].OfType<T>().First(p => p.ContentLink == child.ContentLink));

            A.CallTo(() => this.ContentRepository.Get<ContentData>(child.ContentLink))
                .Returns(this.childPages[parentLink].OfType<T>().First(p => p.ContentLink == child.ContentLink));

            A.CallTo(() => this.ContentRepository.Get<PageData>(child.ContentLink))
                .Returns(this.childPages[parentLink].OfType<T>().First(p => p.ContentLink == child.ContentLink));

            A.CallTo(() => this.ContentRepository.Get<T>(child.ContentLink, A<CultureInfo>.Ignored))
                .ReturnsLazily(
                    m =>
                    this.childPages[parentLink].OfType<T>()
                        .First(
                            p => p.ContentLink == child.ContentLink && p.Language.Equals(m.GetArgument<CultureInfo>(1))));

            A.CallTo(() => this.ContentRepository.GetLanguageBranches<T>(child.ContentLink))
                .Returns(this.childPages[parentLink].OfType<T>().Where(c => c.ContentLink == child.ContentLink));

            A.CallTo(() => this.ContentRepository.GetChildren<T>(parentLink, A<CultureInfo>.Ignored))
                .ReturnsLazily(
                    m =>
                    this.childPages[parentLink].OfType<T>().Where(p => p.Language.Equals(m.GetArgument<CultureInfo>(1))));

            A.CallTo(() => this.ContentRepository.GetAncestors(child.ContentLink))
                .ReturnsLazily(m => this.GetAncestors(child));
        }

        /// <summary>
        ///     Clears the response.
        /// </summary>
        private void ClearResponse()
        {
            this.responseHeaderCollection.Clear();
            this.responseOutput.Clear();
        }

        /// <summary>
        ///     Fakes the HTTP context.
        /// </summary>
        /// <returns>
        ///     The <see cref="HttpContextBase" />.
        /// </returns>
        [NotNull]
        private HttpContextBase CreateFakeHttpContext()
        {
            HttpRequestBase httpRequestBase = A.Fake<HttpRequestBase>();
            A.CallTo(() => httpRequestBase.HttpMethod).Returns("GET");
            A.CallTo(() => httpRequestBase.Headers).Returns(this.requestHeaderCollection);
            A.CallTo(() => httpRequestBase.Form).Returns(this.formCollection);
            A.CallTo(() => httpRequestBase.QueryString).Returns(this.queryStringCollection);

            HttpResponseBase httpResponseBase = A.Fake<HttpResponseBase>();
            A.CallTo(() => httpResponseBase.Clear()).Invokes(this.ClearResponse);

            A.CallTo(() => httpResponseBase.Write(A<string>.Ignored))
                .Invokes(m => this.responseOutput.Append(m.GetArgument<string>(0)));
            A.CallTo(() => httpResponseBase.Output).Returns(new StringWriter(this.responseOutput));

            A.CallTo(() => httpResponseBase.AddHeader(A<string>.Ignored, A<string>.Ignored))
                .Invokes(m => this.responseHeaderCollection.Add(m.GetArgument<string>(0), m.GetArgument<string>(1)));
            A.CallTo(() => httpResponseBase.Headers).Returns(this.responseHeaderCollection);

            HttpContextBase httpContextBase = A.Fake<HttpContextBase>();
            A.CallTo(() => httpContextBase.Request).Returns(httpRequestBase);
            A.CallTo(() => httpContextBase.Request.AcceptTypes).Returns(this.acceptHeaders);
            A.CallTo(() => httpContextBase.Response).Returns(httpResponseBase);

            return httpContextBase;
        }

        /// <summary>
        ///     Gets the "descendents".
        /// </summary>
        /// <param name="parentLink">
        ///     The parent link.
        /// </param>
        /// <returns>
        ///     A <see cref="List{ContentReference}" />
        /// </returns>
        [NotNull]
        private IEnumerable<ContentReference> GetAll([NotNull] ContentReference parentLink)
        {
            foreach (IContent content in this.childPages[parentLink])
            {
                PageData child = (PageData)content;
                yield return child.ContentLink;

                if (!this.childPages.ContainsKey(child.ContentLink))
                {
                    continue;
                }

                foreach (IContent content1 in this.childPages[child.ContentLink])
                {
                    PageData grandChild = (PageData)content1;
                    yield return grandChild.ContentLink;
                }
            }
        }

        [NotNull]
        private IEnumerable<IContent> GetAncestors([NotNull] IContent content)
        {
            if (content.ParentLink.Equals(ContentReference.RootPage))
            {
                yield break;
            }

            IContent parent = this.ContentRepository.Get<PageData>(content.ParentLink);

            if (parent == null)
            {
                yield break;
            }

            yield return parent;

            foreach (PageData ancestor in this.GetAncestors(parent).Cast<PageData>())
            {
                yield return ancestor;
            }
        }

        /// <summary>
        ///     Register some mocks for EPiServer calls for getting pagedata.
        /// </summary>
        private void RegisterMocks()
        {
            A.CallTo(() => this.ContentLoader.GetChildren<IContent>(A<ContentReference>.Ignored))
                .ReturnsLazily(m => this.childPages[m.GetArgument<ContentReference>(0)].OfType<PageData>());

            A.CallTo(() => this.ContentLoader.GetChildren<PageData>(A<ContentReference>.Ignored))
                .ReturnsLazily(m => this.childPages[m.GetArgument<ContentReference>(0)].OfType<PageData>());

            A.CallTo(
                () => this.ContentLoader.GetChildren<PageData>(A<ContentReference>.Ignored, A<CultureInfo>.Ignored))
                .ReturnsLazily(
                    m =>
                    this.childPages[m.GetArgument<ContentReference>(0)].OfType<PageData>()
                        .Where(p => p.Language.Equals(m.GetArgument<CultureInfo>(1))));

            A.CallTo(() => this.ContentRepository.GetChildren<IContent>(A<ContentReference>.Ignored))
                .ReturnsLazily(m => this.childPages[m.GetArgument<ContentReference>(0)].OfType<PageData>());

            A.CallTo(() => this.ContentRepository.GetChildren<PageData>(A<ContentReference>.Ignored))
                .ReturnsLazily(m => this.childPages[m.GetArgument<ContentReference>(0)].OfType<PageData>());

            A.CallTo(
                () => this.ContentRepository.GetChildren<PageData>(A<ContentReference>.Ignored, A<CultureInfo>.Ignored))
                .ReturnsLazily(
                    m =>
                    this.childPages[m.GetArgument<ContentReference>(0)].OfType<PageData>()
                        .Where(p => p.Language.Equals(m.GetArgument<CultureInfo>(1))));

            A.CallTo(() => this.ContentRepository.GetDescendents(A<ContentReference>.Ignored))
                .ReturnsLazily(m => this.GetAll(m.GetArgument<ContentReference>(0)).Distinct());
        }

        #endregion
    }
}