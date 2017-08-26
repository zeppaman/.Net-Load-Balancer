# .Net-Load-Balancer
A simple load balancer implemented using ASP.NET Core

This is a lightweight bundle that supports :

1. Filters
2. Chaching
3. Rewrite
4. Balancing
5. Host

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
Using standard .NET Core caching module we can provide cache support for url, defining policy. Caching has many option that are basically a wrap of original module, so you ca referr here to get details.
