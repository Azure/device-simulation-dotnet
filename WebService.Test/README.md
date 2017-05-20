Unit Tests and Integration Tests
================================

## Guidelines



## Conventions

* For each class create a test class with "Test" suffix.
* Flag all the tests with a type, e.g. `[Fact, Trait(Constants.Type, Constants.UnitTest)]`
* Store Integration Tests under `IntegrationTests/` and use
  the `[Fact, Trait(Constants.Type, Constants.IntegrationTest)]` attribute
