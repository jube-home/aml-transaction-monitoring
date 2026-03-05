---
layout: default
title: CLI Concepts
nav_order: 1
parent: CLI
grand_parent: Concepts
---

🚀 Speed up implementation with hands-on, face-to-face [training](https://www.jube.io/jube-training) from the developer.
💬 Join the [Jube WhatsApp Public Support Group](https://whatsapp.com/channel/0029Vb7HM7yICVfihDH17H2P) to chat with the
developer.

# CLI Concepts
The Command Line Interface (CLI) is a basic console application that is intended to provide administrative functions from the terminal.  This will develop over time and is intended as a more disciplined means of delivering functionality that would otherwise be SQL (noting a preference to the use of ORMs in code).

The CLI allows for instructions to be chained and passed in a fluent manner. In the CLI functions are switches denoted with a - character followed by the shorthand name of the function,  thereafter following the functions parameters.

The following functions exist:

| Function | Name                         | Description                                                                                                                                                                            |
|----------|------------------------------|----------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------|
| -cs      | Connection String            | Set connection string for the database in the session.  Most functions will require the database connection string to be set,  hence it should be placed first as a matter of routine. |
| -urpr    | User Registry Password Reset | A function to reset the password for a user given knowledge of the hashing key.                                                                                                        |

If a parameter necessarily contains space characters that cannot safely be removed, wrap the parameter in double quotations,  as follows wrapping the parameter to the -cs function:

``` shell
.Jube.CLI -cs "Host=127.0.0.1;Database=test;Username=postgres;Password=secret;Pooling=true;Minimum Pool Size=0;Maximum Pool Size=100;" -urpr ExtraSuperSecretRandomKey Administrator StrongPassword
```