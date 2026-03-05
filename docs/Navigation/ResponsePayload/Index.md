---
layout: default
title: Response Payload
nav_order: 6
parent: Navigation
---

🚀 Speed up implementation with hands-on, face-to-face [training](https://www.jube.io/jube-training) from the developer.
💬 Join the [Jube WhatsApp Public Support Group](https://whatsapp.com/channel/0029Vb7HM7yICVfihDH17H2P) to chat with the
developer.

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