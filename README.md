# .Net-Load-Balancer
A simple load balancer implemented using ASP.NET Core

This is a lightweight bundle that supports :

1. Filters
2. Chaching
3. Rewrite
4. Balancing
5. Transformation

Whole application is written in .NET Core, so it can be run as embedded app or in a webserver.

The main principle is to provide an easy to use solution that can be installed in 5 minutes.

Every feature of application is implemented with its own owin module that can be enabled or disabled by cofiguration.
Each module is configurable by a dedicated config file.



## Where is the code?
I'm working on it, please be patient, but please open a issue or contact me to tell me that this prject may be useful and you will need. This will make me work harder ;-)

## Contibute
If intersted, plese contact me or open an issues. 

# How .Net Load Balancer works

## Filters
This module provide an easy way to filter request based on some rules. All request that math the filter will be dropped. Each url is tested over a set of **rule**. If the url match the rule the request will be dropped. Only one match determine the rule activations so, basically, all rules are "OR" conditions by default. Each rule can test a set of request parameters (url, agent, headers). Inside the single rule all condition must be true to activate the rule. This means we are working with something like this ( CONDITION A AND CONDITION B) OR (CONDITION C) and this will support most cases.

## Chaching
Using standard .NET Core caching module we can provide cache support for url, defining policy. Caching has many option that are basically a wrap of original module, so you ca referr [here](https://docs.microsoft.com/en-us/aspnet/core/performance/caching/middleware?tabs=aspnetcore2x) to get details.

## Rewrite
This stage will allow static rewrite rule. This is oftne demanded to the application but can be implemented here to simplify server part or to map virtual urls over many different application. This is mostly a way to couple external url with internal one in case there isnt' a way to change balanced application. Balancer itself will balance the output of this transformation.


## Balancig 
This is the core module that define, for each url wath will be the destination. This step generates only the real path, replacing selected host. The host can be selected using one of the following algorithm:

  1. Number of request coming
  2. Number of request pending
  3. Quicker response
  4. Affiliation (Based on Cookie)

## Proxy
After Balanging stage complete the computation of right url, proxy module will invoke the request repling the client.

## Transformation
Basing on rules, similar to filters, you can define some regular expression to alter content. In that way you can replace absolute urls with the proxy one, or change local url with cdn ones.




