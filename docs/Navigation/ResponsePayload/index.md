---
layout: default
title: Response Payload
nav_order: 6
parent: Navigation
---

🚀 Get to pre-production in weeks, not months, with private [training](https://www.jube.io/jube-training) direct from Jube's developer — real sovereignty, zero vendor lock-in.

# Response Payload

All Child Objects have the option to specify whether it should be returned in the response payload via HTTP or AMQP.  A Response Payload means that the content or value created will be returned to the HTTP response stream or AMQP payload,  otherwise excluded.  It is a means to reduce the amount of bandwidth that is used in extremely large throughput, as some hosting providers charge for outbound bandwidth:

![Image](ExampleOfResponsePayloadInRequestXPath.png)

Although JSON processing via HTTP or AMQP is outside the scope of this section of the documentation,  it may be stated that in this example,  the Currency only appears in the response payload because it has been switched as Response Payload:

![Image](CurrencyInResponsePayload.png)

Toggling of Response Payload for the Currency will, after synchronisation, remove the element from the response payload:

![Image](ToggleOffResponsePayload.png)

A like for like request via HTTP will now exclude transport of the Currency element in the response payload:

![Image](MissingCurrency.png)

Irrespective of the Response Payload being selected,  the value will still be compiled into the Archive payload for database insertion.