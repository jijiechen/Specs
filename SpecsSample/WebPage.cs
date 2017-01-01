using OpenQA.Selenium;
using OpenQA.Selenium.Support.PageObjects;

namespace SpecsSample
{

    public class WebPage
    {
        [FindsBy(How = How.TagName, Using = "html")]
        public IWebElement Document
        {
            get;
            set;
        }        
    }
}
