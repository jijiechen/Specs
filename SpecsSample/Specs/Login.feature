Feature: Login

Scenario: Hello World
  Given I am an anonymous user
  When I open a browser
  And I go to bing
  Then I should see a search bar