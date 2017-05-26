Web service
===========

## ASP.NET Web API 2 and OWIN

The web service is built on ASP.NET Web API 2, and hosted via OWIN, i.e. IIS is
not strictly required to run the service, although it would be possible if required.
More information can be found here:

* [Getting Started with ASP.NET Web API 2](https://docs.microsoft.com/en-us/aspnet/web-api/overview/getting-started-with-aspnet-web-api/tutorial-your-first-web-api)
* [Routing in ASP.NET Web API](https://docs.microsoft.com/en-us/aspnet/web-api/overview/web-api-routing-and-actions/routing-in-aspnet-web-api)
* [Use OWIN to Self-Host ASP.NET Web API 2](https://docs.microsoft.com/en-us/aspnet/web-api/overview/hosting-aspnet-web-api/use-owin-to-self-host-web-api)

## Guidelines

The web service is the microservice entry point. There might be other
entry points if the microservice has some background agent, for instance to run
continuous tasks like log aggregation, simulations, watchdogs etc.

The web service takes care of loading the configuration, and injecting it to
underlying dependencies, like the service layer. Most of the business logic
is encapsulated in the service layer, while the web service has the
responsibility of accepting requests and providing responses in the correct
format.

## Conventions

* Web service routing is defined by convention, e.g. the name of the controllers
  defines the supported paths.
* The microservice configuration is defined in the `application.conf` file
  stored in the `WebService` project, using
  [HOCON format](http://getakka.net/docs/concepts/hocon)
