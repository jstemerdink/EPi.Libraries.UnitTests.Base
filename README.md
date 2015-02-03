﻿# A base class for unit testing EPiServer with FakeItEasy. 

By Jeroen Stemerdink

## About

The base library will fake the most used functionalities in EPiServer.

**You can implement the base class as follows:**
```csharp
    public abstract class MySpecs
    {
        [UsedImplicitly]
        private Establish context = () =>
            {
                CmsContext = new CmsContext();

                CmsContext.CreatePageType(typeof(StartPage));
                
                CmsContext.CreateContent<StartPage>("StartPage", ContentReference.RootPage);

                //// ......

            };

        [NotNull]
        public static CmsContext CmsContext { get; set; }        
    }
```
**And use it in your subjects:**
```csharp
[Subject("MySubject")]
    public class Test_something : MySpecs
    {
        private static string result;

        private Because of = () => result = CmsContext.ContentRepository.Get<StartPage>(ContentReference.RootPage).Name;

        private It should_be_named_StartPage = () => result.ShouldEqual("StartPage");
    }
```