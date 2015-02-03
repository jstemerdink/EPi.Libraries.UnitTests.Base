You can implement the base class as follows:

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

And use it in your subjects:

[Subject("MySubject")]
    public class Test_something : MySpecs
    {
        private static string result;

        private Because of = () => result = CmsContext.ContentRepository.Get<StartPage>(ContentReference.RootPage).Name;

        private It should_be_named_StartPage = () => result.ShouldEqual("StartPage");
    }

