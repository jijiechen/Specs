using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using OpenQA.Selenium.Support.PageObjects;
using System;
using TechTalk.SpecFlow;

namespace SpecsSample.StepDefinitions
{

    [Binding]
    public class LoginSteps
    {

        [Given("I am an anonymous user")]
        public void GivenAnAnonymousUser()
        {

        }


        private IWebDriver driver;
        [When("I open a browser")]
        public void OpenBrowser()
        {  
            driver = new ChromeDriver();
        }


        [When("I go to bing")]
        public void GotoBing()
        {
            driver.Navigate().GoToUrl("https://www.bing.com/");
        }

        [Then("I should see a search bar")]
        public void ShouldSeeSearchBar()
        {
            var page = new WebPage();
            PageFactory.InitElements(driver, page);

            var input = page.Document.FindElement(By.Id("sb_form_q"));
            Console.WriteLine("Found input: {0}", input != null);
        }


        // [BeforeScenario()]
        [AfterScenario()]
        public void TearDown()
        {
            driver.Quit();
        }

    }
}
