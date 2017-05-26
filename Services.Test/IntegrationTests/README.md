Integration Tests
=================

## Guidelines

* Store here functional and integration tests, e.g. tests which require network
  access, storage read/write operations, etc.
* Functional and Integration tests typically requires confuiguration settings,
  which should be provided similarly to the entry point application.

## Conventions

* For each scenario create a test class with "Test" suffix.
* Flag all the tests with `[Fact, Trait(Constants.Type, Constants.IntegrationTest)]`
